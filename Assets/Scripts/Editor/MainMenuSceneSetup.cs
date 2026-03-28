using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Editor tool to create the Main Menu scene with full UI hierarchy.
/// Menu: Tools > Create Main Menu Scene
/// </summary>
public class MainMenuSceneSetup
{
    // Colors
    static readonly Color BgColor = new Color(0.08f, 0.08f, 0.15f);
    static readonly Color BtnColor = new Color(0.95f, 0.45f, 0.15f);
    static readonly Color BtnHover = new Color(1f, 0.55f, 0.25f);
    static readonly Color BtnPress = new Color(0.8f, 0.35f, 0.1f);
    static readonly Color AccentColor = new Color(1f, 0.6f, 0.2f);
    static readonly Color ModeNormal = new Color(0.18f, 0.18f, 0.3f);
    static readonly Color SubText = new Color(0.7f, 0.7f, 0.8f);
    static readonly Color SliderBg = new Color(0.2f, 0.2f, 0.3f);

    [MenuItem("Tools/Create Main Menu Scene")]
    public static void CreateMainMenuScene()
    {
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        cam.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();

        // Event System
        var eventGo = new GameObject("EventSystem");
        eventGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Canvas
        var canvasGo = new GameObject("MenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background
        var bg = MakeImage(canvasGo.transform, "Background", BgColor);
        Stretch(bg.GetComponent<RectTransform>());

        // ============================================================
        // MAIN PANEL
        // ============================================================
        var mainPanel = MakePanel(canvasGo.transform, "MainPanel");

        float y = 200f;
        MakeLabel(mainPanel.transform, "Title", "BASKETBALL", 72, AccentColor, new Vector2(0, y), FontStyles.Bold);
        y -= 60f;
        MakeLabel(mainPanel.transform, "Subtitle", "STREET EDITION", 28, SubText, new Vector2(0, y));

        y = 20f;
        var startBtn = MakeButton(mainPanel.transform, "StartGameBtn", "START GAME", new Vector2(0, y));
        y -= 75f;
        var selectModeBtn = MakeButton(mainPanel.transform, "SelectModeBtn", "SELECT MODE", new Vector2(0, y));
        y -= 75f;
        var settingsBtn = MakeButton(mainPanel.transform, "SettingsBtn", "SETTINGS", new Vector2(0, y));
        y -= 75f;
        var quitBtn = MakeButton(mainPanel.transform, "QuitBtn", "QUIT", new Vector2(0, y));

        y -= 95f;
        var currentModeLabel = MakeLabel(mainPanel.transform, "CurrentModeLabel", "Current Mode: Free Play",
            20, SubText, new Vector2(0, y));

        MakeLabel(mainPanel.transform, "Version", "v1.0",
            16, new Color(0.4f, 0.4f, 0.5f), new Vector2(0, -320f));

        // ============================================================
        // MODE PANEL
        // ============================================================
        var modePanel = MakePanel(canvasGo.transform, "ModePanel");

        y = 240f;
        MakeLabel(modePanel.transform, "Title", "SELECT MODE", 48, AccentColor, new Vector2(0, y), FontStyles.Bold);

        y -= 70f;
        var freePlayBtn = MakeModeButton(modePanel.transform, "FreePlayBtn", "FREE PLAY", new Vector2(0, y));
        y -= 70f;
        var timerBtn = MakeModeButton(modePanel.transform, "TimerBtn", "TIMER (60s)", new Vector2(0, y));
        y -= 70f;
        var movingHoopBtn = MakeModeButton(modePanel.transform, "MovingHoopBtn", "MOVING HOOP", new Vector2(0, y));
        y -= 70f;
        var challengeBtn = MakeModeButton(modePanel.transform, "ChallengeBtn", "CHALLENGE", new Vector2(0, y));
        y -= 70f;
        var freeRoamBtn = MakeModeButton(modePanel.transform, "FreeRoamBtn", "FREE ROAM", new Vector2(0, y));

        y -= 40f;
        var modeDesc = MakeLabel(modePanel.transform, "ModeDescription",
            "Click a mode to select it", 20, SubText, new Vector2(0, y));

        y -= 60f;
        var modeBackBtn = MakeButton(modePanel.transform, "BackBtn", "BACK", new Vector2(0, y), 200f);

        modePanel.SetActive(false);

        // ============================================================
        // SETTINGS PANEL
        // ============================================================
        var settingsPanel = MakePanel(canvasGo.transform, "SettingsPanel");

        y = 200f;
        MakeLabel(settingsPanel.transform, "Title", "SETTINGS", 48, AccentColor, new Vector2(0, y), FontStyles.Bold);

        // Volume
        y -= 80f;
        MakeLabel(settingsPanel.transform, "VolumeLabel", "MASTER VOLUME", 22, Color.white, new Vector2(-150, y));
        var volSlider = MakeSlider(settingsPanel.transform, "VolumeSlider", new Vector2(120, y));
        var volValue = MakeLabel(settingsPanel.transform, "VolumeValue", "100%", 22, AccentColor, new Vector2(350, y));

        // Sensitivity
        y -= 80f;
        MakeLabel(settingsPanel.transform, "SensLabel", "MOUSE SENSITIVITY", 22, Color.white, new Vector2(-150, y));
        var sensSlider = MakeSlider(settingsPanel.transform, "SensitivitySlider", new Vector2(120, y));
        var sensValue = MakeLabel(settingsPanel.transform, "SensValue", "2.0", 22, AccentColor, new Vector2(350, y));

        // Fullscreen
        y -= 80f;
        MakeLabel(settingsPanel.transform, "FSLabel", "FULLSCREEN", 22, Color.white, new Vector2(-150, y));
        var fsToggle = MakeToggle(settingsPanel.transform, "FullscreenToggle", new Vector2(120, y));

        y -= 100f;
        var settingsBackBtn = MakeButton(settingsPanel.transform, "BackBtn", "BACK", new Vector2(0, y), 200f);

        settingsPanel.SetActive(false);

        // ============================================================
        // MENU MANAGER — wire all references
        // ============================================================
        var menuGo = new GameObject("MainMenuManager");
        var menu = menuGo.AddComponent<MainMenuManager>();
        var so = new SerializedObject(menu);

        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("modePanel").objectReferenceValue = modePanel;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;

        so.FindProperty("startGameBtn").objectReferenceValue = startBtn;
        so.FindProperty("selectModeBtn").objectReferenceValue = selectModeBtn;
        so.FindProperty("settingsBtn").objectReferenceValue = settingsBtn;
        so.FindProperty("quitBtn").objectReferenceValue = quitBtn;
        so.FindProperty("currentModeLabel").objectReferenceValue = currentModeLabel;

        so.FindProperty("freePlayBtn").objectReferenceValue = freePlayBtn;
        so.FindProperty("timerBtn").objectReferenceValue = timerBtn;
        so.FindProperty("movingHoopBtn").objectReferenceValue = movingHoopBtn;
        so.FindProperty("challengeBtn").objectReferenceValue = challengeBtn;
        so.FindProperty("freeRoamBtn").objectReferenceValue = freeRoamBtn;
        so.FindProperty("modeDescriptionText").objectReferenceValue = modeDesc;
        so.FindProperty("modeBackBtn").objectReferenceValue = modeBackBtn;

        so.FindProperty("volumeSlider").objectReferenceValue = volSlider;
        so.FindProperty("volumeValueText").objectReferenceValue = volValue;
        so.FindProperty("sensitivitySlider").objectReferenceValue = sensSlider;
        so.FindProperty("sensitivityValueText").objectReferenceValue = sensValue;
        so.FindProperty("fullscreenToggle").objectReferenceValue = fsToggle;
        so.FindProperty("settingsBackBtn").objectReferenceValue = settingsBackBtn;

        so.ApplyModifiedPropertiesWithoutUndo();

        // Ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.25f);

        // Save scene
        string scenePath = "Assets/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();
        UpdateBuildSettings(scenePath);

        Debug.Log("[MainMenuSetup] Scene created: " + scenePath);
        EditorUtility.DisplayDialog("Main Menu Created",
            "Scene: " + scenePath +
            "\n\nBuild Settings:\n  0: MainMenu\n  1: New Scene 3" +
            "\n\nYou can now edit all UI elements in the scene!\nPress Play to test.",
            "OK");
    }

    // ================================================================
    // UI FACTORY HELPERS
    // ================================================================

    static GameObject MakePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        Stretch(rt);
        return go;
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string name, string text,
        int size, Color color, Vector2 pos, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(800, size + 20);
        return tmp;
    }

