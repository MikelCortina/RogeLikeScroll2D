using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/SkillFamily", fileName = "NewSkillFamily")]
public class SkillFamily : ScriptableObject
{
    public string familyId;
    [Tooltip("Tres nodos que forman la familia")] public ItemNode[] nodes = new ItemNode[3];
}
