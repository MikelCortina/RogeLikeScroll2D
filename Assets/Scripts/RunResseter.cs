using UnityEngine;
using UnityEngine.SceneManagement;

public class RunResetter : MonoBehaviour
{
    /// <summary>
    /// Reinicia absolutamente todo, como detener y volver a iniciar Play en editor.
    /// </summary>
    public void ResetRun()
    {
        RunInventory.Instance.ResetInventory();
        Debug.Log("=== Reinicio total de la run ===");

        // 1️⃣ Destruir todos los objetos de la escena
        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            Destroy(obj);
        }

        // 2️⃣ Recargar la escena
        // Esto es importante porque destruyendo objetos no reinicia cosas como la escena en sí
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Método de ejemplo para llamar desde PlayerDeath
    public void OnPlayerDeath()
    {
        ResetRun();
    }
}
