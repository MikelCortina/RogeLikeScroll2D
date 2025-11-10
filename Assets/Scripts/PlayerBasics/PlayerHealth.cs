using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [System.Serializable] public class HealthChangedEvent : UnityEvent<float, float> { }

    public HealthChangedEvent OnHealthChanged;
    public UnityEvent OnDeath;

    public AudioClip[] hurtSounds; // 🎵 Lista de sonidos de daño
    public float soundCooldown = 0.3f; // ⏳ Tiempo mínimo entre sonidos
    private float lastSoundTime;

    private AudioSource audioSource;

    public Material myMaterial;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => StatsManager.Instance != null);
        StatsManager.Instance.OnHealthChanged += HandleHealthChanged;
        StatsManager.Instance.OnPlayerDied += HandleDeath;
        //Debug.Log("Todo correcto");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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
    public void TakeMeleDamage(float amount)
    {
        float finalDamage = StatsCommunicator.Instance.CalculateMeleTakenDamage(amount);
        StatsManager.Instance.DamagePlayer(finalDamage);
        PlayRandomHurtSound();
        StartCoroutine(ShaderAnim(0.1f));
        StartCoroutine(HitPause(0.02f)); // Pausa de 0.1 segundos
                                         //myMaterial.SetFloat("_Distorsion", 0f); // Activa
    }

    public IEnumerator ShaderAnim(float amount)
    {
        myMaterial.SetFloat("_Distorsion", 1f); // Activa

        yield return new WaitForSecondsRealtime(amount); // espera tiempo real

        myMaterial.SetFloat("_Distorsion", 0f); // Activa
    }
    public void TakeRangeDamage(float amount)
    {
        float finalDamage = StatsCommunicator.Instance.CalculateRangeTakenDamage(amount);
        StatsManager.Instance.DamagePlayer(finalDamage);
        PlayRandomHurtSound();
        StartCoroutine(HitPause(0.02f));
    }

    private IEnumerator HitPause(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f; // Pausa el juego
        yield return new WaitForSecondsRealtime(duration); // espera tiempo real
        Time.timeScale = originalTimeScale; // reanuda el juego
    }
    private void PlayRandomHurtSound()
    {
        // Cooldown para evitar spam
        if (Time.time - lastSoundTime < soundCooldown)
            return;

        if (hurtSounds.Length == 0)
            return;

        // Selecciona un sonido aleatorio
        AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];

        // Reproduce el sonido
        audioSource.PlayOneShot(clip);

        // Registra el último momento en que se reprodujo
        lastSoundTime = Time.time;
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
