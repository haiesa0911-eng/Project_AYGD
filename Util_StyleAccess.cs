using System.Reflection;
using UnityEngine;
using TMPro;

public static class Util_StyleAccess
{
    public static bool TryGetPieceVariantId(GameObject go, out int id)
        => TryGetInt(go, "PieceVariant", new[] { "id", "variantId", "currentId" }, out id);

    public static bool TryGetTypeVariantId(GameObject go, out int id)
        => TryGetInt(go, "TypeVariant", new[] { "id", "variantId", "typeId", "currentId" }, out id);

    public static bool TryGetObjectColor(GameObject go, out Color col)
    {
        col = Color.white;
        if (go == null) return false;

        if (go.TryGetComponent<TMP_Text>(out var tmp))
        { col = tmp.color; return true; }

        if (go.TryGetComponent<SpriteRenderer>(out var sr))
        { col = sr.color; return true; }

        var rend = go.GetComponent<Renderer>();
        if (rend != null && rend.material != null)
        { col = rend.material.color; return true; }

        return false;
    }

    static bool TryGetInt(GameObject go, string typeName, string[] fields, out int val)
    {
        val = 0;
        var comp = go.GetComponent(typeName);
        if (comp == null) return false;
        var t = comp.GetType();
        foreach (var n in fields)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(int)) { val = (int)f.GetValue(comp); return true; }
            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(int)) { val = (int)p.GetValue(comp); return true; }
        }
        return false;
    }

    public static bool TryGetTMP(GameObject go, out TMP_Text tmp)
    {
        tmp = go.GetComponent<TMP_Text>();
        return tmp != null;
    }

    public static bool IsBold(TMP_Text t) => (t.fontStyle & FontStyles.Bold) != 0;
    public static bool IsItalic(TMP_Text t) => (t.fontStyle & FontStyles.Italic) != 0;
}
