# AI Chat Controller - Quick Start Guide

## 🚀 Setup (5 Minutes)

### Step 1: Get Gemini API Key
1. Go to [Google AI Studio](https://ai.google.dev)
2. Click "Get API Key"
3. Copy your API key

### Step 2: Configure Application
Add to `appsettings.json`:
```json
{
  "Gemini": {
    "ApiKey": "YOUR_GOOGLE_GEMINI_API_KEY_HERE"
  }
}
```

### Step 3: Verify Installation
The following files are already created:
```
Controllers/
  └── AIChatController.cs

Services/AIChat/
  ├── GeminiApiClient.cs
  └── GeminiWebSocketHandler.cs

DTOs/AIChat/
  ├── ChatMessageDto.cs
  ├── ChatRequestDto.cs
  └── ChatResponseDto.cs

Program.cs (already updated)
```

### Step 4: Test the API
```bash
dotnet run
```

## 📝 Text Chat - Quick Example

### Using Postman/cURL
```bash
curl -X POST "http://localhost:5001/api/aichat/chat" \
  -H "Content-Type: application/json" \
  -d '{
    "conversationHistory": [],
    "userMessage": "Say hello to me"
  }'
```

### Response
```json
{
  "success": true,
  "message": "Response generated successfully",
  "generatedText": "Hello! Nice to meet you. I'm the Neurix AI Assistant. How can I help you today?",
  "timestamp": "2026-05-03T10:30:45.123Z"
}
```

### Using C# HttpClient
```csharp
var request = new ChatRequestDto
{
    ConversationHistory = new(),
    UserMessage = "Hello, how are you?"
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await httpClient.PostAsync("http://localhost:5001/api/aichat/chat", content);
```

## 🎤 Voice Chat - Quick Example

### Using JavaScript WebSocket
```javascript
// Connect
const ws = new WebSocket('ws://localhost:5001/api/aichat/ws');

ws.onopen = () => console.log('Connected');

ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);
  console.log(msg); // { type: "audio"|"voiceTranscript"|"turnComplete", ... }
};

// Send audio
ws.send(JSON.stringify({
  type: "audio",
  data: "base64_encoded_pcm_audio"
}));

// Listen for response
ws.onmessage = (event) => {
  const response = JSON.parse(event.data);
  if (response.type === 'voiceTranscript') {
    console.log(`AI: ${response.text}`);
  }
};
```

## 🔧 Common Issues & Fixes

### ❌ Error: "API key not configured"
**Solution**: Make sure `appsettings.json` has the correct key:
```json
"Gemini": {
  "ApiKey": "paste_your_key_here"
}
```

### ❌ Error: 400 Bad Request on chat endpoint
**Solution**: Ensure `userMessage` is not empty:
```javascript
// ❌ Wrong
{ "conversationHistory": [], "userMessage": "" }

// ✅ Correct
{ "conversationHistory": [], "userMessage": "Hello" }
```

### ❌ WebSocket won't connect
**Solution**: 
- Ensure you're using HTTPS/WSS in production
- Use HTTP/WS for localhost testing
- Check browser console for details

### ❌ "401 Unauthorized" from Gemini
**Solution**: Your API key is invalid or expired. Get a new one from [Google AI Studio](https://ai.google.dev).

## 📊 Conversation Flow Example

### Multi-turn Chat
```csharp
// Turn 1
var response1 = await PostChat(new ChatRequestDto
{
    ConversationHistory = new(),
    UserMessage = "What is AI?"
});
// Response: "AI is artificial intelligence..."

// Turn 2
var response2 = await PostChat(new ChatRequestDto
{
    ConversationHistory = new()
    {
        new ChatMessageDto { Role = "user", Text = "What is AI?" },
        new ChatMessageDto { Role = "assistant", Text = "AI is artificial intelligence..." }
    },
    UserMessage = "Give me an example"
});
// Response: "For example, machine learning..."

// Turn 3
var response3 = await PostChat(new ChatRequestDto
{
    ConversationHistory = new()
    {
        new ChatMessageDto { Role = "user", Text = "What is AI?" },
        new ChatMessageDto { Role = "assistant", Text = "AI is artificial intelligence..." },
        new ChatMessageDto { Role = "user", Text = "Give me an example" },
        new ChatMessageDto { Role = "assistant", Text = "For example, machine learning..." }
    },
    UserMessage = "How do I learn ML?"
});
// Response: "To learn machine learning..."
```

## 🎨 Frontend Integration Example

### HTML + JavaScript Chat UI
```html
<!DOCTYPE html>
<html>
<head>
    <title>Neurix AI Chat</title>
    <style>
        .chat-container { max-width: 600px; margin: 20px auto; }
        .messages { border: 1px solid #ddd; height: 400px; overflow-y: auto; padding: 10px; }
        .message { margin: 10px 0; padding: 10px; border-radius: 5px; }
        .user { background: #e3f2fd; text-align: right; }
        .ai { background: #f5f5f5; }
        input { width: 100%; padding: 10px; }
        button { padding: 10px 20px; background: #1976d2; color: white; border: none; border-radius: 5px; cursor: pointer; }
    </style>
</head>
<body>
    <div class="chat-container">
        <h1>Neurix AI Chat</h1>
        <div class="messages" id="messages"></div>
        <input type="text" id="input" placeholder="Type your message...">
        <button onclick="sendMessage()">Send</button>
    </div>

    <script>
        let history = [];

        async function sendMessage() {
            const text = document.getElementById('input').value;
            if (!text) return;

            // Display user message
            addMessage('user', text);
            
            // Add to history
            history.push({ role: 'user', text });
            document.getElementById('input').value = '';

            // Get AI response
            const response = await fetch('/api/aichat/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    conversationHistory: history.slice(0, -1),
                    userMessage: text
                })
            });

            const data = await response.json();
            if (data.success) {
                history.push({ role: 'assistant', text: data.generatedText });
                addMessage('ai', data.generatedText);
            } else {
                addMessage('error', 'Error: ' + data.message);
            }
        }

        function addMessage(role, text) {
            const div = document.createElement('div');
            div.className = 'message ' + role;
            div.textContent = text;
            document.getElementById('messages').appendChild(div);
            document.getElementById('messages').scrollTop = document.getElementById('messages').scrollHeight;
        }

        // Allow Enter key to send
        document.getElementById('input').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') sendMessage();
        });
    </script>
</body>
</html>
```

## 📚 Key Files Reference

| File | Purpose |
|------|---------|
| `AIChatController.cs` | Main controller with HTTP POST and WebSocket GET endpoints |
| `GeminiApiClient.cs` | Handles REST API calls to Gemini |
| `GeminiWebSocketHandler.cs` | Handles WebSocket relay to Gemini Live API |
| `ChatMessageDto.cs` | Single message in conversation |
| `ChatRequestDto.cs` | HTTP request payload |
| `ChatResponseDto.cs` | HTTP response payload |
| `Program.cs` | Service registration and middleware setup |

## 🔒 Security Checklist

- [ ] API key is in `appsettings.json` (not in code)
- [ ] Use HTTPS in production
- [ ] Use WSS (WebSocket Secure) in production
- [ ] Consider adding JWT authentication to endpoints
- [ ] Implement rate limiting to prevent abuse
- [ ] Validate all user inputs
- [ ] Log errors (but never log sensitive data)
- [ ] Rotate API keys regularly

## 🚀 Deployment Considerations

### Azure App Service
```csharp
// Use Key Vault for API key
var keyVaultUrl = "https://your-keyvault.vault.azure.net/";
var credential = new DefaultAzureCredential();
var client = new SecretClient(new Uri(keyVaultUrl), credential);
var secret = await client.GetSecretAsync("GeminiApiKey");
var apiKey = secret.Value.Value;
```

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /app
COPY . .
RUN dotnet build
ENV Gemini__ApiKey="your-key-here"
ENTRYPOINT ["dotnet", "run"]
EXPOSE 5001
```

### Environment Variables
```bash
# Linux/Mac
export Gemini__ApiKey="your-key"

# Windows PowerShell
$env:Gemini__ApiKey="your-key"

# .NET runs with this key
dotnet run
```

## 🧪 Testing Tools

### Postman Collection
```json
{
  "info": { "name": "AI Chat API" },
  "item": [
    {
      "name": "Simple Chat",
      "request": {
        "method": "POST",
        "url": "http://localhost:5001/api/aichat/chat",
        "body": {
          "conversationHistory": [],
          "userMessage": "Hello"
        }
      }
    }
  ]
}
```

### VS Code REST Client
```rest
### Simple Chat
POST http://localhost:5001/api/aichat/chat
Content-Type: application/json

{
  "conversationHistory": [],
  "userMessage": "Hello, what can you help with?"
}
```

## 📞 Support Resources

- [Gemini API Docs](https://ai.google.dev/docs)
- [ASP.NET Core Docs](https://learn.microsoft.com/en-us/aspnet/core/)
- [WebSocket in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets)
- [System.Text.Json Guide](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json)

## ✨ Next Steps

1. ✅ Add your API key
2. ✅ Run `dotnet run`
3. ✅ Test with cURL or Postman
4. ✅ Build your UI with HTML/JS
5. ✅ Deploy to your hosting platform
6. ✅ Monitor usage and performance

**Happy coding! 🎉**

---
Generated for: Daleel E-Shop Project
Date: May 3, 2026
