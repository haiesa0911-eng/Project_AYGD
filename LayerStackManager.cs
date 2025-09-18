using System.Text;
using UnityEngine;

/// Pasang di GameObject mana saja (mis. RightPanel / R_Panel1).
/// Drag `Content_Layer` (yang berisi Layer 1, Layer 2, ...) ke field `contentLayer`.
public class LayerStackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentLayer;     // -> Content_Layer (parent dari Layer 1/2/3/...)
    [SerializeField] private string sortingLayerName = "Default";

    [Header("Ordering")]
    [Tooltip("Order awal semua layer (mulai di atas background). Contoh: jika background order=0, set ini ke 100.")]
    [SerializeField] private int baseStartOrder = 100;

    [Tooltip("Jarak order antar-layer. Besar agar aman untuk banyak objek dalam 1 layer.")]
    [SerializeField] private int orderStep = 1000;

    [Tooltip("Jika true: child paling atas di UI = paling depan (order terbesar).")]
    [SerializeField] private bool topIsFront = true;

    private int sortingLayerID;
    private string lastSignature;

    void Awake()
    {
        sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        Recalculate();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Biar update saat nilai di Inspector berubah (Editor only)
        sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        if (contentLayer)
            Recalculate();
    }
#endif

    void LateUpdate()
    {
        // Deteksi perubahan urutan anak (mis. drag & drop di runtime)
        if (!contentLayer) return;
        var sig = BuildSignature();
        if (sig != lastSignature)
        {
            lastSignature = sig;
            Recalculate();
        }
    }

    /// Hitung baseOrder untuk setiap LayerSlot sesuai urutan anak pada Content_Layer.
    public void Recalculate()
    {
        if (!contentLayer) return;

        int n = contentLayer.childCount;
        for (int i = 0; i < n; i++)
        {
            var t = contentLayer.GetChild(i);
            var slot = t.GetComponent<LayerSlot>();
            if (!slot) continue;

            // i = index atas-ke-bawah sesuai sibling order
            int rank = topIsFront ? (n - 1 - i) : i;

            // baseStartOrder memastikan semua layer berada di atas background
            slot.baseOrder = baseStartOrder + (rank * orderStep);
            slot.sortingLayerID = sortingLayerID;

            // Terapkan ke semua anggota layer
            slot.ReapplyOrders();
        }

        lastSignature = BuildSignature();
    }

    /// Paksa hitung ulang dari script lain
    public void ForceRecalculate() => Recalculate();

    /// Helper: dapatkan LayerSlot dari transform child (mis. yang diset di Spawner)
    public LayerSlot GetSlotByTransform(Transform slotTransform)
    {
        return slotTransform ? slotTransform.GetComponent<LayerSlot>() : null;
    }

    /// (Opsional) Helper: cari LayerSlot berdasarkan LayerId (jika Anda isi manual)
    public LayerSlot GetSlotById(string layerId)
    {
        if (!contentLayer || string.IsNullOrEmpty(layerId)) return null;
        for (int i = 0; i < contentLayer.childCount; i++)
        {
            var slot = contentLayer.GetChild(i).GetComponent<LayerSlot>();
            if (slot && slot.layerId == layerId)
                return slot;
        }
        return null;
    }

    private string BuildSignature()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < contentLayer.childCount; i++)
            sb.Append(contentLayer.GetChild(i).name).Append('|');
        return sb.ToString();
    }
}
