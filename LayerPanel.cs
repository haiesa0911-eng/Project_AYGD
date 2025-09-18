using UnityEngine;

public class LayerPanel : MonoBehaviour
{
    public void MoveUp(RectTransform item)
    {
        int index = item.GetSiblingIndex();
        if (index > 0)
            item.SetSiblingIndex(index - 1);
    }

    public void MoveDown(RectTransform item)
    {
        int index = item.GetSiblingIndex();
        if (index < item.parent.childCount - 1)
            item.SetSiblingIndex(index + 1);
    }
}
