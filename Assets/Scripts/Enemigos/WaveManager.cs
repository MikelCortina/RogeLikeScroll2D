using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestión de oleadas (WaveManager) con spawn de enemigos, timeouts y eventos para UI/otros sistemas.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Spawning")]
    [Tooltip("Prefabs de enemigos (puedes poner diferentes tipos).")]
    public GameObject[] enemyPrefabs;

    [Tooltip("Puntos desde donde pueden aparecer enemigos. Si está vacío se usará la posición del WaveManager.")]
    public Transform[] spawnPoints;

    [Tooltip("Separación entre spawns individuales (segundos).")]
    public float spawnInterval = 0.25f;

    [Tooltip("Radio aleatorio alrededor del spawnPoint donde aparece el enemigo.")]
    public float spawnRandomRadius = 0.5f;

    [Header("Waves")]
    [Tooltip("Tiempo antes de comenzar la primera ola (segundos).")]
    public float initialDelay = 2f;

    [Tooltip("Tiempo entre olas (comienza a contar cuando la ola termina).")]
    public float timeBetweenWaves = 4f;

    [Tooltip("Si true, empieza automáticamente la siguiente ola cuando todos los enemigos mueren.")]
    public bool autoStartNextWaveWhenCleared = true;

    [Header("Wave Flow")]
    [Tooltip("Periodo de gracia tras eliminar el último enemigo antes de empezar la siguiente ola (segundos).")]
    public float gracePeriodAfterClear = 1.5f;

    [Tooltip("Si la ola no se limpia en este tiempo (segundos) se forzará la siguiente ola.")]
    public float maxWaitAfterSpawn = 60f;

    // --- Estado interno ---
    public int currentWave { get; private set; } = 0;
    public int enemiesAlive { get; private set; } = 0;
    public int enemiesToSpawnThisWave { get; private set; } = 0;

    // Eventos para UI / otros sistemas
    public event Action<int, int> OnWaveStarted;    // (waveNumber, enemyCount)
    public event Action<int> OnWaveFinished;        // (waveNumber)
    public event Action<GameObject> OnEnemySpawned;
    public event Action<GameObject> OnEnemyKilled;

    private Coroutine runningWaveCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject); // descomenta si quieres persistencia entre escenas
    }

    private void Start()
    {
        StartCoroutine(StartFirstWaveAfterDelay());
    }

    private IEnumerator StartFirstWaveAfterDelay()
    {
        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }

    /// <summary>
    /// Inicia la siguiente ola (si no hay ya una corriendo).
    /// </summary>
    public void StartNextWave()
    {
        if (runningWaveCoroutine != null)
            return; // ya corriendo

        currentWave++;
        enemiesToSpawnThisWave = CalculateEnemiesForWave(currentWave);
        enemiesAlive = 0;
        runningWaveCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave, enemiesToSpawnThisWave));
    }

    /// <summary>
    /// Calcula el número de enemigos de la ola:
    /// Waves 1..4: triangular (1,3,6,10).
    /// Wave >=5: toma el valor de la 4 (10) y lo escala por 1.2^(wave-4), redondeando hacia arriba.
    /// </summary>
    private int CalculateEnemiesForWave(int wave)
    {
        if (wave <= 4) return wave * (wave + 1) / 2;

        int baseFourth = 10;
        float exponentBase = 1.05f; // más suave que 1.2
        float scaled = baseFourth * Mathf.Pow(exponentBase, wave - 4);
        return Mathf.CeilToInt(scaled);
    }

    private IEnumerator SpawnWaveRoutine(int waveNumber, int totalToSpawn)
    {
        // Aumentar nivel de enemigos si existe el manager correspondiente
        if (EnemyLevelManager.Instance != null)
            EnemyLevelManager.Instance.IncreaseEnemyLevel(1);

        OnWaveStarted?.Invoke(waveNumber, totalToSpawn);

        int spawned = 0;
        // Spawn principal
        while (spawned < totalToSpawn)
        {
            SpawnOneEnemy();
            spawned++;
            yield return new WaitForSeconds(spawnInterval);
        }

        // Si queremos auto-start de la siguiente ola cuando esté limpia
        if (autoStartNextWaveWhenCleared)
        {
            float timer = 0f;
            bool cleared = false;

            // Espera activa: sale si enemiesAlive == 0 o si timer supera maxWaitAfterSpawn
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

            if (cleared)
            {
                // pequeña pausa para animaciones/loot/partículas
                yield return new WaitForSeconds(gracePeriodAfterClear);
                OnWaveFinished?.Invoke(waveNumber);
            }
            else
            {
                Debug.LogWarning($"[WaveManager] Wave {waveNumber} timed out after {maxWaitAfterSpawn}s. Forzando siguiente ola.");
                OnWaveFinished?.Invoke(waveNumber);
            }

            // limpiar referencia antes de esperar el intervalo para la siguiente ola
            runningWaveCoroutine = null;

            // esperar tiempo entre olas (se respetará la pausa antes de iniciar siguiente ola)
            yield return new WaitForSeconds(timeBetweenWaves);

            // comenzar siguiente ola automáticamente
            StartNextWave();
        }
        else
        {
            // no auto start: sólo notificar y liberar el coroutine
            OnWaveFinished?.Invoke(waveNumber);
            runningWaveCoroutine = null;
        }
    }

    private void SpawnOneEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Asignar nivel al enemigo
        EnemyBase enemy = go.GetComponent<EnemyBase>();
        if (enemy != null && EnemyLevelManager.Instance != null)
        {
            // Nivel global actual
            enemy.enemyLevel = Mathf.RoundToInt(EnemyLevelManager.Instance.enemyLevel);
        }

        // Reiniciar velocidades
        Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();
        if (rb2d != null) rb2d.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
    }
    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            return sp.position + (Vector3)offset;
        }
        else
        {
            return transform.position + (Vector3)offset;
        }
    }

    /// <summary>
    /// Llamar cuando un enemigo muere para que el WaveManager lo registre.
    /// </summary>
    public void NotifyEnemyKilled(GameObject enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyKilled?.Invoke(enemy);
    }

    /// <summary>
    /// Reinicia el sistema de olas (útil en runs/roguelikes).
    /// </summary>
    /// <param name="startImmediately">Si true, volverá a iniciar la primera ola tras initialDelay.</param>
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

        if (startImmediately)
        {
            StartCoroutine(StartFirstWaveAfterDelay());
        }
    }
}
