using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private BallController ball;
    [SerializeField] private HoopScoreDetector hoopDetector;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private GameFeedback feedback;
    [SerializeField] private Transform spawnPoint;

    private HoopScoreDetector[] allDetectors;

    [Header("Bonus Systems")]
    [SerializeField] private ParticleEffects particles;
    [SerializeField] private Commentary commentary;
    [SerializeField] private GameModeManager gameModes;

    [Header("Settings")]
    [SerializeField] private float resetDelay = 1.5f;

    private bool waitingToReset;
    private bool scoredThisThrow;
    private int missStreak;
    private Vector3 hoopPosition;

    private void Start()
    {
        var hoopTarget = GameObject.FindGameObjectWithTag("HoopTarget");
        if (hoopTarget != null)
            hoopPosition = hoopTarget.transform.position;
    }

    private void OnEnable()
    {
        allDetectors = FindObjectsByType<HoopScoreDetector>(FindObjectsSortMode.None);
        foreach (var d in allDetectors)
            d.OnScored += HandleScore;

        ball.OnStopped += HandleBallStopped;
        ball.OnPickedUp += HandleBallPickedUp;
        ball.OnThrown += HandleBallThrown;
        ball.OnBounce += HandleBallBounce;
    }

    private void OnDisable()
    {
        if (allDetectors != null)
            foreach (var d in allDetectors)
                if (d != null) d.OnScored -= HandleScore;

        ball.OnStopped -= HandleBallStopped;
        ball.OnPickedUp -= HandleBallPickedUp;
        ball.OnThrown -= HandleBallThrown;
        ball.OnBounce -= HandleBallBounce;
    }

    private void HandleScore(bool isSwish)
    {
        scoredThisThrow = true;
        missStreak = 0;

        scoreManager.RegisterScore(isSwish);

        float distance = Vector3.Distance(ball.LastThrowPosition, hoopPosition);

        feedback.PlayScoreEffect(isSwish);
        if (particles != null) particles.PlayScore(isSwish);

        if (commentary != null)
            commentary.OnScore(isSwish, scoreManager.Streak, distance);

        if (gameModes != null)
            gameModes.CheckStreakRewards(scoreManager.Streak);

        if (gameModes != null)
            gameModes.OnScore();

        if (!waitingToReset)
            StartCoroutine(ResetBallAfterDelay());
    }

    private void HandleBallStopped()
    {
        if (!scoredThisThrow)
        {
            scoreManager.ResetStreak();
            missStreak++;

            if (commentary != null)
                commentary.OnMiss(missStreak);

            if (gameModes != null)
                gameModes.OnStreakBroken();
        }
        else
        {
            missStreak = 0;
        }

        if (!waitingToReset)
            StartCoroutine(ResetBallAfterDelay());
    }

    private void HandleBallPickedUp()
    {
        StopAllCoroutines();
        waitingToReset = false;
    }

    private void HandleBallThrown()
    {
        scoredThisThrow = false;
        feedback.PlayThrowEffect();
    }

    private void HandleBallBounce(float impact)
    {
        feedback.PlayBounceEffect(impact);

        if (particles != null)
            particles.PlayImpact(ball.transform.position, impact);
    }

    private IEnumerator ResetBallAfterDelay()
    {
        waitingToReset = true;
        yield return new WaitForSecondsRealtime(resetDelay);
        ball.ResetToPosition(spawnPoint.position);
        waitingToReset = false;
    }
}
