using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    public enum BallState { Idle, Dribbling, Held, InFlight, Stopped }

    [Header("Slingshot Throw")]
    [Tooltip("Converts pull distance (pixels) to throw force")]
    [SerializeField] private float pullForceMultiplier = 0.035f;
    [SerializeField] private float minThrowSpeed = 4f;
    [SerializeField] private float maxThrowSpeed = 18f;
    [Tooltip("Arc angle at minimal pull")]
    [SerializeField] private float minArcAngle = 45f;
    [Tooltip("Arc angle at maximum pull")]
    [SerializeField] private float maxArcAngle = 55f;
    [Tooltip("Horizontal aim sensitivity (inverted slingshot)")]
    [SerializeField] private float sideAimMultiplier = 0.004f;
    [Tooltip("How much the throw auto-aims toward the hoop (0=none, 1=full)")]
    [SerializeField] private float autoAimStrength = 0.75f;
    [Tooltip("How much the throw power auto-corrects to reach the hoop (0=none, 1=full)")]
    [SerializeField] private float powerAssistStrength = 0.55f;
    [SerializeField] private float backspinStrength = 8f;

    [Header("Hold Settings")]
    [SerializeField] private float holdDistance = 1.0f;
    [SerializeField] private float holdUpOffset = -0.2f;
    [SerializeField] private float holdSmoothSpeed = 20f;
    [SerializeField] private float weightSag = 0.04f;
    [Tooltip("How much the ball visually moves when charging")]
    [SerializeField] private float pullbackVisual = 0.2f;
    [Tooltip("How much each scroll tick changes the arc angle (degrees)")]
    [SerializeField] private float scrollAngleStep = 2f;

    [Header("Dribble")]
    [SerializeField] private float dribbleHeight = 0.4f;
    [SerializeField] private float dribbleSpeed = 4.5f;
    [SerializeField] private float dribbleForward = 0.5f;

    [Header("VR (Optional)")]
    [SerializeField] private VRInputHandler vrInput;

    public BallState State { get; private set; } = BallState.Dribbling;

    public bool FreeAim { get; set; }

    private Vector3 lockedForward;
    public bool HitRimSinceThrow { get; private set; }
    public Vector3 LastThrowPosition { get; private set; }

    public event Action OnPickedUp;
    public event Action OnThrown;
    public event Action OnStopped;
    public event Action<float> OnBounce;

    private Rigidbody rb;
    private Camera cam;
    private TrailRenderer trail;
    private Transform hoopTarget;
    private Transform[] allHoopTargets;

    private Vector2 grabScreenPos;
    private float scrollAngleOffset;

    private const int BufferSize = 10;
    private const int SamplesToUse = 5;
    private readonly Vector3[] worldPosBuffer = new Vector3[BufferSize];
    private readonly float[] timeBuffer = new float[BufferSize];
    private int bufferIdx;

    private float thrownTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
        trail = GetComponent<TrailRenderer>();

        var hoopObjs = GameObject.FindGameObjectsWithTag("HoopTarget");
        if (hoopObjs.Length > 0)
        {
            allHoopTargets = new Transform[hoopObjs.Length];
            for (int i = 0; i < hoopObjs.Length; i++)
                allHoopTargets[i] = hoopObjs[i].transform;
            hoopTarget = allHoopTargets[0];
        }
    }

    private void Start()
    {
        rb.isKinematic = true;
    }

    private void Update()
    {
        UpdateNearestHoop();

        if (vrInput != null && vrInput.IsActive)
        {
            UpdateVR();
            return;
        }

        UpdateMouse();
    }

    private void UpdateNearestHoop()
    {
        if (allHoopTargets == null || allHoopTargets.Length <= 1) return;

        float bestDist = float.MaxValue;
        Transform best = hoopTarget;
        Vector3 pos = transform.position;

        for (int i = 0; i < allHoopTargets.Length; i++)
        {
            if (allHoopTargets[i] == null) continue;
            float d = (allHoopTargets[i].position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = allHoopTargets[i];
            }
        }

        hoopTarget = best;
    }

    private void UpdateMouse()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            CancelAim();
            return;
        }

        switch (State)
        {
            case BallState.Dribbling:
                UpdateDribble();
                if (Input.GetMouseButtonDown(0))
                    PickUpMouse();
                break;

            case BallState.Idle:
                if (Input.GetMouseButtonDown(0))
                    PickUpMouse();
                break;

            case BallState.Held:
                UpdateHoldPosition();
                HandleScrollAngle();
                if (Input.GetMouseButtonUp(0))
                    PerformThrow();
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                    CancelAim();
                break;

            case BallState.InFlight:
                CheckStopped();
                break;
        }
    }

    private void PickUpMouse()
    {
        State = BallState.Held;
        rb.isKinematic = true;
        EnableTrail(false);

        grabScreenPos = Input.mousePosition;
        scrollAngleOffset = 0f;

        if (FreeAim)
        {
            lockedForward = cam.transform.forward;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        OnPickedUp?.Invoke();
    }

    private void UpdateDribble()
    {
        if (cam == null) return;

        float t = Time.time * dribbleSpeed;
        float bounce = Mathf.Abs(Mathf.Sin(t));
        bounce = Mathf.Pow(bounce, 0.6f);

        Vector3 basePos = cam.transform.position
                        + cam.transform.forward * dribbleForward
                        + cam.transform.right * 0.2f
                        + Vector3.down * 0.7f;

        Vector3 targetPos = basePos + Vector3.up * bounce * dribbleHeight;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 20f);
    }

    private void HandleScrollAngle()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            scrollAngleOffset += scroll * scrollAngleStep;
            scrollAngleOffset = Mathf.Clamp(scrollAngleOffset, -(maxArcAngle - minArcAngle), maxArcAngle - minArcAngle);
        }
    }

    private void CancelAim()
    {
        RelockCursorIfFreeAim();
        var spawnObj = GameObject.Find("SpawnPoint");
        Vector3 returnPos = spawnObj != null ? spawnObj.transform.position : transform.position;
        ResetToPosition(returnPos);
    }

    private void RelockCursorIfFreeAim()
    {
        if (FreeAim)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void UpdateHoldPosition()
    {
        Vector3 target = cam.transform.position
                       + cam.transform.forward * holdDistance
                       + cam.transform.up * holdUpOffset
                       + cam.transform.right * 0.25f;

        Vector2 aim = GetAimDelta();
        float aimUpNorm = Mathf.Clamp01(aim.y / (Screen.height * 0.3f));
        target += cam.transform.up * aimUpNorm * pullbackVisual;
        target.y -= weightSag;

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * holdSmoothSpeed);
    }

    private Vector2 GetAimDelta()
    {
        return (Vector2)Input.mousePosition - grabScreenPos;
    }

    public Vector3 GetThrowVelocity()
    {
        Vector2 aim = GetAimDelta();
        float aimDist = aim.magnitude;

        float playerPower = Mathf.Clamp(aimDist * pullForceMultiplier, minThrowSpeed, maxThrowSpeed);

        float aimUpNorm = Mathf.Clamp01(aim.y / (Screen.height * 0.3f));
        float arcAngle = Mathf.Lerp(minArcAngle, maxArcAngle, aimUpNorm) + scrollAngleOffset;
        arcAngle = Mathf.Clamp(arcAngle, 20f, 80f);
        float rad = arcAngle * Mathf.Deg2Rad;

        Vector3 fwd;
        if (FreeAim)
        {
            fwd = lockedForward;
            fwd.y = 0f;
            fwd.Normalize();
        }
        else
        {
            fwd = cam.transform.forward;
            fwd.y = 0f;
            fwd.Normalize();

            if (hoopTarget != null && autoAimStrength > 0f)
            {
                Vector3 toHoop = hoopTarget.position - transform.position;
                toHoop.y = 0f;
                toHoop.Normalize();
                fwd = Vector3.Lerp(fwd, toHoop, autoAimStrength).normalized;
            }
        }

        float power = playerPower;
        if (hoopTarget != null && powerAssistStrength > 0f)
        {
            float idealSpeed = ComputeIdealSpeed(rad);
            if (idealSpeed > 0f)
                power = Mathf.Lerp(playerPower, idealSpeed, powerAssistStrength);
        }

        power = Mathf.Clamp(power, minThrowSpeed, maxThrowSpeed);

        float side = aim.x * sideAimMultiplier;

        return fwd * (Mathf.Cos(rad) * power)
             + Vector3.up * (Mathf.Sin(rad) * power)
             + cam.transform.right * side;
    }

    // v = sqrt(g * dx^2 / (2 * cos^2(a) * (dx * tan(a) - dy)))
    private float ComputeIdealSpeed(float angleRad)
    {
        if (hoopTarget == null) return -1f;

        Vector3 toHoop = hoopTarget.position - transform.position;
        float dx = new Vector3(toHoop.x, 0f, toHoop.z).magnitude;
        float dy = toHoop.y;

        float cosA = Mathf.Cos(angleRad);
        float tanA = Mathf.Tan(angleRad);

        float denominator = dx * tanA - dy;
        if (denominator <= 0.01f) return -1f; // impossible angle

        float vSquared = (Physics.gravity.magnitude * dx * dx) / (2f * cosA * cosA * denominator);
        if (vSquared <= 0f) return -1f;

        return Mathf.Sqrt(vSquared);
    }

    private void PerformThrow()
    {
        Vector3 throwVel = GetThrowVelocity();

        State = BallState.InFlight;
        LastThrowPosition = transform.position;
        HitRimSinceThrow = false;
        rb.isKinematic = false;
        rb.linearVelocity = throwVel;

        Vector3 spinAxis = Vector3.Cross(throwVel.normalized, Vector3.up).normalized;
        rb.angularVelocity = spinAxis * backspinStrength;

        EnableTrail(true);
        thrownTime = Time.time;
        groundHitTime = -1f;

        RelockCursorIfFreeAim();
        OnThrown?.Invoke();
    }

    private void UpdateVR()
    {
        switch (State)
        {
            case BallState.Idle:
                if (vrInput.GripDown)
                    PickUpVR();
                break;

            case BallState.Held:
                transform.position = Vector3.Lerp(
                    transform.position,
                    vrInput.HandPosition,
                    Time.deltaTime * holdSmoothSpeed);
                RecordWorldSample();
                if (vrInput.GripUp)
                    PerformVRThrow();
                break;

            case BallState.InFlight:
                CheckStopped();
                break;
        }
    }

    private void PickUpVR()
    {
        State = BallState.Held;
        rb.isKinematic = true;
        EnableTrail(false);

        Vector3 pos = transform.position;
        float time = Time.unscaledTime;
        for (int i = 0; i < BufferSize; i++)
        {
            worldPosBuffer[i] = pos;
            timeBuffer[i] = time;
        }
        bufferIdx = 0;

        OnPickedUp?.Invoke();
    }

    private void RecordWorldSample()
    {
        bufferIdx = (bufferIdx + 1) % BufferSize;
        worldPosBuffer[bufferIdx] = transform.position;
        timeBuffer[bufferIdx] = Time.unscaledTime;
    }

    private void PerformVRThrow()
    {
        Vector3 throwVel = vrInput.HandVelocity * vrInput.ThrowMultiplier;

        State = BallState.InFlight;
        LastThrowPosition = transform.position;
        HitRimSinceThrow = false;
        rb.isKinematic = false;
        rb.linearVelocity = throwVel;
        rb.angularVelocity = vrInput.HandAngularVelocity;

        EnableTrail(true);
        thrownTime = Time.time;
        OnThrown?.Invoke();
    }

    private float groundHitTime = -1f;

    private void CheckStopped()
    {
        if (Time.time - thrownTime < 0.5f) return;

        bool fellOff = transform.position.y < -3f;

        if (fellOff)
        {
            groundHitTime = -1f;
            State = BallState.Stopped;
            OnStopped?.Invoke();
            return;
        }

        // Start 2s timer when ball first touches ground
        if (transform.position.y < 0.3f && groundHitTime < 0f)
            groundHitTime = Time.time;

        // Reset timer if ball goes back up (bounced off rim etc.)
        if (transform.position.y > 0.5f)
            groundHitTime = -1f;

        // 2 seconds after hitting ground → reset
        if (groundHitTime > 0f && Time.time - groundHitTime > 2f)
        {
            groundHitTime = -1f;
            State = BallState.Stopped;
            OnStopped?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (State != BallState.InFlight) return;

        if (collision.gameObject.CompareTag("Rim"))
            HitRimSinceThrow = true;

        float impact = collision.impulse.magnitude;
        if (impact > 0.3f)
            OnBounce?.Invoke(impact);
    }

    private void EnableTrail(bool on)
    {
        if (trail == null) return;
        if (!on) trail.Clear();
        trail.enabled = on;
    }

    public void ResetToPosition(Vector3 pos)
    {
        EnableTrail(false);
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = pos;
        State = BallState.Dribbling;
    }
}
