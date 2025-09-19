using UnityEngine;

[CreateAssetMenu(menuName = "GD/Rules/ObjectColor")]
public sealed class Rule_ColorObject : RuleBase
{
    [Header("Target")]
    public PieceId targetId;         // piece mana yang dicek (Tagline, Date, dsb.)
    public string requiredHex = "#FF0000"; // warna yang diharapkan (hex string, misal "#FF0000")
    public bool exactMatch = true;   // true = harus sama persis, false = cukup kontras

    public override RuleResult Evaluate(GridState s)
    {
        if (s.pieces == null || !s.pieces.TryGetValue(targetId, out var pi) || !pi.snapped)
        {
            return new RuleResult
            {
                pass = false,
                score01 = 0f,
                reason = $"{targetId} belum ditempatkan"
            };
        }

        // Ambil komponen renderer / TMP yang menempel
        var rend = pi.snap.GetComponentInChildren<Renderer>();
        if (rend == null || rend.material == null)
        {
            return new RuleResult
            {
                pass = false,
                score01 = 0f,
                reason = $"Tidak ditemukan warna untuk {targetId}"
            };
        }

        var color = rend.material.color;
        string hex = ColorUtility.ToHtmlStringRGB(color);
        string currentHex = "#" + hex;

        if (exactMatch)
        {
            bool ok = string.Equals(currentHex, requiredHex, System.StringComparison.OrdinalIgnoreCase);
            return new RuleResult
            {
                pass = ok,
                score01 = ok ? 1f : 0f,
                reason = ok ? $"{targetId} warna sesuai {requiredHex}" : $"{targetId} warna {currentHex}, tidak sesuai {requiredHex}"
            };
        }
        else
        {
            // cek kontras sederhana terhadap background
            ColorUtility.TryParseHtmlString(requiredHex, out var req);
            float diff = Mathf.Abs(color.r - req.r) + Mathf.Abs(color.g - req.g) + Mathf.Abs(color.b - req.b);
            bool ok = diff > 0.5f; // threshold kontras
            return new RuleResult
            {
                pass = ok,
                score01 = ok ? 1f : 0.5f,
                reason = ok ? $"{targetId} kontras dengan background" : $"{targetId} kurang kontras"
            };
        }
    }
}
