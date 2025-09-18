using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PieceVariant:
/// - Menyimpan daftar varian (sprite + footprint grid).
/// - Bisa diganti via index atau ID.
/// - Otomatis update PieceSnapper & SpriteRenderer.
/// - Coba resnap agar pusat grid tetap sama bila sedang snapped.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PieceSnapper))]
public class PieceVariant : MonoBehaviour
{
    [Serializable]
    public class VariantDef
    {
        [Header("Visual")]
        public string id = "default";
        public Sprite sprite;
        public Material overrideMaterial;

        [Header("Footprint (grid)")]
        [Min(1)] public int sizeRows = 1;
        [Min(1)] public int sizeCols = 1;
        public PieceSnapper.PivotAnchor pivot = PieceSnapper.PivotAnchor.Center;
        public Vector2 footprintOffsetCells = Vector2.zero;
    }

    [Header("Target Renderer")]
    public SpriteRenderer spriteRenderer;   // drag di inspector

    [Header("Variants")]
    public List<VariantDef> variants = new List<VariantDef>();

    [SerializeField] private int currentIndex = 0;

    private PieceSnapper snapper;

    public int CurrentIndex => currentIndex;
    public string CurrentId =>
        (currentIndex >= 0 && currentIndex < variants.Count) ? variants[currentIndex].id : null;

    void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        snapper = GetComponent<PieceSnapper>();

        if (variants.Count > 0)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, variants.Count - 1);
            ApplyVariant(currentIndex, tryKeepSnap: false);
        }
    }

    /// <summary>Set varian berdasarkan index dalam list.</summary>
    public void SetVariant(int index, bool tryKeepSnap = true)
    {
        if (variants.Count == 0) return;
        index = Mathf.Clamp(index, 0, variants.Count - 1);
        ApplyVariant(index, tryKeepSnap);
    }

    /// <summary>Set varian berdasarkan ID. Return false jika tidak ditemukan.</summary>
    public bool TrySetVariantById(string id, bool tryKeepSnap = true)
    {
        if (string.IsNullOrEmpty(id)) return false;
        int idx = variants.FindIndex(v => v.id == id);
        if (idx < 0) return false; // ID tidak ada → no-op
        ApplyVariant(idx, tryKeepSnap);
        return true;
    }

    /// <summary>Cek apakah ID tertentu tersedia di list varian.</summary>
    public bool HasVariantId(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return variants.Exists(v => v.id == id);
    }

    private void ApplyVariant(int index, bool tryKeepSnap)
    {
        if (index < 0 || index >= variants.Count) return;
        var v = variants[index];

        // 1) Update visual
        if (spriteRenderer)
        {
            spriteRenderer.sprite = v.sprite;
            if (v.overrideMaterial) spriteRenderer.material = v.overrideMaterial;
        }

        // 2) Update footprint ke PieceSnapper
        snapper.sizeRows = Mathf.Max(1, v.sizeRows);
        snapper.sizeCols = Mathf.Max(1, v.sizeCols);
        snapper.pivot = v.pivot;
        snapper.footprintOffsetCells = v.footprintOffsetCells;

        // 3) Coba pertahankan posisi snap (center)
        if (tryKeepSnap && snapper.IsSnapped &&
            snapper.TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1))
        {
            float centerR = (r0 + r1) * 0.5f;
            float centerC = (c0 + c1) * 0.5f;

            int newR0 = Mathf.RoundToInt(centerR - (v.sizeRows - 1) * 0.5f);
            int newC0 = Mathf.RoundToInt(centerC - (v.sizeCols - 1) * 0.5f);
            int newR1 = newR0 + v.sizeRows - 1;
            int newC1 = newC0 + v.sizeCols - 1;

            snapper.TrySnapToRect(newR0, newC0, newR1, newC1, animate: true);
        }

        currentIndex = index;
    }

    // Validator agar ID tidak kosong/duplikat
    void OnValidate()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < variants.Count; i++)
        {
            string id = variants[i]?.id ?? "";
            if (string.IsNullOrWhiteSpace(id))
                Debug.LogWarning($"[PieceVariant] Varian #{i} belum punya Id (disarankan diisi).", this);
            else if (!seen.Add(id))
                Debug.LogWarning($"[PieceVariant] Duplikat Id '{id}' pada varian #{i}. Id harus unik.", this);
        }
    }
}
