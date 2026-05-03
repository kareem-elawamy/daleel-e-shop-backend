# AI Chat Controller - Architecture & Implementation Details

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client Applications                       │
│  (Web Browser, Mobile App, Desktop Client, etc.)               │
└────────────┬─────────────────────────────┬─────────────────────┘
             │                             │
        ┌────▼────────────────┐    ┌──────▼──────────────────┐
        │   HTTP POST          │    │   WebSocket (wss://)    │
        │ /api/aichat/chat     │    │  /api/aichat/ws         │
        └────┬────────────────┘    └──────┬──────────────────┘
             │                             │
        ┌────▼──────────────────────────────▼──────────────────┐
        │         AIChatController (ASP.NET Core)              │
        │                                                       │
        │  POST /chat ────────► Text Flow Handler              │
        │  GET /ws ──────────► WebSocket Flow Handler          │
        └────┬──────────────────────────┬─────────────────────┘
             │                          │
   ┌─────────▼──────────────┐  ┌───────▼────────────────────┐
   │ GeminiApiClient         │  │ GeminiWebSocketHandler     │
   │ (HTTP REST API)         │  │ (WebSocket Relay)          │
   │                         │  │                            │
   │ • System Prompt         │  │ • Connection Setup         │
   │ • Request Formatting    │  │ • Audio Relay              │
   │ • Response Parsing      │  │ • Transcript Relay         │
   │ • Error Handling        │  │ • Turn Completion          │
   └─────────┬───────────────┘  └───────┬────────────────────┘
             │                          │
   ┌─────────▼──────────────┐  ┌───────▼────────────────────┐
   │ HTTP: POST request     │  │ WSS: Bidirectional Stream  │
   │ model=gemini-3-flash   │  │ model=gemini-3.1-flash-live│
   │ Content-Type: JSON     │  │ mimeType: audio/pcm        │
   └─────────┬───────────────┘  └───────┬────────────────────┘
             │                          │
             └──────────────┬───────────┘
                            │
        ┌───────────────────▼────────────────────┐
        │    Google Generative AI API             │
        │  https://generativelanguage.googleapis.com
        │                                         │
        │  ┌──────────────────────────────────┐  │
        │  │ Text Generation API (REST)       │  │
        │  │ /v1beta/models/gemini-3-flash    │  │
        │  └──────────────────────────────────┘  │
        │                                         │
        │  ┌──────────────────────────────────┐  │
        │  │ Live API (WebSocket)             │  │
        │  │ BidiGenerateContent              │  │
        │  │ /ws/google.ai.generativelanguage │  │
        │  └──────────────────────────────────┘  │
        └─────────────────────────────────────────┘
```

## Class Hierarchy

```
IHttpClientFactory (Microsoft.AspNetCore.Http)
    └── Used by: GeminiApiClient

IGeminiApiClient (Custom)
    └── GeminiApiClient
        ├── GenerateChatResponseAsync()
        └── Handles text-only chat via REST API

IGeminiWebSocketHandler (Custom)
    └── GeminiWebSocketHandler
        ├── HandleWebSocketAsync()
        ├── SendSetupMessageAsync()
        ├── RelayClientToGeminiAsync()
        ├── RelayGeminiToClientAsync()
        └── ParseGeminiResponse()

AIChatController : ControllerBase
    ├── Dependency: IGeminiApiClient
    ├── Dependency: IGeminiWebSocketHandler
    ├── Dependency: ILogger<AIChatController>
    ├── Dependency: IConfiguration
    │
    ├── [HttpPost("chat")]
    │   └── PostChat(ChatRequestDto, CancellationToken)
    │
    └── [HttpGet("ws")]
        └── GetWebSocket()

DTOs (Data Transfer Objects):
    ├── ChatMessageDto
    │   ├── Role: string
    │   └── Text: string
    ├── ChatRequestDto
    │   ├── ConversationHistory: List<ChatMessageDto>
    │   └── UserMessage: string
    └── ChatResponseDto
        ├── Success: bool
        ├── Message: string
        ├── GeneratedText: string?
        └── Timestamp: DateTime
```

## Data Flow Diagrams

### Text Chat Flow (HTTP POST)

```
Client Request
    │
    ├─ POST /api/aichat/chat
    └─ Body: {
         "conversationHistory": [...],
         "userMessage": "question"
       }
    │
    ▼
AIChatController.PostChat()
    │
    ├─ Validate request
    ├─ Extract conversation history
    └─ Extract user message
    │
    ▼
GeminiApiClient.GenerateChatResponseAsync()
    │
    ├─ Prepare payload:
    │  ├─ Add conversation history as "contents"
    │  ├─ Add system instruction
    │  └─ Add generation config (temperature, max_tokens)
    │
    ├─ Serialize to JSON (System.Text.Json)
    │
    ├─ Create HTTP POST request
    │  └─ URL: https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={API_KEY}
    │
    ├─ Send via IHttpClientFactory
    │
    ▼ (Network Request to Gemini API)
    │
    ├─ Parse response
    │  └─ Extract: response.candidates[0].content.parts[0].text
    │
    ├─ Return generated text
    │
    ▼
AIChatController
    │
    └─ Return ChatResponseDto
       ├─ Success: true
       └─ GeneratedText: "response from AI"
    │
    ▼
Client Response (200 OK)
    └─ Body: {
         "success": true,
         "message": "Response generated successfully",
         "generatedText": "...",
         "timestamp": "2026-05-03T..."
       }
```

### Voice Chat Flow (WebSocket)

```
Client WebSocket Request
    │
    ├─ GET /api/aichat/ws
    └─ Upgrade to WebSocket
    │
    ▼
AIChatController.GetWebSocket()
    │
    ├─ Accept WebSocket upgrade
    └─ Extract API key from config
    │
    ▼
GeminiWebSocketHandler.HandleWebSocketAsync()
    │
    ├─ Create ClientWebSocket
    │
    ├─ Connect to:
    │  └─ wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={API_KEY}
    │
    ├─ Send SETUP message:
    │  └─ {
    │       "setup": {
    │         "model": "models/gemini-3.1-flash-live-preview",
    │         "generationConfig": {
    │           "speechConfig": {
    │             "voiceConfig": {
    │               "prebuiltVoiceConfig": { "voiceName": "Aoede" }
    │             }
    │           }
    │         },
    │         "systemInstruction": { "parts": [{ "text": "..." }] }
    │       }
    │     }
    │
    └─ Start two relay loops:
       │
       ├─ LOOP 1: Client → Gemini (RelayClientToGeminiAsync)
       │  │
       │  ├─ Receive from client WebSocket
       │  │  └─ Expected: { "type": "audio", "data": "base64_pcm" }
       │  │
       │  ├─ Wrap audio:
       │  │  └─ {
       │  │       "realtimeInput": {
       │  │         "audio": {
       │  │           "data": "base64_pcm",
       │  │           "mimeType": "audio/pcm;rate=16000"
       │  │         }
       │  │       }
       │  │     }
       │  │
       │  └─ Send to Gemini WebSocket
       │
       ├─ LOOP 2: Gemini → Client (RelayGeminiToClientAsync)
       │  │
       │  ├─ Receive from Gemini WebSocket
       │  │
       │  ├─ Parse serverContent:
       │  │  │
       │  │  ├─ IF contains inlineData (audio):
       │  │  │  └─ Send: { "type": "audio", "data": "base64", "mimeType": "audio/pcm;rate=24000" }
       │  │  │
       │  │  ├─ IF contains modelTurn.parts (text):
       │  │  │  └─ Send: { "type": "voiceTranscript", "sender": "ai", "text": "..." }
       │  │  │
       │  │  └─ IF contains turnComplete:
       │  │     └─ Send: { "type": "turnComplete" }
       │  │
       │  └─ Send to client WebSocket
       │
       └─ Loop until one side closes connection
          │
          ├─ Close Gemini WebSocket
          └─ Close client WebSocket
```

## Request/Response Examples

### Example 1: Text Chat Request

```json
POST /api/aichat/chat HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
  "conversationHistory": [
    {
      "role": "user",
      "text": "What is machine learning?"
    },
    {
      "role": "assistant",
      "text": "Machine learning is a subset of artificial intelligence..."
    }
  ],
  "userMessage": "Can you give me an example?"
}
```

**Gemini API Request (Internal)**:

```json
POST /v1beta/models/gemini-3-flash-preview:generateContent?key=XXXX HTTP/1.1
Host: generativelanguage.googleapis.com
Content-Type: application/json

