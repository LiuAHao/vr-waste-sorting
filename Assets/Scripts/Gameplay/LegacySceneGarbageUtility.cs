using UnityEngine;

/// <summary>
/// Hides legacy click-to-clean props that only have <see cref="Garbage"/> and no <see cref="GarbageItem"/>.
/// </summary>
public static class LegacySceneGarbageUtility
{
    private static bool _hasSuppressed;

    public static void SuppressLegacyGarbage(bool force = false)
    {
        if (_hasSuppressed && !force)
        {
            return;
        }

        int hiddenCount = 0;
        Garbage[] legacyComponents = Object.FindObjectsOfType<Garbage>(true);
        for (int i = 0; i < legacyComponents.Length; i++)
        {
            Garbage legacy = legacyComponents[i];
            if (legacy == null || HasGarbageItem(legacy.gameObject))
            {
                continue;
            }

            if (legacy.gameObject.activeSelf)
            {
                legacy.gameObject.SetActive(false);
                hiddenCount++;
            }
        }

        _hasSuppressed = true;

        if (hiddenCount > 0)
        {
            Debug.Log("LegacySceneGarbageUtility: 已隐藏 " + hiddenCount + " 个旧版演示垃圾（仅支持点击，不可抓取）。");
        }
    }

    public static void ResetSuppressionFlag()
    {
        _hasSuppressed = false;
    }

    private static bool HasGarbageItem(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        return target.GetComponent<GarbageItem>() != null
            || target.GetComponentInParent<GarbageItem>() != null
            || target.GetComponentInChildren<GarbageItem>(true) != null;
    }
}
