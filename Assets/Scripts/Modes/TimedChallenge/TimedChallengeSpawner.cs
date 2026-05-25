using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class TimedChallengeSpawner : MonoBehaviour
{
    [SerializeField] private TimedChallengeConfig config;
    [SerializeField] private Transform spawnedItemsRoot;

    private readonly List<GarbageItem> _activeItems = new List<GarbageItem>();
    private readonly List<TimedChallengeSpawnPoint> _spawnPoints = new List<TimedChallengeSpawnPoint>();
    private readonly List<string> _resolvedItemIds = new List<string>();
    private readonly Dictionary<string, GarbageItem> _sceneTemplates = new Dictionary<string, GarbageItem>();

    public IReadOnlyList<GarbageItem> ActiveItems => _activeItems;

    public void Configure(TimedChallengeConfig challengeConfig)
    {
        if (challengeConfig != null)
        {
            config = challengeConfig;
        }
    }

    public void Initialize(TimedChallengeConfig challengeConfig)
    {
        config = challengeConfig;
        RefreshSpawnPoints();
        CacheScenePrototypes();
        BuildResolvedItemPool();
        ClearSpawnedItems();
        FillActiveGarbage(config != null ? config.ActiveGarbageCount : 0);
    }

    public void HandleItemProcessed(GarbageItem item)
    {
        if (item == null)
        {
            return;
        }

        RemoveActiveItem(item);
        ReleaseSpawnPointForItem(item);
        DestroyItem(item);
        ReplenishIfNeeded();
    }

    public void ClearSpawnedItems()
    {
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            GarbageItem item = _activeItems[i];
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        _activeItems.Clear();

        for (int i = 0; i < _spawnPoints.Count; i++)
        {
            _spawnPoints[i].SetOccupied(false);
        }
    }

    private void RefreshSpawnPoints()
    {
        _spawnPoints.Clear();
        TimedChallengeSpawnPoint[] points = FindObjectsOfType<TimedChallengeSpawnPoint>(true);
        string groupId = config != null ? config.SpawnPointGroupId : string.Empty;

        for (int i = 0; i < points.Length; i++)
        {
            TimedChallengeSpawnPoint point = points[i];
            if (point != null && point.MatchesGroup(groupId))
            {
                point.SetOccupied(false);
                _spawnPoints.Add(point);
            }
        }
    }

    private void CacheScenePrototypes()
    {
        _sceneTemplates.Clear();

        GarbageItem[] sceneItems = FindObjectsOfType<GarbageItem>(true);
        for (int i = 0; i < sceneItems.Length; i++)
        {
            GarbageItem item = sceneItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            if (_sceneTemplates.ContainsKey(item.ItemId))
            {
                continue;
            }

            TimedChallengeRuntimeItem runtimeItem = item.GetComponent<TimedChallengeRuntimeItem>();
            if (runtimeItem != null)
            {
                continue;
            }

            if (spawnedItemsRoot != null && item.transform.IsChildOf(spawnedItemsRoot))
            {
                continue;
            }

            _sceneTemplates[item.ItemId] = item;
        }
    }

    private void BuildResolvedItemPool()
    {
        _resolvedItemIds.Clear();

        if (config == null)
        {
            return;
        }

        IReadOnlyList<TimedChallengeGarbagePoolEntry> poolEntries = config.GarbagePool;
        for (int i = 0; i < poolEntries.Count; i++)
        {
            TimedChallengeGarbagePoolEntry entry = poolEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
            {
                continue;
            }

            if (!config.IsItemAllowed(entry.itemId))
            {
                continue;
            }

            if (ResolvePrefab(entry) == null && FindSceneTemplate(entry.itemId) == null)
            {
                continue;
            }

            _resolvedItemIds.Add(entry.itemId);
        }

        if (_resolvedItemIds.Count > 0 || config.ContentCatalog == null)
        {
            return;
        }

        IReadOnlyList<GarbageContentDefinition> catalogItems = config.ContentCatalog.GarbageItems;
        for (int i = 0; i < catalogItems.Count; i++)
        {
            GarbageContentDefinition definition = catalogItems[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.itemId))
            {
                continue;
            }

            if (!config.IsItemAllowed(definition.itemId))
            {
                continue;
            }

            if (ResolvePrefab(definition) == null && FindSceneTemplate(definition.itemId) == null)
            {
                continue;
            }

            _resolvedItemIds.Add(definition.itemId);
        }
    }

    private void FillActiveGarbage(int targetCount)
    {
        if (targetCount <= 0 || _resolvedItemIds.Count <= 0)
        {
            return;
        }

        int spawnCount = Mathf.Min(targetCount, _spawnPoints.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            TrySpawnOne();
        }
    }

    private void ReplenishIfNeeded()
    {
        if (config == null)
        {
            return;
        }

        while (_activeItems.Count < config.ActiveGarbageCount)
        {
            if (!TrySpawnOne())
            {
                break;
            }
        }
    }

    private bool TrySpawnOne()
    {
        TimedChallengeSpawnPoint spawnPoint = PickFreeSpawnPoint();
        if (spawnPoint == null || _resolvedItemIds.Count <= 0)
        {
            return false;
        }

        string itemId = _resolvedItemIds[Random.Range(0, _resolvedItemIds.Count)];
        GameObject instance = CreateGarbageInstance(itemId, spawnPoint.Position, spawnPoint.Rotation);
        if (instance == null)
        {
            return false;
        }

        GarbageItem garbageItem = instance.GetComponent<GarbageItem>();
        if (garbageItem == null)
        {
            Destroy(instance);
            return false;
        }

        spawnPoint.SetOccupied(true);
        BindSpawnPoint(instance, spawnPoint);
        instance.transform.SetParent(GetSpawnRoot(), true);
        _activeItems.Add(garbageItem);
        return true;
    }

    private GameObject CreateGarbageInstance(string itemId, Vector3 position, Quaternion rotation)
    {
        TimedChallengeGarbagePoolEntry poolEntry = config.FindPoolEntry(itemId);
        GameObject prefab = ResolvePrefab(poolEntry);
        bool activateAfterInstantiate = false;
        if (prefab == null)
        {
            GarbageItem template = FindSceneTemplate(itemId);
            if (template != null)
            {
                prefab = template.gameObject;
                activateAfterInstantiate = true;
            }
        }

        if (prefab == null)
        {
            GarbageContentDefinition definition = config.FindCatalogDefinition(itemId);
            prefab = ResolvePrefab(definition);
        }

        if (prefab == null)
        {
            Debug.LogWarning("TimedChallengeSpawner: 无法解析垃圾预制体，itemId=" + itemId);
            return null;
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        if (activateAfterInstantiate)
        {
            instance.SetActive(true);
        }

        instance.name = itemId + "_TimedSpawn";
        ApplyGarbageMetadata(instance, itemId, poolEntry);
        EnsureRuntimePhysics(instance);
        return instance;
    }

    private void ApplyGarbageMetadata(GameObject instance, string itemId, TimedChallengeGarbagePoolEntry poolEntry)
    {
        GarbageItem garbageItem = instance.GetComponent<GarbageItem>();
        if (garbageItem == null)
        {
            garbageItem = instance.AddComponent<GarbageItem>();
        }

        string itemName = poolEntry != null ? poolEntry.itemName : string.Empty;
        WasteCategory category = poolEntry != null ? poolEntry.category : WasteCategory.Recyclable;
        string wrongReason = poolEntry != null ? poolEntry.wrongReason : string.Empty;

        GarbageContentDefinition definition = config.FindCatalogDefinition(itemId);
        if (definition != null)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = definition.itemName;
            }

            if (poolEntry == null)
            {
                category = definition.category;
                wrongReason = definition.wrongReason;
            }
        }

        SetGarbageItemFields(garbageItem, itemId, itemName, category, wrongReason);
        SetGarbageStartPose(garbageItem, instance.transform.position, instance.transform.rotation);
    }

    private static void EnsureRuntimePhysics(GameObject target)
    {
        Rigidbody rigidbody = target.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = target.AddComponent<Rigidbody>();
        }

        rigidbody.mass = 0.8f;
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider collider = target.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider boxCollider = target.AddComponent<BoxCollider>();
            boxCollider.size = Vector3.one;
        }

        if (target.GetComponent<ParkClean.Interaction.SelectableHighlighter>() == null)
        {
            target.AddComponent<ParkClean.Interaction.SelectableHighlighter>();
        }
    }

    private static void SetGarbageItemFields(
        GarbageItem garbageItem,
        string itemId,
        string itemName,
        WasteCategory category,
        string wrongReason)
    {
        System.Reflection.FieldInfo itemIdField = typeof(GarbageItem).GetField("itemId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        System.Reflection.FieldInfo itemNameField = typeof(GarbageItem).GetField("itemName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        System.Reflection.FieldInfo categoryField = typeof(GarbageItem).GetField("category", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        System.Reflection.FieldInfo wrongReasonField = typeof(GarbageItem).GetField("wrongReason", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        itemIdField?.SetValue(garbageItem, itemId);
        itemNameField?.SetValue(garbageItem, itemName);
        categoryField?.SetValue(garbageItem, category);
        wrongReasonField?.SetValue(garbageItem, wrongReason);
    }

    private static void SetGarbageStartPose(GarbageItem garbageItem, Vector3 position, Quaternion rotation)
    {
        System.Reflection.FieldInfo startPositionField = typeof(GarbageItem).GetField("_startPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        System.Reflection.FieldInfo startRotationField = typeof(GarbageItem).GetField("_startRotation", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        startPositionField?.SetValue(garbageItem, position);
        startRotationField?.SetValue(garbageItem, rotation);
    }

    private TimedChallengeSpawnPoint PickFreeSpawnPoint()
    {
        TimedChallengeSpawnPoint selected = null;
        int freeCount = 0;

        for (int i = 0; i < _spawnPoints.Count; i++)
        {
            TimedChallengeSpawnPoint point = _spawnPoints[i];
            if (point == null || point.IsOccupied)
            {
                continue;
            }

            freeCount++;
            if (Random.Range(0, freeCount) == 0)
            {
                selected = point;
            }
        }

        return selected;
    }

    private void RemoveActiveItem(GarbageItem item)
    {
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            if (_activeItems[i] == item)
            {
                _activeItems.RemoveAt(i);
            }
        }
    }

    private void ReleaseSpawnPointForItem(GarbageItem item)
    {
        if (item == null)
        {
            return;
        }

        TimedChallengeRuntimeItem runtimeItem = item.GetComponent<TimedChallengeRuntimeItem>();
        if (runtimeItem != null && runtimeItem.SpawnPoint != null)
        {
            runtimeItem.SpawnPoint.SetOccupied(false);
        }
    }

    private static void DestroyItem(GarbageItem item)
    {
        if (item != null)
        {
            Destroy(item.gameObject);
        }
    }

    private Transform GetSpawnRoot()
    {
        if (spawnedItemsRoot == null)
        {
            GameObject root = GameObject.Find("TimedChallengeSpawnedItems");
            if (root == null)
            {
                root = new GameObject("TimedChallengeSpawnedItems");
            }

            spawnedItemsRoot = root.transform;
        }

        return spawnedItemsRoot;
    }

    private static void BindSpawnPoint(GameObject instance, TimedChallengeSpawnPoint spawnPoint)
    {
        if (instance == null || spawnPoint == null)
        {
            return;
        }

        TimedChallengeRuntimeItem runtimeItem = instance.GetComponent<TimedChallengeRuntimeItem>();
        if (runtimeItem == null)
        {
            runtimeItem = instance.AddComponent<TimedChallengeRuntimeItem>();
        }

        runtimeItem.BindSpawnPoint(spawnPoint);
    }

    private static GameObject ResolvePrefab(TimedChallengeGarbagePoolEntry entry)
    {
        return entry != null ? entry.prefab : null;
    }

    private static GameObject ResolvePrefab(GarbageContentDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.assetPath))
        {
            return null;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(definition.assetPath);
#else
        return null;
#endif
    }

    private GarbageItem FindSceneTemplate(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        if (_sceneTemplates.TryGetValue(itemId, out GarbageItem cachedTemplate))
        {
            return cachedTemplate;
        }

        GarbageItem[] items = FindObjectsOfType<GarbageItem>(true);
        for (int i = 0; i < items.Length; i++)
        {
            GarbageItem item = items[i];
            if (item == null || item.ItemId != itemId)
            {
                continue;
            }

            TimedChallengeRuntimeItem runtimeItem = item.GetComponent<TimedChallengeRuntimeItem>();
            if (runtimeItem != null)
            {
                continue;
            }

            if (spawnedItemsRoot != null && item.transform.IsChildOf(spawnedItemsRoot))
            {
                continue;
            }

            _sceneTemplates[itemId] = item;
            return item;
        }

        _sceneTemplates[itemId] = null;
        return null;
    }
}
