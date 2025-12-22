using UnityEngine;

public class ConsoleManager : MonoBehaviour
{
    public static ConsoleManager Instance { get; private set; }

    [SerializeField] private GameConsole gameConsole;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Log(string message)
    {
        gameConsole.Log(message);
    }
}
