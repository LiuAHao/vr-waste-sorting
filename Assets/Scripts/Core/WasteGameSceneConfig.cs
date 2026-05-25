using UnityEngine;

public sealed class WasteGameSceneConfig : MonoBehaviour
{
    [SerializeField] private int targetCount = 12;
    [SerializeField] private float totalTimeSeconds = 180f;

    public int TargetCount => Mathf.Max(1, targetCount);
    public float TotalTimeSeconds => Mathf.Max(10f, totalTimeSeconds);
}
