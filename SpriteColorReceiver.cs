using UnityEngine;

[DisallowMultipleComponent]
public class SpriteColorReceiver : MonoBehaviour, IColorReceiver
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private string colorId;

    void Reset() { if (!target) target = GetComponent<SpriteRenderer>(); }

    public Color GetColor() => target ? target.color : Color.white;
    public void SetColor(Color c) { if (target) target.color = c; }

    public string GetColorId() => colorId;
    public void SetColorId(string id) { colorId = id; }
}
