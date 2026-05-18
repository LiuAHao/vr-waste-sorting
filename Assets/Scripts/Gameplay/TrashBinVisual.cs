using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(TrashBin))]
public class TrashBinVisual : MonoBehaviour
{
    private const string VisualRootName = "BinVisual";

    [SerializeField] private float bodyHeight = 1.1f;
    [SerializeField] private float bodyRadius = 0.42f;
    [SerializeField] private float labelHeightOffset = 0.35f;
    [SerializeField] private float labelFontSize = 48f;
    [SerializeField] private float dropZonePadding = 0.08f;
    [SerializeField] private bool rebuildInEditMode = true;
    [SerializeField] private bool alignDropZoneOnRebuild = true;

    private TrashBin _trashBin;
    private bool _pendingRebuild;

#if UNITY_EDITOR
    private static bool _editorRebuildQueued;
#endif

    private void Awake()
    {
        _trashBin = GetComponent<TrashBin>();
        BuildOrRefresh();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && rebuildInEditMode && transform.Find(VisualRootName) == null)
        {
            RequestRebuild();
        }
#endif
    }

    private void OnValidate()
    {
        _trashBin = GetComponent<TrashBin>();
        if (rebuildInEditMode)
        {
            RequestRebuild();
        }
    }

    private void Update()
    {
        if (!_pendingRebuild)
        {
            return;
        }

        _pendingRebuild = false;
        BuildOrRefresh();
    }

    private void RequestRebuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_editorRebuildQueued)
            {
                return;
            }

            _editorRebuildQueued = true;
            EditorApplication.delayCall += HandleEditorDelayedRebuild;
            return;
        }
#endif
        _pendingRebuild = true;
    }

#if UNITY_EDITOR
    private void HandleEditorDelayedRebuild()
    {
        EditorApplication.delayCall -= HandleEditorDelayedRebuild;
        _editorRebuildQueued = false;

        if (this == null)
        {
            return;
        }

        BuildOrRefresh();
    }
#endif

    [ContextMenu("Rebuild Visual")]
    public void BuildOrRefresh()
    {
        if (_trashBin == null)
        {
            _trashBin = GetComponent<TrashBin>();
        }

        if (_trashBin == null)
        {
            return;
        }

        Transform root = transform.Find(VisualRootName);
        if (root != null)
        {
            if (Application.isPlaying)
            {
                Destroy(root.gameObject);
            }
            else
            {
                DestroyImmediate(root.gameObject);
            }
        }

        root = new GameObject(VisualRootName).transform;
        root.SetParent(transform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        Color categoryColor = GetCategoryColor(_trashBin.Category);
        string labelText = GetLabelText(_trashBin);

        BuildBody(root, categoryColor);
        BuildLabel(root, labelText, categoryColor);

        if (alignDropZoneOnRebuild)
        {
            AlignDropZone();
        }
    }

    private void AlignDropZone()
    {
        Transform dropZoneTransform = transform.Find("DropZone");
        if (dropZoneTransform == null)
        {
            return;
        }

        float bodyCenterY = bodyHeight * 0.5f;
        float triggerWidth = bodyRadius * 2f + dropZonePadding * 2f;

        dropZoneTransform.localPosition = new Vector3(0f, bodyCenterY, 0f);
        dropZoneTransform.localRotation = Quaternion.identity;
        dropZoneTransform.localScale = Vector3.one;

        BoxCollider boxCollider = dropZoneTransform.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(triggerWidth, bodyHeight, triggerWidth);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(dropZoneTransform.gameObject);
        }
#endif
    }

    private void BuildBody(Transform root, Color color)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "BinBody";
        body.transform.SetParent(root, false);
        body.transform.localPosition = new Vector3(0f, bodyHeight * 0.5f, 0f);
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = new Vector3(bodyRadius * 2f, bodyHeight * 0.5f, bodyRadius * 2f);

        var collider = body.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        var renderer = body.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateSolidMaterial(color);
        }
    }

    private void BuildLabel(Transform root, string text, Color categoryColor)
    {
        float labelY = bodyHeight + labelHeightOffset;

        var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plate.name = "LabelPlate";
        plate.transform.SetParent(root, false);
        plate.transform.localPosition = new Vector3(0f, labelY, 0f);
        plate.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        plate.transform.localScale = new Vector3(1.4f, 0.45f, 1f);

        var plateCollider = plate.GetComponent<Collider>();
        if (plateCollider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(plateCollider);
            }
            else
            {
                DestroyImmediate(plateCollider);
            }
        }

        var plateRenderer = plate.GetComponent<MeshRenderer>();
        if (plateRenderer != null)
        {
            plateRenderer.sharedMaterial = CreateSolidMaterial(Color.Lerp(categoryColor, Color.white, 0.55f));
        }

        var labelGo = new GameObject("LabelText");
        labelGo.transform.SetParent(root, false);
        labelGo.transform.localPosition = new Vector3(0f, labelY, -0.02f);
        labelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        labelGo.transform.localScale = Vector3.one;

        var textMesh = labelGo.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = Mathf.RoundToInt(labelFontSize);
        textMesh.characterSize = 0.08f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = GetLabelTextColor(categoryColor);
        textMesh.fontStyle = FontStyle.Bold;
    }

    private static string GetLabelText(TrashBin bin)
    {
        if (!string.IsNullOrWhiteSpace(bin.DisplayName))
        {
            return bin.DisplayName;
        }

        return GetDefaultDisplayName(bin.Category);
    }

    private static string GetDefaultDisplayName(WasteCategory category)
    {
        switch (category)
        {
            case WasteCategory.Recyclable:
                return "可回收物";
            case WasteCategory.Hazardous:
                return "有害垃圾";
            case WasteCategory.Kitchen:
                return "厨余垃圾";
            case WasteCategory.Other:
            default:
                return "其他垃圾";
        }
    }

    private static Color GetCategoryColor(WasteCategory category)
    {
        switch (category)
        {
            case WasteCategory.Recyclable:
                return new Color(0.18f, 0.45f, 0.92f);
            case WasteCategory.Hazardous:
                return new Color(0.9f, 0.22f, 0.22f);
            case WasteCategory.Kitchen:
                return new Color(0.2f, 0.72f, 0.32f);
            case WasteCategory.Other:
            default:
                return new Color(0.5f, 0.5f, 0.52f);
        }
    }

    private static Color GetLabelTextColor(Color categoryColor)
    {
        float luminance = categoryColor.r * 0.299f + categoryColor.g * 0.587f + categoryColor.b * 0.114f;
        return luminance > 0.55f ? new Color(0.12f, 0.12f, 0.12f) : Color.white;
    }

    private static Material CreateSolidMaterial(Color color)
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Diffuse");
        }

        var material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.25f);
        }

        return material;
    }
}
