using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility that builds the complete basketball scene from primitives.
/// Creates court, hoop assembly, ball, camera, UI, particles, game modes,
/// commentary, VR support, and wires all component references.
/// Menu: Basketball > Setup Scene
/// </summary>
public class BasketballSceneSetup : Editor
{
    // Real-world basketball dimensions (meters)
    private const float RimHeight = 3.05f;
    private const float RimInnerRadius = 0.23f;
    private const float RimTubeRadius = 0.015f;
    private const float BackboardWidth = 1.8f;
    private const float BackboardHeight = 1.05f;
    private const float BallDiameter = 0.24f;
    private const float ShootingDistance = 5.5f;

    [MenuItem("Basketball/Setup Scene")]
    public static void SetupScene()
    {
        EnsureTagExists("Rim");
        EnsureTagExists("Ball");

        // --- Physics Materials ---
        var ballPhysMat = GetOrCreatePhysMat("BallPhysics", 0.75f, 0.6f);
        var rimPhysMat = GetOrCreatePhysMat("RimPhysics", 0.4f, 0.7f);
        var boardPhysMat = GetOrCreatePhysMat("BackboardPhysics", 0.35f, 0.5f);
        var floorPhysMat = GetOrCreatePhysMat("FloorPhysics", 0.65f, 0.7f);

        // ============================================================
        // COURT
        // ============================================================
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Court";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        floor.GetComponent<Collider>().material = floorPhysMat;
        SetColor(floor, new Color(0.76f, 0.6f, 0.42f));

        // ============================================================
        // HOOP ASSEMBLY (parent for moving hoop mode)
        // ============================================================
        var hoopAssembly = new GameObject("HoopAssembly");
        hoopAssembly.transform.position = Vector3.zero;

        // Backboard
        var backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backboard.name = "Backboard";
        backboard.transform.SetParent(hoopAssembly.transform);
        backboard.transform.position = new Vector3(0f, RimHeight + 0.45f, ShootingDistance + 0.15f);
        backboard.transform.localScale = new Vector3(BackboardWidth, BackboardHeight, 0.05f);
        backboard.GetComponent<Collider>().material = boardPhysMat;
        SetColor(backboard, Color.white);

        // Support pole
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "SupportPole";
        pole.transform.SetParent(hoopAssembly.transform);
        pole.transform.position = new Vector3(0f, RimHeight * 0.5f, ShootingDistance + 0.3f);
        pole.transform.localScale = new Vector3(0.08f, RimHeight * 0.5f, 0.08f);
        SetColor(pole, new Color(0.45f, 0.45f, 0.45f));

        // Hoop (rim + triggers)
        var hoop = new GameObject("Hoop");
        hoop.transform.SetParent(hoopAssembly.transform);
        hoop.transform.position = new Vector3(0f, RimHeight, ShootingDistance);
        var hoopDetector = hoop.AddComponent<HoopScoreDetector>();

        // Rim ring from small spheres
        int rimSegments = 20;
        for (int i = 0; i < rimSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / rimSegments;
            float x = Mathf.Cos(angle) * RimInnerRadius;
            float z = Mathf.Sin(angle) * RimInnerRadius;

            var seg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seg.name = $"RimSeg_{i}";
            seg.transform.SetParent(hoop.transform);
            seg.transform.localPosition = new Vector3(x, 0f, z);
            seg.transform.localScale = Vector3.one * (RimTubeRadius * 2f);
            seg.tag = "Rim";
            seg.GetComponent<Collider>().material = rimPhysMat;
            SetColor(seg, new Color(1f, 0.3f, 0.1f));
        }

        // Trigger zones (generous size for forgiving detection)
        CreateTriggerZone("TopTrigger", hoop.transform,
            new Vector3(0f, 0.08f, 0f),
            new Vector3(RimInnerRadius * 2.2f, 0.08f, RimInnerRadius * 2.2f),
            HoopTriggerZone.ZoneType.Top);

        CreateTriggerZone("BottomTrigger", hoop.transform,
            new Vector3(0f, -0.2f, 0f),
            new Vector3(RimInnerRadius * 2.2f, 0.1f, RimInnerRadius * 2.2f),
            HoopTriggerZone.ZoneType.Bottom);

        // Hoop target marker (child of assembly so it moves with it)
        var hoopTarget = new GameObject("HoopTarget");
        hoopTarget.transform.SetParent(hoopAssembly.transform);
        hoopTarget.transform.position = new Vector3(0f, RimHeight, ShootingDistance);

        // ============================================================
        // BASKETBALL
        // ============================================================
        Vector3 spawnPos = new Vector3(0f, 1.2f, 0f);

        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "Basketball";
        ball.tag = "Ball";
        ball.transform.position = spawnPos;
        ball.transform.localScale = Vector3.one * BallDiameter;
        ball.GetComponent<Collider>().material = ballPhysMat;
        SetColor(ball, new Color(1f, 0.55f, 0.15f));

        var ballRb = ball.AddComponent<Rigidbody>();
        ballRb.mass = 0.62f;
        ballRb.linearDamping = 0.1f;
        ballRb.angularDamping = 0.4f;
        ballRb.interpolation = RigidbodyInterpolation.Interpolate;
        ballRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        var ballController = ball.AddComponent<BallController>();

        // Ball trail
        var trail = ball.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.06f;
        trail.endWidth = 0f;
        trail.material = CreateUnlitMaterial(new Color(1f, 0.7f, 0.3f, 0.3f));
        trail.startColor = new Color(1f, 0.7f, 0.3f, 0.35f);
        trail.endColor = new Color(1f, 0.7f, 0.3f, 0f);
        trail.minVertexDistance = 0.05f;
        trail.enabled = false;

        // ============================================================
        // MARKERS
        // ============================================================
        var spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.position = spawnPos;

        // ============================================================
        // CAMERA
        // ============================================================
        if (Camera.main != null)
            DestroyImmediate(Camera.main.gameObject);

        var cameraRig = new GameObject("CameraRig");
        cameraRig.transform.position = new Vector3(0f, 1.75f, -1.5f);

        var camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(cameraRig.transform);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.nearClipPlane = 0.05f;
        camObj.AddComponent<AudioListener>();
        var camShake = camObj.AddComponent<CameraShake>();
        var camController = camObj.AddComponent<CameraController>();

        camObj.transform.LookAt(hoopTarget.transform);

        // ============================================================
        // FIRST-PERSON HANDS (detailed with forearm, palm, fingers)
        // ============================================================
        var handsObj = new GameObject("Hands");
        handsObj.transform.SetParent(camObj.transform);
        handsObj.transform.localPosition = Vector3.zero;
        var handsController = handsObj.AddComponent<HandsController>();

        Color skinColor = new Color(0.87f, 0.67f, 0.53f);

        var leftHand = BuildMeshHand(handsObj.transform, "LeftHand", skinColor, false);
        var rightHand = BuildMeshHand(handsObj.transform, "RightHand", skinColor, true);

        // Wire hands
        WireField(handsController, "ball", ballController);
        WireField(handsController, "leftHand", leftHand.transform);
        WireField(handsController, "rightHand", rightHand.transform);

        leftHand.SetActive(false);
        rightHand.SetActive(false);

        // ============================================================
        // TRAJECTORY PREVIEW
        // ============================================================
        var trajObj = new GameObject("TrajectoryPreview");
        var lineRenderer = trajObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.025f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0.1f, 0.1f, 0.85f);
        lineRenderer.endColor = new Color(1f, 0.1f, 0.1f, 0f);
        lineRenderer.positionCount = 0;
        lineRenderer.numCornerVertices = 4;
        var trajPreview = trajObj.AddComponent<TrajectoryPreview>();

