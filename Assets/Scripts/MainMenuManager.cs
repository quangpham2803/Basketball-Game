using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main Menu controller — references UI via SerializeField.
/// Panels: Main, Mode Select, Settings.
/// Stores selected mode in a static field so GameModeManager can read it on load.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static GameModeManager.GameMode SelectedMode = GameModeManager.GameMode.FreePlay;
    public static float MasterVolume = 1f;
    public static float MouseSensitivity = 2f;

    [Header("Game Scene")]
    [SerializeField] private string gameSceneName1 = "GamePlay";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject modePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Main Panel")]
    [SerializeField] private Button startGameBtn;
    [SerializeField] private Button selectModeBtn;
    [SerializeField] private Button settingsBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private TextMeshProUGUI currentModeLabel;

    [Header("Mode Panel")]
    [SerializeField] private Button freePlayBtn;
    [SerializeField] private Button timerBtn;
    [SerializeField] private Button movingHoopBtn;
    [SerializeField] private Button challengeBtn;
    [SerializeField] private Button freeRoamBtn;
    [SerializeField] private TextMeshProUGUI modeDescriptionText;
    [SerializeField] private Button modeBackBtn;

    [Header("Settings Panel")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityValueText;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button settingsBackBtn;

    // Mode button references for highlight updates
    private Button[] modeButtons;
    private GameModeManager.GameMode[] modeValues;
    private string[] modeDescriptions;

    private static readonly Color ModeNormal = new Color(0.18f, 0.18f, 0.3f);
    private static readonly Color ModeSelected = new Color(0.95f, 0.45f, 0.15f);
    private static readonly Color ModeHover = new Color(0.25f, 0.25f, 0.4f);
    private static readonly Color ModePress = new Color(0.8f, 0.35f, 0.1f);

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Load saved settings
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);

        SetupButtons();
        SetupSettings();
        ShowPanel(mainPanel);
    }

    private void SetupButtons()
    {
        // Main panel
        if (startGameBtn != null) startGameBtn.onClick.AddListener(StartGame);
        if (selectModeBtn != null) selectModeBtn.onClick.AddListener(() => ShowPanel(modePanel));
        if (settingsBtn != null) settingsBtn.onClick.AddListener(() => ShowPanel(settingsPanel));
        if (quitBtn != null) quitBtn.onClick.AddListener(QuitGame);

        // Mode panel
        modeButtons = new[] { freePlayBtn, timerBtn, movingHoopBtn, challengeBtn, freeRoamBtn };
        modeValues = new[]
        {
            GameModeManager.GameMode.FreePlay,
            GameModeManager.GameMode.Timer,
            GameModeManager.GameMode.MovingHoop,
            GameModeManager.GameMode.Challenge,
            GameModeManager.GameMode.FreeRoam
        };
        modeDescriptions = new[]
        {
            "Practice your shots with no pressure. Endless fun!",
            "Score as many points as possible in 60 seconds!",
            "You auto-move side to side. Time your shots!",
            "5 shots from increasing distances. Can you nail them all?",
            "WASD movement + mouse look. Shoot from anywhere!"
        };

        for (int i = 0; i < modeButtons.Length; i++)
        {
            if (modeButtons[i] == null) continue;
            int index = i; // capture for closure
            modeButtons[i].onClick.AddListener(() => SelectMode(index));
        }

        if (modeBackBtn != null) modeBackBtn.onClick.AddListener(() => ShowPanel(mainPanel));

        // Settings panel
        if (settingsBackBtn != null) settingsBackBtn.onClick.AddListener(() =>
        {
            PlayerPrefs.Save();
            ShowPanel(mainPanel);
        });

        RefreshModeButtons();
    }

    private void SetupSettings()
    {
        if (volumeSlider != null)
        {
            volumeSlider.value = MasterVolume;
            UpdateVolumeText(MasterVolume);
            volumeSlider.onValueChanged.AddListener(v =>
            {
                MasterVolume = v;
                PlayerPrefs.SetFloat("MasterVolume", v);
                AudioListener.volume = v;
                UpdateVolumeText(v);
            });
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = MouseSensitivity / 5f;
            UpdateSensitivityText(MouseSensitivity);
            sensitivitySlider.onValueChanged.AddListener(v =>
            {
                MouseSensitivity = v * 5f;
                PlayerPrefs.SetFloat("MouseSensitivity", MouseSensitivity);
                UpdateSensitivityText(MouseSensitivity);
            });
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
        }
    }

    private void UpdateVolumeText(float v)
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(v * 100) + "%";
    }

    private void UpdateSensitivityText(float v)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = v.ToString("F1");
    }

    // ================================================================
    // ACTIONS
    // ================================================================

    private void StartGame()
    {
        AudioListener.volume = MasterVolume;
        SceneManager.LoadScene(gameSceneName1);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SelectMode(int index)
    {
        SelectedMode = modeValues[index];
        if (modeDescriptionText != null)
            modeDescriptionText.text = modeDescriptions[index];
        RefreshModeButtons();
    }

    private void ShowPanel(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (modePanel != null) modePanel.SetActive(panel == modePanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);

        if (panel == mainPanel)
            UpdateCurrentModeLabel();
    }

    private void UpdateCurrentModeLabel()
    {
        if (currentModeLabel == null) return;
        string modeName = SelectedMode switch
        {
            GameModeManager.GameMode.FreePlay => "Free Play",
            GameModeManager.GameMode.Timer => "Timer (60s)",
            GameModeManager.GameMode.MovingHoop => "Moving Hoop",
            GameModeManager.GameMode.Challenge => "Challenge",
            GameModeManager.GameMode.FreeRoam => "Free Roam",
            _ => "Free Play"
        };
        currentModeLabel.text = $"Current Mode: {modeName}";
    }

    private void RefreshModeButtons()
    {
        if (modeButtons == null) return;

        for (int i = 0; i < modeButtons.Length; i++)
        {
            if (modeButtons[i] == null) continue;
            bool sel = SelectedMode == modeValues[i];

            var img = modeButtons[i].GetComponent<Image>();
            if (img != null) img.color = sel ? ModeSelected : ModeNormal;

            var colors = modeButtons[i].colors;
            colors.normalColor = sel ? ModeSelected : ModeNormal;
            colors.highlightedColor = sel ? ModeSelected : ModeHover;
            colors.pressedColor = ModePress;
            colors.selectedColor = sel ? ModeSelected : ModeNormal;
            modeButtons[i].colors = colors;

            // Update label style
            var label = modeButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.fontStyle = sel ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}
