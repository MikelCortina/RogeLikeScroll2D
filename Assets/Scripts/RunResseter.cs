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
        int finalScore = scoreManager.score;  // O tu variable de puntuación

        // Sube score (actualiza si ya existe, toma el más alto)
         lbManager.SubmitScore(finalScore);

        // Opcional: Muestra tu rank personal
        int miScore = await lbManager.GetPlayerScoreSafe();

        Debug.Log($"¡Subido! Tu rank: ~{miScore} pts");  // O en UI

        // Opcional: Refresca leaderboard para ver cambios
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
