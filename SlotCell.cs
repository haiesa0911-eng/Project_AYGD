using System.Collections.Generic;
using UnityEngine;

public class SlotCell : MonoBehaviour
{
    [Header("Grid Index")]
    public int row;
    public int col;

    [Header("Stacking")]
    [Tooltip("Berapa banyak piece yang boleh ditumpuk di cell ini.")]
    public int capacity = 2; // atur 2 (atau lebih) agar bisa stacking

    // BACK-COMPAT: field lama agar kode lama tetap aman. Akan diisi occupant pertama.
    [HideInInspector] public PieceSnapper occupiedBy;

    // Daftar occupant aktual untuk stacking
    public readonly List<PieceSnapper> occupants = new List<PieceSnapper>();

    public bool IsFull => occupants.Count >= Mathf.Max(1, capacity);

    public void AddOccupant(PieceSnapper p)
    {
        if (!occupants.Contains(p))
        {
            occupants.Add(p);
            if (occupiedBy == null) occupiedBy = p; // back-compat
        }
    }

    public void RemoveOccupant(PieceSnapper p)
    {
        if (occupants.Remove(p))
        {
            // perbarui back-compat field
            occupiedBy = occupants.Count > 0 ? occupants[0] : null;
        }
    }
}
