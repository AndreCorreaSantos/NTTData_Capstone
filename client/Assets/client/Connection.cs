using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NativeWebSocket;

public class Connection : MonoBehaviour
{
  WebSocket websocket;

  // URL for WebSocket connection
  public string url = "ws://localhost:8000";

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
      Debug.Log("OnMessage!");
      Debug.Log(bytes);

      // getting the message as a string
      // var message = System.Text.Encoding.UTF8.GetString(bytes);
      // Debug.Log("OnMessage! " + message);
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

  public async void SendWebSocketMessage(byte[] message)
  {
    if (websocket == null)
    {
      Debug.LogWarning("WebSocket is not initialized.");
      return;
    }

    if (websocket.State == WebSocketState.Open)
    {
      Debug.Log("Sending message via WebSocket...");
      await websocket.Send(message);
      Debug.Log("Message sent!");
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
