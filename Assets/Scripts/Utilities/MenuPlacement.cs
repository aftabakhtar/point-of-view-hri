using UnityEngine;

public class MenuPlacement : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private float maxDistance = 2.0f; // Distance in front of the player
    [SerializeField] private float sphereRadius = 0.3f;  // For obstacle detection
    [SerializeField] private float offsetY = 1f;         // Vertical offset (menu height)
    [SerializeField] private float offsetX = 0f;         // Optional horizontal offset (x)
    [SerializeField] private float offsetZ = 0f;         // Optional horizontal offset (z)
    [SerializeField] private Transform centerEyeAnchor;

    private Vector3 foundMenuPosition;


    /// <summary>
    /// Computes an optimal position for the menu so that it is placed a set distance in front of the player,
    /// at the specified vertical offset, and (if possible) avoids obstacles.
    /// </summary>
    private void FindOptimalPosition()
    {
        // Get the horizontal forward direction from the player's camera.
        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0;
        forward.Normalize();

        // Compute the candidate position directly in front of the player.
        Vector3 candidate = centerEyeAnchor.position + forward * maxDistance;
        candidate.y = centerEyeAnchor.position.y + offsetY;
        candidate += new Vector3(offsetX, 0, offsetZ); // Apply additional offsets if desired.

        // Check for obstacles between the camera and the candidate position.
        Vector3 origin = centerEyeAnchor.position;
        Vector3 direction = candidate - origin;
        float distance = direction.magnitude;
        direction.Normalize();

        RaycastHit hit;
        if (!Physics.SphereCast(origin, sphereRadius, direction, out hit, distance))
        {
            // No obstacle detected.
            foundMenuPosition = candidate;
            return;
        }

        // If an obstacle is detected, try adjusting the candidate position by rotating slightly left/right.
        float[] angleOffsets = { 15f, -15f, 30f, -30f, 45f, -45f };
        foreach (float angle in angleOffsets)
        {
            Vector3 adjustedForward = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Vector3 adjustedCandidate = centerEyeAnchor.position + adjustedForward * maxDistance;
            adjustedCandidate.y = centerEyeAnchor.position.y + offsetY;
            adjustedCandidate += new Vector3(offsetX, 0, offsetZ);

            direction = adjustedCandidate - origin;
            distance = direction.magnitude;
            direction.Normalize();

            if (!Physics.SphereCast(origin, sphereRadius, direction, out hit, distance))
            {
                foundMenuPosition = adjustedCandidate;
                return;
            }
        }

        // If all adjustments are obstructed, fall back to the original candidate.
        foundMenuPosition = candidate;
    }

    public Vector3 CalculateLookTarget(GameObject gameObject)
    {
        return new Vector3(centerEyeAnchor.position.x, gameObject.transform.position.y, centerEyeAnchor.position.z);
    }
    public Vector3 GetMainMenuOptimalPosition()
    {
        FindOptimalPosition();
        return foundMenuPosition;
    }
}
