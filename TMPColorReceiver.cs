using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class TMPColorReceiver : MonoBehaviour, IColorReceiver
{
    [SerializeField] private TMP_Text target;
    [SerializeField] private string colorId;

    void Reset()
    {
        if (!target) target = GetComponent<TMP_Text>();
    }

    public Color GetColor() => target ? target.color : Color.white;

    public void SetColor(Color c)
    {
        if (!target) return;
        target.color = c;

        // Pastikan material preset TMP tidak “menggelapkan” vertex color
        var mat = target.fontSharedMaterial;
        if (mat && mat.HasProperty(ShaderUtilities.ID_FaceColor))
            mat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);

        target.SetVerticesDirty();
        target.SetMaterialDirty();
    }

    public string GetColorId() => colorId;
    public void SetColorId(string id) { colorId = id; }
}
