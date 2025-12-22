using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class GameConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI consoleText;
    [SerializeField] private int maxLines = 20;

    private Queue<string> lines = new Queue<string>();

    public void Log(string message)
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
