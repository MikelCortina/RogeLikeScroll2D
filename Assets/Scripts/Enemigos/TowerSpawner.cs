using System;
using UnityEngine;

/// <summary>
/// Encapsula la lógica de spawn de "gusano"/tower:
/// - Probabilidades según StatsManager.RuntimeStats.towerLuck (fallback a luck)
/// - Forzar spawn tras N rondas
/// - Control de spawn points específicos para towers
/// - Método público TrySpawnTowerForWave(...) que WaveManager llamará por cada ola
/// - NUEVO: opción para que la torre se mueva con el mundo (parallax)
/// </summary>
public class TowerSpawner : MonoBehaviour
{
    [Header("Prefabs & Spawn points")]
    [Tooltip("Prefabs de gusano (normalmente 1 o pocos).")]
    public GameObject[] towerPrefabs;

    [Tooltip("Puntos exclusivos para spawnear gusanos. Si está vacío se usará spawnPoints o la posición del objeto.")]
    public Transform[] towerSpawnPoints;

    [Tooltip("Radio aleatorio alrededor del spawnPoint donde aparece la torre.")]
    public float spawnRandomRadius = 0.5f;

    [Header("Reglas de aparición")]
    [Tooltip("Rondas máximas sin spawn antes de forzar uno.")]
    public int forceAfterRounds = 10;

    // Reglas empíricas:
    // si towerLuckPercent >= 70 => minRoundsBetween = 3
    // si towerLuckPercent >= 60 => minRoundsBetween = 4
    // resto => 5
    private int roundsSinceLastTower = 999;

    [Header("Movimiento con el mundo (Parallax)")]
    [Tooltip("Referencia opcional al ParallaxController. Si no se asigna, se buscará en la escena.")]
    public ParallaxController parallaxController;

    [Tooltip("Si está true, la torre se moverá horizontalmente con la velocidad del mundo al instanciarse.")]
    public bool moveWithWorld = true;

    [Tooltip("Si está true, añadirá un componente que actualiza continuamente la velocidad de la torre para seguir cambios dinámicos del parallax.")]
    public bool followWorldContinuously = true;

    private void Awake()
    {
        // nada especial por ahora
    }

    /// <summary>
    /// Intentar spawnear una torre para la ola actual.
    /// - currentWave se pasa para poder imponer condiciones (ej. solo a partir de X ronda)
    /// - Devuelve true si se instanció (y out spawned contiene el GameObject), false si no se instanció.
    /// </summary>
    public bool TrySpawnTowerForWave(int currentWave, out GameObject spawned)
    {
        spawned = null;

        if (towerPrefabs == null || towerPrefabs.Length == 0)
            return false;

        float towerLuckPercent = GetTowerLuckPercent(); // 0..100

        // 1) Forzar spawn si han pasado demasiadas rondas Y estamos lo suficientemente avanzados
        if (roundsSinceLastTower >= forceAfterRounds && currentWave >= 7)
        {
            spawned = DoSpawnTower();
            if (spawned != null)
            {
                roundsSinceLastTower = 0;
                return true;
            }
            return false;
        }

        // 2) Calcular mínimo de rondas entre spawns según towerLuck
        int minRoundsBetween = GetMinRoundsBetweenTowers(towerLuckPercent);
        bool allowedByInterval = roundsSinceLastTower >= minRoundsBetween;

        // 3) Si la probabilidad es <=50% y no han pasado al menos 5 rondas, prohibir spawn
        if (towerLuckPercent <= 50f && roundsSinceLastTower < 5)
        {
            allowedByInterval = false;
        }

        // 4) Intentar spawn según probabilidad si está permitido
        if (allowedByInterval)
        {
            float chance = Mathf.Clamp01(towerLuckPercent / 100f);
            if (UnityEngine.Random.value <= chance)
            {
                spawned = DoSpawnTower();
                if (spawned != null)
                {
                    roundsSinceLastTower = 0;
                    return true;
                }
            }
        }

        // no spawned this wave -> incrementar contador (lo hará WaveManager también si prefiere; aquí lo dejamos local)
        roundsSinceLastTower = Mathf.Min(forceAfterRounds, roundsSinceLastTower + 1);
        return false;
    }

