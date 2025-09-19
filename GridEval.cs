using UnityEngine;

public static class GridEval
{
    public struct RectI
    {
        public int r0, c0, r1, c1; // inklusif
        public int Width => (c1 - c0 + 1);
        public int Height => (r1 - r0 + 1);
        public int CenterRow => (r0 + r1) >> 1;
        public int CenterCol => (c0 + c1) >> 1;
    }

    // Ambil rect dari PieceInfo (fallback kalau belum snapped)
    public static bool TryGetRect(in PieceInfo p, out RectI rect)
    {
        if (p.snapped)
        {
            rect = new RectI { r0 = p.r0, c0 = p.c0, r1 = p.r1, c1 = p.c1 };
            return true;
        }
        rect = default;
        return false;
    }

    // Dapatkan ukuran papan dari BoardGridRef (silakan sesuaikan ke API Anda)
    public static bool TryGetBoardSize(BoardGridRef board, out int rows, out int cols)
    {
        if (board != null)
        {
            rows = board.rows;
            cols = board.cols;
            return true;
        }
        rows = cols = 0;
        return false;
    }

    public static bool Intersects(in RectI a, in RectI b)
    {
        bool sep = (a.r1 < b.r0) || (b.r1 < a.r0) || (a.c1 < b.c0) || (b.c1 < a.c0);
        return !sep;
    }

    // Gap antar rect (dalam sel). 0 berarti berhimpit/overlap. Negatif tidak mungkin.
    public static int MinGapCells(in RectI a, in RectI b)
    {
        int vr = 0;
        if (a.r1 < b.r0) vr = b.r0 - a.r1 - 1;
        else if (b.r1 < a.r0) vr = a.r0 - b.r1 - 1;

        int hc = 0;
        if (a.c1 < b.c0) hc = b.c0 - a.c1 - 1;
        else if (b.c1 < a.c0) hc = a.c0 - b.c1 - 1;

        if (vr == 0 && hc == 0) return 0; // bersinggungan/overlap
        if (vr == 0) return hc;
        if (hc == 0) return vr;
        return Mathf.Min(vr, hc);
    }

    // Margin ke tepi papan (top, right, bottom, left)
    public static Vector4 EdgeMargins(in RectI r, int rows, int cols)
    {
        int top = r.r0;
        int bottom = (rows - 1) - r.r1;
        int left = r.c0;
        int right = (cols - 1) - r.c1;
        return new Vector4(top, right, bottom, left);
    }

    // “Seberapa center” relatif ke pusat papan (0..1, 1 paling center)
    public static float CenterScore(in RectI r, int rows, int cols, int tolCells = 1)
    {
        int br = (rows - 1) >> 1;  // approx pusat baris
        int bc = (cols - 1) >> 1;  // approx pusat kolom
        int dr = Mathf.Abs(r.CenterRow - br);
        int dc = Mathf.Abs(r.CenterCol - bc);
        // Normalisasi kasar: dalam toleransi → 1, lalu turun linear.
        float nr = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - dr / (float)(tolCells + 1)));
        float nc = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - dc / (float)(tolCells + 1)));
        return 0.5f * (nr + nc);
    }

    // Keseimbangan whitespace kiri-kanan & atas-bawah (0..1)
    public static float BalanceScore(in RectI r, int rows, int cols)
    {
        var m = EdgeMargins(r, rows, cols);
        float lr = 1f - Mathf.Clamp01(Mathf.Abs(m.w - m.y) / (cols * 0.5f + 1e-5f)); // left vs right
        float tb = 1f - Mathf.Clamp01(Mathf.Abs(m.x - m.z) / (rows * 0.5f + 1e-5f)); // top vs bottom
        return 0.5f * (lr + tb);
    }
}