    static Button MakeButton(Transform parent, string name, string label, Vector2 pos, float w = 350f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = BtnColor;
        img.type = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.normalColor = BtnColor;
        c.highlightedColor = BtnHover;
        c.pressedColor = BtnPress;
        c.selectedColor = BtnColor;
        c.fadeDuration = 0.1f;
        btn.colors = c;
        btn.targetGraphic = img;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(w, 55f);

        // Label child
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        Stretch(textGo.GetComponent<RectTransform>());

        return btn;
    }

    static Button MakeModeButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = ModeNormal;
        img.type = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();
        var c = btn.colors;
        c.normalColor = ModeNormal;
        c.highlightedColor = new Color(0.25f, 0.25f, 0.4f);
        c.pressedColor = new Color(0.8f, 0.35f, 0.1f);
        c.selectedColor = ModeNormal;
        c.fadeDuration = 0.1f;
        btn.colors = c;
        btn.targetGraphic = img;

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(500f, 55f);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        Stretch(textGo.GetComponent<RectTransform>());

        return btn;
    }

    static Slider MakeSlider(Transform parent, string name, Vector2 pos)
    {
        var resources = new DefaultControls.Resources();
        var go = DefaultControls.CreateSlider(resources);
        go.name = name;
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 20);

        // Style
        go.transform.Find("Background").GetComponent<Image>().color = SliderBg;
        go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = AccentColor;
        go.transform.Find("Handle Slide Area/Handle").GetComponent<Image>().color = Color.white;

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        return slider;
    }

    static Toggle MakeToggle(Transform parent, string name, Vector2 pos)
    {
        var resources = new DefaultControls.Resources();
        var go = DefaultControls.CreateToggle(resources);
        go.name = name;
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(40, 40);

        // Remove default label
        var label = go.transform.Find("Label");
        if (label != null) Object.DestroyImmediate(label.gameObject);

        // Style background
        var bgImg = go.transform.Find("Background");
        if (bgImg != null) bgImg.GetComponent<Image>().color = SliderBg;

        // Style checkmark
        var check = go.transform.Find("Background/Checkmark");
        if (check != null) check.GetComponent<Image>().color = AccentColor;

        return go.GetComponent<Toggle>();
    }

    static GameObject MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void UpdateBuildSettings(string menuScenePath)
    {
        var scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(menuScenePath, true));

        string gamePath = "Assets/Scenes/New Scene 3.unity";
        if (System.IO.File.Exists(
            System.IO.Path.Combine(Application.dataPath, "..", gamePath)))
        {
            scenes.Add(new EditorBuildSettingsScene(gamePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
