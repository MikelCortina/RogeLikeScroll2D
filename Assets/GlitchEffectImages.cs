using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glitch2DEffect : MonoBehaviour
{
    [Header("Renderers que van a glitchear")]
    [Tooltip("Arrastra aquí todos los SpriteRenderer o MeshRenderer que quieras que parpadeen")]
    public List<Renderer> glitchRenderers = new List<Renderer>();

    [Header("Configuración del Glitch")]
    public float glitchDuration = 1.5f;         // Cuánto dura todo el efecto
    public float minFlickerInterval = 0.02f;    // Parpadeo muy rápido
    public float maxFlickerInterval = 0.12f;    // Parpadeo más lento (variedad)
    public bool autoStart = false;              // Si quieres que empiece al iniciar
    public bool destroyAfterGlitch = false;     // Opcional: destruir el objeto al acabar

    private Coroutine glitchCoroutine;

    private void Start()
    {
        if (autoStart)
            StartGlitch();
    }

    // Llámalo desde donde quieras (animación, colisión, etc.)
    public void StartGlitch()
    {
        if (glitchCoroutine != null)
            StopCoroutine(glitchCoroutine);

        glitchCoroutine = StartCoroutine(GlitchRoutine());
    }

    public void StopGlitch()
    {
        if (glitchCoroutine != null)
            StopCoroutine(glitchCoroutine);

        // Aseguramos que todos los renderers queden ACTIVADOS al final
        SetAllRenderersEnabled(true);
    }

    private IEnumerator GlitchRoutine()
    {
        if (glitchRenderers.Count == 0)
        {
            Debug.LogWarning("Glitch2DEffect: No hay renderers asignados.");
            yield break;
        }

        float endTime = Time.time + glitchDuration;

        while (Time.time < endTime)
        {
            // Elegimos aleatoriamente si mostramos u ocultamos cada renderer
            foreach (Renderer rend in glitchRenderers)
            {
                if (rend != null)
                {
                    // 50% de probabilidad de estar apagado en cada frame (glitch fuerte)
                    // Puedes cambiar la probabilidad para más o menos intensidad
                    bool enable = Random.value > 0.5f;
                    rend.enabled = enable;
                }
            }

            // Pequeña pausa aleatoria para que sea irregular y nervioso
            float waitTime = Random.Range(minFlickerInterval, maxFlickerInterval);
            yield return new WaitForSeconds(waitTime);
        }

        // Al terminar, dejamos todo visible y limpio
        SetAllRenderersEnabled(true);

        if (destroyAfterGlitch)
            Destroy(gameObject);
    }

    private void SetAllRenderersEnabled(bool enabled)
    {
        foreach (Renderer rend in glitchRenderers)
        {
            if (rend != null)
                rend.enabled = enabled;
        }
    }

    // OPCIONAL: Método para añadir renderers en runtime
    public void AddRenderer(Renderer renderer)
    {
        if (renderer != null && !glitchRenderers.Contains(renderer))
            glitchRenderers.Add(renderer);
    }

    // Para inspeccionar en el editor
    private void Reset()
    {
        // Auto-rellenar con los renderers del propio objeto y sus hijos
        glitchRenderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
    }
}