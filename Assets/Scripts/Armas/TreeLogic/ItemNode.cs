using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "SkillTree/ItemNode", fileName = "ItemNode")]
public class ItemNode : ScriptableObject
{
    public string nodeId;
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    public ScriptableObject effectToActivate;

    [Header("Visual / Button Prefab")]
    [Tooltip("Prefab del botón que representará este nodo (opcional). Si está vacío se usará el default en SkillTreeUI.")]
    public SkillNodeButton buttonPrefab;

    [Header("Cost & requirements")]
    public int cost = 0; // coste en moneda/puntos

    public List<ItemNode> requiredEffectIdsToRemove = new List<ItemNode>();
    public List<string> prerequisiteNodeIds = new List<string>();

    [HideInInspector] public bool debug_unlocked = false;
}
