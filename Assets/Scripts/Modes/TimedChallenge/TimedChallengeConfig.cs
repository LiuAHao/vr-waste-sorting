using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TimedChallengeConfig", menuName = "ParkClean/Timed Challenge Config")]
public sealed class TimedChallengeConfig : ScriptableObject
{
    [Header("Session")]
    [SerializeField] private float timeLimitSeconds = 120f;
    [SerializeField] private int activeGarbageCount = 10;

    [Header("Scoring")]
    [SerializeField] private int scorePerCorrect = 100;
    [SerializeField] private int penaltyPerWrong = 25;

    [Header("Spawn")]
    [SerializeField] private string spawnPointGroupId = "default";
    [SerializeField] private WasteContentCatalog contentCatalog;
    [SerializeField] private List<string> availableGarbageItemIds = new List<string>();
    [SerializeField] private List<TimedChallengeGarbagePoolEntry> garbagePool = new List<TimedChallengeGarbagePoolEntry>();

    public float TimeLimitSeconds => Mathf.Max(10f, timeLimitSeconds);
    public int ActiveGarbageCount => Mathf.Max(1, activeGarbageCount);
    public int ScorePerCorrect => scorePerCorrect;
    public int PenaltyPerWrong => penaltyPerWrong;
    public string SpawnPointGroupId => spawnPointGroupId;
    public WasteContentCatalog ContentCatalog => contentCatalog;
    public IReadOnlyList<string> AvailableGarbageItemIds => availableGarbageItemIds;
    public IReadOnlyList<TimedChallengeGarbagePoolEntry> GarbagePool => garbagePool;

    public bool IsItemAllowed(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (availableGarbageItemIds == null || availableGarbageItemIds.Count <= 0)
        {
            return true;
        }

        for (int i = 0; i < availableGarbageItemIds.Count; i++)
        {
            if (availableGarbageItemIds[i] == itemId)
            {
                return true;
            }
        }

        return false;
    }

    public GarbageContentDefinition FindCatalogDefinition(string itemId)
    {
        if (contentCatalog == null || string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        IReadOnlyList<GarbageContentDefinition> items = contentCatalog.GarbageItems;
        for (int i = 0; i < items.Count; i++)
        {
            GarbageContentDefinition definition = items[i];
            if (definition != null && definition.itemId == itemId)
            {
                return definition;
            }
        }

        return null;
    }

    public TimedChallengeGarbagePoolEntry FindPoolEntry(string itemId)
    {
        for (int i = 0; i < garbagePool.Count; i++)
        {
            TimedChallengeGarbagePoolEntry entry = garbagePool[i];
            if (entry != null && entry.itemId == itemId)
            {
                return entry;
            }
        }

        return null;
    }
}

[System.Serializable]
public sealed class TimedChallengeGarbagePoolEntry
{
    public string itemId;
    public string itemName;
    public WasteCategory category;
    [TextArea(2, 4)] public string wrongReason;
    public GameObject prefab;
}

public sealed class TimedChallengeRuntimeItem : MonoBehaviour
{
    [SerializeField] private TimedChallengeSpawnPoint spawnPoint;

    public TimedChallengeSpawnPoint SpawnPoint => spawnPoint;

    public void BindSpawnPoint(TimedChallengeSpawnPoint point)
    {
        spawnPoint = point;
    }
}
