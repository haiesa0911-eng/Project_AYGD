using UnityEngine;

public class GridToggleButton : MonoBehaviour
{
    [SerializeField] private BoardGridRef board;
    private bool isActive = false;

    public void ToggleGrid()
    {
        if (!board) return;

        isActive = !isActive;
        board.forceVisible = isActive;

        if (isActive)
        {
            // Tampilkan grid + langsung sorot occupied
            board.RefreshRulerView();
        }
        else
        {
            // Kembali ke default
            board.ClearHighlights();
        }
    }
}
