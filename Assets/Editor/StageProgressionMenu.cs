using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class StageProgressionMenu
{
    private const string ConfigAssetPath = "Assets/Data/StageProgressionConfig.asset";
    private const string CatalogAssetPath = "Assets/Data/WasteContentCatalog.asset";

    [MenuItem("垃圾分类项目/内容配置/创建标准闯关配置")]
    public static void CreateDefaultStageProgressionConfig()
    {
        WasteContentMenu.CreateDefaultCatalog();

        StageProgressionConfig existing = AssetDatabase.LoadAssetAtPath<StageProgressionConfig>(ConfigAssetPath);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log("已存在标准闯关配置，未覆盖现有数据：" + ConfigAssetPath);
            return;
        }

        WasteContentCatalog catalog = AssetDatabase.LoadAssetAtPath<WasteContentCatalog>(CatalogAssetPath);
        StageProgressionConfig config = ScriptableObject.CreateInstance<StageProgressionConfig>();
        AssetDatabase.CreateAsset(config, ConfigAssetPath);
        FillDefaultStages(config, catalog);

        Selection.activeObject = config;
        Debug.Log("已创建默认标准闯关配置：" + ConfigAssetPath);
    }

    [MenuItem("垃圾分类项目/场景工具/添加标准闯关场景组件")]
    public static void AddStageProgressionSceneSetup()
    {
        StageProgressionSceneSetup existing = Object.FindObjectOfType<StageProgressionSceneSetup>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("场景中已存在 StageProgressionSceneSetup。");
            return;
        }

        StageProgressionConfig config = AssetDatabase.LoadAssetAtPath<StageProgressionConfig>(ConfigAssetPath);
        if (config == null)
        {
            CreateDefaultStageProgressionConfig();
            config = AssetDatabase.LoadAssetAtPath<StageProgressionConfig>(ConfigAssetPath);
        }

        GameObject host = new GameObject("StageProgression");
        StageProgressionSceneSetup setup = host.AddComponent<StageProgressionSceneSetup>();
        SerializedObject serializedSetup = new SerializedObject(setup);
        serializedSetup.FindProperty("config").objectReferenceValue = config;
        serializedSetup.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = host;
        Undo.RegisterCreatedObjectUndo(host, "Add Stage Progression Setup");
        Debug.Log("已在当前场景创建 StageProgression 对象，请保存场景。");
    }

    private static void FillDefaultStages(StageProgressionConfig config, WasteContentCatalog catalog)
    {
        SerializedObject serializedConfig = new SerializedObject(config);
        serializedConfig.FindProperty("contentCatalog").objectReferenceValue = catalog;
        serializedConfig.FindProperty("spawnPointGroupId").stringValue = "stage";
        serializedConfig.FindProperty("modeDisplayName").stringValue = "标准闯关";
        serializedConfig.FindProperty("scorePerCorrect").intValue = 100;
        serializedConfig.FindProperty("penaltyPerWrong").intValue = 25;
        serializedConfig.FindProperty("stageTransitionSeconds").floatValue = 2.5f;

        SerializedProperty stages = serializedConfig.FindProperty("stages");
        stages.ClearArray();

        AddStage(
            stages,
            "difficulty_easy",
            "难度：简单",
            5,
            120f,
            5,
            StageSpawnDistribution.NearPlayer,
            new List<string>
            {
                "garbage_plastic_bottle",
                "garbage_cardboard_box",
                "garbage_leftover_rice",
                "garbage_fruit_peel"
            },
            "简单难度以常见四类垃圾为主，适合初学者。");

        AddStage(
            stages,
            "difficulty_medium",
            "难度：中等",
            5,
            180f,
            5,
            StageSpawnDistribution.HalfMapRandom,
            new List<string>
            {
                "garbage_plastic_bottle",
                "garbage_cardboard_box",
                "garbage_aluminum_can",
                "garbage_leftover_rice",
                "garbage_fruit_peel",
                "garbage_battery",
                "garbage_dirty_tissue"
            },
            "中等难度加入更多类别，目标与时间压力更大。");

        AddStage(
            stages,
            "difficulty_hard",
            "难度：困难",
            5,
            240f,
            5,
            StageSpawnDistribution.FullMapRandom,
            new List<string>
            {
                "garbage_dirty_tissue",
                "garbage_milk_tea_cup",
                "garbage_oily_takeout_box",
                "garbage_battery",
                "garbage_expired_medicine",
                "garbage_vegetable_leaf",
                "garbage_aluminum_can"
            },
            "困难难度含易混淆垃圾，时间更紧。");

        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void AddStage(
        SerializedProperty stages,
        string stageId,
        string stageName,
        int targetCount,
        float timeLimitSeconds,
        int initialSpawnCount,
        StageSpawnDistribution spawnDistribution,
        List<string> itemIds,
        string nextStagePreview)
    {
        int index = stages.arraySize;
        stages.InsertArrayElementAtIndex(index);
        SerializedProperty stage = stages.GetArrayElementAtIndex(index);
        stage.FindPropertyRelative("stageId").stringValue = stageId;
        stage.FindPropertyRelative("stageName").stringValue = stageName;
        stage.FindPropertyRelative("targetCount").intValue = targetCount;
        stage.FindPropertyRelative("timeLimitSeconds").floatValue = timeLimitSeconds;
        stage.FindPropertyRelative("initialSpawnCount").intValue = initialSpawnCount;
        stage.FindPropertyRelative("spawnDistribution").enumValueIndex = (int)spawnDistribution;
        stage.FindPropertyRelative("nextStagePreview").stringValue = nextStagePreview;

        SerializedProperty availableIds = stage.FindPropertyRelative("availableGarbageItemIds");
        availableIds.ClearArray();
        for (int i = 0; i < itemIds.Count; i++)
        {
            int itemIndex = availableIds.arraySize;
            availableIds.InsertArrayElementAtIndex(itemIndex);
            availableIds.GetArrayElementAtIndex(itemIndex).stringValue = itemIds[i];
        }
    }
}
