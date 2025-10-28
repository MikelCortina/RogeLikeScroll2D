using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "SkillTree/ItemNode", fileName = "ItemNode")]
public class ItemNode : ScriptableObject
{
    public string nodeId; // id único (ej: "node_jump_1")
    public string displayName;
    public Sprite icon;
    [TextArea] public string description;

    public ScriptableObject effectToActivate; // tu efecto (puede implementar IPersistentEffect)

    [Header("Cost & requirements")]
    public int cost = 0; // coste en moneda/puntos
    public List<string> requiredItemIds = new List<string>(); // ids de items que hay que consumir
    public List<string> prerequisiteNodeIds = new List<string>(); // nodos previos requeridos

    [HideInInspector] public bool debug_unlocked = false; // solo debug
}
