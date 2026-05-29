using UnityEditor;
using UnityEngine;

public static class WasteContentMenu
{
    private const string CatalogAssetPath = "Assets/Data/WasteContentCatalog.asset";

    [MenuItem("垃圾分类项目/内容配置/创建内容目录配置")]
    public static void CreateDefaultCatalog()
    {
        EnsureFolder("Assets/Data");

        WasteContentCatalog catalog = AssetDatabase.LoadAssetAtPath<WasteContentCatalog>(CatalogAssetPath);
        if (catalog != null)
        {
            Selection.activeObject = catalog;
            Debug.Log("已存在内容目录配置，未覆盖现有数据：" + CatalogAssetPath);
            return;
        }

        catalog = ScriptableObject.CreateInstance<WasteContentCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
        FillCatalogWithDefaults(catalog);

        Selection.activeObject = catalog;
        Debug.Log("已创建默认垃圾分类内容目录配置：" + CatalogAssetPath);
    }

    private static void FillCatalogWithDefaults(WasteContentCatalog catalog)
    {
        SerializedObject serializedCatalog = new SerializedObject(catalog);
        SerializedProperty garbageItems = serializedCatalog.FindProperty("garbageItems");
        SerializedProperty trashBins = serializedCatalog.FindProperty("trashBins");

        garbageItems.ClearArray();
        trashBins.ClearArray();

        AddGarbage(garbageItems, "garbage_plastic_bottle", "塑料瓶", WasteCategory.Recyclable, "干净塑料瓶属于可回收物。", "Assets/Art/GarbageItems/garbage_plastic_bottle.glb", new Vector3(-6f, 0.6f, 2f), Vector3.zero, new Vector3(1.05f, 1.05f, 1.05f));
        AddGarbage(garbageItems, "garbage_cardboard_box", "纸箱", WasteCategory.Recyclable, "干净纸箱可以回收再利用。", "Assets/Art/GarbageItems/garbage_cardboard_box.glb", new Vector3(-4f, 0.6f, 2f), Vector3.zero, new Vector3(0.8f, 0.8f, 0.8f));
        AddGarbage(garbageItems, "garbage_aluminum_can", "易拉罐", WasteCategory.Recyclable, "易拉罐属于可回收物。", "Assets/Art/GarbageItems/garbage_aluminum_can.glb", new Vector3(-2f, 0.6f, 2f), Vector3.zero, new Vector3(1f, 1f, 1f));
        AddGarbage(garbageItems, "garbage_leftover_rice", "剩饭", WasteCategory.Kitchen, "剩饭属于厨余垃圾。", "Assets/Art/GarbageItems/garbage_leftover_rice.glb", new Vector3(0f, 0.6f, 2f), Vector3.zero, new Vector3(0.72f, 0.72f, 0.72f));
        AddGarbage(garbageItems, "garbage_fruit_peel", "果皮", WasteCategory.Kitchen, "果皮易腐烂，属于厨余垃圾。", "Assets/Art/GarbageItems/garbage_fruit_peel.glb", new Vector3(2f, 0.6f, 2f), Vector3.zero, new Vector3(0.72f, 0.72f, 0.72f));
        AddGarbage(garbageItems, "garbage_vegetable_leaf", "菜叶", WasteCategory.Kitchen, "菜叶属于厨余垃圾。", "Assets/Art/GarbageItems/garbage_vegetable_leaf.glb", new Vector3(4f, 0.6f, 2f), Vector3.zero, new Vector3(0.68f, 0.68f, 0.68f));
        AddGarbage(garbageItems, "garbage_battery", "旧电池", WasteCategory.Hazardous, "旧电池可能污染环境，属于有害垃圾。", "Assets/Art/GarbageItems/garbage_battery.glb", new Vector3(-6f, 0.6f, -1f), Vector3.zero, new Vector3(1f, 1f, 1f));
        AddGarbage(garbageItems, "garbage_expired_medicine", "过期药品", WasteCategory.Hazardous, "过期药品需要特殊处理，属于有害垃圾。", "Assets/Art/GarbageItems/garbage_expired_medicine.glb", new Vector3(-4f, 0.6f, -1f), Vector3.zero, new Vector3(0.9f, 0.9f, 0.9f));
        AddGarbage(garbageItems, "garbage_lamp_tube", "灯管", WasteCategory.Hazardous, "废旧灯管需要特殊处理，属于有害垃圾。", "Assets/Art/GarbageItems/garbage_lamp_tube.glb", new Vector3(-2f, 0.6f, -1f), Vector3.zero, new Vector3(0.95f, 0.95f, 0.95f));
        AddGarbage(garbageItems, "garbage_dirty_tissue", "污损纸巾", WasteCategory.Other, "污损纸巾不可回收，应投入其他垃圾。", "Assets/Art/GarbageItems/garbage_dirty_tissue.glb", new Vector3(0f, 0.6f, -1f), Vector3.zero, new Vector3(0.78f, 0.78f, 0.78f));
        AddGarbage(garbageItems, "garbage_milk_tea_cup", "奶茶杯", WasteCategory.Other, "带残留液体的奶茶杯应按其他垃圾处理。", "Assets/Art/GarbageItems/garbage_milk_tea_cup.glb", new Vector3(2f, 0.6f, -1f), Vector3.zero, new Vector3(0.92f, 0.92f, 0.92f));
        AddGarbage(garbageItems, "garbage_oily_takeout_box", "油污外卖盒", WasteCategory.Other, "带油污的外卖盒不适合作为可回收物，应投入其他垃圾。", "Assets/Art/GarbageItems/garbage_oily_takeout_box.glb", new Vector3(4f, 0.6f, -1f), Vector3.zero, new Vector3(0.82f, 0.82f, 0.82f));

        AddTrashBin(trashBins, "可回收物", WasteCategory.Recyclable, "Assets/Art/TrashBins/bin_recyclable_blue.glb", new Vector3(-6f, 0f, 12f), new Vector3(0f, -18f, 0f), new Vector3(2.05f, 2.05f, 2.05f));
        AddTrashBin(trashBins, "有害垃圾", WasteCategory.Hazardous, "Assets/Art/TrashBins/bin_hazardous_red.glb", new Vector3(-2f, 0f, 12f), new Vector3(0f, -8f, 0f), new Vector3(2.05f, 2.05f, 2.05f));
        AddTrashBin(trashBins, "厨余垃圾", WasteCategory.Kitchen, "Assets/Art/TrashBins/bin_kitchen_green.glb", new Vector3(2f, 0f, 12f), new Vector3(0f, 8f, 0f), new Vector3(2.05f, 2.05f, 2.05f));
        AddTrashBin(trashBins, "其他垃圾", WasteCategory.Other, "Assets/Art/TrashBins/bin_other_gray.glb", new Vector3(6f, 0f, 12f), new Vector3(0f, 18f, 0f), new Vector3(2.05f, 2.05f, 2.05f));

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void AddGarbage(SerializedProperty array, string itemId, string itemName, WasteCategory category, string wrongReason, string assetPath, Vector3 position, Vector3 rotationEuler, Vector3 scale)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty item = array.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("itemId").stringValue = itemId;
        item.FindPropertyRelative("itemName").stringValue = itemName;
        item.FindPropertyRelative("category").enumValueIndex = (int)category;
        item.FindPropertyRelative("wrongReason").stringValue = wrongReason;
        item.FindPropertyRelative("assetPath").stringValue = assetPath;
        item.FindPropertyRelative("position").vector3Value = position;
        item.FindPropertyRelative("rotationEuler").vector3Value = rotationEuler;
        item.FindPropertyRelative("scale").vector3Value = scale;
    }

    private static void AddTrashBin(SerializedProperty array, string displayName, WasteCategory category, string assetPath, Vector3 position, Vector3 rotationEuler, Vector3 scale)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty item = array.GetArrayElementAtIndex(index);
        item.FindPropertyRelative("displayName").stringValue = displayName;
        item.FindPropertyRelative("category").enumValueIndex = (int)category;
        item.FindPropertyRelative("assetPath").stringValue = assetPath;
        item.FindPropertyRelative("position").vector3Value = position;
        item.FindPropertyRelative("rotationEuler").vector3Value = rotationEuler;
        item.FindPropertyRelative("scale").vector3Value = scale;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
