using UnityEngine;
using UnityEngine.UI;

public class VoicePatternPainter : MonoBehaviour
{
    public RawImage targetImage;

    Texture2D baseTexture;
    Texture2D drawTexture;
    bool[,] drawnPixels;
    int drawnPixelCount = 0;

    [Header("Vis parameters")]
    public float maxLineWidth = 50f;
    public float maxVerticalOffset = 30f;
    public float waveAmplitude = 10f;
    public float waveFrequency = 0.05f;
    public float smoothSpeed = 5f;

    [Header("Pitch Mapping (Hz)")]
    public float minPitch = 80f;
    public float maxPitch = 1000f;

    [Header("Variation")]
    public float verticalNoiseStrength = 25f;
    public float driftSpeed = 0.5f;
    public float colorVariation = 0.05f;

    private float currentY;

    void Start()
    {
        Texture2D original = targetImage.texture as Texture2D;

        baseTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        baseTexture.SetPixels(original.GetPixels());
        baseTexture.Apply();

        drawTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        drawTexture.SetPixels(baseTexture.GetPixels());
        drawTexture.Apply();

        targetImage.texture = drawTexture;

        drawnPixels = new bool[drawTexture.width, drawTexture.height];
        currentY = drawTexture.height / 2;
    }

    void Update()
    {
        if (VoiceInput.loudness <= 0f || VoiceInput.pitch <= 0f) return;

        int lineWidth = Mathf.Max(1, (int)(VoiceInput.loudness * maxLineWidth));

        float normalizedPitch = Mathf.InverseLerp(minPitch, maxPitch, VoiceInput.pitch);

        float baseY = drawTexture.height / 2 +
                      (normalizedPitch - 0.5f) * 2f * maxVerticalOffset;

        float noiseOffset = (Mathf.PerlinNoise(Time.time * 0.3f, 0f) - 0.5f) * verticalNoiseStrength;
        float drift = Mathf.Sin(Time.time * driftSpeed) * 10f;

        float targetY = baseY + noiseOffset + drift;

        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * smoothSpeed);

        DrawOrganicLine(currentY, lineWidth, normalizedPitch);
    }

    void DrawOrganicLine(float yCenter, int lineWidth, float normalizedPitch)
    {
        int width = drawTexture.width;
        int height = drawTexture.height;

        float hue = Mathf.Clamp01(normalizedPitch);
        hue += Random.Range(-colorVariation, colorVariation);
        hue = Mathf.Repeat(hue, 1f);

        Color lineColor = Color.HSVToRGB(hue, 0.8f, 1f);

        for (int x = 0; x < width; x++)
        {
            float wave = Mathf.Sin(x * waveFrequency + Time.time * 2f) * waveAmplitude;
            wave += (Mathf.PerlinNoise(x * 0.1f, Time.time) - 0.5f) * waveAmplitude;

            int yBase = Mathf.Clamp((int)(yCenter + wave), 0, height - 1);

            for (int y = Mathf.Clamp(yBase - lineWidth / 2, 0, height - 1);
                 y <= Mathf.Clamp(yBase + lineWidth / 2, 0, height - 1); y++)
            {
                if (baseTexture.GetPixel(x, y).a > 0.1f)
                {
                    Color old = drawTexture.GetPixel(x, y);
                    drawTexture.SetPixel(x, y, Color.Lerp(old, lineColor, 0.25f));

                    if (!drawnPixels[x, y])
                    {
                        drawnPixels[x, y] = true;
                        drawnPixelCount++;
                    }
                }
            }
        }

        drawTexture.Apply();
    }

    public bool HasSufficientDrawing(int minPixels = 50)
    {
        return drawnPixelCount >= minPixels;
    }

    public void ResetCanvas()
    {
        drawTexture = Instantiate(baseTexture);
        targetImage.texture = drawTexture;

        drawnPixels = new bool[drawTexture.width, drawTexture.height];
        drawnPixelCount = 0;
    }

    public Texture2D GetTexture()
    {
        return drawTexture;
    }
}