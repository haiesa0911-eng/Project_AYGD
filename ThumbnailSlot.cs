using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ThumbnailSlot : MonoBehaviour
{
    public enum FitMode { None, FitInside, Fill }

    [Header("Area slot (kosongkan = pakai RectTransform sendiri)")]
    public RectTransform viewport;

    [Header("Fitting")]
    public FitMode fitMode = FitMode.FitInside;
    public Vector2 padding = Vector2.zero;           // px di kiri/kanan & atas/bawah
    public bool allowUpscale = false;                // true = boleh membesarkan di atas ukuran asli

    [Header("Opsional untuk Image UI")]
    public bool setNativeSizeOnImage = true;         // ambil ukuran asli sprite sebelum dihitung

    RectTransform Viewport => viewport ? viewport : (RectTransform)transform;

    /// <summary>
    /// Tempel RectTransform (UI) sebagai konten thumbnail.
    /// </summary>
    public void AttachContent(RectTransform contentRT)
    {
        if (contentRT == null) return;

        // Jadikan child & reset transform
        contentRT.SetParent(Viewport, worldPositionStays: false);
        contentRT.localRotation = Quaternion.identity;
        contentRT.localScale = Vector3.one;

        // Pastikan anchor & pivot center
        contentRT.anchorMin = contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        contentRT.pivot = new Vector2(0.5f, 0.5f);
        contentRT.anchoredPosition = Vector2.zero;

        // Jika ada Image & diizinkan, setNativeSize agar ukuran sesuai sprite
        var img = contentRT.GetComponent<Image>();
        if (img && img.sprite && setNativeSizeOnImage)
            img.SetNativeSize();

        // Hitung skala sesuai mode
        if (fitMode != FitMode.None)
        {
            var area = Viewport.rect.size - padding * 2f;
            area.x = Mathf.Max(1, area.x);
            area.y = Mathf.Max(1, area.y);

            // ukuran konten saat ini (pakai rect UI)
            var contentSize = contentRT.rect.size;
            contentSize.x = Mathf.Max(1, contentSize.x);
            contentSize.y = Mathf.Max(1, contentSize.y);

            float sx = area.x / contentSize.x;
            float sy = area.y / contentSize.y;
            float scale = (fitMode == FitMode.FitInside) ? Mathf.Min(sx, sy) : Mathf.Max(sx, sy);

            if (!allowUpscale) scale = Mathf.Min(1f, scale);

            contentRT.localScale = new Vector3(scale, scale, 1f);
        }

        // Pastikan tetap center
        contentRT.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Helper: langsung tempel Sprite (dibuatkan GameObject Image sebagai child).
    /// </summary>
    public Image AttachSprite(Sprite sprite, string childName = "ThumbnailContent")
    {
        if (!sprite) return null;

        var go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.sprite = sprite;

        if (setNativeSizeOnImage) img.SetNativeSize();

        AttachContent(rt);
        return img;
    }

    // Quality of life: cepat pusatkan child pertama di Play Mode atau dari Context Menu
    [ContextMenu("Center First Child")]
    void CenterFirstChild()
    {
        if (Viewport.childCount > 0)
            AttachContent(Viewport.GetChild(0) as RectTransform);
    }
}
