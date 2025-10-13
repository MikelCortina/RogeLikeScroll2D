using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [System.Serializable] public class HealthChangedEvent : UnityEvent<float, float> { }

    public HealthChangedEvent OnHealthChanged;
    public UnityEvent OnDeath;
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => StatsManager.Instance != null);
        StatsManager.Instance.OnHealthChanged += HandleHealthChanged;
        StatsManager.Instance.OnPlayerDied += HandleDeath;
        //Debug.Log("Todo correcto");
    }
    private void OnEnable()
    {
       StartCoroutine(Start());
    }

    private void OnDisable()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnHealthChanged -= HandleHealthChanged;
            StatsManager.Instance.OnPlayerDied -= HandleDeath;
        }
    }

    //Cuando un enemigo golpea al jugador, este metodo es llamado para reducir la vida del jugador
    public void TakeDamage(float amount)
    {
        StatsManager.Instance.DamagePlayer(amount);
    }

    //Cuando te curas se llama a este metodo, desde habilidades, items en el suelo etc.
    public void Heal(float amount)
    {
        StatsManager.Instance.HealPlayer(amount);
    }

    // Maneja el evento de cambio de salud
    private void HandleHealthChanged(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }
    // Maneja el evento de muerte del jugador
    private void HandleDeath()
    {
        OnDeath?.Invoke();
    }
}
