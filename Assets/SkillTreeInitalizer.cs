using UnityEngine;

public class SkillTreeInitializer : MonoBehaviour
{
    [SerializeField] private SkillTreeUI skillTreeUI;

    private void Awake()
    {
        // Esto se ejecuta al cargar la escena de juego (siempre)
        if (skillTreeUI != null)
        {
            skillTreeUI.InitializeForRun();
        }
    }
}