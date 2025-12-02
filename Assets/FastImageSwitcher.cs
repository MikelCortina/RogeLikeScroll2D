using System.Collections;
using System.Collections.Generic;  // <-- Necesario para List<T>
using UnityEngine;
using UnityEngine.UI;

public class FastImageSwitcher_FixedSize : MonoBehaviour
{
    [Header("Configuración")]
    public Image targetImage;
    public Sprite[] frames;
    public float frameRate = 24f;

    [Header("Control de Tamaño")]
    public SizeMode sizeMode = SizeMode.CustomSize;
    public Vector2 customSize = new Vector2(200, 200);

    [Header("Comportamiento")]
    public bool playOnStart = true;
    public bool loop = true;
    public bool useUnscaledTime = true;
    public bool randomizeEachLoop = true;          // NUEVA OPCIÓN
    public bool randomizeOnStart = true;           // Opcional: barajar también al iniciar

    public enum SizeMode
    {
        FirstSpriteNativeSize,
        CustomSize,
        FitToParent
    }

    private Coroutine playbackCoroutine;
    private RectTransform rectTransform;

    // Lista que contendrá el orden actual (barajado o secuencial)
    private List<Sprite> playbackOrder;
    private int currentIndex = 0;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        rectTransform = targetImage.rectTransform;
        targetImage.preserveAspect = false;
    }

    private void Start()
    {
        ApplySizeMode();
        if (playOnStart) Play();
    }

    [ContextMenu("Play")]
    public void Play()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("No hay sprites en la lista!");
            return;
        }

        Stop();
        ApplySizeMode();

        // Preparar el orden de reproducción
        PreparePlaybackOrder();

        playbackCoroutine = StartCoroutine(SwitchFrames());
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }
    }

    // Prepara la lista de reproducción (aleatoria o secuencial)
    private void PreparePlaybackOrder()
    {
        playbackOrder = new List<Sprite>(frames);

        if (randomizeEachLoop && (playbackOrder.Count > 1))
        {
            // Algoritmo Fisher-Yates shuffle
            System.Random rng = new System.Random();
            int n = playbackOrder.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Sprite temp = playbackOrder[k];
                playbackOrder[k] = playbackOrder[n];
                playbackOrder[n] = temp;
            }
        }
        // Si no queremos randomizar, queda en el orden original del array

        currentIndex = 0;
    }

    private void ApplySizeMode()
    {
        if (frames == null || frames.Length == 0) return;

        switch (sizeMode)
        {
            case SizeMode.FirstSpriteNativeSize:
                targetImage.sprite = frames[0];
                targetImage.SetNativeSize();
                break;
            case SizeMode.CustomSize:
                rectTransform.sizeDelta = customSize;
                break;
            case SizeMode.FitToParent:
                RectTransform parent = rectTransform.parent as RectTransform;
                if (parent != null)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                }
                break;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private IEnumerator SwitchFrames()
    {
        float frameTime = 1f / frameRate;
        float timer = 0f;

        while (true)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += delta;

            if (timer >= frameTime)
            {
                targetImage.sprite = playbackOrder[currentIndex];
                currentIndex++;

                // Cuando llegamos al final...
                if (currentIndex >= playbackOrder.Count)
                {
                    if (!loop)
                        yield break; // salir si no es loop

                    // Si está activado, barajamos de nuevo para el próximo ciclo
                    if (randomizeEachLoop)
                        PreparePlaybackOrder();
                    else
                        currentIndex = 0; // volver al principio sin barajar
                }

                timer -= frameTime;
            }

            yield return null;
        }
    }

    // Resto de métodos de contexto (FitToParent, etc.) sin cambios...
    [ContextMenu("Ajustar al tamaño del padre")]
    public void FitToParent()
    {
        sizeMode = SizeMode.FitToParent;
        ApplySizeMode();
    }

    [ContextMenu("Usar tamaño del primer sprite")]
    public void UseFirstSpriteSize()
    {
        sizeMode = SizeMode.FirstSpriteNativeSize;
        ApplySizeMode();
    }

    [ContextMenu("Aplicar tamaño personalizado actual")]
    public void ApplyCustomSizeNow()
    {
        sizeMode = SizeMode.CustomSize;
        ApplySizeMode();
    }
}