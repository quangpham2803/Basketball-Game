using UnityEngine;

public class HandsController : MonoBehaviour
{
    [SerializeField] private BallController ball;

    [Header("Hand Transforms")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Smoothing")]
    [SerializeField] private float followSpeed = 18f;

    [Header("Throw Animation")]
    [SerializeField] private float throwDuration = 0.45f;

    private Camera cam;
    private enum HandPhase { Hidden, Dribbling, Holding, Throwing, FollowThrough, Returning }
    private HandPhase phase = HandPhase.Dribbling;
    private float animTimer;

    // Snapshot at throw start
    private Vector3 rSnapPos, lSnapPos;
    private Quaternion rSnapRot, lSnapRot;

    private void Awake() => cam = Camera.main;

    private void OnEnable()
    {
        if (ball == null) return;
        ball.OnPickedUp += () => { phase = HandPhase.Holding; SetVisible(true); };
        ball.OnThrown += StartThrow;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        switch (phase)
        {
            case HandPhase.Hidden:
                SetVisible(false);
                    if (ball != null && ball.State == BallController.BallState.Dribbling)
                    phase = HandPhase.Dribbling;
                break;
            case HandPhase.Dribbling:
                AnimateDribble();
                break;
            case HandPhase.Holding:
                AnimateHolding();
                break;
            case HandPhase.Throwing:
                AnimateThrow();
                break;
            case HandPhase.FollowThrough:
                AnimateFollowThrough();
                break;
            case HandPhase.Returning:
                AnimateReturn();
                break;
        }
    }

    private void AnimateDribble()
    {
        if (ball == null || ball.State != BallController.BallState.Dribbling)
        {
            phase = HandPhase.Hidden;
            return;
        }

        SetVisible(true);
        Vector3 bp = ball.transform.position;
        Vector3 r = cam.transform.right;
        Vector3 u = cam.transform.up;
        Vector3 f = cam.transform.forward;

        Vector3 rPos = bp + u * 0.14f;
        Quaternion rRot = Quaternion.LookRotation(f, -u);
        rightHand.position = Vector3.Lerp(rightHand.position, rPos, Time.deltaTime * 22f);
        rightHand.rotation = Quaternion.Slerp(rightHand.rotation, rRot, Time.deltaTime * 15f);

        Vector3 lPos = cam.transform.position + r * -0.3f + u * -0.35f + f * 0.2f;
        Quaternion lRot = cam.transform.rotation * Quaternion.Euler(10f, 0f, 20f);
        leftHand.position = Vector3.Lerp(leftHand.position, lPos, Time.deltaTime * 10f);
        leftHand.rotation = Quaternion.Slerp(leftHand.rotation, lRot, Time.deltaTime * 10f);
    }

    private void AnimateHolding()
    {
        if (ball == null || ball.State != BallController.BallState.Held)
        {
            phase = HandPhase.Hidden;
            return;
        }

        Vector3 bp = ball.transform.position;
        Vector3 r = cam.transform.right;
        Vector3 u = cam.transform.up;
        Vector3 f = cam.transform.forward;

        Vector3 rPos = bp + r * 0.02f + u * -0.15f + f * -0.03f;
        Quaternion rRot = Quaternion.LookRotation(f, u) * Quaternion.Euler(-20f, 0f, 5f);

        rightHand.position = Vector3.Lerp(rightHand.position, rPos, Time.deltaTime * followSpeed);
        rightHand.rotation = Quaternion.Slerp(rightHand.rotation, rRot, Time.deltaTime * followSpeed);

        Vector3 lPos = bp + r * -0.17f + u * 0f + f * 0.01f;
        Quaternion lRot = Quaternion.LookRotation(f, r) * Quaternion.Euler(0f, 0f, 15f);

        leftHand.position = Vector3.Lerp(leftHand.position, lPos, Time.deltaTime * followSpeed);
        leftHand.rotation = Quaternion.Slerp(leftHand.rotation, lRot, Time.deltaTime * followSpeed);
    }

    private void StartThrow()
    {
        phase = HandPhase.Throwing;
        animTimer = 0f;
        rSnapPos = rightHand.position;
        rSnapRot = rightHand.rotation;
        lSnapPos = leftHand.position;
        lSnapRot = leftHand.rotation;
    }

    private void AnimateThrow()
    {
        animTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(animTimer / throwDuration);

        Vector3 r = cam.transform.right;
        Vector3 u = cam.transform.up;
        Vector3 f = cam.transform.forward;
        Vector3 head = cam.transform.position;

        if (t < 0.12f)
        {
            float p = t / 0.12f;
            Vector3 cockPos = rSnapPos + u * 0.05f + f * -0.05f;
            rightHand.position = Vector3.Lerp(rSnapPos, cockPos, EaseOut(p));
            Quaternion cockRot = Quaternion.LookRotation(f, u) * Quaternion.Euler(-35f, 0f, 0f);
            rightHand.rotation = Quaternion.Slerp(rSnapRot, cockRot, EaseOut(p));
        }
        else if (t < 0.5f)
        {
            float p = (t - 0.12f) / 0.38f;
            Vector3 cockPos = rSnapPos + u * 0.05f + f * -0.05f;
            Vector3 pushPos = head + f * 1.0f + u * 0.25f + r * 0.05f;
            rightHand.position = Vector3.Lerp(cockPos, pushPos, EaseOut(p));
            Quaternion pushRot = Quaternion.LookRotation(f, u);
            rightHand.rotation = Quaternion.Slerp(rightHand.rotation, pushRot, EaseOut(p));
        }
        else if (t < 0.7f)
        {
            float p = (t - 0.5f) / 0.2f;
            Vector3 pushPos = head + f * 1.0f + u * 0.25f + r * 0.05f;
            Vector3 flickPos = head + f * 1.1f + u * 0.15f + r * 0.05f;
            rightHand.position = Vector3.Lerp(pushPos, flickPos, EaseOut(p));
            Quaternion gooseneck = Quaternion.LookRotation(f * 0.5f + (-u) * 0.5f, -u);
            rightHand.rotation = Quaternion.Slerp(rightHand.rotation, gooseneck, EaseOut(p));
        }
        else
        {
            Vector3 flickPos = head + f * 1.1f + u * 0.15f + r * 0.05f;
            rightHand.position = flickPos;
        }

        if (t < 0.15f)
        {
            float p = t / 0.15f;
            Vector3 lSetPos = head + u * 0.1f + f * 0.15f + r * -0.12f;
            leftHand.position = Vector3.Lerp(lSnapPos, lSetPos, EaseOut(p));
            leftHand.rotation = Quaternion.Slerp(lSnapRot, lSnapRot, p);
        }
        else if (t < 0.35f)
        {
            float p = (t - 0.15f) / 0.2f;
            Vector3 lSetPos = head + u * 0.1f + f * 0.15f + r * -0.12f;
            Vector3 lAwayPos = head + u * 0.0f + f * 0.1f + r * -0.35f;
            leftHand.position = Vector3.Lerp(lSetPos, lAwayPos, EaseOut(p));
            Quaternion openRot = Quaternion.LookRotation(-r, u);
            leftHand.rotation = Quaternion.Slerp(leftHand.rotation, openRot, EaseOut(p));
        }
        else
        {
            float p = (t - 0.35f) / 0.65f;
            Vector3 lAwayPos = head + u * 0.0f + f * 0.1f + r * -0.35f;
            Vector3 lRestPos = head + cam.transform.TransformDirection(new Vector3(-0.3f, -0.45f, 0.3f));
            leftHand.position = Vector3.Lerp(lAwayPos, lRestPos, EaseInOut(p));
            leftHand.rotation = Quaternion.Slerp(leftHand.rotation, cam.transform.rotation, p * 0.5f);
        }

        if (t >= 1f)
            phase = HandPhase.FollowThrough;
    }

    private float followThroughTimer;

    private void AnimateFollowThrough()
    {
        followThroughTimer += Time.unscaledDeltaTime;

        if (followThroughTimer >= 0.4f)
        {
            followThroughTimer = 0f;
            phase = HandPhase.Returning;
        }
    }

    private void AnimateReturn()
    {
        Vector3 lTarget = cam.transform.position + cam.transform.TransformDirection(new Vector3(-0.3f, -0.5f, 0.3f));
        Vector3 rTarget = cam.transform.position + cam.transform.TransformDirection(new Vector3(0.2f, -0.5f, 0.3f));

        leftHand.position = Vector3.Lerp(leftHand.position, lTarget, Time.deltaTime * 5f);
        rightHand.position = Vector3.Lerp(rightHand.position, rTarget, Time.deltaTime * 5f);
        leftHand.rotation = Quaternion.Slerp(leftHand.rotation, cam.transform.rotation, Time.deltaTime * 5f);
        rightHand.rotation = Quaternion.Slerp(rightHand.rotation, cam.transform.rotation, Time.deltaTime * 5f);

        if (Vector3.Distance(rightHand.position, rTarget) < 0.05f)
        {
            if (ball != null && ball.State == BallController.BallState.Dribbling)
                phase = HandPhase.Dribbling;
            else
                phase = HandPhase.Hidden;
        }
    }

    private void SetVisible(bool v)
    {
        if (leftHand != null) leftHand.gameObject.SetActive(v);
        if (rightHand != null) rightHand.gameObject.SetActive(v);
    }

    private float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    private float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
}
