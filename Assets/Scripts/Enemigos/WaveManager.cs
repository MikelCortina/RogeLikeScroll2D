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
    private Transform[] spawnPoints;

    [Header("Spawn points por tipo de enemigo (según su waveSpace)")]
    private Transform[] spawnPointsValue1;
    private Transform[] spawnPointsValue2;
    private Transform[] spawnPointsValue10;
    private Transform[] spawnPointsValue20;

    [Header("Side zone provider")]
    [Tooltip("Referencia al SideZoneManager que contiene la lógica y configuración de las zonas laterales")]
    public SideZoneManager sideZoneManager;

    [Header("Spawning extras")]
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

    [Header("Opciones de desbloqueo")]
    [Tooltip("Valor inicial de Wave Space para la primera ola.")]
    public float initialWaveSpace = 1.5f; // valor inicial
    [Tooltip("Si está activo, suma el coste del último enemigo desbloqueado a currentWaveSpace al empezar la siguiente ola.")]
    public bool addLastUnlockedCostToNextWave = true;

    [Header("Debug / Inspector overrides")]
    [Tooltip("Si > 0, sobrescribe lo calculado para enemiesToSpawnThisWave al inicio de la ola (solo para debug).")]
    public int inspectorEnemiesToSpawnOverride = 0;

    // --- Estado ---
    public int currentWave { get; private set; } = 0;
    public int enemiesAlive { get; private set; } = 0;
    // Para que puedas verlo en inspector (solo debug), lo dejamos público pero con private set.
    public int enemiesToSpawnThisWave { get; private set; } = 0;

    public event Action<int, int> OnWaveStarted;
    public event Action<int> OnWaveFinished;
    public event Action<GameObject> OnEnemySpawned;
    public event Action<GameObject> OnEnemyKilled;

    private Coroutine runningWaveCoroutine;

    // --- Nuevo estado para lógica de espacio y desbloqueo ---
    public float currentWaveSpace { get; private set; } = 0f; // Ahora privado set
    private List<bool> enemyUnlocked = new List<bool>();
    private int lastUnlockedIndex = -1;

    private Camera mainCam;

    [Serializable]
    public class ZoneConfig
    {
        [Tooltip("Nombre opcional para identificar la subzona en el inspector (ej: 'Top 1').")]
        public string name;

        [Tooltip("Valores de waveSpace permitidos en esta subzona. Ej: 1, 5, 10. Dejar vacío -> no permite ninguno.")]
        public List<int> allowedWaveSpaceValues = new List<int>();

        [Tooltip("Si true, permite ANY waveSpace (ignora la lista).")]
        public bool allowAny = false;
    }

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
        mainCam = Camera.main;

        if (sideZoneManager == null)
        {
            // intentar encontrar uno en la misma escena
            sideZoneManager = FindObjectOfType<SideZoneManager>();
        }

        InitializeUnlocks();
        StartCoroutine(StartFirstWaveAfterDelay());
    }

    private void InitializeUnlocks()
    {
        enemyUnlocked.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            currentWaveSpace = 1f;
            lastUnlockedIndex = -1;
            return;
        }

        for (int i = 0; i < enemyPrefabs.Length; i++)
            enemyUnlocked.Add(i == 0);

        // Asegurarnos de que el primer coste disponible se respete
        float firstCost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[0]));

        // El currentWaveSpace se inicializa con el valor del Inspector (initialWaveSpace),
        // pero se asegura de que sea al menos el coste del primer enemigo.
        currentWaveSpace = Mathf.Max(initialWaveSpace, firstCost);

        lastUnlockedIndex = enemyUnlocked[0] ? 0 : -1;
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

        AddLastUnlockedCostToWave();

        // No es necesario llamar EstimateEnemiesThisWave aquí, se hace al principio de SpawnWaveRoutine.
        enemiesAlive = 0;

        runningWaveCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private void AddLastUnlockedCostToWave()
    {
        if (!addLastUnlockedCostToNextWave || currentWave <= 1 || lastUnlockedIndex < 0 || lastUnlockedIndex >= enemyPrefabs.Length)
            return;

        float lastCost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[lastUnlockedIndex]));
        // Lógica de adición: añade el coste o una porción del coste a la siguiente ola.
        float addAmount = lastCost > 1f ? lastCost * 0.5f : lastCost;

        currentWaveSpace += addAmount;
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
        // Obtener alphaSpawnChance desde StatsManager si existe (mantener compatibilidad)
        alphaSpawnChance = StatsManager.Instance != null ? StatsManager.Instance.RuntimeStats.luck / 100f : alphaSpawnChance;
        EnemyLevelManager.Instance?.IncreaseEnemyLevel(1);

        enemiesToSpawnThisWave = EstimateEnemiesThisWave();

        // Override desde inspector (debug)
        if (inspectorEnemiesToSpawnOverride > 0)
        {
            enemiesToSpawnThisWave = inspectorEnemiesToSpawnOverride;
        }

        enemiesAlive = 0;

        OnWaveStarted?.Invoke(waveNumber, enemiesToSpawnThisWave);

        // Alpha Enemy (chance global)
        if (alphaEnemyPrefabs != null && alphaEnemyPrefabs.Length > 0 && UnityEngine.Random.value <= alphaSpawnChance)
            SpawnAlphaEnemy();

        // Spawn basado en remainingSpace
        float remainingSpace = currentWaveSpace;
        int spawned = 0;
        int safetyCounter = 0;

        // 1. Intentar desbloquear y spawnear si hay espacio
        List<int> newlyUnlockedSpawned = TryUnlockAndSpawnWithRemaining(ref remainingSpace);
        spawned += newlyUnlockedSpawned.Count;
        if (spawned > 0) yield return new WaitForSeconds(spawnInterval);

        // 2. Bucle principal de spawn
        while (true)
        {
            safetyCounter++;
            if (safetyCounter > 2000)
            {
                Debug.LogWarning("[WaveManager] Safety break in spawn loop.");
                break;
            }

            float minUnlockedCost = GetMinUnlockedWaveSpace();
            // Salir si el espacio restante no cubre el enemigo desbloqueado más barato
            if (minUnlockedCost <= 0f || remainingSpace + 1e-6f < minUnlockedCost)
                break;

            // Encontrar candidatos para spawn
            List<int> candidates = new List<int>();
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (!enemyUnlocked[i]) continue;
                float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[i]));
                if (cost <= remainingSpace + 1e-6f) candidates.Add(i);
            }

            if (candidates.Count == 0)
            {
                break;
            }

            // Seleccionar y spawnear
            int pickIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            float pickCost = GetWaveSpaceFromPrefab(enemyPrefabs[pickIndex]);

            SpawnEnemyByIndex(pickIndex);
            remainingSpace -= pickCost;
            spawned++;
            yield return new WaitForSeconds(spawnInterval);

            // Intentar desbloquear y spawnear de nuevo tras un spawn regular
            List<int> unlockedDuringSpawn = TryUnlockAndSpawnWithRemaining(ref remainingSpace);
            spawned += unlockedDuringSpawn.Count;
            if (unlockedDuringSpawn.Count > 0) yield return new WaitForSeconds(spawnInterval);
        }

        // 3. Desbloqueo final (sin usar remainingSpace para la condición de desbloqueo, solo para el spawn)
        List<int> postWaveUnlocked = TryUnlockWithoutSpawning();
        foreach (int idx in postWaveUnlocked)
        {
            float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[idx]));
            if (remainingSpace + 1e-6f >= cost) // Solo spawnea si aún hay espacio restante
            {
                SpawnEnemyByIndex(idx);
                remainingSpace -= cost;
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

        GameObject normalPrefab = enemyPrefabs[prefabIndex];
        float waveSpace = GetWaveSpaceFromPrefab(normalPrefab);

        // -------------------------------
        // PROBABILIDAD ALTERNATIVA PARA ALFA
        // -------------------------------
        // Nota: en tu código original había un intento de 1/10 ó 1/20; aquí mantengo una probabilidad simple:
        if (alphaEnemyPrefabs != null && alphaEnemyPrefabs.Length > 0)
        {
            // ejemplo: 1 en 20 (5%) -> Random.Range(0,20) == 0
            if (UnityEngine.Random.Range(0, 20) == 0)
            {
                // Buscar alfas que tengan el mismo waveSpace
                List<GameObject> sameValueAlphas = new List<GameObject>();

                foreach (var alpha in alphaEnemyPrefabs)
                {
                    if (alpha != null && Mathf.Approximately(GetWaveSpaceFromPrefab(alpha), waveSpace))
                        sameValueAlphas.Add(alpha);
                }

                // Si existen alfas del mismo valor, spawneamos ese
                if (sameValueAlphas.Count > 0)
                {
                    GameObject chosenAlpha = sameValueAlphas[UnityEngine.Random.Range(0, sameValueAlphas.Count)];
                    SpawnEnemyPrefab(chosenAlpha);
                    return;
                }
            }
        }

        // Si no salió alfa o no había alfas del mismo valor → spawnear normal
        SpawnEnemyPrefab(normalPrefab);
    }

    private void SpawnEnemyPrefab(GameObject prefab)
    {
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
        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;

        // 1. Intentar usar Top Zone si está disponible y habilitada
        if (sideZoneManager != null && sideZoneManager.useTopZones && sideZoneManager.topZonesColumns > 0)
        {
            Vector3 topPos;
            if (sideZoneManager.TryGetTopSpawnPosition(waveSpace, spawnRandomRadius, out topPos))
            {
                return topPos;
            }
        }

        // 2. Intentar spawnPoints específicos por waveSpace
        Transform[] points = spawnPoints;

        if (Mathf.Approximately(waveSpace, 1f) && spawnPointsValue1?.Length > 0) points = spawnPointsValue1;
        else if (Mathf.Approximately(waveSpace, 2f) && spawnPointsValue2?.Length > 0) points = spawnPointsValue2;
        else if (Mathf.Approximately(waveSpace, 5f) && spawnPointsValue10?.Length > 0) points = spawnPointsValue10;
        else if (Mathf.Approximately(waveSpace, 10f) && spawnPointsValue20?.Length > 0) points = spawnPointsValue20;

        if (points?.Length > 0)
            return points[UnityEngine.Random.Range(0, points.Length)].position + (Vector3)offset;

        // 3. Intentar usar Side Zones si están habilitadas
        if (sideZoneManager != null && sideZoneManager.useSideZones && sideZoneManager.zonesPerSide > 0)
        {
            bool usedOtherSide;
            Vector3 sidePos;
            if (sideZoneManager.TryGetSideSpawnPosition(waveSpace, spawnRandomRadius, out sidePos, out usedOtherSide))
            {
                return sidePos;
            }
        }

        // 4. Fallback al transform del WaveManager
        return transform.position + (Vector3)offset;
    }


    private float GetWaveSpaceFromPrefab(GameObject prefab)
    {
        if (prefab == null) return 1f;
        // Se asegura de que waveSpace sea al menos 1
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

                float cost = GetWaveSpaceFromPrefab(enemyPrefabs[i]);
                // Desbloquea si el coste es cubierto por el espacio restante
                if (remainingSpace + 1e-6f >= cost)
                {
                    enemyUnlocked[i] = true;
                    lastUnlockedIndex = i;
                    newlyUnlockedAndSpawned.Add(i);

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
            float cost = GetWaveSpaceFromPrefab(enemyPrefabs[i]);

            // Desbloquea si el coste es cubierto por el espacio TOTAL de la ola
            if (currentWaveSpace + 1e-6f >= cost)
            {
                enemyUnlocked[i] = true;
                lastUnlockedIndex = i;
                newlyUnlocked.Add(i);
            }
        }

        return newlyUnlocked;
    }

    private void SpawnAlphaEnemy()
    {
        if (alphaEnemyPrefabs == null || alphaEnemyPrefabs.Length == 0) return;

        // Filtrar solo los alpha cuyo waveSpace esté desbloqueado
        List<GameObject> unlockedAlphas = new List<GameObject>();
        for (int i = 0; i < alphaEnemyPrefabs.Length; i++)
        {
            GameObject alpha = alphaEnemyPrefabs[i];
            float waveSpace = GetWaveSpaceFromPrefab(alpha);

            // Solo agregar si hay algún enemigo normal desbloqueado con el mismo waveSpace
            bool unlocked = false;
            for (int j = 0; j < enemyPrefabs.Length; j++)
            {
                if (enemyUnlocked[j] && Mathf.Approximately(GetWaveSpaceFromPrefab(enemyPrefabs[j]), waveSpace))
                {
                    unlocked = true;
                    break;
                }
            }

            if (unlocked)
                unlockedAlphas.Add(alpha);
        }

        if (unlockedAlphas.Count == 0) return; // No hay alpha disponible para esta ola

        GameObject prefabToSpawn = unlockedAlphas[UnityEngine.Random.Range(0, unlockedAlphas.Count)];
        float prefabWaveSpace = GetWaveSpaceFromPrefab(prefabToSpawn);
        Vector3 spawnPos = GetSpawnPositionForEnemy(prefabWaveSpace);

        GameObject go = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        if (go.TryGetComponent(out EnemyBase enemy) && EnemyLevelManager.Instance != null)
            enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);

        if (go.TryGetComponent(out Rigidbody2D rb2d))
            rb2d.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
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

        InitializeUnlocks();

        if (startImmediately) StartCoroutine(StartFirstWaveAfterDelay());
    }
}
