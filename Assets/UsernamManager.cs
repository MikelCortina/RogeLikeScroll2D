using System.Threading.Tasks;
using TMPro;                       // Para TMP_InputField y TMP_Text
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;              // Para el Button si usas UI clásico

public class UsernameManager : MonoBehaviour
{
    public static UsernameManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInputField;     // Arrastra tu InputField aquí
    [SerializeField] private TMP_Text feedbackText;             // Mensaje de éxito/error
    [SerializeField] private Button saveButton;                 // Botón para guardar

    [Header("Config")]
    [SerializeField] private string defaultNamePrefix = "Jugador"; // Si no hay nada

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Desactivar botón hasta que esté listo
        if (saveButton) saveButton.interactable = false;
    }

    private void OnEnable()
    {
        LeaderboardManager.OnServicesInitialized += OnUnityServicesReady;

        if (saveButton)
            saveButton.onClick.AddListener(OnSaveButtonClicked);
    }

    private void OnDisable()
    {
        LeaderboardManager.OnServicesInitialized -= OnUnityServicesReady;

        if (saveButton)
            saveButton.onClick.RemoveListener(OnSaveButtonClicked);
    }

    private async void OnUnityServicesReady()
    {
        // Ahora seguro: cargar nombre actual si existe
        await LoadAndShowCurrentName();

        // Activar botón solo cuando todo esté inicializado
        if (saveButton) saveButton.interactable = true;

        // Opcional: pre-rellenar el input con el nombre actual
        if (nameInputField && AuthenticationService.Instance.PlayerName != null)
        {
            nameInputField.text = AuthenticationService.Instance.PlayerName.Split('#')[0]; // Quitar el #1234
        }
    }

    private async Task LoadAndShowCurrentName()
    {
        try
        {
            string currentName = await AuthenticationService.Instance.GetPlayerNameAsync();
            if (string.IsNullOrEmpty(currentName))
            {
                currentName = PlayerPrefs.GetString("LocalPlayerName", defaultNamePrefix + Random.Range(1000, 9999));
            }

            if (feedbackText)
                feedbackText.text = $"Nombre actual: <color=yellow>{currentName}</color>";

            PlayerPrefs.SetString("LocalPlayerName", currentName);
        }
        catch
        {
            if (feedbackText) feedbackText.text = "No se pudo cargar el nombre actual.";
        }
    }

    private async void OnSaveButtonClicked()
    {
        if (nameInputField == null) return;

        string newName = nameInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(newName))
        {
            if (feedbackText) feedbackText.text = "<color=red>¡El nombre no puede estar vacío!</color>";
            return;
        }

        if (newName.Length > 50)
        {
            if (feedbackText) feedbackText.text = "<color=red>Máximo 50 caracteres.</color>";
            return;
        }

        if (newName.Contains(" "))
        {
            if (feedbackText) feedbackText.text = "<color=orange>Sin espacios (se añadirá #XXXX automáticamente).</color>";
            newName = newName.Replace(" ", ""); // Quitamos espacios manualmente para evitar rechazo
        }

        await SetPlayerNameAsync(newName);
    }

    public async Task SetPlayerNameAsync(string name)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            if (feedbackText) feedbackText.text = "<color=red>Error: No estás autenticado.</color>";
            Debug.LogError("No autenticado al intentar cambiar nombre");
            return;
        }

        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
            Debug.Log($"Nombre actualizado en la nube: {name}");

            string finalName = AuthenticationService.Instance.PlayerName; // Incluye el #XXXX si se añadió
            PlayerPrefs.SetString("LocalPlayerName", finalName);

            if (feedbackText)
                feedbackText.text = $"<color=green>¡Nombre cambiado a: {finalName}!</color>";

            // Opcional: actualizar UI del leaderboard o menú principal
            // FindObjectOfType<LeaderboardUI>()?.RefreshLeaderboard();
        }
        catch (AuthenticationException ex)
        {
            string msg = ex.ErrorCode == AuthenticationErrorCodes.InvalidSessionToken
                ? "Sesión inválida → reintentando..."
                : $"Error {ex.ErrorCode}: {ex.Message}";

            if (feedbackText) feedbackText.text = $"<color=red>{msg}</color>";
            Debug.LogError("Error al actualizar nombre: " + ex);

            // Reintento automático si es token inválido
            if (ex.ErrorCode == AuthenticationErrorCodes.InvalidSessionToken)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                await SetPlayerNameAsync(name); // Reintentar
            }
        }
        catch (System.Exception ex)
        {
            if (feedbackText) feedbackText.text = "<color=red>Error inesperado.</color>";
            Debug.LogError("Excepción al actualizar nombre: " + ex);
        }
    }

    public string GetUsername()
    {
        return PlayerPrefs.GetString("LocalPlayerName", "Anónimo");
    }
}