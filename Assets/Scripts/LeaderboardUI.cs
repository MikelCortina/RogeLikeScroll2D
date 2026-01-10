using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI Config")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button refreshButton;
    [SerializeField] private ScrollRect scrollRect;

    private LeaderboardManager lbManager;

    private void Awake()
    {
        lbManager = FindObjectOfType<LeaderboardManager>();
        if (refreshButton) refreshButton.onClick.AddListener(() => RefreshLeaderboard(50));
    }

    public async void RefreshLeaderboard(int limit = 50)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (titleText) titleText.text = "Cargando...";
        if (scrollRect) scrollRect.normalizedPosition = new Vector2(0, 1f);

        var entries = await lbManager.LoadTopScores(limit);

        if (titleText) titleText.text = $"Top {limit} Leaderboard";

        if (entries == null || entries.Count == 0)
        {
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

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var go = Instantiate(entryPrefab, contentParent, false);

            var rankTxt = go.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            var nameTxt = go.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var scoreTxt = go.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            if (nameTxt)
            {
                string displayName = string.IsNullOrEmpty(entry.PlayerName)
                    ? "Anónimo"
                    : entry.PlayerName;

                // Fallback muy útil: si parece nombre generado y es nuestro jugador → usamos el nombre local
                if (displayName.Contains("#") && entry.PlayerId == AuthenticationService.Instance.PlayerId)
                {
                    var usernameManager = FindObjectOfType<UsernameManager>();
                    if (usernameManager != null)
                    {
                        string localName = usernameManager.GetUsername();
                        if (!string.IsNullOrWhiteSpace(localName) && localName != "Anónimo")
                        {
                            displayName = $"{localName} ★"; // Indicador visual de que es fallback
                            Debug.Log($"Usando nombre local como fallback: {localName}");
                        }
                    }
                }

                nameTxt.text = displayName;
            }
            if (rankTxt) rankTxt.text = $"#{entry.Rank}";
            if (nameTxt) nameTxt.text = string.IsNullOrEmpty(entry.PlayerName) ? "Anónimo" : entry.PlayerName;
            if (scoreTxt) scoreTxt.text = ((int)entry.Score).ToString("N0");

            var color = i switch
            {
                0 => Color.yellow,
                1 => new Color(0.8f, 0.8f, 0.8f),
                2 => new Color(0.8f, 0.5f, 0.3f),
                _ => Color.white
            };

            if (rankTxt) rankTxt.color = color;
            if (nameTxt) nameTxt.color = color;
            if (scoreTxt) scoreTxt.color = color;
        }

        RebuildLayout();
    }

    private void RebuildLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
        if (contentParent.GetComponent<VerticalLayoutGroup>())
        {
            var vlg = contentParent.GetComponent<VerticalLayoutGroup>();
            vlg.enabled = false;
            vlg.enabled = true;
        }
        if (scrollRect) scrollRect.normalizedPosition = new Vector2(0, 1f);
    }
}
