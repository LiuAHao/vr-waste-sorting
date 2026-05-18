using UnityEngine;

public class DropZone : MonoBehaviour
{
    [SerializeField] private TrashBin targetBin;

    private void OnTriggerEnter(Collider other)
    {
        if (targetBin == null)
        {
            Debug.LogWarning("DropZone: targetBin is not assigned.", this);
            return;
        }

        GarbageItem item = other.GetComponent<GarbageItem>();
        if (item == null)
        {
            item = other.GetComponentInParent<GarbageItem>();
        }

        if (item == null || item.IsCompleted || item.IsHeld)
        {
            return;
        }

        bool isCorrect = targetBin.Accepts(item);
        string reason = isCorrect ? string.Empty : item.WrongReason;

        var result = new ClassificationResult(
            item,
            targetBin,
            isCorrect,
            item.Category,
            targetBin.Category,
            reason);

        if (isCorrect)
        {
            item.MarkCompleted();
        }

        ClassificationEvents.RaiseClassified(result);
    }
}
