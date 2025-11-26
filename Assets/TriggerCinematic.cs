using UnityEngine;
using UnityEngine.UI;

public class TriggerCinematic : MonoBehaviour
{
    private bool triggered = false;
    private bool internalDestroy = false;
    public GameObject[] panelesActivar;

    private void OnEnable()
    {
        Debug.Log($"{name} OnEnable - active in hierarchy: {gameObject.activeInHierarchy}");
    }

    private void Start()
    {
        Debug.Log($"{name} Start - instance id: {GetInstanceID()}, tag: {gameObject.tag}");
        foreach (var panel in panelesActivar)
        {
            panel.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Horse"))
        {
            triggered = true;

            foreach (var panel in panelesActivar)
            {
                panel.SetActive(true);
            }

            var cm = CinematicManager.Instance;
            if (cm == null)
            {
                Debug.LogError("CinematicManager.Instance es null");
                return;
            }

            if (cm.cinematicTarget != null)
            {
                cm.MoveCinematicToTarget(transform.position); // Mueve el objeto al punto del trigger
            }

            cm.StartCinematic();

            // Registramos evento de fin de cinematica
            cm.onCinematicEnd += DestroySafely;

            var button = cm.continueButton;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => cm.OnPressButton());
            }

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
