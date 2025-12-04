using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    void Start()
    {
        // Mostrar cursor y desbloquearlo
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartRun()
    {
        SceneManager.LoadScene("Background Example 2");
    }
}


