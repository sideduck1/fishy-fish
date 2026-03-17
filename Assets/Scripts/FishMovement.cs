using UnityEngine;

public class FishMovement : MonoBehaviour
{
    public float speed = 1f;
    private Vector2 direction;

    void Start()
    {
        direction = Random.insideUnitCircle.normalized;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        // schermranden (voorbeeld, pas aan volgens jouw aquarium bounds)
        if (transform.position.x > 8 || transform.position.x < -8)
            direction.x *= -1;

        if (transform.position.y > 4 || transform.position.y < -4)
            direction.y *= -1;

        // flip sprite
        if (direction.x < 0)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }
}
