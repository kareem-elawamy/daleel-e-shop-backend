using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace daleel_e_shop.Controllers
{
    public class AIChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIChatController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string GeminiLiveModel = "gemini-3.1-flash-live-preview";
        private const string GeminiTextModel = "gemini-3-flash-preview";
        private const string GeminiLiveEndpoint = "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={0}";
        private const string GeminiTextEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";
        private const string SystemPrompt = "You are the Daleel AI Assistant, a helpful and knowledgeable shopping concierge for Daleel e-shop. You help users find products, compare options, understand delivery and payment methods (Aramex delivery, Tabby split payments), and answer questions. Be professional, concise, and insightful. Reply in the user's language.";

        public AIChatController(IConfiguration configuration, ILogger<AIChatController> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        // ── TEXT FLOW: HTTP POST → Gemini REST API ──

        [HttpPost("/api/ai/chat")]
        public async Task<IActionResult> TextChat([FromBody] TextChatRequest request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(503, new { error = "AI service not configured." });

            if (request?.History == null || request.History.Count == 0)
                return BadRequest(new { error = "Conversation history is required." });

            try
            {
                var url = string.Format(GeminiTextEndpoint, GeminiTextModel, apiKey);
                var payload = new
                {
                    contents = request.History.Select(m => new
                    {
                        role = m.Role,
                        parts = new[] { new { text = m.Text } }
                    }).ToArray(),
                    systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                    generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
                };

                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                var resp = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[TextChat] Gemini error: {Status} {Body}", resp.StatusCode, body);
                    return StatusCode(502, new { error = "AI service returned an error.", detail = body });
                }

                var geminiResp = JsonSerializer.Deserialize<JsonElement>(body);
                var text = "";
                if (geminiResp.TryGetProperty("candidates", out var cands))
                {
                    var first = cands.EnumerateArray().FirstOrDefault();
                    if (first.TryGetProperty("content", out var c) && c.TryGetProperty("parts", out var p))
                        foreach (var part in p.EnumerateArray())
                            if (part.TryGetProperty("text", out var t)) text += t.GetString();
                }
                return Ok(new { text });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TextChat] Error");
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        // ── STT: Speech-to-Text using same model as text chat (gemini-3-flash-preview) ──

        [HttpPost("/api/ai/stt")]
        public async Task<IActionResult> SpeechToText([FromBody] SttRequest request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(503, new { error = "AI service not configured." });
            if (string.IsNullOrWhiteSpace(request?.Audio))
                return BadRequest(new { error = "audio is required." });

            try
            {
                var lang = request.Lang ?? "en";
                var prompt = lang == "ar"
                    ? "فرّغ هذا التسجيل الصوتي إلى نص. أعد النص فقط دون أي تعليق."
                    : "Transcribe this audio. Return ONLY the transcript text, nothing else.";
                var mime = (request.MimeType ?? "audio/webm").Split(';')[0].Trim();

                var payload = new
                {
                    contents = new[] { new {
                        role = "user",
                        parts = new object[] {
                            new { text = prompt },
                            new { inline_data = new { mime_type = mime, data = request.Audio } }
                        }
                    }},
                    generationConfig = new { temperature = 0.0, maxOutputTokens = 256 }
                };

                var client = _httpClientFactory.CreateClient();
                var url = string.Format(GeminiTextEndpoint, GeminiTextModel, apiKey);
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
                var resp = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
                var body = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("[STT] Gemini error: {S} {B}", resp.StatusCode, body);
                    return StatusCode(502, new { error = "Transcription failed" });
                }

                var geminiResp = JsonSerializer.Deserialize<JsonElement>(body);
                var text = "";
                if (geminiResp.TryGetProperty("candidates", out var cands))
                {
                    var first = cands.EnumerateArray().FirstOrDefault();
                    if (first.TryGetProperty("content", out var c) && c.TryGetProperty("parts", out var p))
                        foreach (var part in p.EnumerateArray())
                            if (part.TryGetProperty("text", out var t)) text += t.GetString();
                }
                return Ok(new { text = text.Trim() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STT] Error");
                return StatusCode(500, new { error = "Transcription error" });
            }
        }

        // ── VOICE FLOW: WebSocket → Gemini Live API (AUDIO only) ──

        [Route("/ws/chat")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task VoiceWebSocket()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest) { HttpContext.Response.StatusCode = 400; return; }
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) { HttpContext.Response.StatusCode = 503; return; }
            using var clientWs = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await VoiceProxy(clientWs, apiKey);
        }

        private async Task VoiceProxy(WebSocket clientWs, string apiKey)
        {
            using var geminiWs = new ClientWebSocket();
            var cts = new CancellationTokenSource();
            try
            {
                var geminiUri = string.Format(GeminiLiveEndpoint, apiKey);
                _logger.LogWarning("[DIAG] Connecting to Gemini Live at: {Uri}", geminiUri.Replace(apiKey, "***"));
                await geminiWs.ConnectAsync(new Uri(geminiUri), cts.Token);
                _logger.LogWarning("[DIAG] ✅ Connected to Gemini Live. State: {S}", geminiWs.State);

                // ═══════════════════════════════════════════════════════════
                // SETUP MESSAGE — per API reference (ai.google.dev/api/live):
                //
                // Client message wrapper (line 336):
                //   { "setup": BidiGenerateContentSetup, ... }
                //
                // BidiGenerateContentSetup (line 323):
                //   { "model": string, "generationConfig": { "responseModalities": [...], "speechConfig": {...} }, "systemInstruction": {...} }
                //
                // Top-level key = "setup" (NOT "config")
                // responseModalities goes INSIDE generationConfig
                // ═══════════════════════════════════════════════════════════
                var setupPayload = new
                {
                    setup = new
                    {
                        model = $"models/{GeminiLiveModel}",
                        generationConfig = new
                        {
                            responseModalities = new[] { "AUDIO" },
                            speechConfig = new
                            {
                                voiceConfig = new
                                {
                                    prebuiltVoiceConfig = new
                                    {
                                        voiceName = "Aoede"
                                    }
                                }
                            }
                        },
                        systemInstruction = new
                        {
                            parts = new[] { new { text = SystemPrompt } }
                        }
                    }
                };

                var setupJson = JsonSerializer.Serialize(setupPayload, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = true
                });
                _logger.LogWarning("[DIAG] Sending Setup JSON:\n{Json}", setupJson);

                await SendJson(geminiWs, setupPayload, cts.Token);
                _logger.LogWarning("[DIAG] ✅ Setup sent. Gemini WS State: {S}", geminiWs.State);

                // Wait for setup response
                _logger.LogWarning("[DIAG] Waiting for Gemini setup response...");
                var setupResp = await ReceiveJson(geminiWs, cts.Token);

                if (setupResp == null)
                {
                    _logger.LogError("[DIAG] ❌ Setup response was NULL. Gemini WS State: {S}, CloseStatus: {CS}",
                        geminiWs.State, geminiWs.CloseStatus);
                    await SendError(clientWs, "Voice AI did not respond.");
                    return;
                }

                var respText = setupResp.RootElement.ToString();
                _logger.LogWarning("[DIAG] ✅ Gemini setup response:\n{R}", respText);

                if (setupResp.RootElement.TryGetProperty("error", out var errProp))
                {
                    _logger.LogError("[DIAG] ❌ Gemini returned ERROR: {E}", errProp.ToString());
                    await SendError(clientWs, $"Gemini error: {errProp}");
                    return;
                }

                // Notify frontend
                await SendText(clientWs, JsonSerializer.Serialize(new { type = "voiceReady" }), cts.Token);
                _logger.LogWarning("[DIAG] ✅ Sent voiceReady to client. Starting relay loops.");

                var t1 = AudioRelay(clientWs, geminiWs, cts);
                var t2 = GeminiRelay(geminiWs, clientWs, cts);
                var completed = await Task.WhenAny(t1, t2);
                _logger.LogWarning("[DIAG] Relay loop exited: {Loop}", completed == t1 ? "AudioRelay" : "GeminiRelay");
                cts.Cancel();
                try { await Task.WhenAll(t1, t2); } catch (OperationCanceledException) { }
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "[DIAG] ❌ WebSocket error. Gemini: {GS}, Client: {CS}", geminiWs.State, clientWs.State);
                await SendError(clientWs, "Voice connection failed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DIAG] ❌ Unexpected error");
                await SendError(clientWs, "Voice connection failed.");
            }
            finally
            {
                _logger.LogWarning("[DIAG] Cleanup. Client: {C}, Gemini: {G}", clientWs.State, geminiWs.State);
                if (clientWs.State == WebSocketState.Open) try { await clientWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); } catch { }
                if (geminiWs.State == WebSocketState.Open) try { await geminiWs.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); } catch { }
            }
        }

        private async Task AudioRelay(WebSocket client, ClientWebSocket gemini, CancellationTokenSource cts)
        {
            var buf = new byte[1024 * 16];
            try
            {
                while (!cts.Token.IsCancellationRequested && client.State == WebSocketState.Open && gemini.State == WebSocketState.Open)
                {
                    var r = await client.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
                    if (r.CloseStatus.HasValue) break;
                    if (r.MessageType != WebSocketMessageType.Text) continue;
                    var msg = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(buf, 0, r.Count));
                    if (msg.TryGetProperty("type", out var tp) && tp.GetString() == "audio" && msg.TryGetProperty("data", out var d))
                        await SendJson(gemini, new { realtimeInput = new { audio = new { data = d.GetString(), mimeType = "audio/pcm;rate=16000" } } }, cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
        }

        private async Task GeminiRelay(ClientWebSocket gemini, WebSocket client, CancellationTokenSource cts)
        {
            var buf = new byte[1024 * 64];
            try
            {
                while (!cts.Token.IsCancellationRequested && gemini.State == WebSocketState.Open && client.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult r;
                    do { r = await gemini.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token); ms.Write(buf, 0, r.Count); } while (!r.EndOfMessage);
                    if (r.CloseStatus.HasValue) break;
                    if (r.MessageType == WebSocketMessageType.Close) break;
                    var srv = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(ms.ToArray()));
                    if (srv.TryGetProperty("setupComplete", out _)) continue;
                    if (!srv.TryGetProperty("serverContent", out var sc)) continue;

                    if (sc.TryGetProperty("modelTurn", out var mt) && mt.TryGetProperty("parts", out var parts))
                        foreach (var part in parts.EnumerateArray())
                            if (part.TryGetProperty("inlineData", out var id))
                                await SendText(client, JsonSerializer.Serialize(new { type = "audio", data = id.GetProperty("data").GetString(), mimeType = id.TryGetProperty("mimeType", out var m) ? m.GetString() : "audio/pcm;rate=24000" }), cts.Token);

                    if (sc.TryGetProperty("outputTranscription", out var ot) && ot.TryGetProperty("text", out var otT))
                        await SendText(client, JsonSerializer.Serialize(new { type = "voiceTranscript", sender = "ai", text = otT.GetString() }), cts.Token);
                    if (sc.TryGetProperty("inputTranscription", out var it) && it.TryGetProperty("text", out var itT))
                        await SendText(client, JsonSerializer.Serialize(new { type = "voiceTranscript", sender = "user", text = itT.GetString() }), cts.Token);
                    if (sc.TryGetProperty("turnComplete", out var tc) && tc.GetBoolean())
                        await SendText(client, JsonSerializer.Serialize(new { type = "turnComplete" }), cts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
        }

        // Helpers
        static async Task SendJson(ClientWebSocket ws, object p, CancellationToken ct) { var b = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(p, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })); await ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Text, true, ct); }
        static async Task<JsonDocument?> ReceiveJson(ClientWebSocket ws, CancellationToken ct)
        {
            var buf = new byte[1024 * 16];
            using var ms = new MemoryStream();
            WebSocketReceiveResult r;
            do
            {
                r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                ms.Write(buf, 0, r.Count);
            } while (!r.EndOfMessage);

            if (r.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine($"[DIAG] ❌ Gemini sent CLOSE frame. Status: {r.CloseStatus}, Desc: {r.CloseStatusDescription}");
                return null;
            }
            if (r.MessageType == WebSocketMessageType.Text || r.MessageType == WebSocketMessageType.Binary)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return await JsonDocument.ParseAsync(ms, cancellationToken: ct);
            }
            Console.WriteLine($"[DIAG] ⚠ Unexpected message type from Gemini: {r.MessageType}");
            return null;
        }
        static async Task SendText(WebSocket ws, string m, CancellationToken ct) { if (ws.State != WebSocketState.Open) return; await ws.SendAsync(Encoding.UTF8.GetBytes(m), WebSocketMessageType.Text, true, ct); }
        static async Task SendError(WebSocket ws, string e) { try { if (ws.State == WebSocketState.Open) await ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { type = "error", text = e })), WebSocketMessageType.Text, true, CancellationToken.None); } catch { } }
    }

    public class TextChatRequest { [JsonPropertyName("history")] public List<ChatMessage> History { get; set; } = new(); }
    public class ChatMessage { [JsonPropertyName("role")] public string Role { get; set; } = "user"; [JsonPropertyName("text")] public string Text { get; set; } = ""; }
    public class SttRequest { [JsonPropertyName("audio")] public string Audio { get; set; } = ""; [JsonPropertyName("mimeType")] public string? MimeType { get; set; } [JsonPropertyName("lang")] public string? Lang { get; set; } }
}
