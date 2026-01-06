using UnityEngine;

[CreateAssetMenu(
    fileName = "XpOnEnemyKillEffect",
    menuName = "Effects/Persistent/Xp On Enemy Kill"
)]
public class XpOnEnemyKillEffect : ScriptableObject, IPersistentEffect
{
    [SerializeField] private float xpIncreasePerKill = 0.01f;

 
    private bool isApplied;

    // Aplica el efecto persistentemente al jugador
    public void ApplyTo(GameObject player)
    {
        if (isApplied) return;

        EnemyEvents.OnEnemyDied += OnEnemyDied;
        isApplied = true;
    }

    // Remueve el efecto persistentemente
    public void RemoveFrom(GameObject player)
    {
        if (!isApplied) return;

        EnemyEvents.OnEnemyDied -= OnEnemyDied;
        isApplied = false;
    }

    // Resetea el efecto en tiempo de ejecución
    public void ResetRuntime()
    {
        EnemyEvents.OnEnemyDied -= OnEnemyDied;
        isApplied = false;
    }

    // Método adicional para ejecutar el efecto manualmente
    public void Execute(Vector2 position, GameObject owner = null)
    {
      
            StatsManager.Instance.XpGainMultiplier(xpIncreasePerKill);
        
     
    }

    // Lógica cuando un enemigo muere
    private void OnEnemyDied(EnemyBase enemy)
    {
        StatsManager.Instance.XpGainMultiplier(xpIncreasePerKill);

    }
}
