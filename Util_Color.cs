using UnityEngine;

public static class Util_Color
{
    public static float RelLum(Color c)
    {
        float f(float x) => (x <= 0.03928f) ? (x / 12.92f) : Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
        float r = f(c.r), g = f(c.g), b = f(c.b);
        return 0.2126f * r + 0.7152f * g + 0.0722f * b;
    }

    public static float Contrast(Color a, Color b)
    {
        float L1 = RelLum(a), L2 = RelLum(b);
        if (L1 < L2) { var t = L1; L1 = L2; L2 = t; }
        return (L1 + 0.05f) / (L2 + 0.05f); // 1..21
    }
}
