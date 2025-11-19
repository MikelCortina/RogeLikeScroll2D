using System;
using System.Collections.Generic;
using UnityEngine;
 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    #region Singleton
    public static WaveManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    #region Inspector Fields

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

    [Header("Side zones (camera-based)")]
    [Tooltip("Si está activo, se usan zonas laterales calculadas desde la cámara cuando no hay spawnPoints específicos asignados.")]
    public bool useSideZones = true;

    [Range(0.01f, 0.45f)]
    [Tooltip("Anchura lateral en coordenada viewport (0..1). Ej: 0.15 -> 15% del ancho de pantalla a cada lado.")]
    public float sideWidthViewport = 0.18f;

    [Range(0f, 1f)]
    [Tooltip("Cuánto (porcentaje, 0..0.5) fuera del viewport se colocan las zonas.")]
    public float outsideViewportOffset = 0.08f;

    [Range(0f, 2f)]
    public float verticalViewportOffset = 0.08f;

    [Tooltip("Cuántas zonas verticales por lado (división).")]
    public int zonesPerSide = 4;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que el spawn ocurra en el lado izquierdo (0 = siempre derecho, 1 = siempre izquierdo)")]
    public float leftSpawnChance = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que el spawn ocurra en el lado de arriba")]
    public float upSpawnChance = 0.5f;

    [Header("Zonas configurables por inspector")]
    public List<ZoneConfig> leftZoneConfigs = new List<ZoneConfig>();
    public List<ZoneConfig> rightZoneConfigs = new List<ZoneConfig>();
    public List<ZoneConfig> upZoneConfigs = new List<ZoneConfig>();

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
    public float initialWaveSpace = 1.5f;
    [Tooltip("Si está activo, suma el coste del último enemigo desbloqueado a currentWaveSpace al empezar la siguiente ola.")]
    public bool addLastUnlockedCostToNextWave = true;

    [Header("Debug / Inspector overrides")]
    [Tooltip("Si > 0, sobrescribe lo calculado para enemiesToSpawnThisWave al inicio de la ola (solo para debug).")]
    public int inspectorEnemiesToSpawnOverride = 0;

    #endregion

    #region State

    public int currentWave { get; private set; } = 0;
    public int enemiesAlive { get; private set; } = 0;
    public int enemiesToSpawnThisWave { get; private set; } = 0;

    public float currentWaveSpace { get; private set; } = 0f;
    private List<bool> enemyUnlocked = new List<bool>();
    private int lastUnlockedIndex = -1;

    private Camera mainCam;
    private Coroutine runningWaveCoroutine;

    public event Action<int, int> OnWaveStarted;
    public event Action<int> OnWaveFinished;
    public event Action<GameObject> OnEnemySpawned;
    public event Action<GameObject> OnEnemyKilled;

    #endregion

    #region Types

    [Serializable]
    public class ZoneConfig
    {
        [Tooltip("Nombre opcional para identificar la subzona en el inspector.")]
        public string name;
        [Tooltip("Valores de waveSpace permitidos en esta subzona.")]
        public List<int> allowedWaveSpaceValues = new List<int>();
        [Tooltip("Si true, permite ANY waveSpace (ignora la lista).")]
        public bool allowAny = false;
    }

    #endregion

    #region Initialization

    private void OnValidate()
    {
        SyncZoneConfigs(leftZoneConfigs);
        SyncZoneConfigs(rightZoneConfigs);
        SyncZoneConfigs(upZoneConfigs);
    }

    private void SyncZoneConfigs(List<ZoneConfig> list)
    {
        if (list == null) return;

        while (list.Count < zonesPerSide)
            list.Add(new ZoneConfig() { name = $"Zone {list.Count}" });

        while (list.Count > zonesPerSide && zonesPerSide > 0)
            list.RemoveAt(list.Count - 1);
    }

    private void Start()
    {
        mainCam = Camera.main;
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
        currentWaveSpace = Mathf.Max(initialWaveSpace, firstCost);

        lastUnlockedIndex = enemyUnlocked[0] ? 0 : -1;
    }

    private IEnumerator StartFirstWaveAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }

    #endregion

    #region Wave Control

    public void StartNextWave()
    {
        if (runningWaveCoroutine != null) return;

        currentWave++;
        AddLastUnlockedCostToWave();
        enemiesAlive = 0;
        runningWaveCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private void AddLastUnlockedCostToWave()
    {
        if (!addLastUnlockedCostToNextWave || currentWave <= 1 || lastUnlockedIndex < 0 || lastUnlockedIndex >= enemyPrefabs.Length)
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

    #endregion

    #region Spawning

    private IEnumerator SpawnWaveRoutine(int waveNumber)
    {
        alphaSpawnChance = StatsManager.Instance != null ? StatsManager.Instance.RuntimeStats.luck / 100f : alphaSpawnChance;
        EnemyLevelManager.Instance?.IncreaseEnemyLevel(1);

        enemiesToSpawnThisWave = inspectorEnemiesToSpawnOverride > 0 ? inspectorEnemiesToSpawnOverride : EstimateEnemiesThisWave();
        enemiesAlive = 0;

        OnWaveStarted?.Invoke(waveNumber, enemiesToSpawnThisWave);

        if (alphaEnemyPrefabs != null && alphaEnemyPrefabs.Length > 0 && UnityEngine.Random.value <= alphaSpawnChance)
            SpawnAlphaEnemy();

        float remainingSpace = currentWaveSpace;
        int spawned = 0;
        int safetyCounter = 0;

        spawned += TryUnlockAndSpawnWithRemaining(ref remainingSpace).Count;
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

            if (candidates.Count == 0) break;

            int pickIndex = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            float pickCost = GetWaveSpaceFromPrefab(enemyPrefabs[pickIndex]);

            SpawnEnemyByIndex(pickIndex);
            remainingSpace -= pickCost;
            spawned++;
            yield return new WaitForSeconds(spawnInterval);

            spawned += TryUnlockAndSpawnWithRemaining(ref remainingSpace).Count;
            if (spawned > 0) yield return new WaitForSeconds(spawnInterval);
        }

        foreach (int idx in TryUnlockWithoutSpawning())
        {
            float cost = Mathf.Max(1f, GetWaveSpaceFromPrefab(enemyPrefabs[idx]));
            if (remainingSpace + 1e-6f >= cost)
            {
                SpawnEnemyByIndex(idx);
                remainingSpace -= cost;
            }
        }

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

        if (UnityEngine.Random.Range(0, 1) == 0 && alphaEnemyPrefabs != null)
        {
            List<GameObject> alphas = new List<GameObject>();
            foreach (var alpha in alphaEnemyPrefabs)
                if (alpha != null && Mathf.Approximately(GetWaveSpaceFromPrefab(alpha), waveSpace))
                    alphas.Add(alpha);

            if (alphas.Count > 0)
            {
                GameObject chosen = alphas[UnityEngine.Random.Range(0, alphas.Count)];
                SpawnEnemyPrefab(chosen);
                return;
            }
        }

        SpawnEnemyPrefab(prefab);
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

    #endregion

    #region Positioning / Zones

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

        if (useSideZones && zonesPerSide > 0)
            return GetPositionInSideZones(waveSpace);

        return transform.position + (Vector3)offset;
    }

    private Vector3 GetPositionInSideZones(float waveSpace)
    {
        float rand = UnityEngine.Random.value;

        bool chooseLeft = false;
        bool chooseRight = false;
        bool chooseUp = false;

        List<ZoneConfig> configs = null;

        if (rand <= upSpawnChance)
        {
            chooseUp = true;
            configs = upZoneConfigs;
        }
        else if (rand <= upSpawnChance + leftSpawnChance)
        {
            chooseLeft = true;
            configs = leftZoneConfigs;
        }
        else
        {
            chooseRight = true;
            configs = rightZoneConfigs;
        }

        List<int> eligible = new List<int>();
        for (int i = 0; i < zonesPerSide; i++)
            if (i < configs.Count && ZoneConfigAllowsWaveSpace(configs[i], waveSpace))
                eligible.Add(i);

        int zoneIndex = -1;
        if (eligible.Count > 0)
        {
            zoneIndex = eligible[UnityEngine.Random.Range(0, eligible.Count)];
        }
        else
        {
            // Intenta con las otras zonas si la elegida no tiene eligible
            List<ZoneConfig> otherConfigs = null;
            if (chooseUp)
                otherConfigs = leftZoneConfigs.Count > 0 ? leftZoneConfigs : rightZoneConfigs;
            else if (chooseLeft)
                otherConfigs = rightZoneConfigs.Count > 0 ? rightZoneConfigs : upZoneConfigs;
            else
                otherConfigs = leftZoneConfigs.Count > 0 ? leftZoneConfigs : upZoneConfigs;

            List<int> otherEligible = new List<int>();
            for (int i = 0; i < zonesPerSide; i++)
                if (i < otherConfigs.Count && ZoneConfigAllowsWaveSpace(otherConfigs[i], waveSpace))
                    otherEligible.Add(i);

            if (otherEligible.Count > 0)
            {
                configs = otherConfigs;
                zoneIndex = otherEligible[UnityEngine.Random.Range(0, otherEligible.Count)];
            }
            else
            {
                Vector2 jitter = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;
                return transform.position + (Vector3)jitter;
            }
        }

        Vector3 worldPos;
        if (chooseUp)
            worldPos = GetRandomPositionInUpZone(zoneIndex);
        else
            worldPos = GetRandomPositionInSideZone(chooseLeft, zoneIndex);

        Vector2 jitter2 = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;
        return worldPos + (Vector3)jitter2;
    }

    private bool ZoneConfigAllowsWaveSpace(ZoneConfig cfg, float waveSpace)
    {
        if (cfg == null) return false;
        if (cfg.allowAny) return true;
        if (cfg.allowedWaveSpaceValues == null || cfg.allowedWaveSpaceValues.Count == 0) return false;
        return cfg.allowedWaveSpaceValues.Contains(Mathf.RoundToInt(waveSpace));
    }

    private Vector3 GetRandomPositionInSideZone(bool left, int zoneIndex)
    {
        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null) return transform.position;

        float xMin = left ? -outsideViewportOffset - sideWidthViewport : 1f + outsideViewportOffset;
        float xMax = left ? -outsideViewportOffset : 1f + outsideViewportOffset + sideWidthViewport;

        float zoneHeight = 1f / Mathf.Max(1, zonesPerSide);
        float yMin = zoneIndex * zoneHeight;
        float yMax = (zoneIndex + 1) * zoneHeight;

        float vx = UnityEngine.Random.Range(xMin, xMax);
        float vy = UnityEngine.Random.Range(yMin + 0.01f, yMax - 0.01f);

        Vector3 viewPoint = new Vector3(vx, vy, Mathf.Abs(cam.transform.position.z));
        viewPoint.z = cam.orthographic ? cam.nearClipPlane + 1f : Mathf.Abs(cam.transform.position.z);

        Vector3 world = cam.ViewportToWorldPoint(viewPoint);
        world.z = 0f;
        world.y += verticalViewportOffset;

        return world;
    }

    private Vector3 GetRandomPositionInUpZone(int zoneIndex)
    {
        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null) return transform.position;

        // Zona vertical superior
        float yMin = 1f - 0.1f; // por ejemplo, 10% superior del viewport
        float yMax = 1f;

        // Dividimos en columnas según zonesPerSide
        float zoneWidth = 1f / Mathf.Max(1, zonesPerSide);
        float xMin = zoneIndex * zoneWidth;
        float xMax = (zoneIndex + 1) * zoneWidth;

        float vx = UnityEngine.Random.Range(xMin + 0.01f, xMax - 0.01f);
        float vy = UnityEngine.Random.Range(yMin + 0.01f, yMax - 0.01f);

        Vector3 viewPoint = new Vector3(vx, vy, Mathf.Abs(cam.transform.position.z));
        viewPoint.z = cam.orthographic ? cam.nearClipPlane + 1f : Mathf.Abs(cam.transform.position.z);

        Vector3 world = cam.ViewportToWorldPoint(viewPoint);
        world.z = 0f;
        world.y += verticalViewportOffset;

        return world;
    }
    #endregion

    #region Alpha Enemy

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

    #endregion

    #region WaveSpace / Unlocks

    private float GetWaveSpaceFromPrefab(GameObject prefab)
    {
        if (prefab == null) return 1f;
        return prefab.TryGetComponent(out EnemyBase eb) ? Mathf.Max(1f, eb.waveSpace) : 1f;
    }

    private List<int> TryUnlockAndSpawnWithRemaining(ref float remainingSpace)
    {
        List<int> newlyUnlocked = new List<int>();
        if (enemyPrefabs == null) return newlyUnlocked;

        bool unlockedAny;
        do
        {
            unlockedAny = false;
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyUnlocked[i]) continue;
                float cost = GetWaveSpaceFromPrefab(enemyPrefabs[i]);
                if (remainingSpace + 1e-6f >= cost)
                {
                    enemyUnlocked[i] = true;
                    lastUnlockedIndex = i;
                    newlyUnlocked.Add(i);

                    SpawnEnemyByIndex(i);
                    remainingSpace -= cost;
                    unlockedAny = true;
                    break;
                }
            }
        } while (unlockedAny);

        return newlyUnlocked;
    }

    private List<int> TryUnlockWithoutSpawning()
    {
        List<int> newlyUnlocked = new List<int>();
        if (enemyPrefabs == null) return newlyUnlocked;

        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyUnlocked[i]) continue;
            float cost = GetWaveSpaceFromPrefab(enemyPrefabs[i]);
            if (currentWaveSpace + 1e-6f >= cost)
            {
                enemyUnlocked[i] = true;
                lastUnlockedIndex = i;
                newlyUnlocked.Add(i);
            }
        }

        return newlyUnlocked;
    }

    #endregion

    #region External API

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

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!useSideZones) return;

        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null) return;

        // -----------------------------
        // ZONAS LEFT Y RIGHT
        // -----------------------------
        for (int side = 0; side <= 1; side++)
        {
            bool left = side == 0;
            float xMin = left ? -outsideViewportOffset - sideWidthViewport : 1f + outsideViewportOffset;
            float xMax = left ? -outsideViewportOffset : 1f + outsideViewportOffset + sideWidthViewport;

            float zoneHeight = 1f / Mathf.Max(1, zonesPerSide);

            for (int z = 0; z < zonesPerSide; z++)
            {
                float yMin = z * zoneHeight;
                float yMax = (z + 1) * zoneHeight;

                Vector3 bl = cam.ViewportToWorldPoint(new Vector3(xMin, yMin, Mathf.Abs(cam.transform.position.z)));
                Vector3 tr = cam.ViewportToWorldPoint(new Vector3(xMax, yMax, Mathf.Abs(cam.transform.position.z)));
                bl.z = 0f; tr.z = 0f;

                bl.y += verticalViewportOffset;
                tr.y += verticalViewportOffset;

                Vector3 center = (bl + tr) / 2f;
                Vector3 size = new Vector3(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y), 0.01f);

                List<ZoneConfig> configs = left ? leftZoneConfigs : rightZoneConfigs;
                bool allows = z < configs.Count && configs[z] != null &&
                              (configs[z].allowAny || (configs[z].allowedWaveSpaceValues != null && configs[z].allowedWaveSpaceValues.Count > 0));

                if (allows)
                    Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.12f); // verde semi-transparente
                else
                    Gizmos.color = new Color(0, 0, 0, 0);

                Gizmos.DrawCube(center, size * 0.99f);

                Gizmos.color = left ? Color.green : Color.cyan;
                Gizmos.DrawWireCube(center, size);
            }
        }

        // -----------------------------
        // ZONAS UP (división vertical)
        // -----------------------------
        if (upZoneConfigs != null && upZoneConfigs.Count > 0)
        {
            float yMin = 1f - 0.1f; // altura de la zona superior (10% top)
            float yMax = 1f;
            float zoneWidth = 1f / Mathf.Max(1, zonesPerSide);

            for (int z = 0; z < zonesPerSide; z++)
            {
                float xMin = z * zoneWidth;
                float xMax = (z + 1) * zoneWidth;

                Vector3 bl = cam.ViewportToWorldPoint(new Vector3(xMin, yMin, Mathf.Abs(cam.transform.position.z)));
                Vector3 tr = cam.ViewportToWorldPoint(new Vector3(xMax, yMax, Mathf.Abs(cam.transform.position.z)));
                bl.z = 0f; tr.z = 0f;

                bl.y += verticalViewportOffset;
                tr.y += verticalViewportOffset;

                Vector3 center = (bl + tr) / 2f;
                Vector3 size = new Vector3(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y), 0.01f);

                bool allows = z < upZoneConfigs.Count && upZoneConfigs[z] != null &&
                              (upZoneConfigs[z].allowAny || (upZoneConfigs[z].allowedWaveSpaceValues != null && upZoneConfigs[z].allowedWaveSpaceValues.Count > 0));

                if (allows)
                    Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.12f); // amarillo semi-transparente
                else
                    Gizmos.color = new Color(0, 0, 0, 0);

                Gizmos.DrawCube(center, size * 0.99f);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }



    #endregion
}
