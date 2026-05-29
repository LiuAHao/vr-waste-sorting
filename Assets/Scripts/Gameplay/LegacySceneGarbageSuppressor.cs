using UnityEngine;

[DisallowMultipleComponent]
public sealed class LegacySceneGarbageSuppressor : MonoBehaviour
{
    private void Awake()
    {
        LegacySceneGarbageUtility.SuppressLegacyGarbage();
    }
}
