using UnityEngine;
using UnityEngine.UI;

public class DrawingCanvas : MonoBehaviour
{
    [Header("UI")]
    public RawImage targetImage;     // RawImage van de vis
    public Color currentColor = Color.red;
    public int brushSize = 5;

    private Texture2D baseTexture;   // origineel
    private Texture2D drawTexture;   // runtime kopie
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = targetImage.GetComponent<RectTransform>();

        // maak base texture of kopie van bestaande
        if (targetImage.texture == null)
        {
            baseTexture = new Texture2D(Mathf.RoundToInt(rectTransform.rect.width), Mathf.RoundToInt(rectTransform.rect.height), TextureFormat.ARGB32, false);
            Color clear = new Color(0, 0, 0, 0);
            for (int x = 0; x < baseTexture.width; x++)
                for (int y = 0; y < baseTexture.height; y++)
                    baseTexture.SetPixel(x, y, clear);
            baseTexture.Apply();
        }
        else
        {
            baseTexture = Instantiate(targetImage.texture as Texture2D);
        }

        // maak runtime kopie
        drawTexture = Instantiate(baseTexture);
        targetImage.texture = drawTexture;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            DrawAtMouse();
        }
    }

    private void DrawAtMouse()
    {
        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out localPos))
            return;

        float x = (localPos.x + rectTransform.rect.width / 2f) / rectTransform.rect.width;
        float y = (localPos.y + rectTransform.rect.height / 2f) / rectTransform.rect.height;

        int px = Mathf.RoundToInt(x * drawTexture.width);
        int py = Mathf.RoundToInt(y * drawTexture.height);

        for (int i = -brushSize; i <= brushSize; i++)
            for (int j = -brushSize; j <= brushSize; j++)
            {
                int nx = px + i;
                int ny = py + j;

                if (nx < 0 || nx >= drawTexture.width || ny < 0 || ny >= drawTexture.height)
                    continue;

                Color baseColor = baseTexture.GetPixel(nx, ny); // check alpha
                if (baseColor.a > 0.1f)
                    drawTexture.SetPixel(nx, ny, currentColor);
            }

        drawTexture.Apply();
    }

    // geef de runtime getekende texture door
    public Texture2D GetTexture()
    {
        return drawTexture;
    }

    public void ResetCanvas()
    {
        // maak nieuwe drawTexture gebaseerd op baseTexture
        drawTexture = Instantiate(baseTexture);
        targetImage.texture = drawTexture;
    }
}