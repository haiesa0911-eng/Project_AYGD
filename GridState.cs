using System.Collections.Generic;
using UnityEngine;

// Info 1 keping pada grid
public struct PieceInfo
{
    public PieceId id;
    public PieceSnapper snap;   // referensi ke komponen snap
    public bool snapped;        // sudah ditempatkan?
    public int r0, c0, r1, c1;  // rect grid jika snapped
}

// Snapshot seluruh papan pada saat evaluasi
public class GridState
{
    public BoardGridRef board;                          // referensi papan aktif
    public Dictionary<PieceId, PieceInfo> pieces;       // kumpulan keping
}
