using System;
using System.Collections.Generic;

[Serializable]
public sealed class EndlessDifficultyStage
{
    public float startSecond = 0f;
    public string stageName = "阶段 1：基础垃圾";
    public int activeGarbageCount = 6;
    public List<string> availableGarbageItemIds = new List<string>();
}
