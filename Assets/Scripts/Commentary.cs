using UnityEngine;
using System.Collections;
using TMPro;

public class Commentary : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI commentaryText;
    [SerializeField] private float displayDuration = 2f;

    private Coroutine displayCoroutine;
    private int lastMessageIndex = -1;

    private static readonly string[] ScoreMessages =
    {
        "Nice shot!",
        "That's a bucket!",
        "Money in the bank!",
        "CASH IT!",
        "Smooth operator!",
        "Too easy!",
        "Textbook!",
        "That's how it's done!",
        "Automatic!",
    };

    private static readonly string[] SwishMessages =
    {
        "SWISH! Nothing but net!",
        "CLEAN! Absolutely clean!",
        "Silky smooth!",
        "The net barely moved!",
        "Pure as spring water!",
        "That was DISGUSTING!\n(in the best way)",
        "*chef's kiss*",
        "Poetry in motion!",
    };

    private static readonly string[] MissMessages =
    {
        "Air ball... air ball...",
        "BRICK!",
        "The rim sends its regards.",
        "Have you tried aiming?",
        "My grandma shoots better.\nShe's a lamp.",
        "Bold strategy, Cotton.",
        "404: Basket Not Found",
        "That ball had a family!",
        "Somewhere, a hoop is crying.",
        "Plot twist: you're not playing bowling.",
        "Close! ...not really.",
        "The backboard filed a restraining order.",
    };

    private static readonly string[] LongShotMessages =
    {
        "FROM DOWNTOWN!",
        "SNIPER!",
        "Is that even legal from there?!",
        "GPS-guided missile!",
        "Call the police, that's THEFT!",
    };

    private static readonly string[] MissStreakMessages =
    {
        "The hoop is the round thing. Up there.",
        "Maybe try standing closer?",
        "Fun fact: the ball goes IN the hoop.",
        "You miss 100% of the shots you...\nwait, you miss those too.",
        "Pro tip: aim where the hoop IS, not where it isn't.",
        "Installing aim-assist...\nJust kidding. Git gud.",
    };

    public void OnScore(bool isSwish, int streak, float distance)
    {
        string msg;

        // Streak messages take priority
        if (streak >= 7)
            msg = "LEGENDARY! Someone call ESPN!";
        else if (streak == 6)
            msg = "Are you using aimbot?!";
        else if (streak == 5)
            msg = "UNSTOPPABLE!";
        else if (streak == 4)
            msg = "ON FIRE!";
        else if (streak == 3)
            msg = "HAT TRICK!";
        else if (streak == 2)
            msg = "Back to back!";
        // Long distance shot
        else if (distance > 12f)
            msg = PickRandom(LongShotMessages);
        // Swish
        else if (isSwish)
            msg = PickRandom(SwishMessages);
        // Regular score
        else
            msg = PickRandom(ScoreMessages);

        ShowMessage(msg, isSwish ? new Color(1f, 0.9f, 0.3f) : Color.white);
    }

    public void OnMiss(int missStreak)
    {
        string msg;

        if (missStreak >= 4)
            msg = PickRandom(MissStreakMessages);
        else
            msg = PickRandom(MissMessages);

        ShowMessage(msg, new Color(1f, 0.6f, 0.6f));
    }

    public void OnTimerEnd(int finalScore)
    {
        string msg;

        if (finalScore >= 30)
            msg = "INCREDIBLE! Are you a professional?!";
        else if (finalScore >= 20)
            msg = "Impressive! The crowd goes wild!";
        else if (finalScore >= 10)
            msg = "Not bad! Room for improvement.";
        else if (finalScore > 0)
            msg = "Hey, at least you tried!";
        else
            msg = "...did you know you're supposed\nto throw the ball?";

        ShowMessage(msg, new Color(0.5f, 0.8f, 1f));
    }

    private void ShowMessage(string msg, Color color)
    {
        if (commentaryText == null) return;

        if (displayCoroutine != null)
            StopCoroutine(displayCoroutine);

        displayCoroutine = StartCoroutine(AnimateMessage(msg, color));
    }

    private IEnumerator AnimateMessage(string msg, Color color)
    {
        commentaryText.text = msg;
        commentaryText.color = color;
        commentaryText.gameObject.SetActive(true);
        commentaryText.alpha = 0f;

        // Fade in
        float fadeIn = 0.2f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.unscaledDeltaTime;
            commentaryText.alpha = elapsed / fadeIn;
            yield return null;
        }

        commentaryText.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade out
        float fadeOut = 0.5f;
        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            elapsed += Time.unscaledDeltaTime;
            commentaryText.alpha = 1f - (elapsed / fadeOut);
            yield return null;
        }

        commentaryText.gameObject.SetActive(false);
    }

    private string PickRandom(string[] pool)
    {
        int idx;
        do
        {
            idx = Random.Range(0, pool.Length);
        } while (idx == lastMessageIndex && pool.Length > 1);

        lastMessageIndex = idx;
        return pool[idx];
    }
}