{
  "contents": [
    {
      "role": "user",
      "parts": [
        {
          "text": "What is machine learning?"
        }
      ]
    },
    {
      "role": "model",
      "parts": [
        {
          "text": "Machine learning is a subset of artificial intelligence..."
        }
      ]
    },
    {
      "role": "user",
      "parts": [
        {
          "text": "Can you give me an example?"
        }
      ]
    }
  ],
  "systemInstruction": {
    "parts": [
      {
        "text": "You are the Neurix AI Assistant..."
      }
    ]
  },
  "generationConfig": {
    "temperature": 0.7,
    "topP": 0.9,
    "topK": 40,
    "maxOutputTokens": 2048
  }
}
```

**Response**:

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true,
  "message": "Response generated successfully",
  "generatedText": "Sure! Here's an example of machine learning. Consider a spam filter...",
  "timestamp": "2026-05-03T10:30:45.123Z"
}
```

### Example 2: WebSocket Setup

**Client connects**:
```
GET /api/aichat/ws HTTP/1.1
Host: localhost:5001
Upgrade: websocket
Connection: Upgrade
```

**Server sends setup**:
```json
{
  "setup": {
    "model": "models/gemini-3.1-flash-live-preview",
    "generationConfig": {
      "speechConfig": {
        "voiceConfig": {
          "prebuiltVoiceConfig": {
            "voiceName": "Aoede"
          }
        }
      }
    },
    "systemInstruction": {
      "parts": [
        {
          "text": "You are the Neurix AI Assistant..."
        }
      ]
    },
    "tools": []
  }
}
```

