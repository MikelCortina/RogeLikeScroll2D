using UnityEngine;

[CreateAssetMenu(menuName = "Roguelike/Upgrade_SkillNode", fileName = "UnlockSkillNode_")]
public class Upgrade_SkillNode : Upgrade
{
    public ItemNode nodeToUnlock; // El nodo del árbol que desbloquea

    public override void Apply(StatsManager statsManager)
    {
        if (nodeToUnlock == null)
        {
            Debug.LogWarning("Upgrade_SkillNode: nodeToUnlock es null!");
            return;
        }

        var skillTree = SkillTreeUI.GetInstance(); // ← ¡ESTE ES EL TRUCO!

        if (skillTree == null)
        {
            Debug.LogError("SkillTreeUI no encontrado en la escena. Asegúrate de que existe un GameObject con SkillTreeUI (puede estar desactivado).");
            return;
        }

        skillTree.TryUnlock(nodeToUnlock);
        skillTree.ForceSpawnFamilyContainingNode(nodeToUnlock);
    }

    // Para que en el panel de mejoras se vea bien
    public override string GetStatsPreview()
    {
        return $"¡DESBLOQUEA EL NODO!\n{nodeToUnlock?.displayName ?? "???"}\n{nodeToUnlock?.description ?? ""}";
    }
}