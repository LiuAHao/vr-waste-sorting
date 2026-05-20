using UnityEngine;

public class ClassificationTestListener : MonoBehaviour
{
    private void OnEnable()
    {
        ClassificationEvents.OnClassified += OnResult;
    }

    private void OnDisable()
    {
        ClassificationEvents.OnClassified -= OnResult;
    }

    private void OnResult(ClassificationResult result)
    {
        Debug.Log(
            $"[{result.Item.ItemName}] 投进 [{result.Bin.DisplayName}] | " +
            $"正确:{result.IsCorrect} | 原因:{result.Reason}");
    }
}