**Client sends audio**:
```json
{
  "type": "audio",
  "data": "SUQzBAAAI1/tNiAgICAgICBMYVZmL01QMy9..."
}
```

**Server relays to Gemini**:
```json
{
  "realtimeInput": {
    "audio": {
      "data": "SUQzBAAAI1/tNiAgICAgICBMYVZmL01QMy9...",
      "mimeType": "audio/pcm;rate=16000"
    }
  }
}
```

**Server receives from Gemini and relays to client**:
```json
{
  "type": "audio",
  "data": "//NExAAhbkAAAH2AAAA...",
  "mimeType": "audio/pcm;rate=24000"
}
```

## Configuration Settings

**appsettings.json**:
```json
{
  "Gemini": {
    "ApiKey": "YOUR_GOOGLE_GEMINI_API_KEY"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "daleel_e_shop.Services.AIChat": "Debug"
    }
  }
}
```

## Error Handling Strategy

```
Try-Catch Hierarchy:
│
├─ AIChatController
│  │
│  ├─ Catch: OperationCanceledException
│  │  └─ Return: 408 Request Timeout
│  │
│  ├─ Catch: Exception (unexpected)
│  │  └─ Return: 500 Internal Server Error
│  │
│  └─ Validate Input
│     ├─ If null → 400 Bad Request
│     └─ If empty → 400 Bad Request
│
├─ GeminiApiClient
│  │
│  ├─ Catch: HttpRequestException
│  │  └─ Log error, return null
│  │
│  ├─ Catch: JsonException
│  │  └─ Log error, return null
│  │
│  └─ Check HTTP response status
│     ├─ If not 2xx → Log and return null
│     └─ If 2xx but invalid JSON → Log warning
│
└─ GeminiWebSocketHandler
   │
   ├─ Catch: OperationCanceledException
   │  └─ Log cancellation
   │
   ├─ Catch: WebSocketException
   │  └─ Log and cleanup sockets
   │
   └─ Catch: JsonException (message parsing)
      └─ Log warning, continue processing
```

## Logging Strategy

**Log Levels**:
- **Information**: Major operations (connection established, request processed)
- **Debug**: Detailed operations (message content, data relayed)
- **Warning**: Unexpected but recoverable conditions
- **Error**: Significant errors that affect operation

**Log Examples**:
```
INFO: WebSocket connection established
DEBUG: Sending request to Gemini API: https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key=...
INFO: Successfully generated response from Gemini API
WARN: Unexpected response structure from Gemini API
ERROR: Gemini API error: 401 - {"error": {"code": 401, "message": "Invalid API key"}}
DEBUG: Audio relayed to Gemini
INFO: WebSocket relay completed
```

## Performance Considerations

### 1. Connection Pooling
```csharp
builder.Services.AddHttpClient(); // Reuses connections
```

### 2. Async/Await
All I/O operations use async to free threads for other requests.

### 3. Buffer Size
4KB buffers for WebSocket messages (balance between memory and throughput).

### 4. Keep-Alive
30-second WebSocket keep-alive to prevent idle disconnections.

### 5. Cancellation Token
Proper propagation allows immediate cleanup on client disconnect.

## Security Considerations

### 1. API Key Management
```csharp
_apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException(...);
```
- Stored in secure configuration
- Never logged
- Passed via environment variables (production)

### 2. Input Validation
```csharp
if (string.IsNullOrWhiteSpace(request.UserMessage))
    return BadRequest(...);
```

### 3. Rate Limiting (Recommended)
Add middleware to prevent abuse:
```csharp
builder.Services.AddRateLimiter(options => {
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }
        )
    );
});
```

### 4. HTTPS Only
WebSockets should always use `wss://` (WebSocket Secure).

## Testing Approach

1. **Unit Tests**: Test individual methods in isolation
2. **Integration Tests**: Test controller endpoints with mocked dependencies
3. **E2E Tests**: Test against actual Gemini API (with rate limiting)
4. **Load Tests**: Verify performance under concurrent connections

## Future Enhancements

1. **Authentication**: Add JWT validation to both endpoints
2. **Conversation Persistence**: Store chat history in database
3. **User Management**: Track usage per user
4. **Analytics**: Collect metrics (response times, error rates)
5. **Model Selection**: Allow client to choose different models
6. **Language Support**: Multilingual system prompts
7. **Audio Format Support**: Support WAV, MP3 in addition to PCM
8. **Rate Limiting**: Implement per-user rate limiting
9. **Streaming Responses**: Stream text responses instead of waiting for completion
10. **Context Windows**: Implement sliding context windows for long conversations

## References

- [Google Generative AI API Documentation](https://ai.google.dev/docs)
- [Gemini 3 Flash Model](https://ai.google.dev/models/gemini-3-flash-preview)
- [Gemini Live API (Voice)](https://ai.google.dev/docs/gemini-live)
- [ASP.NET Core WebSocket Support](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets)
- [System.Text.Json Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.text.json)
- [IHttpClientFactory Usage](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)

---

**Version**: 1.0  
**Last Updated**: May 3, 2026  
**Architecture**: Senior .NET Backend Architect
