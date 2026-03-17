using UnityEngine;
using System.Collections.Generic;

public class FishLimitManager : MonoBehaviour
{
    [Header("Fish Settings")]
    public int maxFish = 20;

    private Queue<GameObject> spawnedFish = new Queue<GameObject>();

    public void RegisterFish(GameObject newFish)
    {
        // Voeg nieuwe vis toe
        spawnedFish.Enqueue(newFish);

        // Check limiet
        if (spawnedFish.Count > maxFish)
        {
            GameObject oldestFish = spawnedFish.Dequeue();

            if (oldestFish != null)
            {
                Destroy(oldestFish);
            }
        }
    }
}