        // ============================================================
        // UI CANVAS
        // ============================================================
        var canvas = new GameObject("UI Canvas");
        var canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComp.sortingOrder = 10;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvas.AddComponent<GraphicRaycaster>();

        // Score display (top center)
        var scoreText = CreateUIText(canvas.transform, "ScoreText",
            "0", 80, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -50f), new Vector2(300f, 100f));

        // Score popup (center-upper)
        var popupText = CreateUIText(canvas.transform, "PopupText",
            "", 64, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
            Vector2.zero, new Vector2(500f, 120f));
        popupText.fontStyle = FontStyles.Bold;
        popupText.gameObject.SetActive(false);

        // Commentary text (center)
        var commentaryText = CreateUIText(canvas.transform, "CommentaryText",
            "", 36, TextAlignmentOptions.Center, Color.white,
            new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f),
            Vector2.zero, new Vector2(700f, 100f));
        commentaryText.fontStyle = FontStyles.Italic;
        commentaryText.gameObject.SetActive(false);

        // Streak text (top right)
        var streakText = CreateUIText(canvas.transform, "StreakText",
            "", 42, TextAlignmentOptions.Right, new Color(1f, 0.6f, 0.2f),
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-30f, -50f), new Vector2(300f, 60f));
        streakText.fontStyle = FontStyles.Bold;
        streakText.gameObject.SetActive(false);

        // Timer text (top left)
        var timerText = CreateUIText(canvas.transform, "TimerText",
            "60s", 48, TextAlignmentOptions.Left, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -50f), new Vector2(200f, 60f));
        timerText.gameObject.SetActive(false);

        // Mode info (bottom)
        var modeInfoText = CreateUIText(canvas.transform, "ModeInfoText",
            "[1] FREE PLAY  [2] Timer  [3] Moving Hoop  [4] Challenge  [5] Free Roam",
            20, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.45f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 70f), new Vector2(800f, 40f));

        // Instruction hint
        CreateUIText(canvas.transform, "InstructionText",
            "Click + swipe up to shoot!  |  Move mouse to aim",
            22, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.4f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 35f), new Vector2(700f, 35f));

        // Screen flash overlay (full screen Image)
        var flashObj = new GameObject("ScreenFlash");
        flashObj.transform.SetParent(canvas.transform, false);
        var flashImage = flashObj.AddComponent<Image>();
        flashImage.color = new Color(1, 1, 1, 0);
        flashImage.raycastTarget = false;
        var flashRect = flashObj.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.sizeDelta = Vector2.zero;
        flashObj.SetActive(false);

        // ============================================================
        // GAME MANAGEMENT + BONUS SYSTEMS
        // ============================================================
        var gmObj = new GameObject("GameManagement");
        var scoreManager = gmObj.AddComponent<ScoreManager>();
        var gameFeedback = gmObj.AddComponent<GameFeedback>();
        var particleEffects = gmObj.AddComponent<ParticleEffects>();
        var commentaryComp = gmObj.AddComponent<Commentary>();
        var gameModeManager = gmObj.AddComponent<GameModeManager>();
        var gameManager = gmObj.AddComponent<GameManager>();
        var musicManager = gmObj.AddComponent<MusicManager>();

        // VR Handler (on camera rig so tracking space = player position)
        var vrHandler = cameraRig.AddComponent<VRInputHandler>();

        // ============================================================
        // WIRE ALL REFERENCES
        // ============================================================

        // Camera
        WireField(camController, "ball", ballController);
        WireField(camController, "hoopTarget", hoopTarget.transform);

        // Trajectory
        WireField(trajPreview, "ball", ballController);

        // Ball VR input
        WireField(ballController, "vrInput", vrHandler);

        // GameManager (core)
        WireField(gameManager, "ball", ballController);
        WireField(gameManager, "hoopDetector", hoopDetector);
        WireField(gameManager, "scoreManager", scoreManager);
        WireField(gameManager, "feedback", gameFeedback);
        WireField(gameManager, "spawnPoint", spawnPoint.transform);
        // GameManager (bonus)
        WireField(gameManager, "particles", particleEffects);
        WireField(gameManager, "commentary", commentaryComp);
        WireField(gameManager, "gameModes", gameModeManager);

        // GameFeedback
        WireField(gameFeedback, "cameraShake", camShake);
        WireField(gameFeedback, "popupText", popupText);
        WireField(gameFeedback, "screenFlash", flashImage);

        // ScoreManager
        WireField(scoreManager, "scoreText", scoreText);

        // MusicManager
        WireField(musicManager, "scoreManager", scoreManager);

        // ParticleEffects
        WireField(particleEffects, "ball", ball.transform);
        WireField(particleEffects, "hoopTransform", hoop.transform);

        // Commentary
        WireField(commentaryComp, "commentaryText", commentaryText);

        // GameModeManager
        WireField(gameModeManager, "hoopAssembly", hoopAssembly.transform);
        WireField(gameModeManager, "spawnPoint", spawnPoint.transform);
        WireField(gameModeManager, "ball", ballController);
        WireField(gameModeManager, "scoreManager", scoreManager);
        WireField(gameModeManager, "particles", particleEffects);
        WireField(gameModeManager, "commentary", commentaryComp);
        WireField(gameModeManager, "cameraRig", cameraRig.transform);
        WireField(gameModeManager, "cameraController", camController);
        WireField(gameModeManager, "timerText", timerText);
        WireField(gameModeManager, "modeInfoText", modeInfoText);
        WireField(gameModeManager, "streakText", streakText);

        Debug.Log("<b>Basketball scene setup complete!</b> Press Play to test.\n" +
                  "Modes: [1] Free Play  [2] 60s Timer  [3] Moving Hoop  [4] Challenge\n" +
                  "VR: Install XR Plugin Management for headset support.");
    }

    // ================================================================
    // HELPER METHODS
    // ================================================================

    private static GameObject CreateTriggerZone(
        string name, Transform parent, Vector3 localPos, Vector3 size,
        HoopTriggerZone.ZoneType zoneType)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;

        var box = obj.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        var zone = obj.AddComponent<HoopTriggerZone>();
        var so = new SerializedObject(zone);
        so.FindProperty("zone").enumValueIndex = (int)zoneType;
        so.ApplyModifiedProperties();

        return obj;
    }

    private static TextMeshProUGUI CreateUIText(
        Transform parent, string name, string text,
        float fontSize, TextAlignmentOptions alignment, Color color,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;

        var rect = tmp.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        return tmp;
    }

    private static void WireField(Component target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"Could not find field '{fieldName}' on {target.GetType().Name}");
        }
    }

    private static PhysicsMaterial GetOrCreatePhysMat(string name, float bounce, float friction)
    {
        string dir = "Assets/PhysicsMaterials";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "PhysicsMaterials");

        string path = $"{dir}/{name}.physicMaterial";
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (existing != null)
        {
            existing.bounciness = bounce;
            existing.dynamicFriction = friction;
            existing.staticFriction = friction;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var mat = new PhysicsMaterial(name)
        {
            bounciness = bounce,
            dynamicFriction = friction,
            staticFriction = friction,
            bounceCombine = PhysicsMaterialCombine.Average,
            frictionCombine = PhysicsMaterialCombine.Average
        };

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void SetColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        renderer.sharedMaterial = mat;
    }

    private static Material CreateUnlitMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        return mat;
    }

    // ================================================================
    // HAND BUILDER — detailed hand with forearm, palm, 5 fingers
    // ================================================================

    /// <summary>
    /// Builds a hand from a single smooth procedural mesh.
    /// Uses HandMeshBuilder for organic geometry with tapered fingers,
    /// curved palm, fingernails, and forearm — all in one mesh.
    /// </summary>
    private static GameObject BuildMeshHand(Transform parent, string name, Color skin, bool isRight)
    {
        var hand = new GameObject(name);
        hand.transform.SetParent(parent);
        hand.transform.localPosition = Vector3.zero;

        var meshObj = new GameObject("Mesh");
        meshObj.transform.SetParent(hand.transform);
        meshObj.transform.localPosition = Vector3.zero;
        meshObj.transform.localRotation = Quaternion.identity;

        var filter = meshObj.AddComponent<MeshFilter>();
        filter.sharedMesh = HandMeshBuilder.BuildHandMesh(isRight);

        var renderer = meshObj.AddComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", skin);
        mat.color = skin;
        mat.SetFloat("_Smoothness", 0.35f);
        renderer.sharedMaterial = mat;

        hand.SetActive(false);
        return hand;
    }

    private static void EnsureTagExists(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        var tags = tagManager.FindProperty("tags");

        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}
