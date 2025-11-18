using UnityEngine;
using UnityEngine.UI;

public class UnscaledButtonPlayer : MonoBehaviour
{
    private Animator animator;
    private Button button;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        animator = GetComponent<Animator>();
        button = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Aseguramos que el botón siga recibiendo eventos de UI
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    void Update()
    {
        // Mantener el animator del botón en unscaled time
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Asegurar que el botón siga interactuable en pausa
        if (button != null && !button.interactable)
            button.interactable = true;
    }
}
