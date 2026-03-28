using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreview : MonoBehaviour
{
    [SerializeField] private BallController ball;
    [SerializeField] private int maxPoints = 80;
    [SerializeField] private float timeStep = 0.03f;
    [SerializeField] private float ballRadius = 0.12f;
    [SerializeField] private int maxBounces = 3;
    [SerializeField] private float bounciness = 0.6f;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void LateUpdate()
    {
        if (ball == null || ball.State != BallController.BallState.Held)
        {
            line.positionCount = 0;
            return;
        }

        Vector3 throwVel = ball.GetThrowVelocity();
        SimulateTrajectory(ball.transform.position, throwVel);
    }

    private void SimulateTrajectory(Vector3 startPos, Vector3 startVel)
    {
        Vector3 pos = startPos;
        Vector3 vel = startVel;
        Vector3 gravity = Physics.gravity;
        int pointIndex = 0;
        int bounces = 0;

        var points = new Vector3[maxPoints];
        points[pointIndex++] = pos;

        for (int i = 1; i < maxPoints; i++)
        {
            Vector3 nextVel = vel + gravity * timeStep;
            Vector3 nextPos = pos + vel * timeStep + 0.5f * gravity * (timeStep * timeStep);

            Vector3 moveDir = nextPos - pos;
            float moveDist = moveDir.magnitude;

            if (moveDist > 0.001f && bounces < maxBounces)
            {
                if (Physics.SphereCast(pos, ballRadius, moveDir.normalized, out RaycastHit hit, moveDist))
                {
                    points[pointIndex++] = hit.point + hit.normal * ballRadius;

                    vel = Vector3.Reflect(nextVel, hit.normal) * bounciness;
                    pos = hit.point + hit.normal * (ballRadius + 0.01f);
                    bounces++;

                    if (pointIndex >= maxPoints) break;
                    continue;
                }
            }

            pos = nextPos;
            vel = nextVel;
            points[pointIndex++] = pos;

            if (pos.y < 0f) break;
        }

        line.positionCount = pointIndex;
        for (int i = 0; i < pointIndex; i++)
            line.SetPosition(i, points[i]);
    }
}
