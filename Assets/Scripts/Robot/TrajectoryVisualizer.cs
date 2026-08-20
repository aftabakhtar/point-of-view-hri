using System.Collections.Generic;
using UnityEngine;

public class TrajectoryVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [SerializeField] private bool showTrajectoryLine = true;
    [SerializeField] private Color pastTrajectoryColor = Color.green;
    [SerializeField] private Color futureTrajectoryColor = Color.blue;
    [SerializeField] private float trajectoryLineWidth = 0.05f;
    [SerializeField] private float dashLength = 0.2f;
    [SerializeField] private float gapLength = 0.1f;
    [SerializeField] private Material trajectoryLineMaterial;

    [Header("Dense Trajectory Handling")]
    [SerializeField] private float minPointDistance = 0.15f; // Minimum distance between sampled points
    [SerializeField] private bool decimateDensePoints = true; // Enable smart point sampling

    private TrajectoryPlayer trajectoryPlayer;
    private GameObject pastTrajectoryContainer;
    private GameObject futureTrajectoryContainer;
    private List<LineRenderer> pastLineRenderers = new List<LineRenderer>();
    private List<LineRenderer> futureLineRenderers = new List<LineRenderer>();
    private List<Vector3> pastTrajectoryPoints = new List<Vector3>();
    private float updateThreshold = 0.1f; // Distance before adding new past point

    void Start()
    {
        trajectoryPlayer = GetComponent<TrajectoryPlayer>();
        
        if (trajectoryPlayer == null)
        {
            Debug.LogError("TrajectoryVisualizer requires TrajectoryPlayer component!");
            enabled = false;
            return;
        }

        SetupVisualization();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SetupVisualization()
    {
        if (!showTrajectoryLine) return;

        pastTrajectoryContainer = new GameObject("PastTrajectoryLines");
        pastTrajectoryContainer.transform.parent = transform;

        futureTrajectoryContainer = new GameObject("FutureTrajectoryLines");
        futureTrajectoryContainer.transform.parent = transform;
    }

    private void SubscribeToEvents()
    {
        trajectoryPlayer.OnTrajectoryStarted += OnTrajectoryStarted;
        trajectoryPlayer.OnTrajectoryCompleted += OnTrajectoryCompleted;
        trajectoryPlayer.OnTrajectoryLooped += OnTrajectoryLooped;
        trajectoryPlayer.OnPositionUpdated += OnPositionUpdated;
    }

    private void UnsubscribeFromEvents()
    {
        if (trajectoryPlayer != null)
        {
            trajectoryPlayer.OnTrajectoryStarted -= OnTrajectoryStarted;
            trajectoryPlayer.OnTrajectoryCompleted -= OnTrajectoryCompleted;
            trajectoryPlayer.OnTrajectoryLooped -= OnTrajectoryLooped;
            trajectoryPlayer.OnPositionUpdated -= OnPositionUpdated;
        }
    }

    private void OnTrajectoryStarted()
    {
        pastTrajectoryPoints.Clear();
        pastTrajectoryPoints.Add(trajectoryPlayer.CurrentPosition);
        UpdateVisualization();
    }

    private void OnTrajectoryCompleted()
    {
        // Keep visualization on completion
    }

    private void OnTrajectoryLooped()
    {
        Clear();
    }

    private void OnPositionUpdated(Vector3 newPosition)
    {
        if (!showTrajectoryLine) return;

        // Add to past trajectory if robot moved enough
        if (pastTrajectoryPoints.Count > 0)
        {
            Vector3 lastPoint = pastTrajectoryPoints[pastTrajectoryPoints.Count - 1];
            if (Vector3.Distance(newPosition, lastPoint) > updateThreshold)
            {
                pastTrajectoryPoints.Add(newPosition);
                UpdateVisualization();
            }
        }
    }

    private void UpdateVisualization()
    {
        if (!showTrajectoryLine) return;
        UpdatePastTrajectory();
        UpdateFutureTrajectory();
    }

    private void UpdatePastTrajectory()
    {
        ClearLineRenderers(pastLineRenderers);

        if (pastTrajectoryPoints.Count < 2) return;

        CreateDottedLineRenderers(pastTrajectoryPoints, pastTrajectoryContainer, 
            pastTrajectoryColor, pastLineRenderers, false);
    }

    private void UpdateFutureTrajectory()
    {
        ClearLineRenderers(futureLineRenderers);

        if (trajectoryPlayer.TrajectoryData == null || trajectoryPlayer.TrajectoryData.Count == 0)
            return;

        List<Vector3> futurePoints = new List<Vector3>();
        futurePoints.Add(trajectoryPlayer.CurrentPosition);

        // Add remaining waypoints
        for (int i = trajectoryPlayer.CurrentPointIndex; i < trajectoryPlayer.TrajectoryData.Count; i++)
        {
            futurePoints.Add(trajectoryPlayer.TrajectoryData[i].position);
        }

        if (futurePoints.Count < 2) return;

        // Use decimation for future trajectory to handle dense points
        CreateDottedLineRenderers(futurePoints, futureTrajectoryContainer, 
            futureTrajectoryColor, futureLineRenderers, decimateDensePoints);
    }

    private void CreateDottedLineRenderers(List<Vector3> points, GameObject container, 
        Color color, List<LineRenderer> lineList, bool shouldDecimate)
    {
        if (points.Count < 2) return;

        // Optionally decimate dense points
        List<Vector3> processedPoints = shouldDecimate ? DecimatePoints(points) : points;

        float totalDashGap = dashLength + gapLength;
        int dashIndex = 0;

        for (int i = 0; i < processedPoints.Count - 1; i++)
        {
            Vector3 start = processedPoints[i];
            Vector3 end = processedPoints[i + 1];
            Vector3 direction = end - start;
            float segmentLength = direction.magnitude;
            Vector3 normalizedDirection = direction.normalized;

            float currentDistance = 0f;
            bool isDash = true;

            while (currentDistance < segmentLength)
            {
                if (isDash)
                {
                    Vector3 dashStart = start + normalizedDirection * currentDistance;
                    float remainingLength = segmentLength - currentDistance;
                    float actualDashLength = Mathf.Min(dashLength, remainingLength);
                    Vector3 dashEnd = dashStart + normalizedDirection * actualDashLength;

                    GameObject dashObj = new GameObject($"Dash_{dashIndex++}");
                    dashObj.transform.parent = container.transform;
                    LineRenderer lr = dashObj.AddComponent<LineRenderer>();

                    ConfigureLineRenderer(lr, color);
                    lr.SetPosition(0, dashStart + Vector3.up * 0.05f);
                    lr.SetPosition(1, dashEnd + Vector3.up * 0.05f);

                    lineList.Add(lr);

                    currentDistance += actualDashLength;
                }
                else
                {
                    currentDistance += gapLength;
                }

                isDash = !isDash;
            }
        }
    }

    private List<Vector3> DecimatePoints(List<Vector3> points)
    {
        if (points.Count < 2) return points;

        List<Vector3> decimated = new List<Vector3>();
        decimated.Add(points[0]); // Always keep first point

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 lastAdded = decimated[decimated.Count - 1];
            float distance = Vector3.Distance(points[i], lastAdded);

            // Only add point if it's far enough from the last added point
            if (distance >= minPointDistance || i == points.Count - 1)
            {
                decimated.Add(points[i]);
            }
        }

        return decimated;
    }

    private void ConfigureLineRenderer(LineRenderer lr, Color color)
    {
        lr.startWidth = trajectoryLineWidth;
        lr.endWidth = trajectoryLineWidth;
        lr.material = trajectoryLineMaterial != null ? 
            trajectoryLineMaterial : new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
        lr.numCapVertices = 2;
        lr.positionCount = 2;
    }

    private void ClearLineRenderers(List<LineRenderer> rendererList)
    {
        foreach (var lr in rendererList)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        rendererList.Clear();
    }

    public void Clear()
    {
        ClearLineRenderers(pastLineRenderers);
        ClearLineRenderers(futureLineRenderers);
        pastTrajectoryPoints.Clear();
    }

    public void SetVisualizationEnabled(bool enabled)
    {
        showTrajectoryLine = enabled;
        if (!enabled)
        {
            Clear();
        }
    }

    // Public configuration methods
    public void SetColors(Color pastColor, Color futureColor)
    {
        pastTrajectoryColor = pastColor;
        futureTrajectoryColor = futureColor;
        UpdateVisualization();
    }

    public void SetDashParameters(float dash, float gap)
    {
        dashLength = dash;
        gapLength = gap;
        UpdateVisualization();
    }

    public void SetMinPointDistance(float distance)
    {
        minPointDistance = distance;
        UpdateVisualization();
    }
}