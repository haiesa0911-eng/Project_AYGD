using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class TypeVariant : MonoBehaviour
{
    [Serializable]
    public class VariantDef
    {
        public string id = "Default";
        public TMP_FontAsset font;   // HANYA font asset
    }

    [Header("Variants")]
    public List<VariantDef> variants = new List<VariantDef>();

    [Header("Target")]
    public TMP_Text tmp;            // target TMP_Text

    [SerializeField] private int currentIndex = 0;
    public int CurrentIndex => currentIndex;
    public string CurrentId =>
        (currentIndex >= 0 && currentIndex < variants.Count) ? variants[currentIndex].id : null;

    void Awake()
    {
        if (!tmp) tmp = GetComponentInChildren<TMP_Text>(true);

        // Pada state awal: jika belum cocok dengan opsi mana pun, pakai index 0
        if (!MapFromCurrentFont())
            SetIndex(0, apply: true);
    }

    bool MapFromCurrentFont()
    {
        if (!tmp || variants == null || variants.Count == 0) return false;
        int idx = variants.FindIndex(v => v != null && v.font == tmp.font);
        if (idx < 0) return false;
        currentIndex = idx;
        return true;
    }

    public bool HasVariantId(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return variants.Exists(v => v != null && v.id == id);
    }

    public bool TrySetById(string id)
    {
        if (string.IsNullOrEmpty(id) || variants == null) return false;
        int idx = variants.FindIndex(v => v != null && v.id == id);
        if (idx < 0) return false;
        SetIndex(idx, apply: true);
        return true;
    }

    public void SetIndex(int index, bool apply)
    {
        if (variants == null || variants.Count == 0) return;
        index = Mathf.Clamp(index, 0, variants.Count - 1);
        currentIndex = index;

        if (apply && tmp && variants[index] != null && variants[index].font)
        {
            var keepColor = tmp.color;
            tmp.font = variants[index].font;
            tmp.color = keepColor;      // jaga warna
            tmp.SetVerticesDirty();
            tmp.SetMaterialDirty();
        }
    }

    // Untuk dibaca SpriteDropdown
    public string GetCurrentId() => CurrentId;
}
