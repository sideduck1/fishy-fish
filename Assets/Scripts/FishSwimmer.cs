/*
 * FishSwimmer.cs
 * ─────────────────────────────────────────────────────────────────────────────
 * Attached to each spawned fish prefab. Drives the swimming animation:
 *   • Horizontal movement across the screen
 *   • Sine-wave vertical bobbing
 *   • Flips sprite when changing direction at screen edges
 *   • Destroys itself after swimming off screen (if looping is disabled)
 *
 * SETUP:
 *  Add this script to your fish prefab together with a SpriteRenderer.
 */

using UnityEngine;

public class FishSwimmer : MonoBehaviour
{
    [Header("Movement")]
    [Range(0.5f, 5f)] public float speed      = 1.5f;
    [Range(0f,   2f)] public float bobHeight  = 0.3f;
    [Range(0.5f, 4f)] public float bobSpeed   = 1.2f;

    [Header("Behaviour")]
    [Tooltip("Fish loops back and forth instead of swimming off screen")]
    public bool looping = true;

    [Tooltip("Scale of the fish in world units")]
    public float fishScale = 1.4f;

    [Tooltip("Randomise speed slightly so each fish feels unique")]
    public bool randomiseSpeed = true;

    // ── internals ──
    private float      _dir;        // +1 = right, -1 = left
    private float      _bobOffset;
    private Vector3    _basePos;
    private Camera     _cam;
    private float      _halfW;
    private SpriteRenderer _sr;

    public void Initialize(bool movingRight)
    {
        _dir = movingRight ? 1f : -1f;
        ApplyFlip();
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        transform.localScale = new Vector3(fishScale, fishScale, 1f);
        _bobOffset = Random.Range(0f, Mathf.PI * 2f);
        if (randomiseSpeed)
            speed *= Random.Range(0.7f, 1.4f);
    }

    private void Start()
    {
        _cam   = Camera.main;
        _halfW = _cam != null ? _cam.orthographicSize * _cam.aspect : 8f;
        _basePos = transform.position;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Horizontal drift
        float newX = transform.position.x + _dir * speed * dt;

        // Vertical bob
        _basePos.x  = newX;
        float bob   = Mathf.Sin(Time.time * bobSpeed + _bobOffset) * bobHeight;
        float newY  = _basePos.y + bob;

        transform.position = new Vector3(newX, newY, transform.position.z);

        // Check screen edges
        float edge = _halfW + fishScale;
        if (looping)
        {
            if (_dir > 0 && newX > edge)
            {
                // Wrap around to left side
                _basePos.x = -edge;
                transform.position = new Vector3(-edge, newY, transform.position.z);
                // Randomise vertical position on wrap
                _basePos.y = Random.Range(
                    -(_cam != null ? _cam.orthographicSize * 0.6f : 3f),
                     (_cam != null ? _cam.orthographicSize * 0.6f : 3f));
            }
            else if (_dir < 0 && newX < -edge)
            {
                _basePos.x = edge;
                transform.position = new Vector3(edge, newY, transform.position.z);
                _basePos.y = Random.Range(
                    -(_cam != null ? _cam.orthographicSize * 0.6f : 3f),
                     (_cam != null ? _cam.orthographicSize * 0.6f : 3f));
            }
        }
        else
        {
            // Bounce off edges
            if ((newX > edge && _dir > 0) || (newX < -edge && _dir < 0))
            {
                _dir = -_dir;
                ApplyFlip();
            }
        }
    }

    private void ApplyFlip()
    {
        if (_sr != null)
            _sr.flipX = _dir < 0; // flip when swimming left
    }
}
