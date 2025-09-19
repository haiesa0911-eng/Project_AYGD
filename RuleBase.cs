using UnityEngine;

public abstract class RuleBase : ScriptableObject
{
    [Header("Common Settings")]
    [Tooltip("Jika true, rule ini bersifat Hard Gate (jika gagal → level otomatis fail).")]
    public bool isHardGate = false;

    [Range(0f, 10f)]
    [Tooltip("Bobot rule dalam perhitungan skor.")]
    public float weight = 1f;

    // Semua rule harus override fungsi ini
    public abstract RuleResult Evaluate(GridState s);
}

[System.Serializable]
public struct RuleResult
{
    public bool pass;      // true jika lulus rule
    public float score01;  // skor 0..1
    public string reason;  // alasan/penjelasan (untuk debug/LD feedback)
}
