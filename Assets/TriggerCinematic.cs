using UnityEngine;
using UnityEngine.UI;

public class TriggerCinematic : MonoBehaviour
{
    private bool triggered = false;
    private bool internalDestroy = false;

    private void OnEnable()
    {
        Debug.Log($"{name} OnEnable - active in hierarchy: {gameObject.activeInHierarchy}");
    }

    private void Start()
    {
        Debug.Log($"{name} Start - instance id: {GetInstanceID()}, tag: {gameObject.tag}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{name} OnTriggerEnter2D by {other.name} (tag: {other.tag})");

        if (triggered)
        {
            Debug.Log($"{name} already triggered, ignoring.");
            return;
        }

        if (other.CompareTag("Horse"))
        {
           
            triggered = true;
            Debug.Log($"{name} triggered by Horse. Calling CinematicManager.Instance.StartCinematic()");
             DestroySafely();

            var cm = CinematicManager.Instance;
            if (cm == null)
            {
                Debug.LogError("CinematicManager.Instance es null");
                return;
            }

            cm.StartCinematic();

            Button button = cm.continueButton;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    Debug.Log("Continue button pressed -> CinematicManager.OnPressButton()");
                    cm.OnPressButton();
                });
            }
            else
            {
                Debug.LogError("El botón Continue en CinematicManager es null");
            }

            // Opcional: desactivar el collider para evitar reentradas físicas
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }

    public void DestroySafely()
    {
        internalDestroy = true;
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        Debug.Log($"{name} OnDisable");
    }

    private void OnDestroy()
    {
        Debug.Log($"{name} OnDestroy (posible causa: buscar 'Destroy(' en proyecto, o escena cargada, o Timeline).");
    }
}
