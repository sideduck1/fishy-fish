using UnityEngine;
using UnityEngine.UI;

public class VoicePatternPainter : MonoBehaviour
{
    public RawImage targetImage;

    Texture2D baseTexture;
    Texture2D drawTexture;
    bool[,] drawnPixels;

    [Header("Vis parameters")]
    public float maxLineWidth = 50f;
    public float maxVerticalOffset = 20f;
    public float waveAmplitude = 10f;
    public float waveFrequency = 0.05f;
    public float smoothSpeed = 5f;

    [Header("Variation")]
    public float verticalNoiseStrength = 30f;
    public float colorShiftSpeed = 0.1f;

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
        if (VoiceInput.loudness <= 0f) return;

        int lineWidth = Mathf.Max(1, (int)(VoiceInput.loudness * maxLineWidth));

        // 🎯 BASIS positie van pitch
        float baseY = drawTexture.height / 2 +
            (VoiceInput.pitch / 256f - 0.5f) * 2f * maxVerticalOffset;

        // 🔥 EXTRA VARIATIE zodat lagen niet stapelen
        float noiseOffset = (Mathf.PerlinNoise(Time.time * 0.3f, 0f) - 0.5f) * verticalNoiseStrength;
        float drift = Mathf.Sin(Time.time * 0.5f) * 10f;

        float targetY = baseY + noiseOffset + drift;

        // Smooth movement
        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * smoothSpeed);

        DrawOrganicLine(currentY, lineWidth, VoiceInput.loudness, VoiceInput.pitch);
    }

    void DrawOrganicLine(float yCenter, int lineWidth, float loudness, float pitch)
    {
        int width = drawTexture.width;
        int height = drawTexture.height;

        // 🎨 KLEUR met variatie (heel belangrijk!)
        float hue = Mathf.Repeat((pitch / 256f) + Time.time * colorShiftSpeed, 1f);
        float saturation = Mathf.Clamp01(0.7f + loudness * 0.5f);
        float value = Mathf.Clamp01(0.8f + loudness * 0.5f);

        Color lineColor = Color.HSVToRGB(hue, saturation, value);

        for (int x = 0; x < width; x++)
        {
            float wave = Mathf.Sin(x * waveFrequency + Time.time * 2f) * waveAmplitude * loudness;
            wave += (Mathf.PerlinNoise(x * 0.1f, Time.time) - 0.5f) * waveAmplitude * 0.5f;

            int yBase = Mathf.Clamp((int)(yCenter + wave), 0, height - 1);

            for (int y = Mathf.Clamp(yBase - lineWidth / 2, 0, height - 1);
                 y <= Mathf.Clamp(yBase + lineWidth / 2, 0, height - 1); y++)
            {
                if (baseTexture.GetPixel(x, y).a > 0.1f)
                {
                    Color old = drawTexture.GetPixel(x, y);

                    // 🔥 Belangrijk: blijven layeren maar niet alles overschrijven
                    drawTexture.SetPixel(x, y, Color.Lerp(old, lineColor, 0.25f));

                    drawnPixels[x, y] = true;
                }
            }
        }

        drawTexture.Apply();
    }

    public bool HasSufficientDrawing(int minPixels = 50)
    {
        int count = 0;
        int width = drawTexture.width;
        int height = drawTexture.height;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (drawnPixels[x, y])
                {
                    count++;
                    if (count >= minPixels) return true;
                }

        return false;
    }

    public void ResetCanvas()
    {
        drawTexture = Instantiate(baseTexture);
        targetImage.texture = drawTexture;
        drawnPixels = new bool[drawTexture.width, drawTexture.height];
    }

    public Texture2D GetTexture()
    {
        return drawTexture;
    }
}