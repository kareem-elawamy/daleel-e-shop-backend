// Testing examples for AIChatController

// ============================================
// 1. TEXT CHAT ENDPOINT TESTS
// ============================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using daleel_e_shop.BLL.DTOs.AIChat;

namespace AIChatTests
{
    public class AIChatControllerTests
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://localhost:5001";

        public AIChatControllerTests()
        {
            _httpClient = new HttpClient();
            // For testing, disable SSL certificate validation (NOT for production!)
            // var handler = new HttpClientHandler();
            // handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            // _httpClient = new HttpClient(handler);
        }

        // Test 1: Simple single message
        public async Task TestSimpleChatAsync()
        {
            var request = new ChatRequestDto
            {
                ConversationHistory = new List<ChatMessageDto>(),
                UserMessage = "What is the capital of France?"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/aichat/chat", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Response: {responseText}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ChatResponseDto>(responseText);
                Console.WriteLine($"Generated Text: {result?.GeneratedText}");
            }
        }

        // Test 2: Multi-turn conversation
        public async Task TestMultiTurnConversationAsync()
        {
            var history = new List<ChatMessageDto>();

            // First turn
            var request1 = new ChatRequestDto
            {
                ConversationHistory = history,
                UserMessage = "Tell me about AI"
            };

            var response1 = await SendChatRequestAsync(request1);
            if (response1 != null && response1.Success)
            {
                history.Add(new ChatMessageDto { Role = "user", Text = request1.UserMessage });
                history.Add(new ChatMessageDto { Role = "assistant", Text = response1.GeneratedText });
                Console.WriteLine($"AI: {response1.GeneratedText}\n");
            }

            // Second turn
            var request2 = new ChatRequestDto
            {
                ConversationHistory = history,
                UserMessage = "How does it relate to machine learning?"
            };

            var response2 = await SendChatRequestAsync(request2);
            if (response2 != null && response2.Success)
            {
                history.Add(new ChatMessageDto { Role = "user", Text = request2.UserMessage });
                history.Add(new ChatMessageDto { Role = "assistant", Text = response2.GeneratedText });
                Console.WriteLine($"AI: {response2.GeneratedText}\n");
            }

            // Third turn
            var request3 = new ChatRequestDto
            {
                ConversationHistory = history,
                UserMessage = "What are the main applications?"
            };

            var response3 = await SendChatRequestAsync(request3);
            if (response3 != null && response3.Success)
            {
                Console.WriteLine($"AI: {response3.GeneratedText}\n");
            }
        }

        // Test 3: Error handling - empty message
        public async Task TestErrorHandlingAsync()
        {
            var request = new ChatRequestDto
            {
                ConversationHistory = new List<ChatMessageDto>(),
                UserMessage = "" // Empty message should fail
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/aichat/chat", content);
            
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Expected: 400 Bad Request");
            
            if (!response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ChatResponseDto>(responseText);
                Console.WriteLine($"Error Message: {result?.Message}");
            }
        }

        // Helper method to send chat request
        private async Task<ChatResponseDto> SendChatRequestAsync(ChatRequestDto request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/aichat/chat", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ChatResponseDto>(responseText);
            }

            return null;
        }

        public static async Task Main(string[] args)
        {
            var tests = new AIChatControllerTests();

            Console.WriteLine("=== Test 1: Simple Chat ===\n");
            await tests.TestSimpleChatAsync();

            Console.WriteLine("\n=== Test 2: Multi-Turn Conversation ===\n");
            await tests.TestMultiTurnConversationAsync();

            Console.WriteLine("\n=== Test 3: Error Handling ===\n");
            await tests.TestErrorHandlingAsync();

            Console.WriteLine("\n=== All Tests Completed ===");
        }
    }
}


// ============================================
// 2. WEBSOCKET VOICE CHAT CLIENT (JavaScript)
// ============================================

