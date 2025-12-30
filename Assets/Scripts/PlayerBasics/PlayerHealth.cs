using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [System.Serializable]
    public class HealthChangedEvent : UnityEvent<float, float> { }

    public HealthChangedEvent OnHealthChanged;
    public UnityEvent OnDeath;

    public AudioClip[] hurtSounds;       // Lista de sonidos de daño
    public AudioClip dodgeSound;         // Sonido de dodge
    public float soundCooldown = 0.2f;   // Tiempo mínimo entre sonidos (ajustable)

    private float lastSoundTime;
    private AudioSource audioSource;

    public Material myMaterial;          // Material del jugador (debe ser una instancia o shared)

    // IDs de propiedades para mejor rendimiento (opcional pero recomendado)
    private int distorsionID;
    private int dodgeLineID;

    private IEnumerator Start()
    {
        // Inicializamos los valores del shader
        distorsionID = Shader.PropertyToID("_Distorsion");
        dodgeLineID = Shader.PropertyToID("_DodgeLineAmount");

        myMaterial.SetFloat(distorsionID, 0f);
        myMaterial.SetVector(dodgeLineID, new Vector4(0f, 150f, 0f, 0f)); // Valor por defecto

        // Esperamos a que StatsManager esté disponible
        yield return new WaitUntil(() => StatsManager.Instance != null);

        StatsManager.Instance.OnHealthChanged += HandleHealthChanged;
        StatsManager.Instance.OnPlayerDied += HandleDeath;

        // Configuramos AudioSource
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

    // Daño cuerpo a cuerpo
    public void TakeMeleDamage(float amount)
    {
        float finalDamage = StatsCommunicator.Instance.CalculateMeleTakenDamage(amount);
        StatsManager.Instance.DamagePlayer(finalDamage);

        if (finalDamage > 0)
        {
            PlayRandomHurtSound();
            StartCoroutine(ShaderAnim(0.1f, 1f));
        }
        else
        {
            PlayRandomDodgeSound();
            StartCoroutine(DodgeAnim(0.1f, new Vector2(0f, 600f)));
        }
    }

    // Animación de distorsión por golpe recibido
    private IEnumerator ShaderAnim(float duration, float distorsionValue)
    {
        myMaterial.SetFloat(distorsionID, distorsionValue);
        yield return new WaitForSecondsRealtime(duration);
        myMaterial.SetFloat(distorsionID, 0f);
    }

    // Animación de dodge (líneas/scanlines)
    private IEnumerator DodgeAnim(float duration, Vector2 lineAmount)
    {
        myMaterial.SetVector(dodgeLineID, new Vector4(lineAmount.x, lineAmount.y, 0f, 0f));
        yield return new WaitForSecondsRealtime(duration);
        myMaterial.SetVector(dodgeLineID, new Vector4(0f, 150f, 0f, 0f)); // Valor por defecto
    }

    // Daño a distancia
    public void TakeRangeDamage(float amount)
    {
        float finalDamage = StatsCommunicator.Instance.CalculateRangeTakenDamage(amount);
        StatsManager.Instance.DamagePlayer(finalDamage);

        if (finalDamage > 0)
            PlayRandomHurtSound();
    }

    // Curación
    public void Heal(float amount)
    {
        StatsManager.Instance.HealPlayer(amount);
    }

    // Reproduce un sonido de daño aleatorio con cooldown
    private void PlayRandomHurtSound()
    {
        if (Time.time - lastSoundTime < soundCooldown) return;
        if (hurtSounds == null || hurtSounds.Length == 0) return;

        AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
        audioSource.PlayOneShot(clip);
        lastSoundTime = Time.time;
    }

    // Reproduce el sonido de dodge con cooldown
    private void PlayRandomDodgeSound()
    {
        if (Time.time - lastSoundTime < soundCooldown) return;
        if (dodgeSound == null) return;

        audioSource.PlayOneShot(dodgeSound);
        lastSoundTime = Time.time;
    }

    // Eventos de StatsManager
    private void HandleHealthChanged(float current, float max)
    {
        OnHealthChanged?.Invoke(current, max);
    }

    private void HandleDeath()
    {
        OnDeath?.Invoke();
    }
}