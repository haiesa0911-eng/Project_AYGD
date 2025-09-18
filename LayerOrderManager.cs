using UnityEngine;

public class LayerOrderManager : MonoBehaviour
{
    public static LayerOrderManager I;
    public RectTransform contentLayer;

    void Awake() => I = this;

    // Dipanggil dari ReorderableLayerItem setelah drag selesai
    public void NotifyOrderChanged()
    {
        int childCount = contentLayer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var binding = contentLayer.GetChild(i).GetComponent<LayerItemBinding>();
            if (binding != null)
            {
                // Kirim ke sistem dunia Anda untuk set sorting
                ApplyOrderToWorld(binding.LayerId, i);
            }
        }
    }

    void ApplyOrderToWorld(string id, int orderIndex)
    {
        // Cari world object by ID
        var worldObj = FindWorldObjectById(id);
        if (worldObj != null)
        {
            // Contoh: atur sorting order renderer
            var renderer = worldObj.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sortingOrder = orderIndex;
        }
    }

    GameObject FindWorldObjectById(string id)
    {
        // Implementasi sesuai sistem Anda
        return null;
    }
}
