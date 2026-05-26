using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class EndlessScoreSpawner : MonoBehaviour
{
    private struct SpawnPointRecord
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private static readonly FieldInfo ItemIdField = typeof(GarbageItem).GetField("itemId", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ItemNameField = typeof(GarbageItem).GetField("itemName", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo CategoryField = typeof(GarbageItem).GetField("category", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo WrongReasonField = typeof(GarbageItem).GetField("wrongReason", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo IsCompletedField = typeof(GarbageItem).GetField("_isCompleted", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo IsHeldField = typeof(GarbageItem).GetField("_isHeld", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo StartPositionField = typeof(GarbageItem).GetField("_startPosition", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo StartRotationField = typeof(GarbageItem).GetField("_startRotation", BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly List<GarbageItem> _templates = new List<GarbageItem>();
    private readonly List<GarbageItem> _activeItems = new List<GarbageItem>();
    private readonly List<SpawnPointRecord> _spawnPoints = new List<SpawnPointRecord>();
    private readonly Dictionary<GarbageItem, bool> _templateActiveStates = new Dictionary<GarbageItem, bool>();
    private readonly Dictionary<GarbageItem, int> _spawnPointIndexByItem = new Dictionary<GarbageItem, int>();

    public IReadOnlyList<GarbageItem> ActiveItems => _activeItems;

    public void Initialize(EndlessDifficultyStage stage)
    {
        RestoreScene();
        CaptureSceneItems();
        if (_templates.Count <= 0)
        {
            return;
        }

        HideSceneTemplates();
        EnsureActiveCount(stage);
    }

    public void HandleItemProcessed(GarbageItem item, EndlessDifficultyStage stage)
    {
        if (item != null)
        {
            _activeItems.Remove(item);
            _spawnPointIndexByItem.Remove(item);
            Destroy(item.gameObject);
        }

        CullCompletedItems();
        EnsureActiveCount(stage);
    }

    public void RefreshForStage(EndlessDifficultyStage stage)
    {
        CullCompletedItems();
        EnsureActiveCount(stage);
    }

    public void RestoreScene()
    {
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            GarbageItem activeItem = _activeItems[i];
            if (activeItem != null)
            {
                Destroy(activeItem.gameObject);
            }
        }

        _activeItems.Clear();
        _spawnPointIndexByItem.Clear();

        foreach (KeyValuePair<GarbageItem, bool> pair in _templateActiveStates)
        {
            if (pair.Key != null)
            {
                pair.Key.gameObject.SetActive(pair.Value);
            }
        }
    }

    private void CaptureSceneItems()
    {
        _templates.Clear();
        _activeItems.Clear();
        _spawnPoints.Clear();
        _templateActiveStates.Clear();
        _spawnPointIndexByItem.Clear();

        GarbageItem[] items = FindObjectsOfType<GarbageItem>(true);
        for (int i = 0; i < items.Length; i++)
        {
            GarbageItem item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            _templates.Add(item);
            _templateActiveStates[item] = item.gameObject.activeSelf;
            _spawnPoints.Add(new SpawnPointRecord
            {
                position = item.transform.position,
                rotation = item.transform.rotation
            });
        }
    }

    private void EnsureActiveCount(EndlessDifficultyStage stage)
    {
        int targetCount = ResolveActiveCount(stage);
        int guard = 0;
        while (_activeItems.Count < targetCount && _templates.Count > 0 && guard < 64)
        {
            guard++;
            GarbageItem template = PickTemplate(stage);
            if (template == null)
            {
                return;
            }

            GarbageItem spawned = SpawnFromTemplate(template);
            if (spawned != null)
            {
                _activeItems.Add(spawned);
            }
        }
    }

    private void HideSceneTemplates()
    {
        for (int i = 0; i < _templates.Count; i++)
        {
            GarbageItem item = _templates[i];
            if (item != null)
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private GarbageItem PickTemplate(EndlessDifficultyStage stage)
    {
        List<GarbageItem> candidates = new List<GarbageItem>();
        for (int i = 0; i < _templates.Count; i++)
        {
            GarbageItem template = _templates[i];
            if (template != null && StageAllowsItem(stage, template))
            {
                candidates.Add(template);
            }
        }

        if (candidates.Count <= 0)
        {
            candidates.AddRange(_templates);
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    private GarbageItem SpawnFromTemplate(GarbageItem template)
    {
        int spawnPointIndex = PickSpawnPointIndex();
        Vector3 position = template.transform.position;
        Quaternion rotation = template.transform.rotation;
        if (spawnPointIndex >= 0 && spawnPointIndex < _spawnPoints.Count)
        {
            SpawnPointRecord spawnPoint = _spawnPoints[spawnPointIndex];
            position = spawnPoint.position;
            rotation = spawnPoint.rotation;
        }

        GarbageItem spawned = Instantiate(template, position, rotation);
        spawned.name = template.ItemId + "_EndlessSpawn";
        spawned.gameObject.SetActive(true);
        CopySerializedGarbageData(template, spawned);
        ResetGarbageRuntimeState(spawned, position, rotation);
        EnsureInteractablePhysics(spawned);
        _spawnPointIndexByItem[spawned] = spawnPointIndex;
        return spawned;
    }

    private int PickSpawnPointIndex()
    {
        if (_spawnPoints.Count <= 0)
        {
            return -1;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < _spawnPoints.Count; i++)
        {
            bool isOccupied = false;
            foreach (KeyValuePair<GarbageItem, int> pair in _spawnPointIndexByItem)
            {
                if (pair.Key != null && pair.Value == i)
                {
                    isOccupied = true;
                    break;
                }
            }

            if (!isOccupied)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count <= 0)
        {
            return -1;
        }

        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    private void CullCompletedItems()
    {
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            GarbageItem item = _activeItems[i];
            if (item == null || item.IsCompleted)
            {
                _spawnPointIndexByItem.Remove(item);
                _activeItems.RemoveAt(i);
            }
        }
    }

    private static int ResolveActiveCount(EndlessDifficultyStage stage)
    {
        return stage != null && stage.activeGarbageCount > 0 ? stage.activeGarbageCount : 6;
    }

    private static bool StageAllowsItem(EndlessDifficultyStage stage, GarbageItem item)
    {
        if (stage == null || stage.availableGarbageItemIds == null || stage.availableGarbageItemIds.Count <= 0)
        {
            return true;
        }

        for (int i = 0; i < stage.availableGarbageItemIds.Count; i++)
        {
            if (stage.availableGarbageItemIds[i] == item.ItemId)
            {
                return true;
            }
        }

        return false;
    }

    private static void CopySerializedGarbageData(GarbageItem source, GarbageItem target)
    {
        ItemIdField?.SetValue(target, source.ItemId);
        ItemNameField?.SetValue(target, source.ItemName);
        CategoryField?.SetValue(target, source.Category);
        WrongReasonField?.SetValue(target, source.WrongReason);
    }

    private static void ResetGarbageRuntimeState(GarbageItem item, Vector3 position, Quaternion rotation)
    {
        IsCompletedField?.SetValue(item, false);
        IsHeldField?.SetValue(item, false);
        StartPositionField?.SetValue(item, position);
        StartRotationField?.SetValue(item, rotation);

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true;
            }
        }
    }

    private static void EnsureInteractablePhysics(GarbageItem item)
    {
        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = item.gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        if (colliders.Length <= 0)
        {
            item.gameObject.AddComponent<BoxCollider>().size = Vector3.one;
        }
        else
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = true;
                }
            }
        }
    }
}
