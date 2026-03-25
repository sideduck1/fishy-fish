/*
 * FishReceiver.cs
 * ─────────────────────────────────────────────────────────────────────────────
 * Connects to the Node.js server via WebSocket and relays incoming fish images
 * to FishSpawner on the Unity main thread.
 *
 * SETUP:
 *  1. Install NativeWebSocket:
 *     Window → Package Manager → + → "Add package from git URL…"
 *     → https://github.com/endel/NativeWebSocket.git#upm
 *  2. Drop this script + FishSpawner + FishSwimmer into Assets/Scripts/
 *  3. Add a GameObject "FishManager" to the scene, attach FishReceiver & FishSpawner
 *  4. Set "Server Url" to ws://<your-PC-IP>:3000  (e.g. ws://192.168.1.10:3000)
 *  5. Press Play.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

[RequireComponent(typeof(FishSpawner))]
public class FishReceiver : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("WebSocket URL of the Node.js server, e.g. ws://192.168.1.10:3000")]
    public string serverUrl = "ws://localhost:3000";

    [Tooltip("Seconds between reconnect attempts")]
    public float reconnectDelay = 3f;

    // Thread-safe queue of raw base64 image strings received from server
    private readonly Queue<string> _pendingImages = new Queue<string>();
    private readonly object _lock = new object();

    private WebSocket _ws;
    private FishSpawner _spawner;
    private bool _quitting;

    private void Awake()
    {
        _spawner = GetComponent<FishSpawner>();
    }

    private void Start()
    {
        StartCoroutine(ConnectLoop());
    }

    private IEnumerator ConnectLoop()
    {
        while (!_quitting)
        {
            yield return StartCoroutine(Connect());
            if (!_quitting)
            {
                Debug.Log($"[FishReceiver] Reconnecting in {reconnectDelay}s…");
                yield return new WaitForSeconds(reconnectDelay);
            }
        }
    }

    private IEnumerator Connect()
    {
        Debug.Log($"[FishReceiver] Connecting to {serverUrl}…");
        _ws = new WebSocket(serverUrl);

        _ws.OnOpen    += () => Debug.Log("[FishReceiver] ✅ Connected to server");
        _ws.OnError   += (e) => Debug.LogWarning($"[FishReceiver] ⚠️ Error: {e}");
        _ws.OnClose   += (e) => Debug.Log($"[FishReceiver] Connection closed: {e}");

        _ws.OnMessage += (bytes) =>
        {
            try
            {
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                var msg     = JsonUtility.FromJson<ServerMessage>(json);
                if (msg.type == "fish" && !string.IsNullOrEmpty(msg.imageData))
                {
                    lock (_lock)
                    {
                        _pendingImages.Enqueue(msg.imageData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FishReceiver] Parse error: {ex.Message}");
            }
        };

        yield return _ws.Connect(); // NativeWebSocket coroutine – waits until closed
    }

    private void Update()
    {
        // Must be called every frame for NativeWebSocket to dispatch messages
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif

        // Drain the queue on the main thread
        lock (_lock)
        {
            while (_pendingImages.Count > 0)
            {
                string imageData = _pendingImages.Dequeue();
                _spawner.SpawnFish(imageData);
            }
        }
    }

    private void OnApplicationQuit()
    {
        _quitting = true;
        _ws?.Close();
    }

    [Serializable]
    private class ServerMessage
    {
        public string type;
        public string imageData;
    }
}
