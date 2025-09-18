// IColorReceiver.cs
public interface IColorReceiver
{
    UnityEngine.Color GetColor();
    void SetColor(UnityEngine.Color c);
    string GetColorId();
    void SetColorId(string id);
}
