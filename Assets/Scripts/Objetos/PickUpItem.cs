using UnityEngine;

public class PickupEffectItem : MonoBehaviour
{
    [SerializeField] private ScriptableObject effectToActivate;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RunEffectManager.Instance.ActivateEffect(effectToActivate);
            Destroy(gameObject); // recogerlo una sola vez
        }
    }
}
