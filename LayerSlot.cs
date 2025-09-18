using System.Collections.Generic;
using UnityEngine;

public class LayerSlot : MonoBehaviour
{
    [Tooltip("Nama/ID unik layer (opsional, untuk debugging atau lookup).")]
    public string layerId;

    [HideInInspector] public int baseOrder;     // Diisi oleh LayerStackManager
    [HideInInspector] public int sortingLayerID; // Diisi oleh LayerStackManager

    readonly List<Sortable> members = new();

    public void Register(Sortable s)
    {
        if (!members.Contains(s))
            members.Add(s);
    }

    public void Unregister(Sortable s)
    {
        members.Remove(s);
    }

    /// Dipanggil saat urutan layer berubah atau saat spawn.
    public void ReapplyOrders()
    {
        foreach (var m in members)
            m.ApplyOrder(sortingLayerID, baseOrder + m.localOffset);
    }
}