    /// <summary>
    /// Efectúa la instanciación real del prefab de tower y devuelve el GameObject instanciado (o null si falló).
    /// NO asigna enemyLevel aquí: lo dejamos para WaveManager si quiere hacerlo (compatibilidad).
    /// </summary>
    private GameObject DoSpawnTower()
    {
        if (towerPrefabs == null || towerPrefabs.Length == 0)
            return null;

        Vector3 spawnPos = GetRandomTowerSpawnPosition();
        GameObject prefab = towerPrefabs[UnityEngine.Random.Range(0, towerPrefabs.Length)];
        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Buscar ParallaxController si no se ha asignado manualmente
        ParallaxController pc = parallaxController;
        if (pc == null)
        {
            pc = FindObjectOfType<ParallaxController>();
        }

        Rigidbody2D rb2d = go.GetComponent<Rigidbody2D>();

        // Si el prefab tiene Rigidbody2D y queremos que se mueva con el mundo:
        if (rb2d != null)
        {
            // Calcula la velocidad horizontal del "mundo".
            float worldSpeed = 0f;
            if (pc != null)
            {
                worldSpeed = pc.baseSpeed * pc.cameraMoveMultiplier;
            }

            if (moveWithWorld)
            {
                // Aplicar velocidad inicial de arrastre por el mundo.
                // Se usa -worldSpeed para que el objeto se mueva en la dirección opuesta
                // si worldSpeed representa la magnitud del desplazamiento del mundo.
                rb2d.linearVelocity = new Vector2(-worldSpeed, rb2d.linearVelocity.y);

                // Si queremos que la torre siga cambiando su velocidad si el parallax varía en runtime,
                // añadimos un componente que actualice la velocidad cada FixedUpdate.
                if (followWorldContinuously && pc != null)
                {
                    // Añadir o reutilizar componente
                    TowerWorldFollower follower = go.GetComponent<TowerWorldFollower>();
                    if (follower == null)
                        follower = go.AddComponent<TowerWorldFollower>();

                    follower.Initialize(pc);
                }
            }
            else
            {
                // Si no queremos moverla con el mundo, dejar la velocidad como estaba (o zero).
                // dejemos la velocidad en 0 para evitar movimientos indeseados.
                rb2d.linearVelocity = new Vector2(0f, rb2d.linearVelocity.y);
            }
        }

        Debug.Log($"[TowerSpawner] Tower spawned at {spawnPos}");
        return go;
    }

    private Vector3 GetRandomTowerSpawnPosition()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnRandomRadius;

        if (towerSpawnPoints != null && towerSpawnPoints.Length > 0)
        {
            Transform sp = towerSpawnPoints[UnityEngine.Random.Range(0, towerSpawnPoints.Length)];
            return sp.position + (Vector3)offset;
        }

        // fallback a la posición del objeto (si no hay spawn points)
        return transform.position + (Vector3)offset;
    }

    private float GetTowerLuckPercent()
    {
        float percent = 0f;
        try
        {
            percent = StatsManager.Instance.RuntimeStats.towerLuck;
        }
        catch
        {
            percent = StatsManager.Instance.RuntimeStats.luck;
        }
        return Mathf.Clamp(percent, 0f, 100f);
    }

    private int GetMinRoundsBetweenTowers(float towerLuckPercent)
    {
        if (towerLuckPercent >= 70f) return 3;
        if (towerLuckPercent >= 60f) return 4;
        return 5;
    }

    /// <summary>
    /// Reinicia el contador de rondas (útil desde WaveManager.ResetWaves).
    /// </summary>
    public void ResetRounds()
    {
        roundsSinceLastTower = 999;
    }

    /// <summary>
    /// Incrementa manualmente el contador de rondas (si quieres que WaveManager controle el incremento).
    /// </summary>
    public void IncrementRounds()
    {
        roundsSinceLastTower = Mathf.Min(forceAfterRounds, roundsSinceLastTower + 1);
    }

    /// <summary>
    /// Consulta del contador (por si WaveManager quiere leerlo).
    /// </summary>
    public int GetRoundsSinceLastTower()
    {
        return roundsSinceLastTower;
    }
}

/// <summary>
/// Componente que mantiene la velocidad horizontal del Rigidbody2D sincronizada con el ParallaxController.
/// Se añade dinámicamente a las torres cuando se requiere seguimiento continuo.
/// </summary>
public class TowerWorldFollower : MonoBehaviour
{
    private ParallaxController parallaxController;
    private Rigidbody2D rb;

    public void Initialize(ParallaxController pc)
    {
        parallaxController = pc;
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (parallaxController == null || rb == null) return;

        float worldSpeed = parallaxController.baseSpeed * parallaxController.cameraMoveMultiplier;
        // Aplicamos la velocidad horizontal para que la torre sea arrastrada por el mundo.
        rb.linearVelocity = new Vector2(-worldSpeed, rb.linearVelocity.y);
    }
}

