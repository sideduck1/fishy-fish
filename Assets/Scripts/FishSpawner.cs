/*
 * FishSpawner.cs
 * ─────────────────────────────────────────────────────────────────────────────
 * Decodes a base64 PNG into a Texture2D, applies it to a fish prefab, and
 * instantiates the fish in the scene.
 *
 * SETUP:
 *  1. Create a Sprite/Quad prefab for the fish and assign it to "Fish Prefab".
 *     The prefab should have a SpriteRenderer (or MeshRenderer + material with
 *     _MainTex slot) and a FishSwimmer component.
 *  2. Attach this script to the same GameObject as FishReceiver.
 */

using System;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    [Header("Fish Prefab")]
    [Tooltip("Prefab with SpriteRenderer + FishSwimmer component")]
    public GameObject fishPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Max number of fish swimming at once (oldest removed when exceeded)")]
    public int maxFish = 10;

    [Tooltip("Z depth for spawned fish")]
    public float spawnZ = 0f;

    private readonly System.Collections.Generic.Queue<GameObject> _fishPool
        = new System.Collections.Generic.Queue<GameObject>();

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
        float spawnX  = fromLeft ? -halfW - 1f : halfW + 1f;
        float spawnY  = UnityEngine.Random.Range(-halfH * 0.6f, halfH * 0.6f);

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
}
