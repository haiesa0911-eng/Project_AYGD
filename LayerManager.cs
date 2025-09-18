using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LayerManager : MonoBehaviour
{
    [Header("Root objek di tengah (yang layernya mau disinkronkan)")]
    public RectTransform designRoot;

    // Seleksi layer
    GameObject selected;

    public void Select(GameObject item)
    {
        selected = item;
        Debug.Log("Selected: " + item.name);
        // Di sini bisa tambahkan highlight (ubah warna background, dll.)
    }

    public void MoveUp(GameObject item)
    {
        int idx = item.transform.GetSiblingIndex();
        if (idx > 0)
        {
            item.transform.SetSiblingIndex(idx - 1);
            SyncToWorld();
        }
    }

    public void MoveDown(GameObject item)
    {
        int idx = item.transform.GetSiblingIndex();
        if (idx < transform.childCount - 1)
        {
            item.transform.SetSiblingIndex(idx + 1);
            SyncToWorld();
        }
    }

    void SyncToWorld()
    {
        // Balik urutan: item UI paling atas = object paling depan
        int n = transform.childCount;
        for (int uiIndex = 0; uiIndex < n; uiIndex++)
        {
            var uiChild = transform.GetChild(uiIndex).gameObject;

            // Cari objek target dengan nama yang sama di designRoot
            var target = designRoot.Find(uiChild.name);
            if (target != null)
            {
                int worldIndex = (designRoot.childCount - 1) - uiIndex;
                target.SetSiblingIndex(worldIndex);
            }
        }
    }
}
