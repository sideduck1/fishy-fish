using UnityEngine;
using UnityEngine.UI;

public class FishSpawner : MonoBehaviour
{
    public DrawingCanvas drawingCanvas;
    public GameObject fishPrefab;       // prefab met RawImage + RectTransform
    public RectTransform spawnParent;   // AquariumPanel

    public void SpawnFish()
    {
        Texture2D tex = drawingCanvas.GetTexture();

        GameObject fish = Instantiate(fishPrefab, spawnParent);

        // zet texture op de RawImage
        RawImage ri = fish.GetComponent<RawImage>();
        ri.texture = tex;
        ri.color = Color.white;

        // RectTransform instellen
        RectTransform rt = fish.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero; // startpositie wordt random in FishMovement
        rt.sizeDelta = new Vector2(100, 100); // pas aan naar wens

        // movement toevoegen
        fish.AddComponent<FishMovementUI>();

        // movement toevoegen
        fish.AddComponent<FishMovementUI>();

        // --- NIEUW: reset de teken canvas zodat speler een nieuwe vis kan maken ---
        drawingCanvas.ResetCanvas();
    }
}