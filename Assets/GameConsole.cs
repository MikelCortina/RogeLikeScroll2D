using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class GameConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI consoleText;
    [SerializeField] private int maxLines = 20;

    private Queue<string> lines = new Queue<string>();

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // SOLO Debug.Log normales
        if (type != LogType.Log)
            return;

        // logString es ÚNICAMENTE el texto pasado a Debug.Log()
        AddLine(logString);
    }

    public void Log(string message)
    {
        AddLine(message);
    }

    private void AddLine(string message)
    {
        if (lines.Count >= maxLines)
            lines.Dequeue();

        lines.Enqueue(message);
        RefreshText();
    }

    private void RefreshText()
    {
        consoleText.text = string.Join("\n", lines);
    }
}
