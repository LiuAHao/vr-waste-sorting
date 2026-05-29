using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageProgressionSceneSetup : MonoBehaviour
{
    [SerializeField] private StageProgressionConfig config;
    [SerializeField] private int spawnPointCount = 12;
    [SerializeField] private Vector3 mapBoundsCenter = new Vector3(56f, 0.6f, 35f);
    [SerializeField] private Vector2 mapFullHalfExtents = new Vector2(40f, 40f);
    [SerializeField] private float spawnPointRadius = 8f;

    private void Awake()
    {
        StageProgressionModeController controller = GetComponent<StageProgressionModeController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<StageProgressionModeController>();
        }

        StageGarbageSpawner spawner = GetComponent<StageGarbageSpawner>();
        if (spawner == null)
        {
            spawner = gameObject.AddComponent<StageGarbageSpawner>();
        }

        controller.Configure(config, spawner);
        spawner.Configure(config);
        spawner.SetMapBounds(mapBoundsCenter, mapFullHalfExtents);
        EnsureSpawnPoints();
    }

    private void EnsureSpawnPoints()
    {
        TimedChallengeSpawnPoint[] existingPoints = GetComponentsInChildren<TimedChallengeSpawnPoint>(true);
        if (existingPoints.Length >= spawnPointCount)
        {
            return;
        }

        Transform spawnRoot = transform.Find("StageProgressionSpawnPoints");
        if (spawnRoot == null)
        {
            GameObject spawnRootObject = new GameObject("StageProgressionSpawnPoints");
            spawnRoot = spawnRootObject.transform;
            spawnRoot.SetParent(transform, false);
        }

        string groupId = ResolveSpawnPointGroupId();
        int missingCount = spawnPointCount - existingPoints.Length;
        for (int i = 0; i < missingCount; i++)
        {
            int index = existingPoints.Length + i;
            float angle = (Mathf.PI * 2f / spawnPointCount) * index;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnPointRadius;

            GameObject pointObject = new GameObject("SpawnPoint_" + (index + 1));
            pointObject.transform.SetParent(spawnRoot, false);
            pointObject.transform.position = mapBoundsCenter + offset;
            TimedChallengeSpawnPoint point = pointObject.AddComponent<TimedChallengeSpawnPoint>();
            point.SetGroupId(groupId);
        }
    }

    private string ResolveSpawnPointGroupId()
    {
        return config != null && !string.IsNullOrWhiteSpace(config.SpawnPointGroupId)
            ? config.SpawnPointGroupId
            : "stage";
    }
}
