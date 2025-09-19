// PiecesCompletionRule.cs (ganti isi Evaluate jadi seperti ini)
using UnityEngine;

[CreateAssetMenu(menuName = "GD/Rules/PiecesCompletion")]
public sealed class Rule_ContentCompletion : RuleBase
{
    public PieceId[] requiredPieces;   // isi di Inspector: Title, Tagline, Date, Asset1, dst.

    public override RuleResult Evaluate(GridState s)
    {
        // tidak ada piece di scene -> gagal (agar tidak hijau saat awal)
        if (s.pieces == null || s.pieces.Count == 0)
            return new RuleResult { pass = false, score01 = 0f, reason = "Belum ada keping di level" };

        // jika Anda isi requiredPieces, cek satu per satu
        if (requiredPieces != null && requiredPieces.Length > 0)
        {
            foreach (var id in requiredPieces)
            {
                if (!s.pieces.TryGetValue(id, out var p) || !p.snapped)
                {
                    return new RuleResult { pass = false, score01 = 0f, reason = $"{id} belum ditempatkan" };
                }
            }
            return new RuleResult { pass = true, score01 = 1f, reason = "Semua keping wajib sudah ditempatkan" };
        }

        // fallback: kalau tidak diisi, minimal semua piece yang TERBACA harus snapped
        foreach (var kv in s.pieces)
        {
            if (!kv.Value.snapped)
            {
                return new RuleResult { pass = false, score01 = 0f, reason = $"{kv.Key} belum ditempatkan" };
            }
        }
        return new RuleResult { pass = true, score01 = 1f, reason = "Semua keping sudah ditempatkan" };
    }
}
