# AI Chat Controller - Dual-Path Architecture

## Overview

The `AIChatController` implements a robust dual-path AI chat system connecting to Google's Gemini APIs:
- **Text Flow**: HTTP POST endpoint for traditional chat using Gemini REST API
- **Voice Flow**: WebSocket endpoint for real-time audio communication using Gemini Live API

## Configuration

### 1. Add Gemini API Key to appsettings.json

```json
{
  "Gemini": {
    "ApiKey": "YOUR_GOOGLE_GEMINI_API_KEY_HERE"
  }
}
```

Get your API key from: https://ai.google.dev

### 2. Verify Service Registration in Program.cs

The following services are registered:
- `IHttpClientFactory` - For REST API calls
- `IGeminiApiClient` - Text flow implementation
- `IGeminiWebSocketHandler` - Voice flow implementation
- `WebSockets` middleware - For real-time communication

## API Endpoints

### 1. Text Chat Endpoint

**Endpoint**: `POST /api/aichat/chat`

**Description**: Send chat messages and receive AI-generated responses using the Gemini Flash model.

**Request Body**:
```json
{
  "conversationHistory": [
    {
      "role": "user",
      "text": "Hello, what can you help me with?"
    },
    {
      "role": "assistant",
      "text": "I'm the Neurix AI Assistant. I can help you with..."
    }
  ],
  "userMessage": "Tell me about your capabilities"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Response generated successfully",
  "generatedText": "I can help you with various tasks...",
  "timestamp": "2026-05-03T10:30:45.123Z"
}
```

**Status Codes**:
- `200 OK` - Successful response generation
- `400 Bad Request` - Invalid request (missing required fields)
- `408 Request Timeout` - Request cancelled/timeout
- `500 Internal Server Error` - Unexpected error

**Example using cURL**:
```bash
curl -X POST "https://localhost:5001/api/aichat/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "conversationHistory": [],
    "userMessage": "What is the capital of France?"
  }'
```

**Example using JavaScript (Fetch API)**:
```javascript
const response = await fetch('/api/aichat/chat', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    conversationHistory: [
      { role: 'user', text: 'Hello' },
      { role: 'assistant', text: 'Hi there!' }
    ],
    userMessage: 'How are you?'
  })
});

const data = await response.json();
console.log(data.generatedText);
```

### 2. Voice Chat WebSocket Endpoint

**Endpoint**: `GET /api/aichat/ws` (WebSocket upgrade)

