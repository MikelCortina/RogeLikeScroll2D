using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;

    private float timer;
    private int score;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        score = 0;
        UpdateScoreUI();
    }

    void Update()
    {
        // Sumar 1 punto cada segundo
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            score += 1;
            timer = 0f;
            UpdateScoreUI();
        }
    }

    // Llamar a esta función cuando un enemigo muere
    public void EnemyDied()
    {
        score *= 2;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public int GetScore()
    {
        return score;
    }
}
