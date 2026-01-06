using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;  // Para LeaderboardEntry, LeaderboardScoresPage
using UnityEngine;
using Unity.Services.Leaderboards.Models;
using System.Linq;  // Para .FirstOrDefault()

public class LeaderboardManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string LeaderboardId = "mi-leaderboard-simple"; // ¡Pon TU ID aquí!

    private bool initialized = false;

    async void Start()
    {
        await Initialize();
    }

    public async Task Initialize()
    {
        if (initialized) return;

        try
        {
            await UnityServices.InitializeAsync();

            // Solo autenticar si no está autenticado
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("✅ Autenticado: " + AuthenticationService.Instance.PlayerId);
            }
            else
            {
                Debug.Log("ℹ️ Ya estaba autenticado: " + AuthenticationService.Instance.PlayerId);
            }

            initialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error init: " + e.Message);
        }
    }


    // Subir puntuación
    public async void SubmitScore(int score)
    {
        if (!initialized) { await Initialize(); return; }
        try
        {
            var response = await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score);
            Debug.Log($"✅ Score subido: {response.Score} (Rank: {response.Rank})");
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error submit: " + e.Message);
        }
    }

    // Cargar top N (global)
    public async Task<List<LeaderboardEntry>> LoadTopScores(int limit = 10)
    {
        if (!initialized) { await Initialize(); return null; }
        try
        {
            var options = new GetScoresOptions { Limit = limit };
            var response = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, options);

            Debug.Log($"✅ Top {limit} cargados (Total results: {response.Total}):");
            foreach (var entry in response.Results)  // ← ¡Results, no Entries!
            {
                string name = entry.PlayerName ?? "Anónimo";
                Debug.Log($"   #{entry.Rank} {name}: {entry.Score}");
            }
            return response.Results;  // ← List<LeaderboardEntry> directo
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error load: " + e.Message);
            return null;
        }
    }

    // Tu puntuación
    public async Task<int> GetPlayerScoreSafe()
    {
        if (!initialized) { await Initialize(); return 0; }

        try
        {
            // Pedimos solo 1: el jugador actual (mínimo permitido)
            var options = new GetPlayerRangeOptions { RangeLimit = 1 };
            var response = await LeaderboardsService.Instance.GetPlayerRangeAsync(LeaderboardId, options);

            // Buscamos nuestra entry en los resultados
            var playerEntry = response.Results.FirstOrDefault(e => e.PlayerId == AuthenticationService.Instance.PlayerId);

            if (playerEntry != null)
            {
                int score = (int)playerEntry.Score;
                Debug.Log($"✅ Mi score (safe): {score} (Rank: {playerEntry.Rank})");
                return score;
            }
            else
            {
                // Esto pasa si el jugador NUNCA ha subido un score
                Debug.Log("ℹ️ Mi score: Aún no tiene puntuación subida (nuevo jugador)");
                return 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error GetPlayerRange: " + e.Message);
            return 0;
        }
    }
}