using UnityEngine;
using UnityEngine.SceneManagement;

public class RunResetter : MonoBehaviour
{
    public RunEffectManager runEffectManager;
    public GameObject panel;
    public GameObject panelPrefab;
    public GameObject panel2;
    public GameObject panel3;
    [SerializeField] private ScoreManager scoreManager;     // Tu manager de score
    [SerializeField] private LeaderboardUI leaderboardUI;  // Opcional, para refresh
    [SerializeField] private LeaderboardManager lbManager;  // Arrastra en Inspector


    /// <summary>
    /// Reinicia absolutamente todo, como detener y volver a iniciar Play en editor.
    /// </summary>
    /// 
    public void Awake()
    {
        lbManager = FindObjectOfType<LeaderboardManager>();
        if (lbManager == null)
            Debug.LogWarning("No se encontró ningún LeaderboardManager en la escena");

        leaderboardUI = FindObjectOfType<LeaderboardUI>();
        if (leaderboardUI == null)
            Debug.LogWarning("No se encontró ningún LeaderboardUI en la escena");
    }
    public void ResetRun()
    {
        scoreManager.score = 0;
        runEffectManager.ClearAllEffects();
        RunInventory.Instance.ResetInventory();
       //Debug.Log("=== Reinicio total de la run ===");

        // 1️⃣ Destruir todos los objetos de la escena
        foreach (var obj in FindObjectsOfType<GameObject>())
        {
            Destroy(obj);
        }

        // 2️⃣ Recargar la escena
        // Esto es importante porque destruyendo objetos no reinicia cosas como la escena en sí
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        SceneManager.LoadScene("MainMenu");
    }

    // Método de ejemplo para llamar desde PlayerDeath
    public async void OnPlayerDeath()
    {
        int finalScore = scoreManager.score;

        // Subir score
        string playerName = FindObjectOfType<UsernameManager>().GetUsername();

        lbManager.SubmitScore(finalScore);

        // Obtener tu score y rank
        int miScore = await lbManager.GetPlayerScoreAsync();

        Debug.Log($"¡Subido! Tu rank: ~{miScore} pts");

        // Refrescar leaderboard
        if (leaderboardUI) leaderboardUI.RefreshLeaderboard();

        EndGameScreen();
    }

    public void EndGameScreen()
    {
        panel.SetActive(true);
        panelPrefab.SetActive(true);
        panel2.SetActive(true);
        panel3.SetActive(true);
    }
}
