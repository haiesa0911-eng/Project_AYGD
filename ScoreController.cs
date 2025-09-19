using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class ScoreController : MonoBehaviour
{
    [Header("Star Badge (UI Sprite Box)")]
    public Image starBadge; // drag Image kotak sprite di Canvas
    public Color colFail = Color.black;
    public Color colOne = Color.yellow;
    public Color colTwo = new Color(0.2f, 0.5f, 1f); // biru enak dilihat
    public Color colThree = Color.green;
    public Color colPink = new Color(1f, 0.4f, 0.7f); // pink hidden

    [Header("Star Thresholds")]
    [Range(0f, 1f)] public float oneStarMin = 0.33f;
    [Range(0f, 1f)] public float twoStarMin = 0.66f;
    [Range(0f, 1f)] public float threeStarMin = 0.90f;

    // Optional: untuk sementara kalau belum ada Pink rule
    public bool debugForcePink = false;

    [Header("References")]
    public BoardGridRef board;
    public LevelRubric rubric;
    public Button shareButton;
    public Image shareBG;

    [Tooltip("Optional: batasi pencarian PieceSnapper hanya di subtree ini (mis. parent: Puzzle_Piece).")]
    public Transform piecesRoot;

    [Header("Options")]
    public bool evaluateOnStart = true;

    void Start()
    {
        SetShare(false, Color.gray);
        if (evaluateOnStart) EvaluateNow();
    }

    // === Dipanggil oleh PieceSnapStateNotifier atau tombol debug ===
    public void EvaluateNow()
    {
        if (rubric == null) return;

        var state = BuildGridState();

        // === GUARD ===
        // Kalau belum ada keping sama sekali, paksa gagal
        if (state.pieces == null || state.pieces.Count == 0)
        {
            ApplyShareState(false, 0f);   // tombol = abu-abu
            return; // hentikan evaluasi di sini
        }

        bool gates = true;
        float sum = 0f, wsum = 0f;

        if (rubric.rules != null)
        {
            foreach (var rule in rubric.rules)
            {
                if (rule == null) continue;

                var res = rule.Evaluate(state);
                if (rule.isHardGate && !res.pass) gates = false;

                float w = Mathf.Max(0f, rule.weight);
                sum += Mathf.Clamp01(res.score01) * w;
                wsum += w;
            }
        }

        float score = (wsum > 0f) ? (sum / wsum) : 0f;
        ApplyShareState(gates, score);
    }

    // === Bangun snapshot state papan tanpa constructor khusus ===
    GridState BuildGridState()
    {
        var s = new GridState
        {
            board = board,
            pieces = new Dictionary<PieceId, PieceInfo>()
        };

        PieceSnapper[] snaps;
        if (piecesRoot != null)
        {
            // lebih aman: hanya cari di bawah parent yang Anda tentukan
            snaps = piecesRoot.GetComponentsInChildren<PieceSnapper>(true);
        }
        else
        {
            // fallback global
#if UNITY_2023_1_OR_NEWER
            snaps = Object.FindObjectsByType<PieceSnapper>(FindObjectsSortMode.None);
#else
            snaps = FindObjectsOfType<PieceSnapper>();
#endif
        }

        foreach (var snap in snaps)
        {
            if (snap == null) continue;
            var go = snap.gameObject;

            // abaikan yang tidak aktif
            if (!go.activeInHierarchy || !snap.isActiveAndEnabled) continue;

            var tag = snap.GetComponent<PieceTag>();
            if (tag == null) continue;

            var info = new PieceInfo
            {
                id = tag.id,
                snap = snap,
                snapped = snap.IsSnapped
            };

            if (info.snapped &&
                snap.TryGetCurrentRect(out info.r0, out info.c0, out info.r1, out info.c1))
            {
                // rect terisi
            }

            // overwrite by key: pastikan tiap PieceId punya 1 entry terakhir yang aktif
            s.pieces[info.id] = info;
        }

        return s;
    }

    void ApplyShareState(bool gates, float score)
    {
        if (shareButton == null || shareBG == null || rubric == null) return;

        switch (rubric.mode)
        {
            case PublishMode.CompletionOnly:
                {
                    bool ok = gates;
                    SetShare(ok, ok ? Color.green : Color.gray);
                    break;
                }
            case PublishMode.QualityThreshold:
                {
                    bool ok = gates && score >= rubric.publishThreshold;
                    SetShare(ok, ok ? Color.green : Color.gray);
                    break;
                }
            default: // Hybrid
                if (!gates) SetShare(false, Color.gray);
                else if (score >= rubric.publishThreshold) SetShare(true, Color.green);
                else SetShare(true, Color.yellow);
                break;
        }
    }

    void SetShare(bool interactable, Color c)
    {
        shareButton.interactable = interactable;
        shareBG.color = c;
    }

}
