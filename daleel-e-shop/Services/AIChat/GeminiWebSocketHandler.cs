using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace daleel_e_shop.Services.AIChat
{
    public interface IGeminiWebSocketHandler
    {
        Task HandleWebSocketAsync(WebSocket clientSocket, ILogger logger, string apiKey, CancellationToken cancellationToken = default);
    }

    public class GeminiWebSocketHandler : IGeminiWebSocketHandler
    {
        private const string GeminiLiveModel = "gemini-3.1-flash-live-preview";
        private const string GeminiLiveApiUri = "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent";
        private const string SystemInstruction = "You are the Neurix AI Assistant. You provide helpful, accurate, and concise responses. Maintain context from the conversation and respond naturally.";
        private const string VoiceName = "Aoede";
        private const int BufferSize = 4096;

        public async Task HandleWebSocketAsync(WebSocket clientSocket, ILogger logger, string apiKey, CancellationToken cancellationToken = default)
        {
            ClientWebSocket? geminiSocket = null;
            try
            {
                logger.LogInformation("WebSocket connection established");

                // Connect to Gemini Live API
                geminiSocket = new ClientWebSocket();
                geminiSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

                var geminiUri = $"{GeminiLiveApiUri}?key={apiKey}";
                await geminiSocket.ConnectAsync(new Uri(geminiUri), cancellationToken);
                logger.LogInformation("Connected to Gemini Live API");

                // Send initial setup message
                await SendSetupMessageAsync(geminiSocket, logger, cancellationToken);

                // Start relay tasks
                var clientToGeminiTask = RelayClientToGeminiAsync(clientSocket, geminiSocket, logger, cancellationToken);
                var geminiToClientTask = RelayGeminiToClientAsync(geminiSocket, clientSocket, logger, cancellationToken);

                // Wait for either task to complete (one disconnects)
                await Task.WhenAny(clientToGeminiTask, geminiToClientTask);

                logger.LogInformation("WebSocket relay completed");
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("WebSocket operation cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling WebSocket connection");
            }
            finally
            {
                if (geminiSocket?.State == WebSocketState.Open)
                {
                    await geminiSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
                }
                geminiSocket?.Dispose();

                if (clientSocket?.State == WebSocketState.Open)
                {
                    await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
                }
            }
        }

        private async Task SendSetupMessageAsync(ClientWebSocket geminiSocket, ILogger logger, CancellationToken cancellationToken)
        {
            var setupPayload = new
            {
                setup = new
                {
                    model = $"models/{GeminiLiveModel}",
                    generationConfig = new
                    {
                        speechConfig = new
                        {
                            voiceConfig = new
                            {
                                prebuiltVoiceConfig = new
                                {
                                    voiceName = VoiceName
                                }
                            }
                        }
                    },
                    systemInstruction = new
                    {
                        parts = new[] { new { text = SystemInstruction } 
                    },
                    tools = Array.Empty<object>()
                }
            }          };

            var json = JsonSerializer.Serialize(setupPayload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var bytes = Encoding.UTF8.GetBytes(json);

            await geminiSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
            logger.LogInformation("Setup message sent to Gemini");
        }

        private async Task RelayClientToGeminiAsync(WebSocket clientSocket, ClientWebSocket geminiSocket, ILogger logger, CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            try
            {
                while (clientSocket.State == WebSocketState.Open && geminiSocket.State == WebSocketState.Open)
                {
                    var result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.LogInformation("Client initiated close");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        logger.LogDebug("Received from client: {Message}", message);

                        try
                        {
                            var clientMessage = JsonSerializer.Deserialize<JsonElement>(message);

                            if (clientMessage.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "audio")
                            {
                                if (clientMessage.TryGetProperty("data", out var dataElement))
                                {
                                    var audioData = dataElement.GetString();
                                    var wrappedMessage = new
                                    {
                                        realtimeInput = new
                                        {
                                            audio = new
                                            {
                                                data = audioData,
                                                mimeType = "audio/pcm;rate=16000"
                                            }
                                        }
                                    };

                                    var wrappedJson = JsonSerializer.Serialize(wrappedMessage, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                                    var wrappedBytes = Encoding.UTF8.GetBytes(wrappedJson);

                                    await geminiSocket.SendAsync(new ArraySegment<byte>(wrappedBytes), WebSocketMessageType.Text, true, cancellationToken);
                                    logger.LogDebug("Audio relayed to Gemini");
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            logger.LogWarning(ex, "Failed to parse client message");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Client to Gemini relay cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error relaying client to Gemini");
            }
        }

        private async Task RelayGeminiToClientAsync(ClientWebSocket geminiSocket, WebSocket clientSocket, ILogger logger, CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            try
            {
                while (geminiSocket.State == WebSocketState.Open && clientSocket.State == WebSocketState.Open)
                {
                    var result = await geminiSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        logger.LogInformation("Gemini initiated close");
                        break;
                    }

                    var messageContent = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    logger.LogDebug("Received from Gemini: {Message}", messageContent.Substring(0, Math.Min(200, messageContent.Length)));

                    try
                    {
                        var geminiMessage = JsonSerializer.Deserialize<JsonElement>(messageContent);
                        var clientResponses = ParseGeminiResponse(geminiMessage);

                        foreach (var response in clientResponses)
                        {
                            var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                            await clientSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, cancellationToken);
                            logger.LogDebug("Response relayed to client: {ResponseType}", response.GetType().Name);
                        }
                    }
                    catch (JsonException ex)
                    {
                        logger.LogWarning(ex, "Failed to parse Gemini response");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Gemini to client relay cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error relaying Gemini to client");
            }
        }

        private List<object> ParseGeminiResponse(JsonElement geminiMessage)
        {
            var responses = new List<object>();

            if (!geminiMessage.TryGetProperty("serverContent", out var serverContent))
                return responses;

            // Check for audio (inlineData)
            if (serverContent.TryGetProperty("inlineData", out var inlineData))
            {
                if (inlineData.TryGetProperty("data", out var audioData))
                {
                    responses.Add(new
                    {
                        type = "audio",
                        data = audioData.GetString(),
                        mimeType = "audio/pcm;rate=24000"
                    });
                }
            }

            // Check for output transcription
            if (serverContent.TryGetProperty("modelTurn", out var modelTurn))
            {
                if (modelTurn.TryGetProperty("parts", out var parts))
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var text))
                        {
                            responses.Add(new
                            {
                                type = "voiceTranscript",
                                sender = "ai",
                                text = text.GetString()
                            });
                        }
                    }
                }
            }

            // Check for user turn content
            if (serverContent.TryGetProperty("turnComplete", out var turnComplete))
            {
                responses.Add(new
                {
                    type = "turnComplete"
                });
            }

            return responses;
        }
    }
}
