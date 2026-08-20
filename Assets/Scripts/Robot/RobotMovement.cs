using UnityEngine;

/// <summary>
/// Main controller for robot trajectory playback with visualization.
/// Orchestrates TrajectoryPlayer and TrajectoryVisualizer components.
/// </summary>
public class RobotMovement : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TrajectoryPlayer trajectoryPlayer;
    [SerializeField] private TrajectoryVisualizer trajectoryVisualizer;

    [Header("Default Trajectory")]
    [SerializeField] private string defaultTrajectoryFilename = "trajectory_2024-01-01_12-00-00.json";
    [SerializeField] private bool autoPlayOnStart = true;

    [Header("Input Controls")]
    [SerializeField] private KeyCode playTrajectoryKey = KeyCode.P;
    [SerializeField] private KeyCode stopTrajectoryKey = KeyCode.O;

    void Start()
    {
        // Get components if not assigned
        if (trajectoryPlayer == null)
            trajectoryPlayer = GetComponent<TrajectoryPlayer>();
        
        if (trajectoryVisualizer == null)
            trajectoryVisualizer = GetComponent<TrajectoryVisualizer>();

        // Validate
        if (trajectoryPlayer == null)
        {
            Debug.LogError("TrajectoryPlayer component not found!");
            return;
        }

        // Auto-play if requested
        if (autoPlayOnStart && !string.IsNullOrEmpty(defaultTrajectoryFilename))
        {
            PlayTrajectory(defaultTrajectoryFilename);
        }
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(playTrajectoryKey))
        {
            PlayTrajectory(defaultTrajectoryFilename);
        }

        if (Input.GetKeyDown(stopTrajectoryKey))
        {
            StopTrajectory();
        }
    }

    // Public API for programmatic control

    /// <summary>
    /// Load and play a trajectory from a file.
    /// </summary>
    public void PlayTrajectory(string filename, int? targetPedOverride = null)
    {
        if (trajectoryPlayer == null)
        {
            Debug.LogError("Cannot play trajectory: TrajectoryPlayer not found!");
            return;
        }

        trajectoryPlayer.LoadAndPlay(filename, targetPedOverride);
    }

    /// <summary>
    /// Stop the currently playing trajectory.
    /// </summary>
    public void StopTrajectory()
    {
        if (trajectoryPlayer != null)
        {
            trajectoryPlayer.Stop();
        }
    }

    /// <summary>
    /// Check if a trajectory is currently playing.
    /// </summary>
    public bool IsPlayingTrajectory()
    {
        return trajectoryPlayer != null && trajectoryPlayer.IsPlaying;
    }

    /// <summary>
    /// Get the current trajectory player instance.
    /// </summary>
    public TrajectoryPlayer GetTrajectoryPlayer()
    {
        return trajectoryPlayer;
    }

    /// <summary>
    /// Get the current trajectory visualizer instance.
    /// </summary>
    public TrajectoryVisualizer GetTrajectoryVisualizer()
    {
        return trajectoryVisualizer;
    }

    // Configuration methods

    public void SetPlaybackSpeed(float speed)
    {
        if (trajectoryPlayer != null)
            trajectoryPlayer.SetPlaybackSpeed(speed);
    }

    public void SetLooping(bool loop)
    {
        if (trajectoryPlayer != null)
            trajectoryPlayer.SetLooping(loop);
    }

    public void SetVisualizationEnabled(bool enabled)
    {
        if (trajectoryVisualizer != null)
            trajectoryVisualizer.SetVisualizationEnabled(enabled);
    }
}