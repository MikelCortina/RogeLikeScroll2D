using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ButtonGlitcher : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Glitch Settings")]
    public float glitchInterval = 0.03f;          // Qué tan rápido glitchea
    public string glitchChars = "@#$%&*!?<>";     // Caracteres de glitch
    public int intensity = 8;                     // Cuántos caracteres cambiar por ciclo

    [Header("Audio")]
    public AudioSource audioSource;               // Sonido al soltar el click
    public AudioClip releaseSound;

    private List<TMP_Text> allTexts = new List<TMP_Text>();
    private Dictionary<TMP_Text, string> originalTexts = new Dictionary<TMP_Text, string>();
    private bool isHolding = false;
    private Coroutine glitchRoutine;

    private void Awake()
    {
        // Buscar textos del botón y sus hijos
        allTexts.AddRange(GetComponentsInChildren<TMP_Text>(true));

        foreach (var txt in allTexts)
            originalTexts[txt] = txt.text;

        // Buscar AudioSource si no está asignado
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;

        if (glitchRoutine == null)
            glitchRoutine = StartCoroutine(GlitchLoop());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;

        // Restaurar texto original
        foreach (var txt in allTexts)
            txt.text = originalTexts[txt];

        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        // ▶️ Reproducir sonido al soltar el click
        if (audioSource != null && releaseSound != null)
            PlaySoundDetached(releaseSound);
    }
    public static void PlaySoundDetached(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("OneShotAudio");
        AudioSource src = go.AddComponent<AudioSource>();

        src.clip = clip;
        src.volume = volume;
        src.Play();

        Object.Destroy(go, clip.length);
    }

    private IEnumerator GlitchLoop()
    {
        while (isHolding)
        {
            foreach (var txt in allTexts)
            {
                txt.text = GenerateGlitch(txt.text);
            }

            yield return new WaitForSecondsRealtime(glitchInterval);
        }
    }

    private string GenerateGlitch(string current)
    {
        if (string.IsNullOrEmpty(current))
            return current;

        char[] chars = current.ToCharArray();

        for (int i = 0; i < intensity; i++)
        {
            int index = Random.Range(0, chars.Length);
            chars[index] = glitchChars[Random.Range(0, glitchChars.Length)];
        }

        return new string(chars);
    }
}
