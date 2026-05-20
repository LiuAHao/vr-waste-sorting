using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WasteContentCatalog", menuName = "ParkClean/Waste Content Catalog")]
public sealed class WasteContentCatalog : ScriptableObject
{
    [SerializeField] private List<GarbageContentDefinition> garbageItems = new List<GarbageContentDefinition>();
    [SerializeField] private List<TrashBinContentDefinition> trashBins = new List<TrashBinContentDefinition>();

    public IReadOnlyList<GarbageContentDefinition> GarbageItems => garbageItems;
    public IReadOnlyList<TrashBinContentDefinition> TrashBins => trashBins;
}

[Serializable]
public sealed class GarbageContentDefinition
{
    public string itemId;
    public string itemName;
    public WasteCategory category;
    [TextArea(2, 4)] public string wrongReason;
    public string assetPath;
    public Vector3 position;
    public Vector3 rotationEuler;
    public Vector3 scale = Vector3.one;
}

[Serializable]
public sealed class TrashBinContentDefinition
{
    public string displayName;
    public WasteCategory category;
    public string assetPath;
    public Vector3 position;
    public Vector3 rotationEuler;
    public Vector3 scale = Vector3.one;
}
