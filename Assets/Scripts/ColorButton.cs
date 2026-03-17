using UnityEngine;

public class ColorButton : MonoBehaviour
{
    public DrawingCanvas canvas;
    public Color color;

    public void SelectColor()
    {
        canvas.currentColor = color;
    }
}