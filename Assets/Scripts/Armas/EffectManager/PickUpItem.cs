using UnityEngine;

public class PickupEffectItem : MonoBehaviour
{
    public SkillTreeUI skillTreeUI; // asignar en inspector (el panel)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Abrir panel (pausa dentro de SkillTreeUI)
            if (skillTreeUI != null)
            {
                skillTreeUI.Show(true);
            }
            else
            {
                Debug.LogWarning("SkillTreeUI no asignado en PickupEffectItem");
            }
        }
    }
}
