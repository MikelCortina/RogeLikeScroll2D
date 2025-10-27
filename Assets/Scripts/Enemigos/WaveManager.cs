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
    private Transform[] spawnPointsValue1;
    private Transform[] spawnPointsValue2;
    private Transform[] spawnPointsValue10;
    private Transform[] spawnPointsValue20;
    [Tooltip("Puntos exclusivos para spawnear gusanos (towers).")]
    public Transform[] tower;

    [Header("Side zones (camera-based)")]
    [Tooltip("Si está activo, se usan zonas laterales calculadas desde la cámara cuando no hay spawnPoints específicos asignados.")]
    public bool useSideZones = true;
    [Range(0.01f, 0.45f)]
    [Tooltip("Anchura lateral en coordenada viewport (0..1). Ej: 0.15 -> 15% del ancho de pantalla a cada lado (además del outside offset).")]
    public float sideWidthViewport = 0.18f; // un poco más ancha por defecto

    [Range(0f, 0.5f)]
    [Tooltip("Cuánto (porcentaje, 0..0.5) fuera del viewport se colocan las zonas (ej. 0.08 = 8% fuera).")]
    public float outsideViewportOffset = 0.08f; // ahora configurable (antes estaba hardcoded)
    [Range(0f, 0.5f)]
    public float verticalViewportOffset = 0.08f; // ahora configurable (antes estaba hardcoded)

    [Tooltip("Cuántas zonas verticales por lado (división).")]
    public int zonesPerSide = 4;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que el spawn ocurra en el lado izquierdo (0 = siempre derecho, 1 = siempre izquierdo)")]
    public float leftSpawnChance = 0.5f;

    [Header("Zonas configurables por inspector")]
    [Tooltip("Configura, para cada subcuadro (de arriba a abajo), qué valores de waveSpace están permitidos. " +
             "LeftZones y RightZones deben tener la misma cantidad que zonesPerSide (se sincronizan automáticamente).")]
    public List<ZoneConfig> leftZoneConfigs = new List<ZoneConfig>();
    public List<ZoneConfig> rightZoneConfigs = new List<ZoneConfig>();

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

    private Camera mainCam;

    #region ZonaConfig Type
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
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnValidate()
    {
        // sincroniza la cantidad de ZoneConfig al cambiar zonesPerSide
        SyncZoneConfigs(leftZoneConfigs);
        SyncZoneConfigs(rightZoneConfigs);
    }

    private void SyncZoneConfigs(List<ZoneConfig> list)
    {
        if (list == null) return;
        while (list.Count < zonesPerSide)
        {
            list.Add(new ZoneConfig() { name = $"Zone {list.Count}" });
        }
        while (list.Count > zonesPerSide && zonesPerSide > 0)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    private void Start()
    {
        mainCam = Camera.main;

        if (towerSpawner == null)
            towerSpawner = GetComponent<TowerSpawner>();
        if (towerSpawner == null)
            towerSpawner = FindObjectOfType<TowerSpawner>();

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

        float firstCost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[0]));
        currentWaveSpace = firstCost;
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

        enemiesToSpawnThisWave = EstimateEnemiesThisWave();
        enemiesAlive = 0;

        runningWaveCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private void AddLastUnlockedCostToWave()
    {
        if (currentWave <= 1 || lastUnlockedIndex < 0 || lastUnlockedIndex >= enemyPrefabs.Length)
            return;

        float lastCost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[lastUnlockedIndex]));
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
        alphaSpawnChance = StatsManager.Instance != null ? StatsManager.Instance.RuntimeStats.luck / 100f : alphaSpawnChance;
        EnemyLevelManager.Instance?.IncreaseEnemyLevel(1);

        enemiesToSpawnThisWave = EstimateEnemiesThisWave();
        enemiesAlive = 0;

        OnWaveStarted?.Invoke(waveNumber, enemiesToSpawnThisWave);

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
                break;
            }

            int pickIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            float pickCost = GetWaveSpaceFromPrefab(enemyPrefabs[pickIndex]);

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
        // Prioriza spawnPoints específicos si fueron asignados (lógica anterior)
        Transform[] points = spawnPoints;

        if (Mathf.Approximately(waveSpace, 1f) && spawnPointsValue1?.Length > 0) points = spawnPointsValue1;
        else if (Mathf.Approximately(waveSpace, 2f) && spawnPointsValue2?.Length > 0) points = spawnPointsValue2;
        else if (Mathf.Approximately(waveSpace, 5f) && spawnPointsValue10?.Length > 0) points = spawnPointsValue10;
        else if (Mathf.Approximately(waveSpace, 10f) && spawnPointsValue20?.Length > 0) points = spawnPointsValue20;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;
        if (points?.Length > 0)
            return points[UnityEngine.Random.Range(0, points.Length)].position + (Vector3)offset;

        // Si no hay puntos definidos y está activo el sistema de zonas laterales -> usar zonas en cámara
        if (useSideZones && zonesPerSide > 0)
        {
            // Elegir lado por probabilidad configurable
            bool chooseLeft = UnityEngine.Random.value <= leftSpawnChance;

            // Buscar subzonas del lado elegido que permitan el waveSpace
            List<int> eligibleZoneIndices = new List<int>();
            List<ZoneConfig> configs = chooseLeft ? leftZoneConfigs : rightZoneConfigs;

            for (int i = 0; i < zonesPerSide; i++)
            {
                if (i >= configs.Count) continue; // seguridad
                if (ZoneConfigAllowsWaveSpace(configs[i], waveSpace))
                    eligibleZoneIndices.Add(i);
            }

            int zoneIndex;
            if (eligibleZoneIndices.Count > 0)
            {
                // elegimos aleatoriamente entre las zonas válidas
                zoneIndex = eligibleZoneIndices[UnityEngine.Random.Range(0, eligibleZoneIndices.Count)];
            }
            else
            {
                // fallback heurístico: mapear waveSpace a zona por valor (si no hay ninguna configuración que permita)
                zoneIndex = Mathf.Clamp(Mathf.RoundToInt(waveSpace) - 1, 0, zonesPerSide - 1);
            }

            Vector3 worldPos = GetRandomPositionInSideZone(chooseLeft, zoneIndex);
            // aplicar jitter
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;
            return worldPos + (Vector3)jitter;
        }

        // fallback al transform del WaveManager
        return transform.position + (Vector3)offset;
    }

    private bool ZoneConfigAllowsWaveSpace(ZoneConfig cfg, float waveSpace)
    {
        if (cfg == null) return false;
        if (cfg.allowAny) return true;
        if (cfg.allowedWaveSpaceValues == null || cfg.allowedWaveSpaceValues.Count == 0) return false;

        int rounded = Mathf.RoundToInt(waveSpace);
        return cfg.allowedWaveSpaceValues.Contains(rounded);
    }

    private Vector3 GetRandomPositionInSideZone(bool left, int zoneIndex)
    {
        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null)
            return transform.position;

        // --- DEFINIR RANGOS EN VIEWPORT (AHORA USANDO outsideViewportOffset public) ---
        float xMin, xMax;
        if (left)
        {
            xMin = -outsideViewportOffset - sideWidthViewport;
            xMax = -outsideViewportOffset;
        }
        else
        {
            xMin = 1f + outsideViewportOffset;
            xMax = 1f + outsideViewportOffset + sideWidthViewport;
        }

        // Dividimos el eje Y en "zonesPerSide" secciones
        float zoneHeight = 1f / Mathf.Max(1, zonesPerSide);
        float yMin = zoneIndex * zoneHeight;
        float yMax = (zoneIndex + 1) * zoneHeight;

        // Seleccionamos coordenadas de viewport fuera del rango visible
        float vx = UnityEngine.Random.Range(xMin, xMax);
        float vy = UnityEngine.Random.Range(yMin + 0.01f, yMax - 0.01f);

        Vector3 viewPoint = new Vector3(vx, vy, Mathf.Abs(cam.transform.position.z));

        // Ajustar la Z según tipo de cámara
        if (cam.orthographic)
            viewPoint.z = cam.nearClipPlane + 1f;
        else
            viewPoint.z = Mathf.Abs(cam.transform.position.z);

        // Convertir a coordenadas de mundo
        Vector3 world = cam.ViewportToWorldPoint(viewPoint);
        world.z = 0f;

        // --- APLICAR OFFSET VERTICAL EN MUNDO ---
        world.y += verticalViewportOffset;

        return world;
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

    private void OnDrawGizmosSelected()
    {
        if (!useSideZones) return;

        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null) return;

        // dibujar rectángulos de zonas (mostrando visualmente las subzonas que permiten spawns)
        for (int side = 0; side <= 1; side++)
        {
            bool left = side == 0;

            float xMin, xMax;
            if (left)
            {
                xMin = -outsideViewportOffset - sideWidthViewport;
                xMax = -outsideViewportOffset;
            }
            else
            {
                xMin = 1f + outsideViewportOffset;
                xMax = 1f + outsideViewportOffset + sideWidthViewport;
            }

            float zoneHeight = 1f / Mathf.Max(1, zonesPerSide);

            for (int z = 0; z < zonesPerSide; z++)
            {
                float yMin = z * zoneHeight;
                float yMax = (z + 1) * zoneHeight;

                Vector3 bl = cam.ViewportToWorldPoint(new Vector3(xMin, yMin, Mathf.Abs(cam.transform.position.z)));
                Vector3 tr = cam.ViewportToWorldPoint(new Vector3(xMax, yMax, Mathf.Abs(cam.transform.position.z)));
                bl.z = 0f;
                tr.z = 0f;

                // --- aplicar offset vertical ---
                bl.y += verticalViewportOffset;
                tr.y += verticalViewportOffset;

                Vector3 center = (bl + tr) / 2f;
                Vector3 size = new Vector3(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y), 0.01f);

                // comprobar si esta subzona permite algún waveSpace para pintar distinto
                List<ZoneConfig> configs = left ? leftZoneConfigs : rightZoneConfigs;
                bool allows = false;
                if (z < configs.Count)
                    allows = configs[z] != null &&
                             (configs[z].allowAny ||
                             (configs[z].allowedWaveSpaceValues != null && configs[z].allowedWaveSpaceValues.Count > 0));

                if (allows)
                {
                    Color fill = new Color(0.2f, 0.8f, 0.2f, 0.12f); // verde claro semi-transparente
                    Gizmos.color = fill;
                    Gizmos.DrawCube(center, size * 0.99f);
                }

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }

}


