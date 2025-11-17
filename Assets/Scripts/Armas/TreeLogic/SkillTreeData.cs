using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/SkillTreeData", fileName = "SkillTreeData")]
public class SkillTreeData : ScriptableObject
{
    public List<ItemNode> nodes = new List<ItemNode>();
}
