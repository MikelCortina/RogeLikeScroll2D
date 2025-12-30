using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Leaderboards.Models;
using System.Linq;  // Para LINQ si hace falta

public class LeaderboardUI : MonoBehaviour
{
    [Header("Config UI")]
    [SerializeField] private Transform contentParent;      // Content del Scroll View
    [SerializeField] private GameObject entryPrefab;       // Prefab EXACTO
    [SerializeField] private TextMeshProUGUI titleText;    // "Top Leaderboard"
    [SerializeField] private Button refreshButton;
    [SerializeField] private ScrollRect scrollRect;        // ← ¡AÑADE! ScrollRect del Scroll View

    private LeaderboardManager lbManager;

    private void Awake()
    {
        lbManager = FindObjectOfType<LeaderboardManager>();
        if (refreshButton) refreshButton.onClick.AddListener(() => RefreshLeaderboard(50));
    }

    public async void RefreshLeaderboard(int limit = 50)
    {
        Debug.Log("🔄 Refresh leaderboard iniciado...");

        if (lbManager == null)
        {
            Debug.LogError("❌ LeaderboardManager no encontrado!");
            return;
        }

        // Limpia TODO
        foreach (Transform child in contentParent)
            if (child != null) Destroy(child.gameObject);
        Debug.Log("🧹 Content limpiado");

        if (titleText) titleText.text = "Cargando...";
        if (scrollRect) scrollRect.normalizedPosition = new Vector2(0, 1f);  // Scroll arriba

        // Carga
        var entries = await lbManager.LoadTopScores(limit);
        Debug.Log($"📊 Entries cargados: {entries?.Count ?? 0}");  // ← DEBUG CLAVE

        if (titleText) titleText.text = $"Top {limit} Leaderboard";

        if (entries == null || entries.Count == 0)
        {
            Debug.Log("📭 No hay scores, creando mensaje vacío");
            var emptyGo = new GameObject("NoScores");
            emptyGo.transform.SetParent(contentParent, false);
            var txt = emptyGo.AddComponent<TextMeshProUGUI>();
            txt.text = "¡No hay puntuaciones aún!\nSube tu score para aparecer.";
            txt.fontSize = 28;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            emptyGo.transform.localScale = Vector3.one;
            RebuildLayout();
            return;
        }

        // Pobla entries
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var go = Instantiate(entryPrefab, contentParent, false);  // false = no world pos

            Debug.Log($"🎨 Creando entry #{entry.Rank} {entry.PlayerName ?? "Anon"}: {entry.Score}");

            // NOMBRES EXACTOS en prefab hijos
            var rankTxt = go.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            var nameTxt = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var scoreTxt = go.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

            // DEBUG si no encuentra
            if (rankTxt == null) Debug.LogWarning($"⚠️ Prefab sin 'RankText' en {go.name}");
            if (nameTxt == null) Debug.LogWarning($"⚠️ Prefab sin 'NameText' en {go.name}");
            if (scoreTxt == null) Debug.LogWarning($"⚠️ Prefab sin 'ScoreText' en {go.name}");

            if (rankTxt) rankTxt.text = $"#{entry.Rank}";
            if (nameTxt) nameTxt.text = string.IsNullOrEmpty(entry.PlayerName) ? "Anónimo" : entry.PlayerName;
            if (scoreTxt) scoreTxt.text = ((int)entry.Score).ToString("N0");

            // Colores top 3 (¡esto pinta AMARILLO el #1!)
            var color = i switch
            {
                0 => Color.yellow,      // Oro
                1 => new Color(0.8f, 0.8f, 0.8f),  // Plata
                2 => new Color(0.8f, 0.5f, 0.3f),  // Bronce
                _ => Color.white
            };
            if (rankTxt) rankTxt.color = color;
            if (nameTxt) nameTxt.color = color;
            if (scoreTxt) scoreTxt.color = color;
        }

        Debug.Log("✅ Entries poblados, rebuild...");
        RebuildLayout();
        Debug.Log("🏁 Refresh completado!");
    }

    private void RebuildLayout()
    {
        // Doble rebuild para asegurar
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
        if (contentParent.GetComponent<VerticalLayoutGroup>())
        {
            contentParent.GetComponent<VerticalLayoutGroup>().enabled = false;
            contentParent.GetComponent<VerticalLayoutGroup>().enabled = true;
        }
        // Scroll arriba
        if (scrollRect) scrollRect.normalizedPosition = new Vector2(0, 1f);
    }
}