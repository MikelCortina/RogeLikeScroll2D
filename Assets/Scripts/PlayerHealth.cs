using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private float iFrameDuration = 0.8f;      // tiempo de invulnerabilidad tras recibir daño
    [SerializeField] private float flashInterval = 0.08f;      // parpadeo visual durante iframes

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer[] renderersToFlash; // renderers que parpadean al recibir daño

    // Evento: (healthActual, healthMax)
    [System.Serializable] public class HealthChangedEvent : UnityEvent<int, int> { }
    public HealthChangedEvent OnHealthChanged;
    public UnityEvent OnDeath;

    private int currentHealth;
    private bool isInvulnerable = false;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(maxHealth, 1, int.MaxValue);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Método público que los enemigos pueden invocar para aplicar daño.
    /// Compatible con: hit.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (isInvulnerable) return;

        currentHealth -= amount;
        Debug.Log("Vida perdida: " + amount + ". Vida actual: " + currentHealth);
        currentHealth = Mathf.Max(currentHealth, 0);

        // Feedback: animación/sonido/etc
        // Si tienes un Animator: animator.SetTrigger("Hurt"); -> lo puedes añadir aquí.

        // Notifica a la UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // Inicia invulnerabilidad e efecto visual
        if (iFrameDuration > 0f)
            StartCoroutine(InvulnerabilityCoroutine());

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>
    /// Cura al jugador (p. ej. pickups)
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsAlive() => currentHealth > 0;

    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;

        if (renderersToFlash != null && renderersToFlash.Length > 0)
        {
            float elapsed = 0f;
            bool visible = true;
            while (elapsed < iFrameDuration)
            {
                foreach (var r in renderersToFlash)
                    if (r != null) r.enabled = visible;

                visible = !visible;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }

            // asegurar que queden visibles al final
            foreach (var r in renderersToFlash)
                if (r != null) r.enabled = true;
        }
        else
        {
            // si no hay renderers asignados, simplemente espera el tiempo
            yield return new WaitForSeconds(iFrameDuration);
        }

        isInvulnerable = false;
    }

    protected virtual void Die()
    {
        // Desactiva controles, reproduce animación, etc.
        OnDeath?.Invoke();
        Debug.Log($"{name} murió.");
    }

    private void OnValidate()
    {
        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                renderersToFlash = new SpriteRenderer[] { sr };
        }
    }
}
