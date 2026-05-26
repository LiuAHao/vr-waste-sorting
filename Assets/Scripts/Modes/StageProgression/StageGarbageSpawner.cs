using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class StageGarbageSpawner : MonoBehaviour
{
    [SerializeField] private StageProgressionConfig config;
    [SerializeField] private Transform spawnedItemsRoot;
    [SerializeField] private float minSpawnDistanceFromPlayer = 2.5f;
    [SerializeField] private float maxSpawnDistanceFromPlayer = 8f;
    [SerializeField] private float minSeparationBetweenItems = 1.2f;
    [SerializeField] private float spawnSurfaceOffset = 0.08f;
    [SerializeField] private float groundRaycastHeight = 24f;
    [SerializeField] private float maxGroundSlopeDot = 0.65f;
    [SerializeField] private int maxPlacementAttemptsPerItem = 36;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Vector3 fallbackSpawnOrigin = new Vector3(80f, 0.6f, 48f);
    [SerializeField] private Vector3 mapBoundsCenter = new Vector3(80f, 0.6f, 52f);
    [SerializeField] private Vector2 mapFullHalfExtents = new Vector2(28f, 32f);
    [SerializeField] private int mapPlacementAttempts = 48;
    [SerializeField] private float mediumSpawnRadius = 15f;
    [SerializeField] private int maxTotalSpawnAttempts = 600;
    [SerializeField] private float minSpawnDistanceFromBins = 2.5f;
    [SerializeField] private bool autoDetectPlayableBounds = true;
    [SerializeField] private float playableBoundsPadding = 0.5f;

    private readonly List<GarbageItem> _activeItems = new List<GarbageItem>();
    private readonly List<Vector3> _binExclusionCenters = new List<Vector3>();
    private readonly List<string> _resolvedItemIds = new List<string>();
    private readonly Dictionary<string, GarbageItem> _sceneTemplates = new Dictionary<string, GarbageItem>();

    private StageSpawnDistribution _activeDistribution = StageSpawnDistribution.NearPlayer;
    private bool _mapBoundsResolved;
    private Vector3 _resolvedMapCenter;
    private Vector2 _resolvedMapHalfExtents;
    private Vector3 _sessionPlayerSpawnOrigin;

    public IReadOnlyList<GarbageItem> ActiveItems => _activeItems;

    public void Configure(StageProgressionConfig progressionConfig)
    {
        if (progressionConfig != null)
        {
            config = progressionConfig;
        }

        _mapBoundsResolved = false;
    }

    public void SetMapBounds(Vector3 center, Vector2 fullHalfExtents)
    {
        mapBoundsCenter = center;
        mapFullHalfExtents = new Vector2(
            Mathf.Max(1f, fullHalfExtents.x),
            Mathf.Max(1f, fullHalfExtents.y));
        _mapBoundsResolved = false;
    }

    public int SpawnStage(StageDefinition stage)
    {
        ClearSpawnedItems();
        CacheScenePrototypes();
        BuildResolvedItemPool(stage);

        if (stage == null || _resolvedItemIds.Count <= 0)
        {
            Debug.LogWarning("StageGarbageSpawner: 本关没有可生成的垃圾，请检查关卡垃圾池与内容目录配置。");
            return 0;
        }

        _activeDistribution = stage.spawnDistribution;

        Vector3 playerOrigin = ResolveSpawnOrigin();
        Vector3 playerForward = ResolvePlayerForward();
        _sessionPlayerSpawnOrigin = playerOrigin;
        _mapBoundsResolved = false;
        ResolveMapBounds();
        CacheBinExclusionCenters();
        int desiredCount = stage.ResolvedSpawnCount;
        int spawnedCount = 0;
        int totalAttempts = 0;
        int attemptBudget = ResolveTotalSpawnAttemptBudget(desiredCount);
        List<Vector3> placedPositions = new List<Vector3>(desiredCount);

        while (spawnedCount < desiredCount && totalAttempts < attemptBudget)
        {
            totalAttempts++;
            string itemId = PickRandomItemId();
            if (!TryFindSpawnPosition(playerOrigin, playerForward, placedPositions, out Vector3 position))
            {
                continue;
            }

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (!TrySpawnAt(itemId, position, rotation))
            {
                continue;
            }

            placedPositions.Add(position);
            spawnedCount++;
        }

        if (spawnedCount < desiredCount)
        {
            Debug.LogWarning(
                "StageGarbageSpawner: 本关计划生成 " + desiredCount + " 件垃圾，实际生成 " + spawnedCount
                + " 件（已尝试 " + totalAttempts + " 次）。请检查场景模板、Catalog 资源路径或可放置地面范围。");
        }

        return spawnedCount;
    }

    private int ResolveTotalSpawnAttemptBudget(int desiredCount)
    {
        int perItemAttempts = _activeDistribution == StageSpawnDistribution.NearPlayer
            ? maxPlacementAttemptsPerItem
            : Mathf.Max(maxPlacementAttemptsPerItem, mapPlacementAttempts);

        int scaledBudget = Mathf.Max(1, desiredCount) * perItemAttempts * 4;
        return Mathf.Max(maxTotalSpawnAttempts, scaledBudget);
    }

    public void HandleItemProcessed(GarbageItem item)
    {
        if (item == null)
        {
            return;
        }

        RemoveActiveItem(item);
        DestroyItem(item);
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
    }

    private Vector3 ResolveSpawnOrigin()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            return player.transform.position;
        }

        return fallbackSpawnOrigin;
    }

    private Vector3 ResolvePlayerForward()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                return forward.normalized;
            }
        }

        return Vector3.forward;
    }

    private string PickRandomItemId()
    {
        return _resolvedItemIds[Random.Range(0, _resolvedItemIds.Count)];
    }

    private bool TryFindSpawnPosition(
        Vector3 playerOrigin,
        Vector3 playerForward,
        List<Vector3> placedPositions,
        out Vector3 position)
    {
        switch (_activeDistribution)
        {
            case StageSpawnDistribution.HalfMapRandom:
                return TryFindRandomMapCenterCirclePosition(playerOrigin, placedPositions, out position);
            case StageSpawnDistribution.FullMapRandom:
                return TryFindRandomMapGroundPosition(playerOrigin, fullMap: true, placedPositions, out position);
            default:
                return TryFindRandomNearPlayerGroundPosition(playerOrigin, playerForward, placedPositions, out position);
        }
    }

    private bool TryFindRandomNearPlayerGroundPosition(
        Vector3 origin,
        Vector3 playerForward,
        List<Vector3> placedPositions,
        out Vector3 position)
    {
        position = Vector3.zero;
        float minDistance = Mathf.Max(1f, minSpawnDistanceFromPlayer);
        float maxDistance = Mathf.Max(minDistance + 0.5f, maxSpawnDistanceFromPlayer);
        float minSeparation = Mathf.Max(0.6f, minSeparationBetweenItems);

        for (int attempt = 0; attempt < maxPlacementAttemptsPerItem; attempt++)
        {
            float relativeAngle = Random.Range(90f, 270f);
            Vector3 direction = Quaternion.AngleAxis(relativeAngle, Vector3.up) * playerForward;
            float distance = Random.Range(minDistance, maxDistance);
            Vector3 sample = origin + direction.normalized * distance;

            if (!TryProjectToGroundForPlayerSpawn(sample, origin.y, out Vector3 candidate))
            {
                continue;
            }

            Vector3 flatOffset = candidate - origin;
            flatOffset.y = 0f;
            if (flatOffset.sqrMagnitude > 0.0001f && Vector3.Dot(flatOffset.normalized, playerForward) > 0.05f)
            {
                continue;
            }

            float horizontalDistance = flatOffset.magnitude;
            if (horizontalDistance < minDistance || horizontalDistance > maxDistance + 0.5f)
            {
                continue;
            }

            if (IsTooCloseToBin(candidate))
            {
                continue;
            }

            if (!HasEnoughSeparation(candidate, placedPositions, minSeparation))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    private bool TryFindRandomMapCenterCirclePosition(
        Vector3 playerOrigin,
        List<Vector3> placedPositions,
        out Vector3 position)
    {
        position = Vector3.zero;
        float radius = Mathf.Max(1f, mediumSpawnRadius);
        float minSeparation = Mathf.Max(0.6f, minSeparationBetweenItems);
        float raycastBaseY = playerOrigin.y;
        int attempts = Mathf.Max(maxPlacementAttemptsPerItem, mapPlacementAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            if (!TrySampleMapCenterCircleCoordinate(radius, out float sampleX, out float sampleZ))
            {
                continue;
            }

            Vector3 sample = new Vector3(sampleX, raycastBaseY, sampleZ);
            if (!TryProjectToGroundForPlayerSpawn(sample, raycastBaseY, out Vector3 candidate))
            {
                continue;
            }

            if (!IsInsideMapCenterCircle(candidate, radius))
            {
                continue;
            }

            if (IsTooCloseToBin(candidate))
            {
                continue;
            }

            if (!HasEnoughSeparation(candidate, placedPositions, minSeparation))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    private bool TryFindRandomMapGroundPosition(
        Vector3 playerOrigin,
        bool fullMap,
        List<Vector3> placedPositions,
        out Vector3 position)
    {
        position = Vector3.zero;
        float minSeparation = Mathf.Max(0.6f, minSeparationBetweenItems);
        float raycastBaseY = playerOrigin.y;
        int attempts = Mathf.Max(maxPlacementAttemptsPerItem, mapPlacementAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            if (!TrySampleMapCoordinate(fullMap, out float sampleX, out float sampleZ))
            {
                continue;
            }

            Vector3 sample = new Vector3(sampleX, raycastBaseY, sampleZ);
            if (!TryProjectToGroundForPlayerSpawn(sample, raycastBaseY, out Vector3 candidate))
            {
                continue;
            }

            if (!IsInsideMapBounds(candidate, fullMap))
            {
                continue;
            }

            if (IsTooCloseToBin(candidate))
            {
                continue;
            }

            if (!HasEnoughSeparation(candidate, placedPositions, minSeparation))
            {
                continue;
            }

            position = candidate;
            return true;
        }

        return false;
    }

    private bool TrySampleMapCenterCircleCoordinate(float radius, out float sampleX, out float sampleZ)
    {
        sampleX = 0f;
        sampleZ = 0f;
        if (radius <= 0f)
        {
            return false;
        }

        float radiusSq = radius * radius;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float offsetX = Random.Range(-radius, radius);
            float offsetZ = Random.Range(-radius, radius);
            if (offsetX * offsetX + offsetZ * offsetZ > radiusSq)
            {
                continue;
            }

            sampleX = _resolvedMapCenter.x + offsetX;
            sampleZ = _resolvedMapCenter.z + offsetZ;
            return true;
        }

        return false;
    }

    private bool IsInsideMapCenterCircle(Vector3 candidate, float radius)
    {
        float tolerance = 0.5f;
        Vector3 offset = candidate - _resolvedMapCenter;
        offset.y = 0f;
        return offset.sqrMagnitude <= (radius + tolerance) * (radius + tolerance);
    }

    private bool TrySampleMapCoordinate(bool fullMap, out float sampleX, out float sampleZ)
    {
        sampleX = 0f;
        sampleZ = 0f;

        float halfWidth = _resolvedMapHalfExtents.x;
        float halfDepth = _resolvedMapHalfExtents.y;
        if (halfWidth <= 0f || halfDepth <= 0f)
        {
            return false;
        }

        sampleX = Random.Range(_resolvedMapCenter.x - halfWidth, _resolvedMapCenter.x + halfWidth);

        if (fullMap)
        {
            sampleZ = Random.Range(_resolvedMapCenter.z - halfDepth, _resolvedMapCenter.z + halfDepth);
            return true;
        }

        bool playerInUpperHalf = _sessionPlayerSpawnOrigin.z >= _resolvedMapCenter.z;
        float zMin = playerInUpperHalf ? _resolvedMapCenter.z : _resolvedMapCenter.z - halfDepth;
        float zMax = playerInUpperHalf ? _resolvedMapCenter.z + halfDepth : _resolvedMapCenter.z;
        sampleZ = Random.Range(zMin, zMax);
        return true;
    }

    private bool IsInsideMapBounds(Vector3 candidate, bool fullMap)
    {
        float halfWidth = _resolvedMapHalfExtents.x;
        float halfDepth = _resolvedMapHalfExtents.y;
        float dx = Mathf.Abs(candidate.x - _resolvedMapCenter.x);
        if (dx > halfWidth + 0.5f)
        {
            return false;
        }

        float dz = Mathf.Abs(candidate.z - _resolvedMapCenter.z);
        if (fullMap)
        {
            return dz <= halfDepth + 0.5f;
        }

        bool playerInUpperHalf = _sessionPlayerSpawnOrigin.z >= _resolvedMapCenter.z;
        if (playerInUpperHalf)
        {
            return candidate.z >= _resolvedMapCenter.z - 0.5f
                && candidate.z <= _resolvedMapCenter.z + halfDepth + 0.5f;
        }

        return candidate.z <= _resolvedMapCenter.z + 0.5f
            && candidate.z >= _resolvedMapCenter.z - halfDepth - 0.5f;
    }

    private bool TryProjectToGround(Vector3 sample, float raycastBaseY, LayerMask mask, out Vector3 position)
    {
        position = Vector3.zero;
        Vector3 rayOrigin = new Vector3(sample.x, raycastBaseY + groundRaycastHeight, sample.z);
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRaycastHeight + 10f, mask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (hit.normal.y < maxGroundSlopeDot)
        {
            return false;
        }

        position = hit.point + hit.normal * spawnSurfaceOffset;
        return true;
    }

    private bool TryProjectToGroundForPlayerSpawn(Vector3 sample, float raycastBaseY, out Vector3 position)
    {
        position = Vector3.zero;
        LayerMask mask = ResolvePlayerSpawnGroundMask();
        Vector3 rayOrigin = new Vector3(sample.x, raycastBaseY + groundRaycastHeight, sample.z);
        float rayLength = groundRaycastHeight + 10f;
        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            rayLength,
            mask,
            QueryTriggerInteraction.Ignore);

        if (hits.Length <= 0)
        {
            return TryProjectToGround(sample, raycastBaseY, mask, out position);
        }

        bool found = false;
        float lowestGroundY = float.MaxValue;
        Vector3 bestPoint = Vector3.zero;
        Vector3 bestNormal = Vector3.up;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.normal.y < maxGroundSlopeDot)
            {
                continue;
            }

            if (hit.point.y >= lowestGroundY)
            {
                continue;
            }

            lowestGroundY = hit.point.y;
            bestPoint = hit.point;
            bestNormal = hit.normal;
            found = true;
        }

        if (!found)
        {
            return false;
        }

        position = bestPoint + bestNormal * spawnSurfaceOffset;
        return true;
    }

    private LayerMask ResolvePlayerSpawnGroundMask()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            return 1 << groundLayer;
        }

        return ResolveGroundMask();
    }

    private void ResolveMapBounds()
    {
        if (_mapBoundsResolved)
        {
            return;
        }

        if (autoDetectPlayableBounds && TryComputePlayableBoundsFromGround(out Vector3 center, out Vector2 halfExtents))
        {
            _resolvedMapCenter = new Vector3(center.x, mapBoundsCenter.y, center.z);
            _resolvedMapHalfExtents = halfExtents;
        }
        else
        {
            _resolvedMapCenter = mapBoundsCenter;
            _resolvedMapHalfExtents = new Vector2(
                Mathf.Max(1f, mapFullHalfExtents.x),
                Mathf.Max(1f, mapFullHalfExtents.y));
        }

        _mapBoundsResolved = true;
    }

    private bool TryComputePlayableBoundsFromGround(out Vector3 center, out Vector2 halfExtents)
    {
        center = mapBoundsCenter;
        halfExtents = Vector2.zero;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            return false;
        }

        Collider[] colliders = FindObjectsOfType<Collider>(true);
        bool hasBounds = false;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            if (collider.gameObject.layer != groundLayer)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            if (bounds.size.sqrMagnitude < 0.01f)
            {
                continue;
            }

            hasBounds = true;
            minX = Mathf.Min(minX, bounds.min.x);
            maxX = Mathf.Max(maxX, bounds.max.x);
            minZ = Mathf.Min(minZ, bounds.min.z);
            maxZ = Mathf.Max(maxZ, bounds.max.z);
        }

        if (!hasBounds || maxX <= minX || maxZ <= minZ)
        {
            return false;
        }

        float padding = Mathf.Max(0f, playableBoundsPadding);
        minX += padding;
        maxX -= padding;
        minZ += padding;
        maxZ -= padding;

        center = new Vector3((minX + maxX) * 0.5f, mapBoundsCenter.y, (minZ + maxZ) * 0.5f);
        halfExtents = new Vector2(
            Mathf.Max(5f, (maxX - minX) * 0.5f),
            Mathf.Max(5f, (maxZ - minZ) * 0.5f));
        return true;
    }

    private void CacheBinExclusionCenters()
    {
        _binExclusionCenters.Clear();

        TrashBin[] bins = FindObjectsOfType<TrashBin>(true);
        for (int i = 0; i < bins.Length; i++)
        {
            TrashBin bin = bins[i];
            if (bin != null)
            {
                _binExclusionCenters.Add(bin.transform.position);
            }
        }
    }

    private bool IsTooCloseToBin(Vector3 candidate)
    {
        if (_binExclusionCenters.Count <= 0)
        {
            return false;
        }

        float minDistance = Mathf.Max(1f, minSpawnDistanceFromBins);
        for (int i = 0; i < _binExclusionCenters.Count; i++)
        {
            Vector3 binPosition = _binExclusionCenters[i];
            float distance = Vector3.Distance(
                new Vector3(candidate.x, 0f, candidate.z),
                new Vector3(binPosition.x, 0f, binPosition.z));

            if (distance < minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasEnoughSeparation(Vector3 candidate, List<Vector3> placedPositions, float minSeparation)
    {
        for (int i = 0; i < placedPositions.Count; i++)
        {
            Vector3 placed = placedPositions[i];
            float distance = Vector3.Distance(
                new Vector3(candidate.x, 0f, candidate.z),
                new Vector3(placed.x, 0f, placed.z));

            if (distance < minSeparation)
            {
                return false;
            }
        }

        return true;
    }

    private LayerMask ResolveGroundMask()
    {
        if (groundMask.value != 0)
        {
            return groundMask;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            return LayerMask.GetMask("Ground", "Default");
        }

        return Physics.DefaultRaycastLayers;
    }

    private bool TrySpawnAt(string itemId, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        GameObject instance = CreateGarbageInstance(itemId, position, rotation);
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

        instance.transform.SetParent(GetSpawnRoot(), true);
        _activeItems.Add(garbageItem);
        return true;
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

            if (item.GetComponent<TimedChallengeRuntimeItem>() != null)
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

    private void BuildResolvedItemPool(StageDefinition stage)
    {
        _resolvedItemIds.Clear();

        if (config == null || stage == null)
        {
            return;
        }

        if (stage.availableGarbageItemIds != null && stage.availableGarbageItemIds.Count > 0)
        {
            for (int i = 0; i < stage.availableGarbageItemIds.Count; i++)
            {
                string itemId = stage.availableGarbageItemIds[i];
                if (string.IsNullOrWhiteSpace(itemId) || !config.IsItemAllowed(stage, itemId))
                {
                    continue;
                }

                if (CanSpawnItem(itemId))
                {
                    _resolvedItemIds.Add(itemId);
                }
            }

            if (_resolvedItemIds.Count > 0)
            {
                return;
            }
        }

        if (config.ContentCatalog == null)
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

            if (!config.IsItemAllowed(stage, definition.itemId))
            {
                continue;
            }

            if (CanSpawnItem(definition.itemId))
            {
                _resolvedItemIds.Add(definition.itemId);
            }
        }
    }

    private bool CanSpawnItem(string itemId)
    {
        if (FindSceneTemplate(itemId) != null)
        {
            return true;
        }

        GarbageContentDefinition definition = config.FindCatalogDefinition(itemId);
        return ResolvePrefab(definition) != null;
    }

    private GameObject CreateGarbageInstance(string itemId, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = null;
        bool activateAfterInstantiate = false;

        GarbageItem template = FindSceneTemplate(itemId);
        if (template != null)
        {
            prefab = template.gameObject;
            activateAfterInstantiate = true;
        }

        if (prefab == null)
        {
            GarbageContentDefinition definition = config.FindCatalogDefinition(itemId);
            prefab = ResolvePrefab(definition);
        }

        if (prefab == null)
        {
            Debug.LogWarning("StageGarbageSpawner: 无法解析垃圾预制体，itemId=" + itemId);
            return null;
        }

        GameObject instance = Instantiate(prefab, position, rotation);
        if (activateAfterInstantiate)
        {
            instance.SetActive(true);
        }

        instance.name = itemId + "_StageSpawn";
        ApplyGarbageMetadata(instance, itemId);
        EnsureRuntimePhysics(instance);
        SnapToGround(instance);

        GarbageItem garbageItem = instance.GetComponent<GarbageItem>();
        if (garbageItem != null)
        {
            SetGarbageStartPose(garbageItem, instance.transform.position, instance.transform.rotation);
        }

        return instance;
    }

    private void SnapToGround(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Vector3 position = instance.transform.position;
        if (TryProjectToGroundForPlayerSpawn(position, position.y + 0.5f, out Vector3 groundedPosition))
        {
            instance.transform.position = groundedPosition;
        }

        Rigidbody rigidbody = instance.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.Sleep();
        }
    }

    private void ApplyGarbageMetadata(GameObject instance, string itemId)
    {
        GarbageItem garbageItem = instance.GetComponent<GarbageItem>();
        if (garbageItem == null)
        {
            garbageItem = instance.AddComponent<GarbageItem>();
        }

        GarbageContentDefinition definition = config.FindCatalogDefinition(itemId);
        string itemName = definition != null ? definition.itemName : itemId;
        WasteCategory category = definition != null ? definition.category : WasteCategory.Other;
        string wrongReason = definition != null ? definition.wrongReason : string.Empty;

        SetGarbageItemFields(garbageItem, itemId, itemName, category, wrongReason);
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
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (target.GetComponent<Collider>() == null)
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
            GameObject root = GameObject.Find("StageProgressionSpawnedItems");
            if (root == null)
            {
                root = new GameObject("StageProgressionSpawnedItems");
            }

            spawnedItemsRoot = root.transform;
        }

        return spawnedItemsRoot;
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

            if (item.GetComponent<TimedChallengeRuntimeItem>() != null)
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
