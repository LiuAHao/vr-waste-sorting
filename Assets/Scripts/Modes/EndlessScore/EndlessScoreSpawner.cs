using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public sealed class EndlessScoreSpawner : MonoBehaviour
{
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
    private readonly List<Transform> _spawnPoints = new List<Transform>();

    public IReadOnlyList<GarbageItem> ActiveItems => _activeItems;

    public void Initialize(EndlessDifficultyStage stage)
    {
        CaptureSceneItems();
        HideSceneTemplates();
        EnsureActiveCount(stage);
    }

    public void HandleItemProcessed(GarbageItem item, EndlessDifficultyStage stage)
    {
        if (item != null)
        {
            _activeItems.Remove(item);
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

    private void CaptureSceneItems()
    {
        _templates.Clear();
        _activeItems.Clear();
        _spawnPoints.Clear();

        GarbageItem[] items = FindObjectsOfType<GarbageItem>(true);
        for (int i = 0; i < items.Length; i++)
        {
            GarbageItem item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            _templates.Add(item);

            GameObject spawnPoint = new GameObject("EndlessSpawnPoint_" + item.ItemId);
            spawnPoint.transform.SetPositionAndRotation(item.transform.position, item.transform.rotation);
            _spawnPoints.Add(spawnPoint.transform);
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
        Transform spawnPoint = PickSpawnPoint();
        Vector3 position = spawnPoint != null ? spawnPoint.position : template.transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : template.transform.rotation;
        GarbageItem spawned = Instantiate(template, position, rotation);
        spawned.name = template.ItemId + "_EndlessSpawn";
        spawned.gameObject.SetActive(true);
        CopySerializedGarbageData(template, spawned);
        ResetGarbageRuntimeState(spawned, position, rotation);
        EnsureInteractablePhysics(spawned);
        return spawned;
    }

    private Transform PickSpawnPoint()
    {
        return _spawnPoints.Count > 0 ? _spawnPoints[Random.Range(0, _spawnPoints.Count)] : null;
    }

    private void CullCompletedItems()
    {
        for (int i = _activeItems.Count - 1; i >= 0; i--)
        {
            GarbageItem item = _activeItems[i];
            if (item == null || item.IsCompleted)
            {
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
