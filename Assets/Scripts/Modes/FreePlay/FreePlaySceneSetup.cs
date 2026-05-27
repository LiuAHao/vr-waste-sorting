using UnityEngine;

[DisallowMultipleComponent]
public sealed class FreePlaySceneSetup : MonoBehaviour
{
    [SerializeField] private Vector3 mapBoundsCenter = new Vector3(56f, 0.6f, 35f);
    [SerializeField] private Vector2 mapFullHalfExtents = new Vector2(40f, 40f);

    private void Awake()
    {
        FreePlayModeController controller = GetComponent<FreePlayModeController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<FreePlayModeController>();
        }

        StageGarbageSpawner spawner = GetComponent<StageGarbageSpawner>();
        if (spawner == null)
        {
            spawner = gameObject.AddComponent<StageGarbageSpawner>();
        }

        controller.Configure(spawner);
        spawner.Configure(null);
        spawner.SetMapBounds(mapBoundsCenter, mapFullHalfExtents);
    }
}