**Protocol**: WebSocket over HTTPS (wss://)

**Description**: Real-time audio chat with AI using Gemini Live API with audio streaming capabilities.

#### WebSocket Message Flow

**Client to Server (Audio)**:
```json
{
  "type": "audio",
  "data": "base64_encoded_pcm_audio_data"
}
```

**Server to Client (Responses)**:

Audio Response:
```json
{
  "type": "audio",
  "data": "base64_encoded_pcm_audio_data",
  "mimeType": "audio/pcm;rate=24000"
}
```

Voice Transcript (AI Response):
```json
{
  "type": "voiceTranscript",
  "sender": "ai",
  "text": "Transcribed response text"
}
```

User Input Transcript:
```json
{
  "type": "voiceTranscript",
  "sender": "user",
  "text": "Transcribed user input"
}
```

Turn Complete (End of AI Response):
```json
{
  "type": "turnComplete"
}
```

**Example using JavaScript (WebSocket API)**:
```javascript
// Connect to WebSocket
const ws = new WebSocket('wss://localhost:5001/api/aichat/ws');

// Handle connection open
ws.addEventListener('open', () => {
  console.log('Connected to AI Chat');
});

// Handle incoming messages
ws.addEventListener('message', (event) => {
  const message = JSON.parse(event.data);
  
  switch (message.type) {
    case 'audio':
      // Handle audio response
      const audioBuffer = atob(message.data);
      // Play audio...
      break;
    case 'voiceTranscript':
      console.log(`${message.sender}: ${message.text}`);
      break;
    case 'turnComplete':
      console.log('AI finished responding');
      break;
  }
});

// Send audio data
function sendAudio(base64AudioData) {
  ws.send(JSON.stringify({
    type: 'audio',
    data: base64AudioData
  }));
}

// Handle disconnection
ws.addEventListener('close', () => {
  console.log('Disconnected from AI Chat');
});

// Handle errors
ws.addEventListener('error', (error) => {
  console.error('WebSocket error:', error);
});
```

## Technical Details

### Models Used

- **Text Chat**: `gemini-3-flash-preview`
  - Fast, efficient text generation
  - Suitable for real-time chat applications
  
- **Voice Chat**: `gemini-3.1-flash-live-preview`
  - Real-time audio processing
  - Supports bidirectional streaming
  - Voice output with "Aoede" prebuilt voice

### System Prompt

Both endpoints inject the following system instruction:
```
You are the Neurix AI Assistant. You provide helpful, accurate, and concise responses to user queries. You maintain context from the conversation history to provide coherent and relevant answers.
```

### Audio Configuration

**Input Audio**:
- Format: PCM (Pulse Code Modulation)
- Sample Rate: 16,000 Hz
- Encoding: Base64-encoded

**Output Audio**:
- Format: PCM
- Sample Rate: 24,000 Hz
- Encoding: Base64-encoded

### Error Handling

All endpoints include comprehensive error handling:
- **Logging**: All requests and errors are logged using ILogger
- **Cancellation**: Proper CancellationToken handling for clean disconnections
- **Timeouts**: Built-in timeout protection for long-running requests
- **WebSocket Cleanup**: Automatic resource cleanup on disconnection

### API Key Security

⚠️ **IMPORTANT**: Never hardcode API keys in production!

Best practices:
1. Use Azure Key Vault or AWS Secrets Manager
2. Use environment variables for local development
3. Implement rate limiting and usage monitoring
4. Rotate keys regularly

## Integration Examples

### Example 1: Simple Text Chat in ASP.NET MVC

```csharp
@using daleel_e_shop.BLL.DTOs.AIChat;

@{
    ViewData["Title"] = "AI Chat";
}

<div class="chat-container">
    <div id="chatMessages" style="border: 1px solid #ccc; height: 400px; overflow-y: auto; padding: 10px;">
    </div>
    <input type="text" id="userInput" placeholder="Type your message..." style="width: 100%; padding: 10px;" />
    <button onclick="sendMessage()">Send</button>
</div>

<script>
let conversationHistory = [];

async function sendMessage() {
    const userInput = document.getElementById('userInput').value;
    if (!userInput.trim()) return;

    // Add to conversation
    conversationHistory.push({ role: 'user', text: userInput });
    
    // Display user message
    displayMessage('user', userInput);
    document.getElementById('userInput').value = '';

    // Send to AI
    const response = await fetch('/api/aichat/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            conversationHistory: conversationHistory.slice(0, -1),
            userMessage: userInput
        })
    });

    const result = await response.json();
    if (result.success) {
        conversationHistory.push({ role: 'assistant', text: result.generatedText });
        displayMessage('assistant', result.generatedText);
    } else {
        displayMessage('error', result.message);
    }
}

function displayMessage(role, text) {
    const div = document.createElement('div');
    div.style.marginBottom = '10px';
    div.style.padding = '10px';
    div.style.backgroundColor = role === 'user' ? '#e3f2fd' : role === 'assistant' ? '#f5f5f5' : '#ffebee';
    div.innerHTML = `<strong>${role}:</strong> ${text}`;
    document.getElementById('chatMessages').appendChild(div);
    document.getElementById('chatMessages').scrollTop = document.getElementById('chatMessages').scrollHeight;
}
</script>
```

### Example 2: Voice Chat Implementation

```html
<!DOCTYPE html>
<html>
<head>
    <title>AI Voice Chat</title>
</head>
<body>
    <h1>AI Voice Chat</h1>
    <button onclick="startVoiceChat()">Start Voice Chat</button>
    <button onclick="stopVoiceChat()" disabled id="stopBtn">Stop Voice Chat</button>
    <div id="transcript"></div>

    <script>
        let ws = null;
        let mediaRecorder = null;
        let audioContext = null;

        async function startVoiceChat() {
            // Connect WebSocket
            ws = new WebSocket(
                window.location.protocol === 'https:' ? 'wss:' : 'ws:' +
                '//' + window.location.host + '/api/aichat/ws'
            );

            ws.onmessage = (event) => {
                const message = JSON.parse(event.data);
                if (message.type === 'voiceTranscript') {
                    addTranscript(message.sender, message.text);
                }
            };

            ws.onerror = (error) => {
                console.error('WebSocket error:', error);
                alert('Connection error');
            };

            // Start recording audio
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            audioContext = new AudioContext();
            mediaRecorder = new MediaRecorder(stream);

            mediaRecorder.ondataavailable = (event) => {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const audioData = e.target.result.split(',')[1];
                    ws.send(JSON.stringify({
                        type: 'audio',
                        data: audioData
                    }));
                };
                reader.readAsDataURL(event.data);
            };

            mediaRecorder.start(1000); // Send audio chunks every 1 second
            document.getElementById('stopBtn').disabled = false;
        }

        function stopVoiceChat() {
            if (mediaRecorder) mediaRecorder.stop();
            if (ws) ws.close();
            document.getElementById('stopBtn').disabled = true;
        }

        function addTranscript(sender, text) {
            const div = document.createElement('div');
            div.innerHTML = `<strong>${sender}:</strong> ${text}`;
            document.getElementById('transcript').appendChild(div);
        }
    </script>
</body>
</html>
```

## Performance Considerations

1. **Connection Pooling**: IHttpClientFactory handles efficient connection reuse
2. **Async/Await**: Non-blocking I/O for better scalability
3. **WebSocket Keep-Alive**: 30-second intervals to prevent disconnections
4. **Buffer Size**: 4KB buffers for optimal memory usage
5. **CancellationToken**: Proper request cancellation to free resources

## Troubleshooting

### Issue: 400 Bad Request on Chat Endpoint
**Solution**: Ensure `userMessage` field is not empty in the request body.

### Issue: WebSocket connection fails
**Solution**: 
- Verify WebSockets are enabled in `Program.cs`
- Check HTTPS is being used (wss://)
- Ensure API key is configured in appsettings.json

### Issue: 401 Unauthorized errors
**Solution**: Implement JWT authentication if required - add `[Authorize]` attribute to controller methods.

### Issue: High latency on audio streaming
**Solution**: 
- Check network connectivity
- Reduce audio buffer size
- Monitor API rate limits

## Future Enhancements

1. **Authentication**: Add JWT token validation to WebSocket
2. **Rate Limiting**: Implement per-user rate limiting
3. **Audio Format Support**: Add support for more audio formats
4. **Conversation Storage**: Persist conversations to database
5. **Multi-Language Support**: Extend system prompts for different languages
6. **Analytics**: Track usage metrics and performance

## Support

For issues or questions:
1. Check Gemini API documentation: https://ai.google.dev
2. Review error logs in application logs
3. Verify API key and network connectivity
4. Check WebSocket browser compatibility

---

**Version**: 1.0  
**Last Updated**: May 3, 2026  
**Maintainer**: Senior .NET Backend Architect