/*

// voiceChat.js - Browser-based voice chat client

class AIChatVoiceClient {
    constructor(wsUrl, onTranscript, onAudio, onTurnComplete) {
        this.wsUrl = wsUrl;
        this.onTranscript = onTranscript;
        this.onAudio = onAudio;
        this.onTurnComplete = onTurnComplete;
        this.ws = null;
        this.mediaRecorder = null;
        this.audioContext = null;
        this.isConnected = false;
    }

    async connect() {
        return new Promise((resolve, reject) => {
            this.ws = new WebSocket(this.wsUrl);

            this.ws.onopen = () => {
                console.log('Connected to AI Chat WebSocket');
                this.isConnected = true;
                resolve();
            };

            this.ws.onmessage = (event) => {
                try {
                    const message = JSON.parse(event.data);
                    this.handleMessage(message);
                } catch (error) {
                    console.error('Error parsing message:', error);
                }
            };

            this.ws.onerror = (error) => {
                console.error('WebSocket error:', error);
                this.isConnected = false;
                reject(error);
            };

            this.ws.onclose = () => {
                console.log('WebSocket closed');
                this.isConnected = false;
            };

            // Timeout after 5 seconds
            setTimeout(() => {
                if (!this.isConnected) {
                    reject(new Error('Connection timeout'));
                }
            }, 5000);
        });
    }

    handleMessage(message) {
        switch (message.type) {
            case 'audio':
                if (this.onAudio) {
                    this.onAudio(message.data, message.mimeType);
                }
                break;
            case 'voiceTranscript':
                if (this.onTranscript) {
                    this.onTranscript(message.sender, message.text);
                }
                break;
            case 'turnComplete':
                if (this.onTurnComplete) {
                    this.onTurnComplete();
                }
                break;
        }
    }

    async startRecording() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ 
                audio: { sampleRate: 16000 } 
            });
            
            this.mediaRecorder = new MediaRecorder(stream);
            this.mediaRecorder.ondataavailable = (event) => {
                this.sendAudioChunk(event.data);
            };
            
            this.mediaRecorder.start(500); // Send chunks every 500ms
            console.log('Recording started');
        } catch (error) {
            console.error('Error accessing microphone:', error);
        }
    }

    stopRecording() {
        if (this.mediaRecorder) {
            this.mediaRecorder.stop();
            console.log('Recording stopped');
        }
    }

    sendAudioChunk(audioBlob) {
        const reader = new FileReader();
        reader.onload = (event) => {
            const audioData = event.target.result.split(',')[1]; // Get base64
            this.sendMessage({
                type: 'audio',
                data: audioData
            });
        };
        reader.readAsDataURL(audioBlob);
    }

    sendMessage(message) {
        if (this.ws && this.isConnected) {
            this.ws.send(JSON.stringify(message));
        }
    }

    playAudio(base64Data, mimeType) {
        // Convert base64 to ArrayBuffer
        const binaryString = atob(base64Data);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        // Create audio context and buffer
        if (!this.audioContext) {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }

        // Note: Actual playback would require proper WAV/PCM handling
        console.log('Audio received, ready to play');
    }

    disconnect() {
        if (this.mediaRecorder) {
            this.mediaRecorder.stop();
        }
        if (this.ws) {
            this.ws.close();
        }
        this.isConnected = false;
    }
}

// Usage Example:
/*
const voiceChat = new AIChatVoiceClient(
    'wss://localhost:5001/api/aichat/ws',
    (sender, text) => {
        console.log(`${sender}: ${text}`);
    },
    (audioData, mimeType) => {
        console.log('Audio received:', mimeType);
    },
    () => {
        console.log('AI finished turn');
    }
);

await voiceChat.connect();
await voiceChat.startRecording();

// Later...
voiceChat.stopRecording();
voiceChat.disconnect();
*/




// ============================================
// 3. CURL COMMANDS FOR TESTING
// ============================================

/*

# Test 1: Simple chat request
curl -X POST "https://localhost:5001/api/aichat/chat" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "conversationHistory": [],
    "userMessage": "What is 2+2?"
  }'

# Test 2: Chat with conversation history
curl -X POST "https://localhost:5001/api/aichat/chat" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "conversationHistory": [
      {"role": "user", "text": "Hi"},
      {"role": "assistant", "text": "Hello! How can I help you?"}
    ],
    "userMessage": "Tell me a joke"
  }'

# Test 3: WebSocket connection (requires wscat or similar)
wscat -c wss://localhost:5001/api/aichat/ws --no-check

# After connecting, send:
{"type":"audio","data":"base64_encoded_audio_data"}

# Test 4: Error case - empty message
curl -X POST "https://localhost:5001/api/aichat/chat" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "conversationHistory": [],
    "userMessage": ""
  }'

*/
