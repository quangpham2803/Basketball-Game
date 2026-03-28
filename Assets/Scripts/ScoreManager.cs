using UnityEngine;
using TMPro;
using System;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int pointsPerScore = 2;
    [SerializeField] private int pointsPerSwish = 3;

    public int Score { get; private set; }
    public int Streak { get; private set; }
    public int ShotsMade { get; private set; }

    public event Action<int> OnScoreChanged;

    private void Start()
    {
        UpdateDisplay();
    }

    public void RegisterScore(bool isSwish)
    {
        int points = isSwish ? pointsPerSwish : pointsPerScore;
        Streak++;
        ShotsMade++;
        Score += points;
        UpdateDisplay();
        OnScoreChanged?.Invoke(Score);
    }

    public void ResetStreak()
    {
        Streak = 0;
    }

    public void ResetScore()
    {
        Score = 0;
        Streak = 0;
        ShotsMade = 0;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (scoreText != null)
            scoreText.text = Score.ToString();
    }
}
