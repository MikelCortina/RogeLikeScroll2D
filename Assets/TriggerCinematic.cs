using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class TriggerCinematic : MonoBehaviour
{
    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        if (other.CompareTag("Horse"))
        {
            triggered = true;

            // Iniciar la cinemática a través de la instancia
            CinematicManager.Instance.StartCinematic();

            // Asignar el listener al botón del manager si no está asignado
            Button button = CinematicManager.Instance.continueButton;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    CinematicManager.Instance.OnPressButton();
                });
            }
            else
            {
                Debug.LogError("El botón Continue en CinematicManager es null");
            }
        }
    }
}
