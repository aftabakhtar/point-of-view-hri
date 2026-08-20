using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TrajectoryPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArticulationBody robotBaseLink;
    [SerializeField] private HSRAnimateHead headAnimator;

    [Header("Playback Settings")]
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool loopTrajectory = false;

    [Header("Head Nod Settings")]
    [SerializeField] private int targetPedId = 0; // Which pedestrian to look at (-1 for first available)
    [SerializeField] private float headNodSlowdownFactor = 0.3f;
    [SerializeField] private float headNodRotationSpeed = 2f;
    [SerializeField] private float headNodTiltAngle = 15f;

    // Public state
    public Vector3 CurrentPosition { get; private set; }
    public Quaternion CurrentRotation { get; private set; }
    public bool IsPlaying { get; private set; }
    public int CurrentPointIndex { get; private set; }
    public float Progress { get; private set; }
    public List<TrajectoryPointData> TrajectoryData { get; private set; }

    // Events
    public event Action OnTrajectoryStarted;
    public event Action OnTrajectoryCompleted;
    public event Action OnTrajectoryLooped;
    public event Action<Vector3> OnPositionUpdated;

    // Head nod state
    private bool isPerformingHeadNod = false;
    private bool hasTriggeredHeadNod = false;
    private enum HeadNodPhase { Approaching, WaitingForHeadNod, Complete }
    private HeadNodPhase currentHeadNodPhase = HeadNodPhase.Complete;

    public class TrajectoryPointData
    {
        public Vector3 position;
        public List<HeadNodDirectionData> headNodDirections; // Multiple head nod directions
        public bool hasHeadNod;
    }

    public class HeadNodDirectionData
    {
        public int targetPedId;
        public Vector3 direction;
    }

    void FixedUpdate()
    {
        if (IsPlaying)
        {
            UpdateTrajectoryPlayback();
        }
    }

    public bool LoadTrajectory(string filename, int? targetPedIdOverride = null)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "User Study", filename);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Trajectory file not found: {filePath}");
            return false;
        }

        TrajectoryData data = JsonManager<TrajectoryData>.ReadJson(filePath);

        if (data == null || data.points == null || data.points.Count == 0)
        {
            Debug.LogError("Failed to load trajectory data or trajectory is empty.");
            return false;
        }

        int pedIdToUse = targetPedIdOverride ?? targetPedId;
        targetPedId = pedIdToUse;

        TrajectoryData = new List<TrajectoryPointData>();
        foreach (var point in data.points)
        {
            TrajectoryPointData pointData = new TrajectoryPointData
            {
                position = point.position.ToVector3(),
                headNodDirections = new List<HeadNodDirectionData>()
            };

            // Process head nod data if present
            if (point.headNod != null && point.headNod.Count > 0)
            {
                foreach (var headNod in point.headNod)
                {
                    pointData.headNodDirections.Add(new HeadNodDirectionData
                    {
                        targetPedId = headNod.target_ped_id,
                        direction = headNod.direction.ToVector3()
                    });
                }
                pointData.hasHeadNod = true;
            }
            else
            {
                pointData.hasHeadNod = false;
            }

            TrajectoryData.Add(pointData);
        }

        Debug.Log($"Trajectory loaded: {TrajectoryData.Count} points from {filename}");
        Debug.Log($"Using head nod directions for target_ped_id: {pedIdToUse}");
        
        return true;
    }

    public void Play()
    {
        if (TrajectoryData == null || TrajectoryData.Count == 0)
        {
            Debug.LogWarning("Cannot play: No trajectory loaded");
            return;
        }

        IsPlaying = true;
        CurrentPointIndex = 0;
        Progress = 0f;
        isPerformingHeadNod = false;
        hasTriggeredHeadNod = false;
        currentHeadNodPhase = HeadNodPhase.Complete;

        // Teleport to start
        CurrentPosition = TrajectoryData[0].position;

        if (TrajectoryData.Count > 1)
        {
            Vector3 startDirection = (TrajectoryData[1].position - TrajectoryData[0].position).normalized;
            if (startDirection.magnitude > 0.01f)
            {
                CurrentRotation = Quaternion.LookRotation(startDirection);
            }
            else
            {
                CurrentRotation = robotBaseLink.transform.rotation;
            }
        }
        else
        {
            CurrentRotation = robotBaseLink.transform.rotation;
        }

        robotBaseLink.TeleportRoot(CurrentPosition, CurrentRotation);

        OnTrajectoryStarted?.Invoke();
        OnPositionUpdated?.Invoke(CurrentPosition);

        Debug.Log($"Playing trajectory with {TrajectoryData.Count} points.");
    }

    public void Stop()
    {
        IsPlaying = false;
        CurrentPointIndex = 0;
        Progress = 0f;
        isPerformingHeadNod = false;
        hasTriggeredHeadNod = false;
        currentHeadNodPhase = HeadNodPhase.Complete;

        Debug.Log("Trajectory playback stopped.");
    }

    public void LoadAndPlay(string filename, int? targetPedIdOverride = null)
    {
        if (LoadTrajectory(filename, targetPedIdOverride))
        {
            Play();
        }
    }

    private void UpdateTrajectoryPlayback()
    {
        if (TrajectoryData == null || TrajectoryData.Count == 0)
        {
            Stop();
            return;
        }

        TrajectoryPointData currentPoint = TrajectoryData[CurrentPointIndex];
        bool shouldPerformHeadNod = currentPoint.hasHeadNod && !hasTriggeredHeadNod;

        TrajectoryPointData nextPoint = null;
        bool nextHasHeadNod = false;
        if (CurrentPointIndex + 1 < TrajectoryData.Count)
        {
            nextPoint = TrajectoryData[CurrentPointIndex + 1];
            nextHasHeadNod = nextPoint.hasHeadNod;
        }

        // Calculate speed
        float currentSpeed = playbackSpeed;

        if (shouldPerformHeadNod && !isPerformingHeadNod)
        {
            currentSpeed *= headNodSlowdownFactor;
            StartHeadNodSequence(currentPoint);
        }
        else if (nextHasHeadNod && Progress > 0.7f && !isPerformingHeadNod)
        {
            currentSpeed *= headNodSlowdownFactor;
        }

        if (isPerformingHeadNod)
        {
            currentSpeed *= headNodSlowdownFactor;
        }

        // Advance progress
        Progress += currentSpeed * Time.fixedDeltaTime;

        if (Progress >= 1f)
        {
            // Check for missed head nod
            if (currentPoint.hasHeadNod && !hasTriggeredHeadNod && !isPerformingHeadNod)
            {
                Debug.LogWarning($"Missed head nod at point {CurrentPointIndex}. Triggering now.");
                StartHeadNodSequence(currentPoint);
                Progress = 0.99f;
                return;
            }

            Progress = 0f;
            CurrentPointIndex++;
            hasTriggeredHeadNod = false;

            // Check if at end
            if (CurrentPointIndex >= TrajectoryData.Count)
            {
                if (loopTrajectory)
                {
                    CurrentPointIndex = 0;
                    OnTrajectoryLooped?.Invoke();
                    Debug.Log("Looping trajectory");
                }
                else
                {
                    Debug.Log("Trajectory completed");
                    IsPlaying = false;
                    OnTrajectoryCompleted?.Invoke();
                    return;
                }
            }
        }

        // Interpolate position and rotation - Body always faces movement direction
        if (CurrentPointIndex < TrajectoryData.Count - 1)
        {
            Vector3 currentPt = TrajectoryData[CurrentPointIndex].position;
            Vector3 nextPt = TrajectoryData[CurrentPointIndex + 1].position;

            CurrentPosition = Vector3.Lerp(currentPt, nextPt, Progress);

            // Body always faces movement direction, regardless of head nod
            Vector3 direction = (nextPt - currentPt).normalized;
            if (direction.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                CurrentRotation = Quaternion.Slerp(CurrentRotation, targetRotation, 0.1f);
            }

            robotBaseLink.TeleportRoot(CurrentPosition, CurrentRotation);
        }
        else
        {
            CurrentPosition = TrajectoryData[CurrentPointIndex].position;
            robotBaseLink.TeleportRoot(CurrentPosition, CurrentRotation);
        }

        OnPositionUpdated?.Invoke(CurrentPosition);
    }

    private void StartHeadNodSequence(TrajectoryPointData nodPoint)
    {
        isPerformingHeadNod = true;
        hasTriggeredHeadNod = true;
        currentHeadNodPhase = HeadNodPhase.WaitingForHeadNod;

        // Find the appropriate head nod direction based on targetPedId
        Vector3? nodDirection = GetHeadNodDirectionForTarget(nodPoint);

        if (!nodDirection.HasValue)
        {
            Debug.LogWarning($"No head nod direction found for target_ped_id {targetPedId} at point {CurrentPointIndex}. Using first available.");
        }

        Debug.Log($"Starting head nod at point {CurrentPointIndex} for target_ped_id {targetPedId}");
        
        // Trigger the head animation with direction and callback
        if (headAnimator != null)
        {
            headAnimator.AnimateHeadNodWithDirection(nodDirection, headNodTiltAngle, OnHeadNodComplete);
        }
        else
        {
            // If no head animator, complete immediately
            OnHeadNodComplete();
        }
    }

    private Vector3? GetHeadNodDirectionForTarget(TrajectoryPointData nodPoint)
    {
        if (nodPoint.headNodDirections == null || nodPoint.headNodDirections.Count == 0)
        {
            return null;
        }

        // If targetPedId is -1, use the first available direction
        if (targetPedId == -1)
        {
            return nodPoint.headNodDirections[0].direction;
        }

        // Search for matching target_ped_id
        foreach (var headNod in nodPoint.headNodDirections)
        {
            if (headNod.targetPedId == targetPedId)
            {
                return headNod.direction;
            }
        }

        // If no match found, use the first available as fallback
        Debug.LogWarning($"Target ped ID {targetPedId} not found. Using first available head nod direction.");
        return nodPoint.headNodDirections[0].direction;
    }

    // Callback when head nod animation completes
    private void OnHeadNodComplete()
    {
        currentHeadNodPhase = HeadNodPhase.Complete;
        isPerformingHeadNod = false;
        Debug.Log("Head nod complete");
    }

    // Public setters for runtime configuration
    public void SetPlaybackSpeed(float speed) => playbackSpeed = speed;
    public void SetLooping(bool loop) => loopTrajectory = loop;
    public void SetHeadNodSlowdown(float factor) => headNodSlowdownFactor = factor;
    public void SetTargetPedId(int pedId) => targetPedId = pedId;
}