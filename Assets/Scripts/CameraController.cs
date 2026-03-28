using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform hoopTarget;
    [SerializeField] private float lookSpeed = 10f;

    public bool AutoLookAtHoop { get; set; } = true;

    private void LateUpdate()
    {
        if (!AutoLookAtHoop || hoopTarget == null) return;

        Quaternion targetRot = Quaternion.LookRotation(hoopTarget.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.unscaledDeltaTime * lookSpeed);
    }
}
