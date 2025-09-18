using UnityEngine;
using UnityEngine.Rendering;
#if TMP_PRESENT
using TMPro;
#endif

/// Pasang di root objek dunia yang perlu di-sort.
public class Sortable : MonoBehaviour
{
    [Tooltip("Offset urutan di dalam layer (mis: urutan spawn di layer tsb).")]
    public int localOffset;

    SortingGroup sg;
    SpriteRenderer sr;
    Renderer anyRenderer;

    void Awake()
    {
        sg = GetComponentInParent<SortingGroup>();
        if (!sg) sr = GetComponentInChildren<SpriteRenderer>();
        if (!sr) anyRenderer = GetComponentInChildren<Renderer>();
    }

    public void ApplyOrder(int sortingLayerID, int order)
    {
        if (sg)
        {
            sg.sortingLayerID = sortingLayerID;
            sg.sortingOrder = order;
        }
        else if (sr)
        {
            sr.sortingLayerID = sortingLayerID;
            sr.sortingOrder = order;
        }
        else if (anyRenderer)
        {
            anyRenderer.sortingLayerID = sortingLayerID;
            anyRenderer.sortingOrder = order;
        }
    }
}
