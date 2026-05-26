using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageProgressionConfig", menuName = "ParkClean/Stage Progression Config")]
public sealed class StageProgressionConfig : ScriptableObject
{
    [Header("Session")]
    [SerializeField] private string modeDisplayName = "标准闯关";

    [Header("Scoring")]
    [SerializeField] private int scorePerCorrect = 100;
    [SerializeField] private int penaltyPerWrong = 25;

    [Header("Spawn")]
    [SerializeField] private string spawnPointGroupId = "stage";
    [SerializeField] private WasteContentCatalog contentCatalog;

    [Header("Stages")]
    [SerializeField] private List<StageDefinition> stages = new List<StageDefinition>();

    [Header("Transition")]
    [SerializeField] private float stageTransitionSeconds = 2.5f;

    public string ModeDisplayName => modeDisplayName;
    public int ScorePerCorrect => scorePerCorrect;
    public int PenaltyPerWrong => penaltyPerWrong;
    public string SpawnPointGroupId => spawnPointGroupId;
    public WasteContentCatalog ContentCatalog => contentCatalog;
    public float StageTransitionSeconds => Mathf.Max(0.5f, stageTransitionSeconds);
    public IReadOnlyList<StageDefinition> Stages => stages;

    public int StageCount => stages != null ? stages.Count : 0;

    public StageDefinition GetStage(int index)
    {
        if (stages == null || index < 0 || index >= stages.Count)
        {
            return null;
        }

        return stages[index];
    }

    public bool IsItemAllowed(StageDefinition stage, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || stage == null)
        {
            return false;
        }

        if (stage.availableGarbageItemIds == null || stage.availableGarbageItemIds.Count <= 0)
        {
            return true;
        }

        for (int i = 0; i < stage.availableGarbageItemIds.Count; i++)
        {
            if (stage.availableGarbageItemIds[i] == itemId)
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
}
