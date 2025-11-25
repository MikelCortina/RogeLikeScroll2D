using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class GlitchText : MonoBehaviour
{
    private TMP_Text textComponent;

    [Header("Glitch Settings")]
    public float glitchFrequency = 0.1f;  // Cada cuánto tiempo puede aparecer un glitch
    public float glitchDuration = 0.2f;   // Duración de cada glitch
    public string glitchChars = "@#$%&*!?<>";  // Caracteres aleatorios para el glitch

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(0.05f, glitchFrequency));

            if (Random.value > 0.7f)
            {
                string originalText = textComponent.text; // Tomar el texto actual dinámicamente
                textComponent.text = GenerateGlitchText(originalText);
                yield return new WaitForSecondsRealtime(Random.Range(0.05f, glitchDuration));
                textComponent.text = originalText;
            }
        }
    }

    private string GenerateGlitchText(string baseText)
    {
        if (string.IsNullOrEmpty(baseText))
            return ""; // texto vacío no hace glitch

        try
        {
            char[] chars = baseText.ToCharArray();

            // Ejemplo: reemplazar 1-3 caracteres aleatoriamente con glitch
            int glitchCount = Random.Range(1, Mathf.Min(4, chars.Length + 1));

            for (int i = 0; i < glitchCount; i++)
            {
                int randomIndex = Random.Range(0, chars.Length); // posible IndexOutOfRange
                chars[randomIndex] = GetRandomGlitchChar();     // tu función de glitch
            }

            return new string(chars);
        }
        catch (System.IndexOutOfRangeException ex)
        {
            Debug.LogWarning($"GlitchText: Error generando glitch en '{baseText}': {ex.Message}");
            return baseText; // devuelve el texto original en caso de error
        }
    }

    // Ejemplo de función que devuelve un carácter aleatorio para glitch
    private char GetRandomGlitchChar()
    {
        const string glitchChars = "!@#$%^&*";
        return glitchChars[Random.Range(0, glitchChars.Length)];
    }
}
