using UnityEngine;

public class ScriptBlockerByAnimation : MonoBehaviour
{
    private MonoBehaviour[] scripts;
    private bool alreadyUnlocked = false;
    private Animator animator;

    private void Awake()
    {
        // Guardamos referencia al Animator
        animator = GetComponent<Animator>();

        // Bloquear otros scripts
        scripts = GetComponents<MonoBehaviour>();
        foreach (var s in scripts)
        {
            if (s != this)
                s.enabled = false;
        }
    }

    // Llamado por Animation Event
    public void UnlockScripts()
    {
        if (alreadyUnlocked) return;

        alreadyUnlocked = true;

        foreach (var s in scripts)
        {
            s.enabled = true;
        }

        Debug.Log("Scripts desbloqueados por animación.");
    }

    // Llamado por Animation Event para cambiar de animación
    public void PlayNextAnimation(string animationName)
    {
        animator.Play(animationName);
        Debug.Log("Cambiada a animación: " + animationName);
    }
}
