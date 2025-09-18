using UnityEngine;
using System.Collections.Generic;

public class SelectionToColourSync : MonoBehaviour
{
    public ColorDropdownBinder binder;

    void OnEnable()
    {
        if (SelectionManager.I != null)
        {
            SelectionManager.I.OnSelectionChanged += HandleSelectionChanged;
            HandleSelectionChanged(SelectionManager.I.Current); // sync awal
        }
    }
    void OnDisable()
    {
        if (SelectionManager.I != null)
            SelectionManager.I.OnSelectionChanged -= HandleSelectionChanged;
    }

    void HandleSelectionChanged(IReadOnlyCollection<SelectionBox> _)
    {
        if (binder != null)
            binder.SyncDropdownToSelectionById(fallbackToColor: false);
        // set true kalau mau jatuh ke pencocokan warna bila ID kosong/tidak ketemu
    }
}
