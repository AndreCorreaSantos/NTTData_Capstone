using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using System.Threading.Tasks;

public class Connection : MonoBehaviour
{
    WebSocket websocket;

    // URL for WebSocket connection
    public string url = "ws://localhost:8000";

    // Event to notify when a message is received from the server
    public event Action<string> OnServerMessage;

    // StartConnection function to initialize and start the WebSocket connection
    public async void StartConnection()
    {
        websocket = new WebSocket(url);

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            // Convert the received bytes to a string
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received from server: " + message);

            // Notify subscribers (e.g., ClientLogic)
            OnServerMessage?.Invoke(message);
        };

        await websocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
#endif
    }

    // Async method to send binary WebSocket messages
    public async Task SendWebSocketMessageAsync(byte[] message)
    {
        if (websocket == null)
        {
            Debug.LogWarning("WebSocket is not initialized.");
            return;
        }

        if (websocket.State == WebSocketState.Open)
        {
            await websocket.Send(message);
        }
        else
        {
            Debug.LogWarning("WebSocket is not open. Current state: " + websocket.State);
        }
    }

    // Async method to send text messages
    public async Task SendTextAsync(string text)
    {
        if (websocket == null)
        {
            Debug.LogWarning("WebSocket is not initialized.");
            return;
        }

        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(text);
        }
        else
        {
            Debug.LogWarning("WebSocket is not open. Current state: " + websocket.State);
        }
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}
