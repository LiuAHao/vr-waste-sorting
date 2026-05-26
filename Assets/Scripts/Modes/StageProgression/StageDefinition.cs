using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageSpawnDistribution
{
    NearPlayer = 0,
    HalfMapRandom = 1,
    FullMapRandom = 2
}

[Serializable]
public sealed class StageDefinition
{
    public string stageId = "stage_01";
    public string stageName = "基础分类";
    [Min(1)] public int targetCount = 5;
    [Min(10f)] public float timeLimitSeconds = 180f;
    [Min(1)] public int initialSpawnCount = 5;
    public StageSpawnDistribution spawnDistribution = StageSpawnDistribution.NearPlayer;
    public List<string> availableGarbageItemIds = new List<string>();
    [TextArea(2, 3)] public string nextStagePreview = string.Empty;

    public int ResolvedSpawnCount => Mathf.Max(targetCount, initialSpawnCount);
}
