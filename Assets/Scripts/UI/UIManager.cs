using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
///  Manages the display and organization of UI windows in the application.
///  It allows showing, hiding, and retrieving UI windows by their unique identifiers.
/// </summary>
public class UIManager : Singleton<UIManager>
{
    // List of all UI windows 
    [SerializeField] private List<UIWindow> uiWindows = new List<UIWindow>();
    
    /// <summary>
    ///  Shows the UI window with the specified identifier.
    /// </summary>
    /// <param name="windowUI"> Window identifier</param>
    public void ShowUI(string windowUI)
    {
        foreach (var window in uiWindows)
        {
            if (window.WindowID == windowUI)
            {
                window.Show();
                return;
            }
        }
        Debug.LogWarning($"UI Window with name {windowUI} not found.");
    }
    
    /// <summary>
    ///  Hides the UI window with the specified identifier.
    /// </summary>
    /// <param name="windowUI"> Window identifier</param>
    public void HideUI(string windowUI)
    {
        foreach (var window in uiWindows)
        {
            if (window.WindowID == windowUI)
            {
                window.Hide();
                return;
            }
        }
        Debug.LogWarning($"UI Window with name {windowUI} not found.");
    }
    
    /// <summary>
    /// Hides all UI windows.
    /// </summary>
    public void HideAllUI()
    {
        foreach (var window in uiWindows)
        {
            window.Hide();
        }
    }

    /// <summary>
    ///  Retrieves the UI window with the specified identifier.
    /// </summary>
    /// <param name="windowUI"></param>
    /// <returns></returns>
    public UIWindow GetUIWindow(string windowUI)
    {
        foreach (var window in uiWindows)
        {
            if (window.WindowID == windowUI)
            {
                return window;
            }
        }
        Debug.LogWarning($"UI Window with name {windowUI} not found.");
        return null;
    }

    #region Editor
    [Button]
    private void GetAllUIWindows()
    {
        uiWindows.Clear();
        UIWindow[] windows = FindObjectsByType<UIWindow>(FindObjectsSortMode.InstanceID);
        uiWindows.AddRange(windows);
    }

    #endregion
}
public static class UIIdentifiers
{
    public const string Ability = "AbilityUI";
    public const string Defeat = "DefeatUI";
    public const string Settings = "SettingsUI";
    public const string MainMenu = "MainMenuUI";
    public const string Pause = "PauseUI";
    public const string Gameplay = "GameplayUI";
}