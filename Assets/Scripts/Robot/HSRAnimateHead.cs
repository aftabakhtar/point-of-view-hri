using UnityEngine;
using System;

public class HSRAnimateHead : MonoBehaviour
{
    [SerializeField] private GameObject headTilt;
    [SerializeField] private GameObject headPanLink;

    [Header("Head Animation Settings")]
    [SerializeField] private float headPanSpeed = 2f; // Speed for head pan rotation
    [SerializeField] private float headTiltDuration = 0.5f; // Duration for tilt animation

    private Quaternion initialHeadPanRotation;
    private bool isAnimating = false;

    void Start()
    {
        if (headTilt == null)
        {
            Debug.LogError("Head Tilt GameObject is not assigned.");
        }
        
        if (headPanLink == null)
        {
            Debug.LogError("Head Pan Link GameObject is not assigned.");
        }
        else
        {
            // Store the initial local rotation of the head pan
            initialHeadPanRotation = headPanLink.transform.localRotation;
        }
    }

    void Update()
    {
        // Example usage: Animate head tilt when the space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Test with a sample direction (forward and to the right)
            Vector3 testDirection = (transform.forward + transform.right).normalized;
            AnimateHeadNodWithDirection(testDirection, 40, null);
        }
    }

    // Method to animate head tilt (legacy method for backward compatibility)
    // tiltAngle: angle in degrees to tilt the head to reach and then return to initial angle
    public void AnimateHeadTilt(float tiltAngle)
    {
        AnimateHeadNodWithDirection(null, tiltAngle, null);
    }

    // NEW METHOD: Animate head nod with direction
    // nodDirection: world space direction to look at (if null, just tilt without panning)
    // tiltAngle: angle in degrees to tilt the head
    // onComplete: callback when animation completes
    public void AnimateHeadNodWithDirection(Vector3? nodDirection, float tiltAngle, Action onComplete)
    {
        if (isAnimating)
        {
            Debug.LogWarning("Head animation already in progress. Skipping new animation request.");
            return;
        }

        if (headTilt == null || headPanLink == null)
        {
            Debug.LogError("Head components not properly assigned.");
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(HeadNodSequenceCoroutine(nodDirection, tiltAngle, onComplete));
    }

    private System.Collections.IEnumerator HeadNodSequenceCoroutine(Vector3? nodDirection, float tiltAngle, Action onComplete)
    {
        isAnimating = true;

        // Store initial pan rotation
        Quaternion initialPanRotation = headPanLink.transform.localRotation;
        Quaternion targetPanRotation = initialPanRotation;

        // Calculate target pan rotation if direction is provided
        if (nodDirection.HasValue)
        {
            Vector3 direction = nodDirection.Value;
            direction.y = 0; // Keep on horizontal plane

            if (direction.magnitude > 0.01f)
            {
                direction.Normalize();
                
                // Convert world direction to local space relative to the robot body
                Vector3 localDirection = headPanLink.transform.parent.InverseTransformDirection(direction);
                
                // Calculate the angle to rotate on Y-axis
                // Use Atan2 with (x, z) to get the angle in the horizontal plane
                float targetYAngle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
                
                Debug.Log($"Nod Direction (world): {direction}, Local: {localDirection}, Target Y Angle: {targetYAngle}");
                
                // Create target rotation (only Y-axis rotation for pan)
                targetPanRotation = Quaternion.Euler(0, targetYAngle, 0);
            }
        }

        // PHASE 1: Pan head to target direction
        float elapsed = 0f;
        float panDuration = 1f / headPanSpeed;

        while (elapsed < panDuration)
        {
            float t = elapsed / panDuration;
            headPanLink.transform.localRotation = Quaternion.Slerp(initialPanRotation, targetPanRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        headPanLink.transform.localRotation = targetPanRotation;

        // PHASE 2: Tilt head down and back up
        // IMPORTANT: Capture tilt rotation AFTER pan is complete
        Quaternion tiltStartRotation = headTilt.transform.localRotation;
        Quaternion targetTiltRotation = tiltStartRotation * Quaternion.Euler(tiltAngle, 0, 0);
        
        Debug.Log($"Tilt Start Rotation: {tiltStartRotation.eulerAngles}, Target Tilt: {targetTiltRotation.eulerAngles}");
        Debug.Log($"Before tilt - HeadPan Y rotation: {headPanLink.transform.localRotation.eulerAngles.y}");
        
        // Store the pan rotation to maintain it during tilt
        Quaternion maintainPanRotation = headPanLink.transform.localRotation;
        
        // Tilt down
        elapsed = 0f;
        while (elapsed < headTiltDuration)
        {
            // CRITICAL: Maintain pan rotation during tilt
            headPanLink.transform.localRotation = maintainPanRotation;
            
            float t = elapsed / headTiltDuration;
            headTilt.transform.localRotation = Quaternion.Slerp(tiltStartRotation, targetTiltRotation, t);
            
            // Debug: Check if pan is being maintained
            if (elapsed < 0.1f) // Log only at start of tilt
            {
                Debug.Log($"During tilt - HeadPan Y rotation: {headPanLink.transform.localRotation.eulerAngles.y}");
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        headTilt.transform.localRotation = targetTiltRotation;

        // Tilt back up to the exact rotation we started with
        elapsed = 0f;
        while (elapsed < headTiltDuration)
        {
            // CRITICAL: Maintain pan rotation during tilt
            headPanLink.transform.localRotation = maintainPanRotation;
            
            float t = elapsed / headTiltDuration;
            headTilt.transform.localRotation = Quaternion.Slerp(targetTiltRotation, tiltStartRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        headTilt.transform.localRotation = tiltStartRotation;

        // PHASE 3: Return head pan to initial rotation
        elapsed = 0f;
        while (elapsed < panDuration)
        {
            float t = elapsed / panDuration;
            headPanLink.transform.localRotation = Quaternion.Slerp(targetPanRotation, initialPanRotation, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        headPanLink.transform.localRotation = initialPanRotation;

        isAnimating = false;
        
        // Invoke completion callback
        onComplete?.Invoke();
    }
}