using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Spawning")]
    public GameObject[] enemyPrefabs;
    public GameObject[] alphaEnemyPrefabs;
    [Tooltip("Spawn genérico (si no se encaja en ningún valor específico).")]
    public Transform[] spawnPoints;

    [Header("Spawn points por tipo de enemigo (según su waveSpace)")]
    public Transform[] spawnPointsValue1;
    public Transform[] spawnPointsValue2;
    public Transform[] spawnPointsValue10;
    public Transform[] spawnPointsValue20;
    [Tooltip("Puntos exclusivos para spawnear gusanos (towers).")]
    public Transform[] tower;

    public float spawnInterval = 0.25f;
    public float spawnRandomRadius = 0.5f;

    [Header("Alpha Enemy Chance")]
    [Range(0f, 1f)]
    public float alphaSpawnChance;

    [Header("Waves")]
    public float initialDelay = 2f;
    public float timeBetweenWaves = 4f;
    public bool autoStartNextWaveWhenCleared = true;

    [Header("Wave Flow")]
    public float gracePeriodAfterClear = 1.5f;
    public float maxWaitAfterSpawn = 60f;

    [Header("Tower Spawner (componente)")]
    public TowerSpawner towerSpawner;

    [Header("Opciones de desbloqueo")]
    [Tooltip("Si está activo, suma el coste del último enemigo desbloqueado a currentWaveSpace al empezar la siguiente ola.")]
    public bool addLastUnlockedCostToNextWave = true;

    // --- Estado ---
    public int currentWave { get; private set; } = 0;
    public int enemiesAlive { get; private set; } = 0;
    public int enemiesToSpawnThisWave { get; private set; } = 0;

    public event Action<int, int> OnWaveStarted;
    public event Action<int> OnWaveFinished;
    public event Action<GameObject> OnEnemySpawned;
    public event Action<GameObject> OnEnemyKilled;

    private Coroutine runningWaveCoroutine;

    // --- Nuevo estado para lógica de espacio y desbloqueo ---
    private float currentWaveSpace = 1f; // empieza con 1 unidad de espacio
    private List<bool> enemyUnlocked = new List<bool>();
    private int lastUnlockedIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (towerSpawner == null)
            towerSpawner = GetComponent<TowerSpawner>();
        if (towerSpawner == null)
            towerSpawner = FindObjectOfType<TowerSpawner>();

        InitializeUnlocks();
        Debug.Log($"[WaveManager] Start: enemyPrefabs={(enemyPrefabs?.Length ?? 0)}, currentWaveSpace={currentWaveSpace}, lastUnlockedIndex={lastUnlockedIndex}");
        StartCoroutine(StartFirstWaveAfterDelay());
    }

    private void InitializeUnlocks()
    {
        enemyUnlocked.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[WaveManager] InitializeUnlocks: enemyPrefabs está vacío!");
            currentWaveSpace = 1f;
            lastUnlockedIndex = -1;
            return;
        }

        for (int i = 0; i < enemyPrefabs.Length; i++)
            enemyUnlocked.Add(i == 0);

        float firstCost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[0]));
        currentWaveSpace = firstCost;
        lastUnlockedIndex = enemyUnlocked[0] ? 0 : -1;

        Debug.Log($"[WaveManager] InitializeUnlocks: first prefab cost={firstCost}, currentWaveSpace ajustado a {currentWaveSpace}");
    }

    private IEnumerator StartFirstWaveAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (runningWaveCoroutine != null) return;

        currentWave++;
        Debug.Log($"[WaveManager] Starting wave {currentWave}. currentWaveSpace antes de ajustes: {currentWaveSpace}");

        AddLastUnlockedCostToWave();

        enemiesToSpawnThisWave = EstimateEnemiesThisWave();
        enemiesAlive = 0;

        runningWaveCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private void AddLastUnlockedCostToWave()
    {
        if (currentWave <= 1 || lastUnlockedIndex < 0 || lastUnlockedIndex >= enemyPrefabs.Length)
        {
            Debug.Log($"[WaveManager] Wave {currentWave} start: no add (first wave or invalid lastUnlocked). currentWaveSpace {currentWaveSpace}");
            return;
        }

        float lastCost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[lastUnlockedIndex]));
        float addAmount = lastCost > 1f ? lastCost * 0.5f : lastCost;

        currentWaveSpace += addAmount;

        Debug.Log($"[WaveManager] Wave {currentWave} start: added {addAmount} of last unlocked ({enemyPrefabs[lastUnlockedIndex].name}). currentWaveSpace ahora {currentWaveSpace}");
    }

    private int EstimateEnemiesThisWave()
    {
        float minCost = GetMinUnlockedWaveSpace();
        return minCost > 0f ? Mathf.FloorToInt(currentWaveSpace / minCost) : 0;
    }

    private float GetMinUnlockedWaveSpace()
    {
        float min = float.MaxValue;
        bool any = false;

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (!enemyUnlocked[i]) continue;

            float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[i]));
            if (cost < min) min = cost;
            any = true;
        }

        return any ? min : 0f;
    }

    private IEnumerator SpawnWaveRoutine(int waveNumber)
    {
        alphaSpawnChance = StatsManager.Instance != null ? StatsManager.Instance.RuntimeStats.luck / 100f : alphaSpawnChance;
        EnemyLevelManager.Instance?.IncreaseEnemyLevel(1);

        enemiesToSpawnThisWave = EstimateEnemiesThisWave();
        enemiesAlive = 0;

        OnWaveStarted?.Invoke(waveNumber, enemiesToSpawnThisWave);
        Debug.Log($"[WaveManager] SpawnWaveRoutine start. wave={waveNumber}, currentWaveSpace={currentWaveSpace}, estimatedEnemies={enemiesToSpawnThisWave}");

        // Alpha Enemy
        if (alphaEnemyPrefabs != null && alphaEnemyPrefabs.Length > 0 && UnityEngine.Random.value <= alphaSpawnChance)
            SpawnAlphaEnemy();

        // Tower spawn
        bool towerSpawnedThisWave = false;
        if (towerSpawner != null && towerSpawner.TrySpawnTowerForWave(currentWave, out GameObject towerGo))
        {
            if (towerGo.TryGetComponent(out EnemyBase enemy))
            {
                if (EnemyLevelManager.Instance != null)
                    enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);
            }
            enemiesAlive++;
            OnEnemySpawned?.Invoke(towerGo);
            towerSpawnedThisWave = true;
        }

        // Spawn basado en remainingSpace
        float remainingSpace = currentWaveSpace;
        int spawned = 0;
        int safetyCounter = 0;

        List<int> newlyUnlockedSpawned = TryUnlockAndSpawnWithRemaining(ref remainingSpace);
        spawned += newlyUnlockedSpawned.Count;
        if (spawned > 0) yield return new WaitForSeconds(spawnInterval);

        while (true)
        {
            safetyCounter++;
            if (safetyCounter > 2000)
            {
                Debug.LogWarning("[WaveManager] Safety break in spawn loop.");
                break;
            }

            float minUnlockedCost = GetMinUnlockedWaveSpace();
            if (minUnlockedCost <= 0f || remainingSpace + 1e-6f < minUnlockedCost)
                break;

            List<int> candidates = new List<int>();
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (!enemyUnlocked[i]) continue;
                float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[i]));
                if (cost <= remainingSpace + 1e-6f) candidates.Add(i);
            }

            if (candidates.Count == 0)
            {
                Debug.Log($"[WaveManager] No candidates to spawn. remainingSpace={remainingSpace}, minUnlockedCost={minUnlockedCost}");
                break;
            }

            int pickIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            float pickCost = GetWaveSpaceFromPrefab(enemyPrefabs[pickIndex]);

            Debug.Log($"[WaveManager] Spawning enemy '{enemyPrefabs[pickIndex].name}' (cost {pickCost}). remainingSpace antes={remainingSpace}");
            SpawnEnemyByIndex(pickIndex);
            remainingSpace -= pickCost;
            spawned++;
            yield return new WaitForSeconds(spawnInterval);

            List<int> unlockedDuringSpawn = TryUnlockAndSpawnWithRemaining(ref remainingSpace);
            spawned += unlockedDuringSpawn.Count;
            if (unlockedDuringSpawn.Count > 0) yield return new WaitForSeconds(spawnInterval);
        }

        if (towerSpawner != null && !towerSpawnedThisWave)
            towerSpawner.IncrementRounds();

        List<int> postWaveUnlocked = TryUnlockWithoutSpawning();
        foreach (int idx in postWaveUnlocked)
        {
            float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[idx]));
            if (remainingSpace + 1e-6f >= cost)
            {
                Debug.Log($"[WaveManager] Spawning post-wave newly unlocked enemy '{enemyPrefabs[idx].name}' (cost {cost}). remainingSpace antes={remainingSpace}");
                SpawnEnemyByIndex(idx);
                remainingSpace -= cost;
            }
            else
            {
                Debug.Log($"[WaveManager] Post-wave: no space para '{enemyPrefabs[idx].name}' (cost {cost}, remainingSpace {remainingSpace}). Se queda desbloqueado para próximas olas.");
            }
        }

        // Fin de la ola
        if (autoStartNextWaveWhenCleared)
        {
            float timer = 0f;
            bool cleared = false;
            while (timer < maxWaitAfterSpawn)
            {
                if (enemiesAlive <= 0)
                {
                    cleared = true;
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            if (cleared) yield return new WaitForSeconds(gracePeriodAfterClear);

            OnWaveFinished?.Invoke(waveNumber);
            runningWaveCoroutine = null;
            yield return new WaitForSeconds(timeBetweenWaves);
            StartNextWave();
        }
        else
        {
            OnWaveFinished?.Invoke(waveNumber);
            runningWaveCoroutine = null;
        }
    }

    private void SpawnEnemyByIndex(int prefabIndex)
    {
        if (enemyPrefabs == null || prefabIndex < 0 || prefabIndex >= enemyPrefabs.Length) return;

        GameObject prefab = enemyPrefabs[prefabIndex];
        float waveSpace = GetWaveSpaceFromPrefab(prefab);
        Vector3 spawnPos = GetSpawnPositionForEnemy(waveSpace);

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        if (go.TryGetComponent(out EnemyBase enemy) && EnemyLevelManager.Instance != null)
            enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);

        if (go.TryGetComponent(out Rigidbody2D rb2d))
            rb2d.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
    }

    private Vector3 GetSpawnPositionForEnemy(float waveSpace)
    {
        Transform[] points = spawnPoints;

        if (Mathf.Approximately(waveSpace, 1f) && spawnPointsValue1?.Length > 0) points = spawnPointsValue1;
        else if (Mathf.Approximately(waveSpace, 2f) && spawnPointsValue2?.Length > 0) points = spawnPointsValue2;
        else if (Mathf.Approximately(waveSpace, 5f) && spawnPointsValue10?.Length > 0) points = spawnPointsValue10;
        else if (Mathf.Approximately(waveSpace, 10f) && spawnPointsValue20?.Length > 0) points = spawnPointsValue20;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;
        if (points?.Length > 0)
            return points[UnityEngine.Random.Range(0, points.Length)].position + (Vector3)offset;

        return transform.position + (Vector3)offset;
    }

    private void SpawnAlphaEnemy()
    {
        if (alphaEnemyPrefabs == null || alphaEnemyPrefabs.Length == 0) return;

        GameObject prefab = alphaEnemyPrefabs[UnityEngine.Random.Range(0, alphaEnemyPrefabs.Length)];
        float waveSpace = GetWaveSpaceFromPrefab(prefab);
        Vector3 spawnPos = GetSpawnPositionForEnemy(waveSpace);

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        if (go.TryGetComponent(out EnemyBase enemy) && EnemyLevelManager.Instance != null)
            enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);

        if (go.TryGetComponent(out Rigidbody2D rb2d))
            rb2d.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
    }

    private float GetWaveSpaceFromPrefab(GameObject prefab)
    {
        if (prefab == null) return 1f;
        return prefab.TryGetComponent(out EnemyBase eb) ? Mathf.Max(1f, eb.waveSpace) : 1f;
    }

    private List<int> TryUnlockAndSpawnWithRemaining(ref float remainingSpace)
    {
        List<int> newlyUnlockedAndSpawned = new List<int>();
        if (enemyPrefabs == null) return newlyUnlockedAndSpawned;

        bool unlockedAny;
        do
        {
            unlockedAny = false;
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyUnlocked[i]) continue;

                float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[i]));
                if (remainingSpace + 1e-6f >= cost)
                {
                    enemyUnlocked[i] = true;
                    lastUnlockedIndex = i;
                    newlyUnlockedAndSpawned.Add(i);

                    Debug.Log($"[WaveManager] Enemy '{enemyPrefabs[i].name}' unlocked AND spawned. remainingSpace antes={remainingSpace}");

                    SpawnEnemyByIndex(i);
                    remainingSpace -= cost;
                    unlockedAny = true;
                    break;
                }
            }
        } while (unlockedAny);

        return newlyUnlockedAndSpawned;
    }

    private List<int> TryUnlockWithoutSpawning()
    {
        List<int> newlyUnlocked = new List<int>();
        if (enemyPrefabs == null) return newlyUnlocked;

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyUnlocked[i]) continue;
            float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[i]));

            if (currentWaveSpace + 1e-6f >= cost)
            {
                enemyUnlocked[i] = true;
                lastUnlockedIndex = i;
                newlyUnlocked.Add(i);
                Debug.Log($"[WaveManager] (post-check) Enemy '{enemyPrefabs[i].name}' unlocked but not spawned.");
            }
        }

        return newlyUnlocked;
    }

    public void NotifyEnemyKilled(GameObject enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyKilled?.Invoke(enemy);
    }

    public void ResetWaves(bool startImmediately = true)
    {
        if (runningWaveCoroutine != null)
        {
            StopCoroutine(runningWaveCoroutine);
            runningWaveCoroutine = null;
        }

        currentWave = 0;
        enemiesAlive = 0;
        enemiesToSpawnThisWave = 0;

        towerSpawner?.ResetRounds();
        InitializeUnlocks();

        if (startImmediately) StartCoroutine(StartFirstWaveAfterDelay());
    }
}
