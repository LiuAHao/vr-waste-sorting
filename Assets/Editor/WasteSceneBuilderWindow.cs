using System.Reflection;
using ParkClean.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class WasteSceneBuilderWindow : EditorWindow
{
    private const string MainScenePath = "Assets/Scenes/2.unity";
    private const string SceneConfigObjectName = "WasteGameSceneConfig";

    private WasteContentCatalog _catalog;
    private bool _clearExistingGarbage = true;
    private bool _clearExistingBins = true;
    private bool _addMissingSceneConfig = true;
    private string _statusMessage = "请选择内容目录配置，然后在主场景 2.unity 中执行构建、同步或回写。";

    [MenuItem("垃圾分类项目/场景工具/主场景构建器")]
    public static void Open()
    {
        WasteSceneBuilderWindow window = GetWindow<WasteSceneBuilderWindow>("主场景构建器");
        window.minSize = new Vector2(520f, 360f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("垃圾分类主场景构建工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(8f);

        bool isPlaying = EditorApplication.isPlaying;
        if (isPlaying)
        {
            EditorGUILayout.HelpBox("当前处于 Play Mode。请先停止运行，再执行打开场景、批量构建、同步交互配置或回写布局。", MessageType.Warning);
            EditorGUILayout.Space(4f);
        }

        _catalog = (WasteContentCatalog)EditorGUILayout.ObjectField("内容目录配置", _catalog, typeof(WasteContentCatalog), false);
        _clearExistingGarbage = EditorGUILayout.Toggle("清理现有垃圾对象", _clearExistingGarbage);
        _clearExistingBins = EditorGUILayout.Toggle("清理现有垃圾桶对象", _clearExistingBins);
        _addMissingSceneConfig = EditorGUILayout.Toggle("补充场景配置对象", _addMissingSceneConfig);

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox("批量构建会严格按 WasteContentCatalog 里保存的位置、旋转和缩放还原对象，不再按主视角重新排布。", MessageType.Info);
        EditorGUILayout.HelpBox("如果你已经手动摆好了模型，只想补齐交互组件，请使用“同步当前主场景对象交互配置”，它不会改动位姿和缩放。", MessageType.Info);

        using (new EditorGUI.DisabledScope(isPlaying || _catalog == null))
        {
            if (GUILayout.Button("打开主场景 2.unity", GUILayout.Height(32f)))
            {
                OpenMainScene();
            }

            if (GUILayout.Button("在主场景中批量构建内容", GUILayout.Height(36f)))
            {
                BuildScene();
            }

            if (GUILayout.Button("同步当前主场景对象交互配置", GUILayout.Height(36f)))
            {
                SyncExistingSceneObjects();
            }

            if (GUILayout.Button("将当前场景布局回写到目录配置", GUILayout.Height(36f)))
            {
                SaveCurrentSceneLayoutToCatalog();
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(_statusMessage, MessageType.None);
    }

    private void OpenMainScene()
    {
        if (!EnsureNotPlaying("打开主场景"))
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            _statusMessage = "已取消场景切换。";
            return;
        }

        Scene openedScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        _statusMessage = "已打开主场景：" + openedScene.path;
    }

    private void BuildScene()
    {
        if (!EnsureNotPlaying("批量构建内容") || !EnsureCatalogAndSceneReady())
        {
            return;
        }

        if (_clearExistingGarbage)
        {
            ClearObjectsWithComponent<GarbageItem>();
        }

        if (_clearExistingBins)
        {
            ClearObjectsWithComponent<TrashBin>();
        }

        EnsureSceneConfig();

        int createdGarbage = 0;
        int createdBins = 0;
        int skippedAssets = 0;

        for (int i = 0; i < _catalog.GarbageItems.Count; i++)
        {
            GarbageContentDefinition definition = _catalog.GarbageItems[i];
            GameObject instance = TryInstantiateModel(definition.assetPath, definition.itemId);
            if (instance == null)
            {
                skippedAssets++;
                continue;
            }

            instance.name = definition.itemId;
            ApplyCatalogTransform(instance.transform, definition.position, definition.rotationEuler, definition.scale);
            EnsureGarbageSetup(instance, definition);
            createdGarbage++;
        }

        for (int i = 0; i < _catalog.TrashBins.Count; i++)
        {
            TrashBinContentDefinition definition = _catalog.TrashBins[i];
            string objectName = GetAssetName(definition.assetPath);
            GameObject instance = TryInstantiateModel(definition.assetPath, objectName);
            if (instance == null)
            {
                skippedAssets++;
                continue;
            }

            instance.name = objectName;
            ApplyCatalogTransform(instance.transform, definition.position, definition.rotationEuler, definition.scale);
            EnsureTrashBinSetup(instance, definition);
            createdBins++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        _statusMessage = $"构建完成。垃圾对象 {createdGarbage} 个，垃圾桶 {createdBins} 个，跳过资源 {skippedAssets} 个。";
    }

    private void SyncExistingSceneObjects()
    {
        if (!EnsureNotPlaying("同步当前主场景对象交互配置") || !EnsureCatalogAndSceneReady())
        {
            return;
        }

        EnsureSceneConfig();

        int syncedGarbage = 0;
        int syncedBins = 0;
        int unmatchedGarbage = 0;
        int unmatchedBins = 0;

        for (int i = 0; i < _catalog.GarbageItems.Count; i++)
        {
            GarbageContentDefinition definition = _catalog.GarbageItems[i];
            GameObject target = FindTopLevelObject(definition.itemId);
            if (target == null)
            {
                unmatchedGarbage++;
                continue;
            }

            EnsureGarbageSetup(target, definition);
            syncedGarbage++;
        }

        for (int i = 0; i < _catalog.TrashBins.Count; i++)
        {
            TrashBinContentDefinition definition = _catalog.TrashBins[i];
            GameObject target = FindTopLevelObject(GetAssetName(definition.assetPath));
            if (target == null)
            {
                unmatchedBins++;
                continue;
            }

            EnsureTrashBinSetup(target, definition);
            syncedBins++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        _statusMessage = $"同步完成。垃圾对象 {syncedGarbage} 个，垃圾桶 {syncedBins} 个，未匹配垃圾 {unmatchedGarbage} 个，未匹配垃圾桶 {unmatchedBins} 个。";
    }

    private void SaveCurrentSceneLayoutToCatalog()
    {
        if (!EnsureNotPlaying("将当前场景布局回写到目录配置") || !EnsureCatalogAndSceneReady())
        {
            return;
        }

        SerializedObject serializedCatalog = new SerializedObject(_catalog);
        SerializedProperty garbageItems = serializedCatalog.FindProperty("garbageItems");
        SerializedProperty trashBins = serializedCatalog.FindProperty("trashBins");

        int savedGarbage = 0;
        int savedBins = 0;
        int missingGarbage = 0;
        int missingBins = 0;

        for (int i = 0; i < garbageItems.arraySize; i++)
        {
            SerializedProperty item = garbageItems.GetArrayElementAtIndex(i);
            string itemId = item.FindPropertyRelative("itemId").stringValue;
            GameObject target = FindTopLevelObject(itemId);
            if (target == null)
            {
                missingGarbage++;
                continue;
            }

            GarbageItem garbageItem = target.GetComponent<GarbageItem>();
            if (garbageItem != null)
            {
                item.FindPropertyRelative("itemName").stringValue = garbageItem.ItemName;
                item.FindPropertyRelative("category").enumValueIndex = (int)garbageItem.Category;
                item.FindPropertyRelative("wrongReason").stringValue = garbageItem.WrongReason;
            }

            string sourceAssetPath = ResolveSourceAssetPath(target);
            if (!string.IsNullOrWhiteSpace(sourceAssetPath))
            {
                item.FindPropertyRelative("assetPath").stringValue = sourceAssetPath;
            }

            item.FindPropertyRelative("position").vector3Value = target.transform.position;
            item.FindPropertyRelative("rotationEuler").vector3Value = NormalizeEulerAngles(target.transform.eulerAngles);
            item.FindPropertyRelative("scale").vector3Value = target.transform.localScale;
            savedGarbage++;
        }

        for (int i = 0; i < trashBins.arraySize; i++)
        {
            SerializedProperty item = trashBins.GetArrayElementAtIndex(i);
            string assetPath = item.FindPropertyRelative("assetPath").stringValue;
            string objectName = GetAssetName(assetPath);
            GameObject target = FindTopLevelObject(objectName);
            if (target == null)
            {
                missingBins++;
                continue;
            }

            TrashBin trashBin = target.GetComponent<TrashBin>();
            if (trashBin != null)
            {
                item.FindPropertyRelative("displayName").stringValue = trashBin.DisplayName;
                item.FindPropertyRelative("category").enumValueIndex = (int)trashBin.Category;
            }

            string sourceAssetPath = ResolveSourceAssetPath(target);
            if (!string.IsNullOrWhiteSpace(sourceAssetPath))
            {
                item.FindPropertyRelative("assetPath").stringValue = sourceAssetPath;
            }

            item.FindPropertyRelative("position").vector3Value = target.transform.position;
            item.FindPropertyRelative("rotationEuler").vector3Value = NormalizeEulerAngles(target.transform.eulerAngles);
            item.FindPropertyRelative("scale").vector3Value = target.transform.localScale;
            savedBins++;
        }

        serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(_catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _statusMessage = $"已回写当前场景布局。垃圾 {savedGarbage} 个，垃圾桶 {savedBins} 个，未找到垃圾 {missingGarbage} 个，未找到垃圾桶 {missingBins} 个。";
    }

    private bool EnsureCatalogAndSceneReady()
    {
        if (_catalog == null)
        {
            _statusMessage = "请先选择 WasteContentCatalog 配置。";
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != MainScenePath)
        {
            _statusMessage = "当前不是主场景 2.unity，请先点击“打开主场景 2.unity”。";
            return false;
        }

        return true;
    }

    private bool EnsureNotPlaying(string actionName)
    {
        if (!EditorApplication.isPlaying)
        {
            return true;
        }

        _statusMessage = $"当前处于运行模式，无法执行“{actionName}”。请先停止 Play Mode。";
        return false;
    }

    private static GameObject TryInstantiateModel(string assetPath, string label)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning($"无法实例化资源：{label}，路径为 {assetPath}。请先确认该资源已经被 Unity 导入为可实例化模型。");
            return null;
        }

        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    private static void EnsureGarbageSetup(GameObject target, GarbageContentDefinition definition)
    {
        GarbageItem garbageItem = target.GetComponent<GarbageItem>();
        if (garbageItem == null)
        {
            garbageItem = target.AddComponent<GarbageItem>();
        }

        SerializedObject serializedItem = new SerializedObject(garbageItem);
        serializedItem.FindProperty("itemId").stringValue = definition.itemId;
        serializedItem.FindProperty("itemName").stringValue = definition.itemName;
        serializedItem.FindProperty("category").enumValueIndex = (int)definition.category;
        serializedItem.FindProperty("wrongReason").stringValue = definition.wrongReason;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();
        SetGarbageStartPose(garbageItem, target.transform.position, target.transform.rotation);

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

        Garbage legacyGarbage = target.GetComponent<Garbage>();
        if (legacyGarbage != null)
        {
            Object.DestroyImmediate(legacyGarbage);
        }

        if (target.GetComponent<SelectableHighlighter>() == null)
        {
            target.AddComponent<SelectableHighlighter>();
        }
    }

    private static void EnsureTrashBinSetup(GameObject target, TrashBinContentDefinition definition)
    {
        TrashBin trashBin = target.GetComponent<TrashBin>();
        if (trashBin == null)
        {
            trashBin = target.AddComponent<TrashBin>();
        }

        SerializedObject serializedBin = new SerializedObject(trashBin);
        serializedBin.FindProperty("category").enumValueIndex = (int)definition.category;
        serializedBin.FindProperty("displayName").stringValue = definition.displayName;
        serializedBin.ApplyModifiedPropertiesWithoutUndo();

        Collider collider = target.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider boxCollider = target.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1.6f, 2.2f, 1.6f);
        }

        Rigidbody rigidbody = target.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = target.AddComponent<Rigidbody>();
        }

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Transform dropZone = target.transform.Find("DropZone");
        if (dropZone == null)
        {
            GameObject dropZoneObject = new GameObject("DropZone");
            dropZone = dropZoneObject.transform;
            dropZone.SetParent(target.transform, false);
        }

        dropZone.localPosition = new Vector3(0f, 0.95f, 0f);
        dropZone.localRotation = Quaternion.identity;
        dropZone.localScale = Vector3.one;

        BoxCollider trigger = dropZone.GetComponent<BoxCollider>();
        if (trigger == null)
        {
            trigger = dropZone.gameObject.AddComponent<BoxCollider>();
        }

        trigger.isTrigger = true;
        trigger.size = new Vector3(1.35f, 1.2f, 1.35f);
        trigger.center = new Vector3(0f, 0.15f, 0f);

        DropZone dropZoneComponent = dropZone.GetComponent<DropZone>();
        if (dropZoneComponent == null)
        {
            dropZoneComponent = dropZone.gameObject.AddComponent<DropZone>();
        }

        SerializedObject serializedDropZone = new SerializedObject(dropZoneComponent);
        serializedDropZone.FindProperty("targetBin").objectReferenceValue = trashBin;
        serializedDropZone.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ClearObjectsWithComponent<T>() where T : Component
    {
        T[] objects = FindObjectsOfType<T>(true);
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                DestroyImmediate(objects[i].gameObject);
            }
        }
    }

    private void EnsureSceneConfig()
    {
        if (!_addMissingSceneConfig)
        {
            return;
        }

        WasteGameSceneConfig config = FindObjectOfType<WasteGameSceneConfig>();
        if (config != null)
        {
            return;
        }

        GameObject configObject = new GameObject(SceneConfigObjectName);
        configObject.AddComponent<WasteGameSceneConfig>();
    }

    private static void ApplyCatalogTransform(Transform target, Vector3 position, Vector3 rotationEuler, Vector3 scale)
    {
        target.position = position;
        target.rotation = Quaternion.Euler(rotationEuler);
        target.localScale = scale;
    }

    private static GameObject FindTopLevelObject(string exactName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == exactName)
            {
                return roots[i];
            }
        }

        return null;
    }

    private static string GetAssetName(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return string.Empty;
        }

        int slashIndex = assetPath.LastIndexOf('/') + 1;
        int dotIndex = assetPath.LastIndexOf('.');
        if (slashIndex < 0 || dotIndex <= slashIndex)
        {
            return assetPath;
        }

        return assetPath.Substring(slashIndex, dotIndex - slashIndex);
    }

    private static string ResolveSourceAssetPath(GameObject target)
    {
        Object source = PrefabUtility.GetCorrespondingObjectFromSource(target);
        if (source == null)
        {
            return string.Empty;
        }

        return AssetDatabase.GetAssetPath(source);
    }

    private static Vector3 NormalizeEulerAngles(Vector3 euler)
    {
        return new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f)
        {
            angle -= 360f;
        }

        while (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private static void SetGarbageStartPose(GarbageItem garbageItem, Vector3 position, Quaternion rotation)
    {
        FieldInfo startPositionField = typeof(GarbageItem).GetField("_startPosition", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo startRotationField = typeof(GarbageItem).GetField("_startRotation", BindingFlags.Instance | BindingFlags.NonPublic);

        if (startPositionField != null)
        {
            startPositionField.SetValue(garbageItem, position);
        }

        if (startRotationField != null)
        {
            startRotationField.SetValue(garbageItem, rotation);
        }
    }
}
