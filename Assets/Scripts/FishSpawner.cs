/*
 * FishSpawner.cs
 * ─────────────────────────────────────────────────────────────────────────────
 * Decodes a base64 PNG into a Texture2D, applies it to a fish prefab, and
 * instantiates the fish in the scene. Also shows the daily challenge in UI.
 *
 * SETUP:
 *  1. Create a Sprite/Quad prefab for the fish and assign it to "Fish Prefab".
 *     The prefab should have a SpriteRenderer (or MeshRenderer + material with
 *     _MainTex slot) and a FishSwimmer component.
 *  2. Attach this script to the same GameObject as FishReceiver.
 *  3. (Optional) For challenge display:
 *     - Create a Canvas → Panel → assign to "Challenge Panel"
 *     - Add two UI Text objects inside the panel:
 *       • "challengeEmojiText"  (large font, shows emoji)
 *       • "challengeTitleText"  (shows "🌊 Oceaanvis")
 *       • "challengeDescText"   (smaller, shows description)
 *     - Assign them in the Inspector under "Challenge UI"
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FishSpawner : MonoBehaviour
{
    // ── Fish Prefab ───────────────────────────────────────
    [Header("Fish Prefab")]
    [Tooltip("Prefab with SpriteRenderer + FishSwimmer component")]
    public GameObject fishPrefab;

    // ── Spawn Settings ────────────────────────────────────
    [Header("Spawn Settings")]
    [Tooltip("Max number of fish swimming at once (oldest removed when exceeded)")]
    public int maxFish = 10;

    [Tooltip("Z depth for spawned fish")]
    public float spawnZ = 0f;

    // ── Challenge UI ──────────────────────────────────────
    [Header("Challenge UI  (optional — assign in Inspector)")]
    [Tooltip("Panel/Canvas to show/hide when a fish arrives")]
    public GameObject challengePanel;

    [Tooltip("Displays the challenge emoji + title, e.g. '🌊 Oceaanvis'")]
    public TMP_Text challengeTitleText;

    [Tooltip("Displays the short challenge description")]
    public TMP_Text challengeDescText;

    [Tooltip("Seconds to display the challenge banner before hiding it (0 = stay forever)")]
    public float challengeDisplaySeconds = 8f;

    // ── Internal ──────────────────────────────────────────
    private readonly Queue<GameObject> _fishPool = new Queue<GameObject>();
    private Coroutine _hideChallengeCoroutine;

    // ─────────────────────────────────────────────────────
    /// <summary>Called by FishReceiver on the main thread with a base64 PNG string.</summary>
    public void SpawnFish(string base64Image)
    {
        // Strip the data URL header if present ("data:image/png;base64,…")
        string b64 = base64Image;
        int commaIdx = base64Image.IndexOf(',');
        if (commaIdx >= 0) b64 = base64Image.Substring(commaIdx + 1);

        byte[] bytes;
        try { bytes = Convert.FromBase64String(b64); }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FishSpawner] Base64 decode failed: {ex.Message}");
            return;
        }

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        if (!tex.LoadImage(bytes))
        {
            Debug.LogWarning("[FishSpawner] Failed to load PNG into texture.");
            Destroy(tex);
            return;
        }
        tex.Apply();

        // Remove oldest fish if at limit
        if (_fishPool.Count >= maxFish)
        {
            GameObject old = _fishPool.Dequeue();
            if (old != null) Destroy(old);
        }

        // Spawn at a random vertical position just off the left (or right) edge
        Camera cam = Camera.main;
        float halfH = cam != null ? cam.orthographicSize : 4f;
        float halfW = cam != null ? cam.orthographicSize * cam.aspect : 7f;

        bool fromLeft = UnityEngine.Random.value > 0.5f;
        float spawnX = fromLeft ? -halfW - 1f : halfW + 1f;
        float spawnY = UnityEngine.Random.Range(-halfH * 0.6f, halfH * 0.6f);

        Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);
        GameObject fish = Instantiate(fishPrefab, spawnPos, Quaternion.identity);

        // Apply texture
        SpriteRenderer sr = fish.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Sprite sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f);
            sr.sprite = sprite;
        }
        else
        {
            // Fallback: set _MainTex on the renderer's material
            Renderer r = fish.GetComponent<Renderer>();
            if (r != null) r.material.mainTexture = tex;
        }

        // Tell the swimmer which direction it starts in
        FishSwimmer swimmer = fish.GetComponent<FishSwimmer>();
        if (swimmer != null) swimmer.Initialize(fromLeft);

        _fishPool.Enqueue(fish);
        Debug.Log($"[FishSpawner] 🐟 Fish spawned! Total: {_fishPool.Count}");
    }

    // ─────────────────────────────────────────────────────
    /// <summary>
    /// Called by FishReceiver to display today's daily challenge in the Unity UI.
    /// Assign Text components and an optional panel in the Inspector.
    /// </summary>
    public void ShowChallenge(string title, string emoji, string description)
    {
        if (string.IsNullOrEmpty(title)) return;

        // Show the panel
        if (challengePanel != null)
            challengePanel.SetActive(true);

        // Update text fields
        if (challengeTitleText != null)
            challengeTitleText.text = $"{emoji}  {title}";

        if (challengeDescText != null)
            challengeDescText.text = description;

        Debug.Log($"[FishSpawner] 🎯 Challenge: {emoji} {title} — {description}");

        // Auto-hide after N seconds
        if (challengeDisplaySeconds > 0f)
        {
            if (_hideChallengeCoroutine != null)
                StopCoroutine(_hideChallengeCoroutine);
            _hideChallengeCoroutine = StartCoroutine(HideChallengeAfter(challengeDisplaySeconds));
        }
    }

    private System.Collections.IEnumerator HideChallengeAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (challengePanel != null)
            challengePanel.SetActive(false);
    }
}
