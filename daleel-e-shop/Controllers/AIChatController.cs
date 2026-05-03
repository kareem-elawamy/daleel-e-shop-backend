using daleel_e_shop.BLL.DTOs.AIChat;
using daleel_e_shop.Services.AIChat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace daleel_e_shop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIChatController : ControllerBase
    {
        private readonly IGeminiApiClient _geminiApiClient;
        private readonly IGeminiWebSocketHandler _websocketHandler;
        private readonly ILogger<AIChatController> _logger;
        private readonly IConfiguration _configuration;

        public AIChatController(
            IGeminiApiClient geminiApiClient,
            IGeminiWebSocketHandler websocketHandler,
            ILogger<AIChatController> logger,
            IConfiguration configuration)
        {
            _geminiApiClient = geminiApiClient;
            _websocketHandler = websocketHandler;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Text flow endpoint for AI chat using Gemini REST API
        /// </summary>
        /// <param name="request">Chat request containing conversation history</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated AI response</returns>
        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponseDto>> PostChat([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                _logger.LogWarning("Chat request is null");
                return BadRequest(new ChatResponseDto
                {
                    Success = false,
                    Message = "Chat request cannot be empty"
                });
            }

            if (string.IsNullOrWhiteSpace(request.UserMessage))
            {
                _logger.LogWarning("User message is empty");
                return BadRequest(new ChatResponseDto
                {
                    Success = false,
                    Message = "User message cannot be empty"
                });
            }

            try
            {
                _logger.LogInformation("Processing chat request with {HistoryCount} history messages", request.ConversationHistory?.Count ?? 0);

                // Convert DTOs to tuples for API call
                var conversationHistory = request.ConversationHistory?
                    .Select(m => (m.Role, m.Text))
                    .ToList() ?? new List<(string, string)>();

                // Call Gemini API
                var generatedText = await _geminiApiClient.GenerateChatResponseAsync(
                    conversationHistory,
                    request.UserMessage,
                    cancellationToken);

                if (generatedText == null)
                {
                    _logger.LogError("Failed to generate response from Gemini API");
                    return StatusCode(StatusCodes.Status500InternalServerError, new ChatResponseDto
                    {
                        Success = false,
                        Message = "Failed to generate response from AI service"
                    });
                }

                _logger.LogInformation("Chat response generated successfully");
                return Ok(new ChatResponseDto
                {
                    Success = true,
                    Message = "Response generated successfully",
                    GeneratedText = generatedText
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chat request cancelled");
                return StatusCode(StatusCodes.Status408RequestTimeout, new ChatResponseDto
                {
                    Success = false,
                    Message = "Request timeout"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in chat endpoint");
                return StatusCode(StatusCodes.Status500InternalServerError, new ChatResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred"
                });
            }
        }

        /// <summary>
        /// Voice flow endpoint using WebSocket for real-time audio communication with Gemini
        /// </summary>
        /// <returns>WebSocket upgrade</returns>
        [HttpGet("ws")]
        public async Task GetWebSocket()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    _logger.LogInformation("WebSocket connection accepted");

                    var apiKey = _configuration["Gemini:ApiKey"];
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        _logger.LogError("Gemini API key is not configured");
                        await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "API key not configured", CancellationToken.None);
                        return;
                    }

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
                    await _websocketHandler.HandleWebSocketAsync(webSocket, _logger, apiKey, cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting WebSocket connection");
                }
            }
            else
            {
                _logger.LogWarning("Received non-WebSocket request at WebSocket endpoint");
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await HttpContext.Response.WriteAsync("WebSocket connection required");
            }
        }
    }
}
