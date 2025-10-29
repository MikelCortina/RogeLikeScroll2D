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


    [Header("Cost & requirements")]
    public int cost = 0; // coste en moneda/puntos

    public List<ItemNode> requiredEffectIdsToRemove = new List<ItemNode>();
    public List<string> prerequisiteNodeIds = new List<string>(); 

    [HideInInspector] public bool debug_unlocked = false; 
}
