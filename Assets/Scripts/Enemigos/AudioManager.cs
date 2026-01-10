using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Configuración de sonidos")]
    public int maxSimultaneousDeathSounds = 5;
    public int maxSimultaneousImpactSounds = 10;

    private int currentDeathSounds = 0;
    private int currentImpactSounds = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ---- Sonidos de muerte ----
    public void PlayDeathSound(AudioClip clip, Vector3 position)
    {
        if (clip == null || currentDeathSounds >= maxSimultaneousDeathSounds) return;

        PlaySound(clip, position, SoundType.Death);
    }

    // ---- Sonidos de impacto ----
    public void PlayImpactSound(AudioClip clip, Vector3 position)
    {
        if (clip == null || currentImpactSounds >= maxSimultaneousImpactSounds) return;

        PlaySound(clip, position, SoundType.Impact);
    }

    private enum SoundType { Death, Impact }

    // ---- Función genérica para reproducir sonidos ----
    private void PlaySound(AudioClip clip, Vector3 position, SoundType type)
    {
        AudioSource audio = new GameObject("TempSound").AddComponent<AudioSource>();
        audio.transform.position = position;
        audio.clip = clip;
        audio.spatialBlend = 1f;
        // Volumen base 0.5, y sube 0.15 por cada muerte simultánea hasta un máximo de 1
        audio.volume = Mathf.Clamp(10f + 1f * currentDeathSounds, 0f, 1f);

        // Pitch con un poco más de variación para sonar más dinámico
        audio.pitch = Random.Range(0.8f, 1.15f);
        audio.Play();

        // Incrementamos el contador correspondiente
        switch (type)
        {
            case SoundType.Death:
                currentDeathSounds++;
                StartCoroutine(DecreaseCounterAfter(clip.length, SoundType.Death));
                break;
            case SoundType.Impact:
                currentImpactSounds++;
                StartCoroutine(DecreaseCounterAfter(clip.length, SoundType.Impact));
                break;
        }

        Destroy(audio.gameObject, clip.length);
    }

    // Coroutine sin ref, usando enum para decidir qué contador decrementar
    private IEnumerator DecreaseCounterAfter(float time, SoundType type)
    {
        yield return new WaitForSeconds(time);

        switch (type)
        {
            case SoundType.Death:
                currentDeathSounds = Mathf.Max(0, currentDeathSounds - 1);
                break;
            case SoundType.Impact:
                currentImpactSounds = Mathf.Max(0, currentImpactSounds - 1);
                break;
        }
    }
}
