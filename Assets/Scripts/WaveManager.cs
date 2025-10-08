using System;
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Spawning")]
    [Tooltip("Prefabs de enemigos (puedes poner diferentes tipos).")]
    public GameObject[] enemyPrefabs;
    [Tooltip("Puntos desde donde pueden aparecer enemigos. Si est� vac�o se usar� la posici�n del WaveManager.")]
    public Transform[] spawnPoints;
    [Tooltip("Separaci�n entre spawns individuales (segundos).")]
    public float spawnInterval = 0.25f;
    [Tooltip("Radio aleatorio alrededor del spawnPoint donde aparece el enemigo.")]
    public float spawnRandomRadius = 0.5f;

    [Header("Waves")]
    [Tooltip("Tiempo antes de comenzar la primera ola (segundos).")]
    public float initialDelay = 2f;
    [Tooltip("Tiempo entre olas (comienza a contar cuando la ola termina).")]
    public float timeBetweenWaves = 4f;
    [Tooltip("Si true, empieza autom�ticamente la siguiente ola cuando todos los enemigos mueren.")]
    public bool autoStartNextWaveWhenCleared = true;

    [Header("Wave Flow")]
    [Tooltip("Periodo de gracia tras eliminar el último enemigo antes de empezar la siguiente ola (segundos).")]
    public float gracePeriodAfterClear = 1.5f;
    [Tooltip("Si la ola no se limpia en este tiempo (segundos) se forzará la siguiente ola.")]
    public float maxWaitAfterSpawn = 60f; // timeout



    // Estado interno
    public int currentWave { get; private set; } = 0;
    public int enemiesAlive { get; private set; } = 0;
    public int enemiesToSpawnThisWave { get; private set; } = 0;

    // Eventos para UI/otros sistemas
    public event Action<int, int> OnWaveStarted; // (waveNumber, enemyCount)
    public event Action<int> OnWaveFinished;     // (waveNumber)
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
        // opcional: DontDestroyOnLoad(gameObject);
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
    /// Llamar para iniciar la siguiente ola manualmente (o por UI).
    /// </summary>
    public void StartNextWave()
    {
        if (runningWaveCoroutine != null) return; // ya corriendo
        currentWave++;
        enemiesToSpawnThisWave = CalculateEnemiesForWave(currentWave);
        enemiesAlive = 0;
        runningWaveCoroutine = StartCoroutine(SpawnWaveRoutine(currentWave, enemiesToSpawnThisWave));
    }

    /// <summary>
    /// Calcula el n�mero de enemigos para una ola dada seg�n tu especificaci�n:
    /// Waves 1..4: triangular (n*(n+1)/2) => 1,3,6,10
    /// Wave >=5: toma el valor de la wave 4 (10) y lo multiplica por 1.2^(wave-4), redondeando hacia arriba.
    /// </summary>
    private int CalculateEnemiesForWave(int wave)
    {
        if (wave <= 0) return 0;
        if (wave <= 4)
        {
            return wave * (wave + 1) / 2; // triangular numbers: 1,3,6,10
        }
        else
        {
            int baseFourth = 4 * (4 + 1) / 2; // 10
            double scaled = baseFourth * Math.Pow(1.2, wave - 4);
            return Mathf.Max(1, Mathf.CeilToInt((float)scaled));
        }
    }

    private IEnumerator SpawnWaveRoutine(int waveNumber, int totalToSpawn)
    {
        // Subir 1 nivel a los enemigos al iniciar la ola
        if (EnemyLevelManager.Instance != null)
            EnemyLevelManager.Instance.IncreaseEnemyLevel(1);

        OnWaveStarted?.Invoke(waveNumber, totalToSpawn);

        int spawned = 0;
        while (spawned < totalToSpawn)
        {
            SpawnOneEnemy();
            spawned++;
            yield return new WaitForSeconds(spawnInterval);
        }

        if (autoStartNextWaveWhenCleared)
        {
            float timer = 0f;
            bool cleared = false;

            // Espera principal: sale si enemiesAlive == 0 o si timer supera maxWaitAfterSpawn
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
                // pequeña pausa para animaciones/loot
                yield return new WaitForSeconds(gracePeriodAfterClear);
                OnWaveFinished?.Invoke(waveNumber);
            }
            else
            {
                // timeout alcanzado: decide comportamiento (forzar next wave)
                Debug.Log($"[WaveManager] Wave {waveNumber} timed out after {maxWaitAfterSpawn}s. Forzando siguiente ola.");
                // opcional: ajustar la siguiente ola si quieres penalizar (no lo hago aquí para mantener simple)
                OnWaveFinished?.Invoke(waveNumber);
            }

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

    private void SpawnOneEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[WaveManager] No enemyPrefabs asignados.");
            return;
        }

        GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Si el enemigo tiene Rigidbody2D es buena pr�ctica limpiar cualquier velocidad residual
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        enemiesAlive++;
        OnEnemySpawned?.Invoke(go);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;
            return sp.position + (Vector3)offset;
        }
        else
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;
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

    // M�todo p�blico para reiniciar sistema de olas (�til en runs roguelike)
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
