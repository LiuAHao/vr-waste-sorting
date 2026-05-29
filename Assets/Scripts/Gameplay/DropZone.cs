using UnityEngine;

public class DropZone : MonoBehaviour
{
    [SerializeField] private TrashBin targetBin;

    private Collider _triggerCollider;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
    }

    public bool CanAccept(GarbageItem item)
    {
        return item != null
            && !item.IsCompleted
            && targetBin != null
            && IsOverlapping(item);
    }

    public ClassificationResult Classify(GarbageItem item)
    {
        if (!CanAccept(item))
        {
            return null;
        }

        bool isCorrect = targetBin.Accepts(item);
        string reason = isCorrect ? string.Empty : item.WrongReason;

        ClassificationResult result = new ClassificationResult(
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
        return result;
    }

    public static ClassificationResult TryClassifyReleasedItem(GarbageItem item)
    {
        if (item == null || item.IsCompleted)
        {
            return null;
        }

        DropZone[] zones = FindObjectsOfType<DropZone>(true);
        DropZone bestZone = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < zones.Length; i++)
        {
            DropZone zone = zones[i];
            if (zone == null || !zone.CanAccept(item))
            {
                continue;
            }

            float distance = Vector3.Distance(item.transform.position, zone.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestZone = zone;
            }
        }

        return bestZone != null ? bestZone.Classify(item) : null;
    }

    /// <summary>
    /// 支持 VR 抛物入桶：物品飞入 Trigger 时自动判定分类。
    /// 桌面模式下不会触发此回调（桌面抓取不使用物理飞入）。
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        GarbageItem item = other.GetComponentInParent<GarbageItem>();
        if (item == null) item = other.GetComponent<GarbageItem>();
        if (item == null || item.IsHeld || item.IsCompleted) return;

        Classify(item);
    }

    private bool IsOverlapping(GarbageItem item)
    {
        if (_triggerCollider == null)
        {
            _triggerCollider = GetComponent<Collider>();
        }

        if (_triggerCollider == null)
        {
            return false;
        }

        Collider[] itemColliders = item.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < itemColliders.Length; i++)
        {
            Collider itemCollider = itemColliders[i];
            if (itemCollider == null || !itemCollider.enabled)
            {
                continue;
            }

            Bounds bounds = itemCollider.bounds;
            Vector3 closestPoint = _triggerCollider.ClosestPoint(bounds.center);
            if (_triggerCollider.bounds.Intersects(bounds) || bounds.Contains(closestPoint))
            {
                return true;
            }
        }

        return false;
    }
}
