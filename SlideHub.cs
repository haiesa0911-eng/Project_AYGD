using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SlideHub : MonoBehaviour
{
    [Header("Active")]
    public SlideItem current;

    [Header("Buttons (opsional, untuk auto-enable/disable)")]
    [SerializeField] private Button btnLeft;   // drag Button Left (Prev)
    [SerializeField] private Button btnRight;  // drag Button Right (Next)

    [Header("Shift Config")]
    [Tooltip("Offset X per langkah untuk Next/Prev (jarak antar slide).")]
    [SerializeField] private float offsetX = 950f;

    [Header("Animation")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField, Min(0.01f)] private float duration = 0.3f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Activation")]
    [Tooltip("Jika aktif, hanya item yang berada di tengah (current) yang SetActive(true).")]
    [SerializeField] private bool centerOnlyActive = true;

    [Tooltip("Aktifkan item yang akan masuk (incoming) tepat sebelum animasi agar terlihat saat transisi.")]
    [SerializeField] private bool activateIncomingAtMoveStart = true;

    [Header("Post Fix")]
    [Tooltip("Rapikan posisi akhir: current→0, left→-offset, right→+offset")]
    [SerializeField] private bool snapAfterMove = true;

    private bool busy;

    void Start()
    {
        // Tata posisi awal (opsional, sesuai versi sebelumnya)
        if (current) current.SetX(0f);
        if (current && current.leftNeighbor) current.leftNeighbor.SetX(-Mathf.Abs(offsetX));
        if (current && current.rightNeighbor) current.rightNeighbor.SetX(+Mathf.Abs(offsetX));

        // Pastikan hanya current yang aktif (opsional sesuai flag)
        ApplyActiveStates();

        UpdateButtons();
    }

    public void Next()
    {
        if (busy || current == null) return;
        if (current.rightNeighbor == null) return; // sudah di batas maksimum → tombol seharusnya nonaktif

        var left = current.leftNeighbor;     // boleh null
        var mid = current;                   // current
        var right = current.rightNeighbor;   // pasti ada

        float dx = -Mathf.Abs(offsetX);      // geser ke kiri

        // Pastikan incoming terlihat saat mulai bergerak
        if (centerOnlyActive && activateIncomingAtMoveStart && right)
            right.gameObject.SetActive(true);

        if (useAnimation)
        {
            StartCoroutine(ShiftTripletAnimated(left, mid, right, dx, moveToRight: true));
        }
        else
        {
            if (left) left.ShiftX(dx);
            mid.ShiftX(dx);
            right.ShiftX(dx);

            current = right;  // pindahkan fokus
            if (snapAfterMove) SnapNeighborsPositions();

            // Hanya current yang aktif setelah perpindahan
            ApplyActiveStates();

            UpdateButtons();
        }
    }

    public void Prev()
    {
        if (busy || current == null) return;
        if (current.leftNeighbor == null) return; // sudah di batas minimum → tombol seharusnya nonaktif

        var left = current.leftNeighbor;     // pasti ada
        var mid = current;                   // current
        var right = current.rightNeighbor;   // boleh null

        float dx = +Mathf.Abs(offsetX);      // geser ke kanan

        // Pastikan incoming terlihat saat mulai bergerak
        if (centerOnlyActive && activateIncomingAtMoveStart && left)
            left.gameObject.SetActive(true);

        if (useAnimation)
        {
            StartCoroutine(ShiftTripletAnimated(left, mid, right, dx, moveToRight: false));
        }
        else
        {
            left.ShiftX(dx);
            mid.ShiftX(dx);
            if (right) right.ShiftX(dx);

            current = left; // pindahkan fokus
            if (snapAfterMove) SnapNeighborsPositions();

            // Hanya current yang aktif setelah perpindahan
            ApplyActiveStates();

            UpdateButtons();
        }
    }

    private IEnumerator ShiftTripletAnimated(SlideItem left, SlideItem mid, SlideItem right, float dx, bool moveToRight)
    {
        busy = true;
        SetButtonsInteractable(false); // cegah spam klik saat animasi

        var coL = (left != null) ? left.ShiftXAnimated(dx, duration, ease) : null;
        var coM = mid.ShiftXAnimated(dx, duration, ease);
        var coR = (right != null) ? right.ShiftXAnimated(dx, duration, ease) : null;

        bool doneL = (coL == null), doneM = false, doneR = (coR == null);

        if (coL != null) StartCoroutine(CoWrap(coL, () => doneL = true));
        StartCoroutine(CoWrap(coM, () => doneM = true));
        if (coR != null) StartCoroutine(CoWrap(coR, () => doneR = true));

        while (!(doneL && doneM && doneR)) yield return null;

        // Pindahkan fokus current setelah transisi
        current = moveToRight ? current.rightNeighbor : current.leftNeighbor;

        if (snapAfterMove) SnapNeighborsPositions();

        // Hanya current yang aktif setelah perpindahan
        ApplyActiveStates();

        busy = false;
        UpdateButtons();           // refresh state tombol sesuai posisi baru
    }

    private IEnumerator CoWrap(IEnumerator co, System.Action onDone)
    {
        while (co.MoveNext()) yield return co.Current;
        onDone?.Invoke();
    }

    private void SnapNeighborsPositions()
    {
        float off = Mathf.Abs(offsetX);

        current.SetX(0f);
        if (current.leftNeighbor) current.leftNeighbor.SetX(-off);
        if (current.rightNeighbor) current.rightNeighbor.SetX(+off);
    }

    private void UpdateButtons()
    {
        // Tombol otomatis nonaktif di batas minimum/maksimum
        bool canPrev = (current != null && current.leftNeighbor != null);
        bool canNext = (current != null && current.rightNeighbor != null);

        if (btnLeft) btnLeft.interactable = canPrev && !busy;
        if (btnRight) btnRight.interactable = canNext && !busy;
    }

    private void SetButtonsInteractable(bool enable)
    {
        // Selama animasi dinonaktifkan; setelah selesai diatur lagi via UpdateButtons()
        if (btnLeft) btnLeft.interactable = enable && (current != null && current.leftNeighbor != null);
        if (btnRight) btnRight.interactable = enable && (current != null && current.rightNeighbor != null);
    }

    // === Activation helpers ===

    /// <summary>
    /// Terapkan kebijakan aktif/nonaktif: hanya current yang aktif (jika centerOnlyActive = true).
    /// Aman dipanggil kapan saja (Start/After Move).
    /// </summary>
    private void ApplyActiveStates()
    {
        if (!centerOnlyActive) return;

        if (current)
        {
            // Pastikan current aktif
            if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);

            // Nonaktifkan semua ke kiri
            var p = current.leftNeighbor;
            while (p != null)
            {
                if (p.gameObject.activeSelf) p.gameObject.SetActive(false);
                p = p.leftNeighbor;
            }

            // Nonaktifkan semua ke kanan
            p = current.rightNeighbor;
            while (p != null)
            {
                if (p.gameObject.activeSelf) p.gameObject.SetActive(false);
                p = p.rightNeighbor;
            }
        }
    }
}
