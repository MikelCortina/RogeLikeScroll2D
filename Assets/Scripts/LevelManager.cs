using UnityEngine;

/// <summary>
/// Controla el nivel global que afecta a los enemigos.
/// - enemyLevel: entero >= 0
/// - GetEnemyMultiplier(): devuelve 1.01^level (1% por nivel multiplicativo)
/// </summary>
public class EnemyLevelManager : MonoBehaviour
{
    public static EnemyLevelManager Instance { get; private set; }

    [Tooltip("Nivel aplicado a todos los enemigos. 0 = sin incremento.")]
    [Min(0)]
    public int enemyLevel = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Multiplicador aplicado a vida/daño de enemigos.
    /// Por ejemplo, level = 5 -> multiplier = 1.01^5 (~1.0510 -> +5.1%).
    /// </summary>
    public float GetEnemyMultiplier()
    {
        return Mathf.Pow(1.01f, enemyLevel);
    }

    // Métodos de conveniencia para cambiar el nivel en runtime
    public void SetEnemyLevel(int level)
    {
        enemyLevel = Mathf.Max(0, level);
    }

    public void IncreaseEnemyLevel(int amount = 1)
    {
        enemyLevel = Mathf.Max(0, enemyLevel + amount);
    }
}
