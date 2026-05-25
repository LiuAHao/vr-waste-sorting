using UnityEngine;

public class TrashBin : MonoBehaviour
{
    [SerializeField] private WasteCategory category;
    [SerializeField] private string displayName;

    public WasteCategory Category => category;
    public string DisplayName => displayName;

    public bool Accepts(GarbageItem item)
    {
        return item != null && item.Category == category;
    }
}
