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

        GarbageItem item = ResolveGarbageItem(other);
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

    private static GarbageItem ResolveGarbageItem(Collider other)
    {
        GarbageItem item = other.GetComponent<GarbageItem>();
        if (item != null)
        {
            return item;
        }

        if (other.attachedRigidbody != null)
        {
            item = other.attachedRigidbody.GetComponent<GarbageItem>();
            if (item != null)
            {
                return item;
            }
        }

        item = other.GetComponentInParent<GarbageItem>();
        if (item != null)
        {
            return item;
        }

        return other.GetComponentInChildren<GarbageItem>();
    }
}
