using UnityEngine;

public class SkillTreeInitializer : MonoBehaviour
{
    [SerializeField] private SkillTreeUI skillTreeUI;

    private void Awake()
    {
        // Esto fuerza que el singleton se cree aunque esté desactivado
        var skillTree = SkillTreeUI.GetInstance();

        if (skillTree != null)
        {
            skillTree.InitializeForRun();
        }
    }
}