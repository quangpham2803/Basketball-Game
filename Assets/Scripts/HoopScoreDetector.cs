using UnityEngine;
using System;

// Ball must pass top trigger then bottom trigger = valid score from above
public class HoopScoreDetector : MonoBehaviour
{
    public event Action<bool> OnScored; // bool = isSwish

    private bool ballPassedTop;
    private float lastScoreTime;
    private const float ScoreCooldown = 1f;

    public void BallEnteredTop()
    {
        ballPassedTop = true;
    }

    public void BallEnteredBottom(BallController ball)
    {
        if (!ballPassedTop) return;
        if (Time.time - lastScoreTime < ScoreCooldown) return;

        lastScoreTime = Time.time;
        ballPassedTop = false;

        bool isSwish = ball != null && !ball.HitRimSinceThrow;
        OnScored?.Invoke(isSwish);
    }
}
