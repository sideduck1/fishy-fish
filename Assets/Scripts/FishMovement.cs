using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float minSpeed = 80f;
    public float maxSpeed = 120f;
    public float verticalAmplitude = 0.2f;
    public float randomTurnIntervalMin = 2f;
    public float randomTurnIntervalMax = 5f;

    private float speed;
    private Vector2 direction;
    private RectTransform rect;
    private RectTransform parentRect;

    private float nextRandomTurnTime;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        parentRect = rect.parent.GetComponent<RectTransform>();

        // startpositie random binnen panel
        rect.anchoredPosition = new Vector2(
            Random.Range(-parentRect.rect.width / 2, parentRect.rect.width / 2),
            Random.Range(-parentRect.rect.height / 2, parentRect.rect.height / 2)
        );

        // snelheid random
        speed = Random.Range(minSpeed, maxSpeed);

        // dominante horizontale richting
        float horizontal = Random.Range(0.5f, 1f);
        if (Random.value < 0.5f) horizontal *= -1;
        float vertical = Random.Range(-verticalAmplitude, verticalAmplitude);
        direction = new Vector2(horizontal, vertical).normalized;

        // start flip (omgekeerd)
        rect.localScale = direction.x < 0 ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);

        ScheduleNextRandomTurn();
    }

    void Update()
    {
        rect.anchoredPosition += direction * speed * Time.deltaTime;

        Vector2 halfSize = rect.sizeDelta / 2f;
        float halfWidth = parentRect.rect.width / 2 - halfSize.x;
        float halfHeight = parentRect.rect.height / 2 - halfSize.y;

        Vector2 pos = rect.anchoredPosition;

        // horizontale grenzen
        if (pos.x > halfWidth) { pos.x = halfWidth; direction.x *= -1; }
        else if (pos.x < -halfWidth) { pos.x = -halfWidth; direction.x *= -1; }

        // verticale grenzen
        if (pos.y > halfHeight) { pos.y = halfHeight; direction.y *= -1; }
        else if (pos.y < -halfHeight) { pos.y = -halfHeight; direction.y *= -1; }

        rect.anchoredPosition = pos;

        // random turns
        if (Time.time >= nextRandomTurnTime)
        {
            RandomTurn();
            ScheduleNextRandomTurn();
        }

        // flip sprite horizontaal (omgekeerd)
        if (Mathf.Abs(direction.x) > 0.01f)
            rect.localScale = direction.x < 0 ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
    }

    void RandomTurn()
    {
        float horizontal = direction.x + Random.Range(-0.5f, 0.5f);
        float vertical = direction.y + Random.Range(-0.2f, 0.2f);
        direction = new Vector2(horizontal, vertical).normalized;
    }

    void ScheduleNextRandomTurn()
    {
        nextRandomTurnTime = Time.time + Random.Range(randomTurnIntervalMin, randomTurnIntervalMax);
    }
}