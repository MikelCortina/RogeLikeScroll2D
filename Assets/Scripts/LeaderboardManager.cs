using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static event System.Action OnServicesInitialized;  // ← AÑADE ESTO
    [Header("Config")]
    [SerializeField] private string LeaderboardId = "mi-leaderboard-simple";

    private bool initialized = false;
    public bool IsInitialized => initialized;

    private void Awake()
    {
        // DDOL
        if (FindObjectsOfType<LeaderboardManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        // Inicializa Unity Services al inicio
        _ = Initialize();
    }

    public async Task Initialize()
    {
        if (initialized) return;

        try
        {
            await UnityServices.InitializeAsync();

            // ← Solo aquí se hace login anónimo, en ningún otro sitio del proyecto
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("✅ Login anónimo completado - PlayerId: " + AuthenticationService.Instance.PlayerId);
            }

            initialized = true;
            OnServicesInitialized?.Invoke();  // ← Dispara el evento cuando todo está 100% listo
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error crítico inicializando Unity Services: " + e);
        }
    }

    // Subir score
    public async void SubmitScore(int score)
    {
        if (!initialized) await Initialize();

        try
        {
            var response = await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score);
            Debug.Log($"✅ Score subido: {response.Score} (Rank: {response.Rank})");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error submit score: " + e.Message);
        }
    }

    // Obtener top N
    public async Task<List<LeaderboardEntry>> LoadTopScores(int limit = 10)
    {
        if (!initialized) await Initialize();

        try
        {
            var options = new GetScoresOptions { Limit = limit };
            var response = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, options);
            return response.Results;
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error LoadTopScores: " + e.Message);
            return null;
        }
    }

    // Obtener score del jugador, devuelve 0 si aún no subió ninguno
    public async Task<int> GetPlayerScoreAsync()
    {
        if (!initialized) await Initialize();

        try
        {
            var options = new GetPlayerRangeOptions { RangeLimit = 1 };
            var response = await LeaderboardsService.Instance.GetPlayerRangeAsync(LeaderboardId, options);

            var playerEntry = response.Results.FirstOrDefault(
                e => e.PlayerId == AuthenticationService.Instance.PlayerId
            );

            return playerEntry != null ? (int)playerEntry.Score : 0;
        }
        catch
        {
            // Cualquier error → asumimos que no hay score (el caso más común)
            Debug.Log("ℹ️ No se encontró puntuación del jugador (o error de conexión)");
            return 0;
        }
    }
}
