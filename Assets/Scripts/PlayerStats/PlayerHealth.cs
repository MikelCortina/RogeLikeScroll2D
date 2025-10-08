using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [System.Serializable] public class HealthChangedEvent : UnityEvent<float, float> { }

    public HealthChangedEvent OnHealthChanged;
    public UnityEvent OnDeath;

    private void OnEnable()
    {
        StatsManager.Instance.OnHealthChanged += HandleHealthChanged;
        StatsManager.Instance.OnPlayerDied += HandleDeath;
    }

    private void OnDisable()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnHealthChanged -= HandleHealthChanged;
            StatsManager.Instance.OnPlayerDied -= HandleDeath;
        }
    }

    public void TakeDamage(float amount)
    {
        StatsManager.Instance.DamagePlayer(amount);
    }

    public void Heal(float amount)
    {
        StatsManager.Instance.HealPlayer(amount);
    }

    private void HandleHealthChanged(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    private void HandleDeath()
    {
        OnDeath?.Invoke();
    }
}
