using UnityEngine;
using UnityEngine.UI;

public class VoiceFishSpawner : MonoBehaviour
{
    public FishLimitManager fishLimitManager;

    [Header("Painter & Prefab")]
    public VoicePatternPainter patternPainter;
    public GameObject fishPrefab;
    public RectTransform spawnParent;

    [Header("Spawn Settings")]
    public float silentTimeToSpawn = 1.0f;
    private float silentTimer = 0f;

    [Header("Voice Thresholds")]
    public float loudnessThreshold = 0.01f;

    private bool hasDrawn = false;

    void Update()
    {
        if (patternPainter == null) return;

        if (!hasDrawn && patternPainter.HasSufficientDrawing())
        {
            hasDrawn = true;
        }

        if (hasDrawn)
        {
            if (VoiceInput.loudness <= loudnessThreshold)
            {
                silentTimer += Time.deltaTime;

                if (silentTimer >= silentTimeToSpawn)
                {
                    SpawnFish();
                    silentTimer = 0f;
                    hasDrawn = false;
                }
            }
            else
            {
                silentTimer = 0f;
            }
        }
    }

    void SpawnFish()
    {
        Texture2D tex = patternPainter.GetTexture();
        if (tex == null) return;

        GameObject fish = Instantiate(fishPrefab, spawnParent);
        fishLimitManager.RegisterFish(fish);

        RawImage ri = fish.GetComponent<RawImage>() ?? fish.GetComponentInChildren<RawImage>();

        if (ri == null)
        {
            Debug.LogError("❌ Geen RawImage op FishPrefab");
            Destroy(fish);
            return;
        }

        ri.texture = tex;
        ri.color = Color.white;

        RectTransform rt = fish.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(100, 100);
        }

        if (fish.GetComponent<FishMovement>() == null)
            fish.AddComponent<FishMovement>();

        patternPainter.ResetCanvas();

        Debug.Log("🐟 Vis gespawned!");
    }
}