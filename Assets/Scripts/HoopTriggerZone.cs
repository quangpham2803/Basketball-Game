using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HoopTriggerZone : MonoBehaviour
{
    public enum ZoneType { Top, Bottom }

    [SerializeField] private ZoneType zone;

    private HoopScoreDetector detector;

    private void Awake()
    {
        detector = GetComponentInParent<HoopScoreDetector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null) return;

        var ball = other.attachedRigidbody.GetComponent<BallController>();
        if (ball == null || ball.State != BallController.BallState.InFlight) return;

        if (zone == ZoneType.Top)
            detector.BallEnteredTop();
        else
            detector.BallEnteredBottom(ball);
    }
}
