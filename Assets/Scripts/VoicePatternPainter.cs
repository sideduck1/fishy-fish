using UnityEngine;
using UnityEngine.UI;

public class VoicePatternPainter : MonoBehaviour
{
    public RawImage targetImage;

    Texture2D baseTexture;
    Texture2D drawTexture;
    bool[,] drawnPixels;

    [Header("Vis parameters")]
    public float maxLineWidth = 50f;       // maximale breedte van lijnen
    public float maxVerticalOffset = 20f;  // verticale verschuiving door pitch
    public float waveAmplitude = 10f;      // golvende patronen
    public float waveFrequency = 0.05f;    // golfsnelheid
    public float smoothSpeed = 5f;         // hoe vloeiend lijnen volgen

    private float currentY;

    void Start()
    {
        Texture2D original = targetImage.texture as Texture2D;

        // Maak nieuwe veilige texture
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

        // Map loudness naar lijnbreedte
        int lineWidth = Mathf.Max(1, (int)(VoiceInput.loudness * maxLineWidth));

        // Map pitch naar verticale positie
        float targetY = drawTexture.height / 2 + (VoiceInput.pitch / 256f - 0.5f) * 2f * maxVerticalOffset;

        // Lerp voor vloeiende verticale beweging
        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * smoothSpeed);

        // Bereken golven op basis van perlin noise + loudness
        DrawOrganicLine(currentY, lineWidth, VoiceInput.loudness, VoiceInput.pitch);
    }

    void DrawOrganicLine(float yCenter, int lineWidth, float loudness, float pitch)
    {
        int width = drawTexture.width;
        int height = drawTexture.height;

        // Kleur: pitch naar hue, zacht verloop voor mooi effect
        float hue = Mathf.Clamp01(pitch / 256f);
        Color lineColor = Color.HSVToRGB(hue, 1f, 1f);

        for (int x = 0; x < width; x++)
        {
            // Golvende verschuiving: combinatie van sinus en perlin noise
            float wave = Mathf.Sin(x * waveFrequency + Time.time * 2f) * waveAmplitude * loudness;
            wave += (Mathf.PerlinNoise(x * 0.1f, Time.time) - 0.5f) * waveAmplitude * 0.5f;

            int yBase = Mathf.Clamp((int)(yCenter + wave), 0, height - 1);

            for (int y = Mathf.Clamp(yBase - lineWidth / 2, 0, height - 1);
                 y <= Mathf.Clamp(yBase + lineWidth / 2, 0, height - 1); y++)
            {
                if (baseTexture.GetPixel(x, y).a > 0.1f)
                {
                    Color old = drawTexture.GetPixel(x, y);
                    drawTexture.SetPixel(x, y, Color.Lerp(old, lineColor, 0.5f));
                    drawnPixels[x, y] = true;
                }
            }
        }

        drawTexture.Apply();
    }

    // Check of er genoeg pixels getekend zijn door stem
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
