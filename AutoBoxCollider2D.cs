using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class AutoBoxCollider2D : MonoBehaviour
{
    private SpriteRenderer sr;
    private BoxCollider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
        UpdateCollider();
    }

    void Update()
    {
        // Kalau sprite bisa berubah runtime
        UpdateCollider();
    }

    void UpdateCollider()
    {
        if (sr.sprite == null) return;
        col.size = sr.sprite.bounds.size;
        col.offset = sr.sprite.bounds.center;
    }
}
