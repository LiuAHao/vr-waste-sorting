using UnityEngine;

[DisallowMultipleComponent]
public sealed class TimedChallengeSceneSetup : MonoBehaviour
{
    [SerializeField] private TimedChallengeConfig config;
    [SerializeField] private int spawnPointCount = 10;
    [SerializeField] private Vector3 spawnAreaCenter = new Vector3(80f, 0.6f, 48f);
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private string spawnPointGroupId = "default";

    private void Awake()
    {
        TimedChallengeModeController controller = GetComponent<TimedChallengeModeController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<TimedChallengeModeController>();
        }

        TimedChallengeSpawner spawner = GetComponent<TimedChallengeSpawner>();
        if (spawner == null)
        {
            spawner = gameObject.AddComponent<TimedChallengeSpawner>();
        }

        controller.Configure(config, spawner);
        spawner.Configure(config);
        EnsureSpawnPoints();
    }

    private void EnsureSpawnPoints()
    {
        TimedChallengeSpawnPoint[] existingPoints = GetComponentsInChildren<TimedChallengeSpawnPoint>(true);
        if (existingPoints.Length >= spawnPointCount)
        {
            return;
        }

        Transform spawnRoot = transform.Find("TimedChallengeSpawnPoints");
        if (spawnRoot == null)
        {
            GameObject spawnRootObject = new GameObject("TimedChallengeSpawnPoints");
            spawnRoot = spawnRootObject.transform;
            spawnRoot.SetParent(transform, false);
        }

        int missingCount = spawnPointCount - existingPoints.Length;
        for (int i = 0; i < missingCount; i++)
        {
            int index = existingPoints.Length + i;
            float angle = (Mathf.PI * 2f / spawnPointCount) * index;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;

            GameObject pointObject = new GameObject("SpawnPoint_" + (index + 1));
            pointObject.transform.SetParent(spawnRoot, false);
            pointObject.transform.position = spawnAreaCenter + offset;
            TimedChallengeSpawnPoint point = pointObject.AddComponent<TimedChallengeSpawnPoint>();
            point.SetGroupId(spawnPointGroupId);
        }
    }
}
