using UnityEngine;

public enum PublishMode { CompletionOnly, QualityThreshold, Hybrid }

[CreateAssetMenu(menuName = "GD/LevelRubric")]
public class LevelRubric : ScriptableObject
{
    public PublishMode mode = PublishMode.Hybrid;
    [Range(0, 1)] public float publishThreshold = 0.75f;
    public RuleBase[] rules;
}