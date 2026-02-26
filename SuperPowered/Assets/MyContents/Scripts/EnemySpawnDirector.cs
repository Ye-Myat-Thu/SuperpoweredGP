using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[System.Serializable]
public struct LevelSpawnBonus
{
    public int levelRequired;

    //extra spawn rate at this level
    public float bonusPercent;
}

public class EnemySpawnDirector : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private BaseCharacter playerChar;
    [SerializeField] private Camera mainCam;

    [Header("Enemy Pool")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 6f;
    [SerializeField] private int positionAttemptsPerSpawn = 8;
    [SerializeField] private float navMeshSampleRadius = 3f;

    [Header("Spawn Timing")]
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private float spawnRatePercent = 100f; //global multiplier in percent
    [SerializeField] private float timeRateIncreasePercentPerMinute = 10f; //spawn rate incr over time
                                                                           //
    [Header("Spawn Amount")]
    [SerializeField] private int enemiesPerSpawn = 1;

    [Header("Level Milestones")]
    [SerializeField] private LevelSpawnBonus[] levelBonuses =
    {
        new LevelSpawnBonus { levelRequired = 5, bonusPercent = 10f },
        new LevelSpawnBonus { levelRequired = 12, bonusPercent = 15f },
        new LevelSpawnBonus { levelRequired = 20, bonusPercent = 20f },
        new LevelSpawnBonus { levelRequired = 25, bonusPercent = 25f },
        new LevelSpawnBonus { levelRequired = 30, bonusPercent = 30f }
    };

    [Header("Debug")]
    [SerializeField] private bool drawSpawnGizmos = true;

    private float spawnTimer;
    private float elapsedTime;

    private void Awake()
    {
        if (!mainCam)
            mainCam = Camera.main;

        if (!playerChar)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
                playerChar = player.GetComponent<BaseCharacter>();
        }
    }

    private void Update()
    {
        if (!CanSpawn())
            return;

        elapsedTime += Time.deltaTime;
        spawnTimer += Time.deltaTime;

        float currentInterval = GetCurrentSpawnInterval();

        if (spawnTimer >= currentInterval)
        {
            spawnTimer = 0f;
            SpawnWaveTick();
        }
    }

    private bool CanSpawn()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return false;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return false;

        if (!mainCam)
            return false;

        return true;
    }

    private void SpawnWaveTick()
    {
        List<Transform> validSpawnPoints = GetOffscreenSpawnPoints();

        if (validSpawnPoints.Count == 0)
            return;

        for (int i = 0; i < enemiesPerSpawn; i++)
        {
            Transform chosenPoint = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
            GameObject chosenEnemy = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            Vector3 spawnPos;
            if (TryGetSpawnPosition(chosenPoint, out spawnPos))
            {
                Instantiate(chosenEnemy, spawnPos, Quaternion.identity); 
            }
        }
    }

    private List<Transform> GetOffscreenSpawnPoints()
    {
        List<Transform> result = new List<Transform>();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (!point) continue;

            if (!IsPointVisible(point.position))
            {
                result.Add(point);
            }
        }

        return result;
    }

    //
    private bool TryGetSpawnPosition(Transform spawnPoint, out Vector3 finalPosition)
    {
        for (int i = 0; i < positionAttemptsPerSpawn; i++)
        {
            Vector2 random2D = Random.insideUnitCircle * spawnRadius;
            Vector3 rawPos = spawnPoint.position + new Vector3(random2D.x, 0f, random2D.y);

            // Ensure the position is on the NavMesh
            if (NavMesh.SamplePosition(rawPos, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                // Extra safety: don't spawn inside the visible camera area
                if (!IsPointVisible(hit.position))
                {
                    finalPosition = hit.position;
                    return true;
                }
            }
        }

        finalPosition = spawnPoint.position;
        return false;
    }

    private bool IsPointVisible(Vector3 worldPos)
    {
        if (!mainCam) return false;

        Vector3 viewportPoint = mainCam.WorldToViewportPoint(worldPos);

        bool inFront = viewportPoint.z > 0f;
        bool insideX = viewportPoint.x >= 0f && viewportPoint.x <= 1f;
        bool insideY = viewportPoint.y >= 0f && viewportPoint.y <= 1f;

        return inFront && insideX && insideY;
    }

    private float GetCurrentSpawnInterval()
    {
        float globalMultiplier = Mathf.Max(0.01f, spawnRatePercent / 100f);

        float elapsedMinutes = elapsedTime / 60f;
        float timeBonusPercent = elapsedMinutes * timeRateIncreasePercentPerMinute;
        float timeMultiplier = 1f + (timeBonusPercent / 100f);

        float levelBonusPercent = GetLevelBonusPercent();
        float levelMultiplier = 1f + (levelBonusPercent / 100f);

        float totalMultiplier = globalMultiplier * timeMultiplier * levelMultiplier;

        return baseSpawnInterval / totalMultiplier;
    }

    private float GetLevelBonusPercent()
    {
        if (!playerChar)
            return 0f;

        float totalBonus = 0f;
        int playerLevel = playerChar.Level;

        for (int i = 0; i < levelBonuses.Length; i++)
        {
            if (playerLevel >= levelBonuses[i].levelRequired)
            {
                totalBonus += levelBonuses[i].bonusPercent;
            }
        }

        return totalBonus;
    }

    // Optional helper if you want to read the live rate in other scripts/UI
    public float GetCurrentSpawnRateMultiplier()
    {
        float globalMultiplier = Mathf.Max(0.01f, spawnRatePercent / 100f);
        float elapsedMinutes = elapsedTime / 60f;
        float timeMultiplier = 1f + ((elapsedMinutes * timeRateIncreasePercentPerMinute) / 100f);
        float levelMultiplier = 1f + (GetLevelBonusPercent() / 100f);

        return globalMultiplier * timeMultiplier * levelMultiplier;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnGizmos || spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnPoints[i]) continue;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoints[i].position, spawnRadius);
        }
    }
}
