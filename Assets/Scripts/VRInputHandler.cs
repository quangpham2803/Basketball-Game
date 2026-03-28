using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;

// Raw XR input — no XR Interaction Toolkit needed.
// Install XR Plugin Management + OpenXR/Oculus in Package Manager to use.
public class VRInputHandler : MonoBehaviour
{
    [SerializeField] private XRNode handNode = XRNode.RightHand;
    [SerializeField] private float throwMultiplier = 1.8f;

    public bool IsActive { get; private set; }
    public Vector3 HandPosition { get; private set; }
    public Vector3 HandVelocity { get; private set; }
    public Vector3 HandAngularVelocity { get; private set; }
    public bool GripDown { get; private set; }
    public bool GripUp { get; private set; }
    public bool GripHeld { get; private set; }

    public float ThrowMultiplier => throwMultiplier;

    private InputDevice controller;
    private bool wasGripping;
    private float retryTimer;

    private void Update()
    {
        if (!IsActive)
        {
            retryTimer -= Time.unscaledDeltaTime;
            if (retryTimer <= 0f)
            {
                TryFindController();
                retryTimer = 2f;
            }
            return;
        }

        if (!controller.isValid)
        {
            IsActive = false;
            return;
        }

        ReadControllerState();
    }

    private void TryFindController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(handNode, devices);

        if (devices.Count > 0)
        {
            controller = devices[0];
            IsActive = true;
            Debug.Log($"[VR] Controller found: {controller.name} ({handNode})");
        }
    }

    private void ReadControllerState()
    {
        controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos);
        controller.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 localVel);
        controller.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 angVel);

        Transform origin = transform;
        HandPosition = origin.TransformPoint(localPos);
        HandVelocity = origin.TransformDirection(localVel);
        HandAngularVelocity = angVel;

        controller.TryGetFeatureValue(CommonUsages.gripButton, out bool gripBtn);
        controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerBtn);
        bool gripping = gripBtn || triggerBtn;

        GripDown = gripping && !wasGripping;
        GripUp = !gripping && wasGripping;
        GripHeld = gripping;
        wasGripping = gripping;
    }

    private void OnDrawGizmos()
    {
        if (!IsActive) return;
        Gizmos.color = GripHeld ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(HandPosition, 0.05f);
    }
}
