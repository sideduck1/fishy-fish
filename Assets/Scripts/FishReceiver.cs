/*
 * FishReceiver.cs
 * ─────────────────────────────────────────────────────────────────────────────
 * Connects to the Node.js server via WebSocket and relays incoming fish images
 * (+ daily challenge data) to FishSpawner on the Unity main thread.
 *
 * On Start() it also fetches today's challenge via HTTP so it shows immediately
 * without waiting for a fish to be submitted.
 *
 * SETUP:
 *  1. Install NativeWebSocket:
 *     Window → Package Manager → + → "Add package from git URL…"
 *     → https://github.com/endel/NativeWebSocket.git#upm
 *  2. Drop this script + FishSpawner + FishSwimmer into Assets/Scripts/
 *  3. Add a GameObject "FishManager" to the scene, attach FishReceiver & FishSpawner
 *  4. Set "Server Url" to ws://<your-PC-IP>:3000  (e.g. ws://192.168.1.10:3000)
 *  5. Press Play — the challenge appears immediately.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using NativeWebSocket;

[RequireComponent(typeof(FishSpawner))]
public class FishReceiver : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("WebSocket URL of the Node.js server, e.g. ws://192.168.1.10:3000")]
    public string serverUrl = "wss://fishy-qr-production.up.railway.app";

    [Tooltip("Seconds between reconnect attempts")]
    public float reconnectDelay = 3f;

    // ── Thread-safe queues ────────────────────────────────
    private readonly Queue<FishPayload> _pendingFish = new Queue<FishPayload>();
    private readonly Queue<ChallengePayload> _pendingChallenges = new Queue<ChallengePayload>();
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
        // Fetch today's challenge immediately via HTTP (don't wait for a fish)
        StartCoroutine(FetchChallengeOnStart());
        // Then open the WebSocket connection (also pushes challenge on connect)
        StartCoroutine(ConnectLoop());
    }

    // ── HTTP fetch on startup ─────────────────────────────
    private IEnumerator FetchChallengeOnStart()
    {
        // Derive HTTP URL from the ws:// serverUrl
        string httpUrl = serverUrl
            .Replace("wss://", "https://")
            .Replace("ws://", "http://");

        using (var req = UnityWebRequest.Get(httpUrl + "/challenge"))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var msg = JsonUtility.FromJson<ServerMessage>(req.downloadHandler.text);
                    if (!string.IsNullOrEmpty(msg.challengeTitle))
                        _spawner.ShowChallenge(msg.challengeTitle, msg.challengeEmoji, msg.challengeDescription);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FishReceiver] Challenge parse error: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[FishReceiver] Could not fetch challenge: {req.error}");
            }
        }
    }

    // ── WebSocket reconnect loop ──────────────────────────
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

        _ws.OnOpen += () => Debug.Log("[FishReceiver] ✅ Connected to server");
        _ws.OnError += (e) => Debug.LogWarning($"[FishReceiver] ⚠️ Error: {e}");
        _ws.OnClose += (e) => Debug.Log($"[FishReceiver] Connection closed: {e}");

        _ws.OnMessage += (bytes) =>
        {
            try
            {
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                var msg = JsonUtility.FromJson<ServerMessage>(json);

                lock (_lock)
                {
                    if (msg.type == "fish" && !string.IsNullOrEmpty(msg.imageData))
                    {
                        _pendingFish.Enqueue(new FishPayload
                        {
                            imageData = msg.imageData,
                            creatureType = msg.creatureType,
                            challengeTitle = msg.challengeTitle,
                            challengeEmoji = msg.challengeEmoji,
                            challengeDescription = msg.challengeDescription,
                        });
                    }
                    else if (msg.type == "challenge" && !string.IsNullOrEmpty(msg.challengeTitle))
                    {
                        _pendingChallenges.Enqueue(new ChallengePayload
                        {
                            title = msg.challengeTitle,
                            emoji = msg.challengeEmoji,
                            description = msg.challengeDescription,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FishReceiver] Parse error: {ex.Message}");
            }
        };

        yield return _ws.Connect();
    }

    // ── Main thread drain ─────────────────────────────────
    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
#endif
        lock (_lock)
        {
            // Show challenge updates (e.g. pushed on WS connect)
            while (_pendingChallenges.Count > 0)
            {
                var c = _pendingChallenges.Dequeue();
                _spawner.ShowChallenge(c.title, c.emoji, c.description);
            }

            // Spawn incoming fish + refresh challenge display
            while (_pendingFish.Count > 0)
            {
                var payload = _pendingFish.Dequeue();
                _spawner.SpawnFish(payload.imageData);
                if (!string.IsNullOrEmpty(payload.challengeTitle))
                    _spawner.ShowChallenge(payload.challengeTitle, payload.challengeEmoji, payload.challengeDescription);
            }
        }
    }

    private void OnApplicationQuit()
    {
        _quitting = true;
        _ws?.Close();
    }

    // ── Data types ────────────────────────────────────────

    [Serializable]
    private class ServerMessage
    {
        public string type;
        public string imageData;
        public string creatureType;
        public string challengeTitle;
        public string challengeEmoji;
        public string challengeDescription;
    }

    private struct FishPayload
    {
        public string imageData;
        public string creatureType;
        public string challengeTitle;
        public string challengeEmoji;
        public string challengeDescription;
    }

    private struct ChallengePayload
    {
        public string title;
        public string emoji;
        public string description;
    }
}
