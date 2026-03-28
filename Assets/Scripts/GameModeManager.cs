using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class GameModeManager : MonoBehaviour
{
    public enum GameMode { FreePlay, Timer, MovingHoop, Challenge, FreeRoam }

    [Header("References")]
    [SerializeField] private Transform hoopAssembly;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private BallController ball;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ParticleEffects particles;
    [SerializeField] private Commentary commentary;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI modeInfoText;
    [SerializeField] private TextMeshProUGUI streakText;

    [Header("Timer Mode")]
    [SerializeField] private float timerDuration = 60f;

    [Header("Moving Hoop")]
    [SerializeField] private float hoopMoveSpeed = 1.2f;
    [SerializeField] private float hoopMoveRange = 2f;

    [Header("Player Movement (Moving Hoop mode)")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private float playerMoveSpeed = 0.8f;
    [SerializeField] private float playerMoveRange = 3f;

    [Header("Free Roam")]
    [SerializeField] private float roamMoveSpeed = 5f;
    [SerializeField] private float roamSprintMultiplier = 2f;
    [SerializeField] private float roamLookSensitivity = 2f;
    [SerializeField] private float roamMaxPitch = 80f;

    [Header("Challenge Mode")]
    [SerializeField] private float[] challengeDistances = { 5.5f, 7f, 9f, 12f, 18f };

    [Header("Streak Rewards")]
    [SerializeField] private float bigHoopScale = 1.5f;
    [SerializeField] private float bigHoopDuration = 10f;

    public GameMode CurrentMode { get; private set; } = GameMode.FreePlay;

    private Vector3 hoopBasePos;
    private Vector3 defaultSpawnPos;
    private Vector3 defaultCameraRigPos;
    private Quaternion defaultCameraRigRot;
    private float timeRemaining;
    private bool timerRunning;
    private bool timerEnded;
    private int challengeIndex;
    private Coroutine bigHoopCoroutine;
    private float roamYaw;
    private float roamPitch;

    private GameObject timerResultOverlay;

    private void Start()
    {
        if (hoopAssembly != null) hoopBasePos = hoopAssembly.position;
        if (spawnPoint != null) defaultSpawnPos = spawnPoint.position;
        if (cameraRig != null)
        {
            defaultCameraRigPos = cameraRig.position;
            defaultCameraRigRot = cameraRig.rotation;
        }

        if (MainMenuManager.SelectedMode != GameMode.FreePlay)
            SwitchMode(MainMenuManager.SelectedMode);
        else
            UpdateModeUI();

        AudioListener.volume = MainMenuManager.MasterVolume;
    }

    private void Update()
    {
        HandleModeInput();

        switch (CurrentMode)
        {
            case GameMode.Timer:
                UpdateTimer();
                break;
            case GameMode.MovingHoop:
                UpdatePlayerMovement();
                break;
            case GameMode.FreeRoam:
                UpdateFreeRoam();
                break;
        }

        UpdateStreakUI();
    }

    private void HandleModeInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchMode(GameMode.FreePlay);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchMode(GameMode.Timer);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchMode(GameMode.MovingHoop);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchMode(GameMode.Challenge);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchMode(GameMode.FreeRoam);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void SwitchMode(GameMode mode)
    {
        CurrentMode = mode;
        timerRunning = false;
        timerEnded = false;
        challengeIndex = 0;

        if (timerResultOverlay != null)
            Destroy(timerResultOverlay);

        if (hoopAssembly != null)
            hoopAssembly.position = hoopBasePos;

        if (spawnPoint != null)
            spawnPoint.position = defaultSpawnPos;

        if (cameraRig != null)
        {
            cameraRig.position = defaultCameraRigPos;
            cameraRig.rotation = defaultCameraRigRot;
            if (cameraRig.childCount > 0)
                cameraRig.GetChild(0).localRotation = Quaternion.identity;
        }

        if (ball != null)
            ball.ResetToPosition(spawnPoint != null ? spawnPoint.position : defaultSpawnPos);

        switch (mode)
        {
            case GameMode.Timer:
                timeRemaining = timerDuration;
                timerRunning = true;
                if (scoreManager != null) scoreManager.ResetScore();
                break;
            case GameMode.Challenge:
                SetChallengePosition(0);
                break;
            case GameMode.FreeRoam:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                if (cameraRig != null)
                {
                    Vector3 lookDir = cameraRig.forward;
                    roamYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;
                    roamPitch = 0f;
                    cameraRig.rotation = Quaternion.Euler(0f, roamYaw, 0f);

                    if (cameraRig.childCount > 0)
                        cameraRig.GetChild(0).localRotation = Quaternion.identity;
                }
                break;
        }

        if (mode != GameMode.FreeRoam)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (cameraController != null)
            cameraController.AutoLookAtHoop = (mode != GameMode.FreeRoam);
        if (ball != null)
            ball.FreeAim = (mode == GameMode.FreeRoam);

        UpdateModeUI();
        if (timerText != null)
            timerText.gameObject.SetActive(mode == GameMode.Timer);
    }

    private void UpdateTimer()
    {
        if (timerEnded)
        {
            if (Input.GetKeyDown(KeyCode.R))
                SwitchMode(GameMode.Timer);
            return;
        }

        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(Mathf.Max(0, timeRemaining)).ToString() + "s";

        if (timerText != null)
            timerText.color = timeRemaining <= 10f
                ? Color.Lerp(Color.red, new Color(1f, 0.3f, 0.3f), Mathf.PingPong(Time.time * 3f, 1f))
                : Color.red;

        if (timeRemaining <= 0f)
        {
            timerRunning = false;
            timerEnded = true;

            if (commentary != null)
                commentary.OnTimerEnd(scoreManager != null ? scoreManager.Score : 0);

            if (ball != null)
            {
                var rb = ball.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }

            ShowTimerResult();
        }
    }

    private void ShowTimerResult()
    {
        int score = scoreManager != null ? scoreManager.Score : 0;
        int shots = scoreManager != null ? scoreManager.ShotsMade : 0;

        string grade;
        if (score >= 40) grade = "S";
        else if (score >= 30) grade = "A";
        else if (score >= 20) grade = "B";
        else if (score >= 10) grade = "C";
        else grade = "D";

        var canvasGo = new GameObject("TimerResultOverlay");
        timerResultOverlay = canvasGo;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

        var bg = new GameObject("Backdrop");
        bg.transform.SetParent(canvasGo.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.75f);
        bgImg.raycastTarget = false;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        MakeResultText(canvasGo.transform, "GAME OVER!", 64, new Color(1f, 0.2f, 0.2f), 150f);

        Color gradeColor = grade == "S" ? new Color(1f, 0.85f, 0f) :
                           grade == "A" ? new Color(0.2f, 1f, 0.4f) :
                           grade == "B" ? new Color(0.4f, 0.8f, 1f) :
                           new Color(0.7f, 0.7f, 0.7f);
        MakeResultText(canvasGo.transform, $"GRADE: {grade}", 52, gradeColor, 70f);

        MakeResultText(canvasGo.transform, $"SCORE: {score}", 36, Color.white, 0f);
        MakeResultText(canvasGo.transform, $"SHOTS MADE: {shots}", 28, new Color(0.8f, 0.8f, 0.9f), -50f);

        MakeResultText(canvasGo.transform, "Press R to RETRY  |  Press 1 for Free Play  |  ESC for Menu",
            22, new Color(0.6f, 0.6f, 0.7f), -130f);
    }

    private void MakeResultText(Transform parent, string text, int size, Color color, float yPos)
    {
        var go = new GameObject(text);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = size >= 48 ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(1200, size + 20);
    }

    private IEnumerator ShowTimerBonus(string text)
    {
        var go = new GameObject("TimerBonus");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 48;
        tmp.color = new Color(0.2f, 1f, 0.4f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt = textGo.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(400, 70);

        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            float scale = t < 0.15f ? Mathf.Lerp(0.5f, 1.3f, t / 0.15f) : Mathf.Lerp(1.3f, 1f, (t - 0.15f) / 0.2f);
            textGo.transform.localScale = Vector3.one * Mathf.Max(scale, 0.5f);

            rt.anchoredPosition = new Vector2(0, t * 80f);
            tmp.alpha = t < 0.3f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);
            yield return null;
        }

        Destroy(go);
    }

    private void UpdateMovingHoop()
    {
        if (hoopAssembly == null) return;

        float x = Mathf.Sin(Time.time * hoopMoveSpeed) * hoopMoveRange;
        hoopAssembly.position = hoopBasePos + Vector3.right * x;
    }

    private void UpdateFreeRoam()
    {
        if (cameraRig == null) return;

        bool aiming = ball != null && ball.State == BallController.BallState.Held;

        if (!aiming)
        {
            float sens = MainMenuManager.MouseSensitivity;
            roamYaw += Input.GetAxis("Mouse X") * sens;
            roamPitch -= Input.GetAxis("Mouse Y") * sens;
            roamPitch = Mathf.Clamp(roamPitch, -roamMaxPitch, roamMaxPitch);
            cameraRig.rotation = Quaternion.Euler(roamPitch, roamYaw, 0f);
        }

        if (!aiming)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (!Mathf.Approximately(h, 0f) || !Mathf.Approximately(v, 0f))
            {
                float speed = roamMoveSpeed;
                if (Input.GetKey(KeyCode.LeftShift))
                    speed *= roamSprintMultiplier;

                Vector3 forward = cameraRig.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 right = cameraRig.right;
                right.y = 0f;
                right.Normalize();

                Vector3 move = (forward * v + right * h).normalized * speed * Time.deltaTime;
                cameraRig.position += move;
            }

            Vector3 pos = cameraRig.position;
            pos.y = Mathf.Max(pos.y, 1.75f);
            cameraRig.position = pos;
        }

        if (spawnPoint != null)
        {
            Vector3 sp = cameraRig.position;
            sp.y = 1.2f;
            spawnPoint.position = sp;
        }

        if (ball != null && (ball.State == BallController.BallState.Dribbling || ball.State == BallController.BallState.Idle))
        {
        }
    }

    private void UpdatePlayerMovement()
    {
        float x = Mathf.Sin(Time.time * playerMoveSpeed) * playerMoveRange;

        if (cameraRig != null)
        {
            Vector3 camPos = cameraRig.position;
            camPos.x = defaultSpawnPos.x + x;
            cameraRig.position = camPos;
        }

        if (spawnPoint != null)
        {
            Vector3 sp = spawnPoint.position;
            sp.x = defaultSpawnPos.x + x;
            spawnPoint.position = sp;
        }

        if (ball != null && ball.State == BallController.BallState.Idle)
            ball.ResetToPosition(spawnPoint.position);
    }

    public void OnScore()
    {
        if (CurrentMode == GameMode.Timer && timerRunning)
        {
            timeRemaining += 5f;
            StartCoroutine(ShowTimerBonus("+5s"));
            return;
        }

        if (CurrentMode != GameMode.Challenge) return;

        challengeIndex++;
        if (challengeIndex >= challengeDistances.Length)
        {
            if (commentary != null)
                commentary.OnTimerEnd(100); // Will show "incredible" message
            UpdateModeUI();
            return;
        }

        SetChallengePosition(challengeIndex);
    }

    private void SetChallengePosition(int index)
    {
        if (index >= challengeDistances.Length) return;

        float dist = challengeDistances[index];
        Vector3 newSpawn = new Vector3(0f, 1.2f, hoopBasePos.z - dist);

        if (spawnPoint != null)
            spawnPoint.position = newSpawn;

        if (ball != null)
            ball.ResetToPosition(newSpawn);

        UpdateModeUI();
    }

    public void CheckStreakRewards(int streak)
    {
        if (particles != null)
            particles.SetBallFire(streak >= 3);

        if (streak == 4 && hoopAssembly != null)
            ActivateBigHoop();

        UpdateStreakUI();
    }

    public void OnStreakBroken()
    {
        if (particles != null)
            particles.SetBallFire(false);

        if (hoopAssembly != null)
            hoopAssembly.localScale = Vector3.one;
    }

    private void ActivateBigHoop()
    {
        if (bigHoopCoroutine != null) StopCoroutine(bigHoopCoroutine);
        bigHoopCoroutine = StartCoroutine(BigHoopSequence());
    }

    private IEnumerator BigHoopSequence()
    {
        float elapsed = 0f;
        float growTime = 0.3f;
        while (elapsed < growTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / growTime;
            float scale = Mathf.Lerp(1f, bigHoopScale, t);
            hoopAssembly.localScale = Vector3.one * scale;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(bigHoopDuration);

        elapsed = 0f;
        while (elapsed < growTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / growTime;
            float scale = Mathf.Lerp(bigHoopScale, 1f, t);
            hoopAssembly.localScale = Vector3.one * scale;
            yield return null;
        }

        hoopAssembly.localScale = Vector3.one;
    }

    private void UpdateModeUI()
    {
        if (modeInfoText == null) return;

        string info = CurrentMode switch
        {
            GameMode.FreePlay => "[1] FREE PLAY  [2] Timer  [3] Moving Hoop  [4] Challenge  [5] Free Roam",
            GameMode.Timer => "[1] Free Play  [2] TIMER (+5s per score)  [3] Moving Hoop  [4] Challenge  [5] Free Roam",
            GameMode.MovingHoop => "[1] Free Play  [2] Timer  [3] MOVING HOOP  [4] Challenge  [5] Free Roam",
            GameMode.Challenge => $"[1] Free Play  [2] Timer  [3] Moving Hoop  [4] CHALLENGE ({challengeIndex + 1}/{challengeDistances.Length})  [5] Free Roam",
            GameMode.FreeRoam => "[1] Free Play  [2] Timer  [3] Moving Hoop  [4] Challenge  [5] FREE ROAM (WASD + Mouse)",
            _ => ""
        };

        modeInfoText.text = info + "\nClick & drag down to aim  |  Scroll to adjust arc  |  R to reset ball  |  ESC for menu";
    }

    private void UpdateStreakUI()
    {
        if (streakText == null || scoreManager == null) return;

        int streak = scoreManager.Streak;
        if (streak >= 2)
        {
            streakText.gameObject.SetActive(true);
            streakText.text = $"x{streak} STREAK";
            streakText.color = streak >= 5
                ? Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 4f, 1f))
                : streak >= 3 ? new Color(1f, 0.6f, 0.2f) : Color.white;
        }
        else
        {
            streakText.gameObject.SetActive(false);
        }
    }
}
