using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class TrajectoryGenerator : MonoBehaviour
{
    [Header("Recording Settings")]
    [SerializeField] private float uniformPointDistance = 0.05f; // Fixed distance between points
    [SerializeField] private LayerMask floorLayerMask;
    
    [Header("Smoothing Settings")]
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField] private int smoothingIterations = 2;
    [SerializeField] [Range(0f, 1f)] private float smoothingStrength = 0.5f;
    
    [Header("Visualization")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color trajectoryColor = Color.green;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    
    [Header("Head Nod Settings")]
    [SerializeField] private GameObject headNodMarkerPrefab;
    [SerializeField] private Color headNodPointColor = Color.yellow;
    [SerializeField] private Color headNodDirectionColor = Color.red;
    [SerializeField] private float headNodMarkerSize = 0.3f;
    [SerializeField] private float pointSelectionRadius = 0.5f;
    
    [Header("UI")]
    [SerializeField] private KeyCode saveKey = KeyCode.S;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
    
    private List<Vector3> currentTrajectoryPoints = new List<Vector3>();
    private List<List<Vector3>> allTrajectorySegments = new List<List<Vector3>>();
    private bool isRecording = false;
    [SerializeField] private Camera mainCamera;
    private Vector3 lastRecordedPosition;
    private bool hasUnsavedTrajectory = false;
    
    // Accumulated distance tracking for uniform sampling
    private Vector3 lastMousePosition;
    private float accumulatedDistance = 0f;
    
    // Head nod functionality
    private int selectedPointIndex = -1;
    private Vector3 selectedPointPosition;
    private bool waitingForDirectionPoint = false;
    private GameObject headNodPointMarker;
    private GameObject headNodDirectionMarker;
    private LineRenderer headNodLineRenderer;
    private Vector3 headNodDirection;
    private bool hasHeadNodDirection = false;

    private void Start()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not assigned in TrajectoryGenerator.");
            return;
        }
        SetupLineRenderer();
        SetupHeadNodVisualization();
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            GameObject lineObj = new GameObject("TrajectoryLine");
            lineObj.transform.parent = transform;
            lineRenderer = lineObj.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = trajectoryColor;
        lineRenderer.endColor = trajectoryColor;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
    }

    private void SetupHeadNodVisualization()
    {
        // Create point marker
        headNodPointMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headNodPointMarker.transform.localScale = Vector3.one * headNodMarkerSize;
        headNodPointMarker.GetComponent<Renderer>().material.color = headNodPointColor;
        Destroy(headNodPointMarker.GetComponent<Collider>());
        headNodPointMarker.SetActive(false);

        // Create direction marker
        headNodDirectionMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headNodDirectionMarker.transform.localScale = Vector3.one * (headNodMarkerSize * 0.7f);
        headNodDirectionMarker.GetComponent<Renderer>().material.color = headNodDirectionColor;
        Destroy(headNodDirectionMarker.GetComponent<Collider>());
        headNodDirectionMarker.SetActive(false);

        // Create line renderer for direction
        GameObject lineObj = new GameObject("HeadNodDirectionLine");
        lineObj.transform.parent = transform;
        headNodLineRenderer = lineObj.AddComponent<LineRenderer>();
        headNodLineRenderer.startWidth = lineWidth * 0.5f;
        headNodLineRenderer.endWidth = lineWidth * 0.5f;
        headNodLineRenderer.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        headNodLineRenderer.startColor = headNodDirectionColor;
        headNodLineRenderer.endColor = headNodDirectionColor;
        headNodLineRenderer.positionCount = 0;
        headNodLineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
    }

    private void HandleMouseInput()
    {
        // Left mouse button - trajectory drawing
        if (Input.GetMouseButtonDown(0) && !waitingForDirectionPoint)
        {
            Vector3 worldPos;
            if (GetMouseWorldPosition(out worldPos))
            {
                if (allTrajectorySegments.Count > 0)
                {
                    List<Vector3> lastSegment = allTrajectorySegments[allTrajectorySegments.Count - 1];
                    if (lastSegment.Count > 0)
                    {
                        Vector3 lastPoint = lastSegment[lastSegment.Count - 1];
                        float distanceFromLastPoint = Vector3.Distance(worldPos, lastPoint);
                        
                        if (distanceFromLastPoint > 0.5f)
                        {
                            Debug.LogWarning("Start your next trajectory segment from the end of the previous path!");
                            return;
                        }
                    }
                }

                StartRecording(worldPos);
            }
        }

        if (Input.GetMouseButton(0) && isRecording)
        {
            Vector3 worldPos;
            if (GetMouseWorldPosition(out worldPos))
            {
                RecordPoint(worldPos);
            }
        }

        if (Input.GetMouseButtonUp(0) && isRecording)
        {
            StopRecording();
        }

        // Right mouse button - head nod selection
        if (Input.GetMouseButtonDown(1) && hasUnsavedTrajectory && !isRecording)
        {
            Vector3 worldPos;
            if (GetMouseWorldPosition(out worldPos))
            {
                if (!waitingForDirectionPoint)
                {
                    // First click - select point on trajectory
                    TrySelectTrajectoryPoint(worldPos);
                }
                else
                {
                    // Second click - set direction
                    SetHeadNodDirection(worldPos);
                }
            }
        }
    }

    private void HandleKeyboardInput()
    {
        if (hasUnsavedTrajectory)
        {
            if (Input.GetKeyDown(saveKey))
            {
                SaveTrajectory();
            }
            else if (Input.GetKeyDown(cancelKey))
            {
                CancelTrajectory();
            }
        }
    }

    private bool GetMouseWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayerMask))
        {
            worldPosition = hit.point;
            return true;
        }

        return false;
    }

    private void StartRecording(Vector3 startPosition)
    {
        isRecording = true;
        currentTrajectoryPoints = new List<Vector3>();
        currentTrajectoryPoints.Add(startPosition);
        lastRecordedPosition = startPosition;
        lastMousePosition = startPosition;
        accumulatedDistance = 0f;
        
        Debug.Log("Started recording trajectory segment with uniform distance sampling");
    }

    private void RecordPoint(Vector3 currentMousePosition)
    {
        // Calculate distance moved since last mouse position
        float distanceMoved = Vector3.Distance(currentMousePosition, lastMousePosition);
        
        if (distanceMoved < 0.001f)
        {
            // Mouse hasn't moved significantly, skip
            return;
        }
        
        // Add to accumulated distance
        accumulatedDistance += distanceMoved;
        
        // Check if we need to add one or more points
        while (accumulatedDistance >= uniformPointDistance)
        {
            // Calculate the direction of movement
            Vector3 direction = (currentMousePosition - lastRecordedPosition).normalized;
            
            // Place a new point exactly uniformPointDistance away from the last recorded point
            Vector3 newPoint = lastRecordedPosition + direction * uniformPointDistance;
            
            currentTrajectoryPoints.Add(newPoint);
            lastRecordedPosition = newPoint;
            accumulatedDistance -= uniformPointDistance;
            
            UpdateLineRenderer();
        }
        
        lastMousePosition = currentMousePosition;
    }

    private void StopRecording()
    {
        if (currentTrajectoryPoints.Count > 1)
        {
            // Apply smoothing before adding to segments
            if (enableSmoothing)
            {
                currentTrajectoryPoints = SmoothTrajectory(currentTrajectoryPoints);
            }
            
            allTrajectorySegments.Add(new List<Vector3>(currentTrajectoryPoints));
            hasUnsavedTrajectory = true;
            
            // Clear current trajectory points to prevent visual artifacts
            currentTrajectoryPoints.Clear();
            UpdateLineRenderer();
            
            Debug.Log($"Stopped recording. Segment has {allTrajectorySegments[allTrajectorySegments.Count - 1].Count} points (uniform spacing: {uniformPointDistance}m). Press '{saveKey}' to save or '{cancelKey}' to cancel.");
            Debug.Log("Right-click on a trajectory point to set head nod direction.");
        }
        else
        {
            Debug.LogWarning("Trajectory too short, not saved as segment.");
        }

        isRecording = false;
        accumulatedDistance = 0f;
    }

    private List<Vector3> SmoothTrajectory(List<Vector3> points)
    {
        if (points.Count < 3) return points;

        List<Vector3> smoothed = new List<Vector3>(points);

        for (int iteration = 0; iteration < smoothingIterations; iteration++)
        {
            List<Vector3> temp = new List<Vector3>(smoothed);
            
            // Keep first and last points fixed
            for (int i = 1; i < temp.Count - 1; i++)
            {
                Vector3 prev = temp[i - 1];
                Vector3 current = temp[i];
                Vector3 next = temp[i + 1];
                
                // Average with neighbors
                Vector3 averaged = (prev + current + next) / 3f;
                
                // Blend between original and averaged based on smoothing strength
                smoothed[i] = Vector3.Lerp(current, averaged, smoothingStrength);
            }
        }

        return smoothed;
    }

    private void TrySelectTrajectoryPoint(Vector3 clickPosition)
    {
        // Get all trajectory points
        List<Vector3> allPoints = new List<Vector3>();
        foreach (var segment in allTrajectorySegments)
        {
            allPoints.AddRange(segment);
        }

        // Find closest point within selection radius
        float closestDistance = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < allPoints.Count; i++)
        {
            float distance = Vector3.Distance(clickPosition, allPoints[i]);
            if (distance < closestDistance && distance <= pointSelectionRadius)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex != -1)
        {
            selectedPointIndex = closestIndex;
            selectedPointPosition = allPoints[closestIndex];
            waitingForDirectionPoint = true;
            
            // Show marker at selected point
            headNodPointMarker.transform.position = selectedPointPosition + Vector3.up * 0.1f;
            headNodPointMarker.SetActive(true);
            
            Debug.Log($"Point {closestIndex} selected. Right-click again to set head nod direction.");
        }
        else
        {
            Debug.LogWarning("No trajectory point found near click position. Try clicking closer to the path.");
        }
    }

    private void SetHeadNodDirection(Vector3 directionPoint)
    {
        headNodDirection = (directionPoint - selectedPointPosition).normalized;
        hasHeadNodDirection = true;
        waitingForDirectionPoint = false;

        // Show direction marker and line
        headNodDirectionMarker.transform.position = directionPoint + Vector3.up * 0.1f;
        headNodDirectionMarker.SetActive(true);

        headNodLineRenderer.positionCount = 2;
        headNodLineRenderer.SetPosition(0, selectedPointPosition + Vector3.up * 0.1f);
        headNodLineRenderer.SetPosition(1, directionPoint + Vector3.up * 0.1f);

        Debug.Log($"Head nod direction set for point {selectedPointIndex}: {headNodDirection}");
    }

    private void UpdateLineRenderer()
    {
        int totalPoints = 0;
        foreach (var segment in allTrajectorySegments)
        {
            totalPoints += segment.Count;
        }
        totalPoints += currentTrajectoryPoints.Count;

        lineRenderer.positionCount = totalPoints;

        int index = 0;
        foreach (var segment in allTrajectorySegments)
        {
            foreach (var point in segment)
            {
                lineRenderer.SetPosition(index++, point);
            }
        }

        foreach (var point in currentTrajectoryPoints)
        {
            lineRenderer.SetPosition(index++, point);
        }
    }

    private void SaveTrajectory()
    {
        if (allTrajectorySegments.Count == 0)
        {
            Debug.LogWarning("No trajectory to save!");
            return;
        }

        List<Vector3> completeTrajectory = new List<Vector3>();
        foreach (var segment in allTrajectorySegments)
        {
            completeTrajectory.AddRange(segment);
        }

        List<TrajectoryPoint> trajectoryPoints = new List<TrajectoryPoint>();
        for (int i = 0; i < completeTrajectory.Count; i++)
        {
            TrajectoryPoint point = new TrajectoryPoint
            {
                position = new SerializableVector3(completeTrajectory[i])
            };

            // Add head nod direction if this is the selected point (kept as single for backward compatibility)
            if (hasHeadNodDirection && i == selectedPointIndex)
            {
                point.headNod = new List<HeadNodData>
                {
                    new HeadNodData
                    {
                        target_ped_id = 0,
                        direction = new SerializableVector3(headNodDirection)
                    }
                };
            }

            trajectoryPoints.Add(point);
        }

        TrajectoryData trajectoryData = new TrajectoryData
        {
            points = trajectoryPoints,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            totalPoints = completeTrajectory.Count
        };

        string directory = Path.Combine(Application.streamingAssetsPath, "Trajectories");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"Created directory: {directory}");
        }

        string filename = $"trajectory_{trajectoryData.timestamp}.json";
        string filePath = Path.Combine(directory, filename);

        JsonManager<TrajectoryData>.WriteJson(filePath, trajectoryData);

        Debug.Log($"Trajectory saved with {completeTrajectory.Count} points (uniform spacing: {uniformPointDistance}m) to: {filePath}");
        if (hasHeadNodDirection)
        {
            Debug.Log($"Head nod direction saved at point {selectedPointIndex}");
        }
        
        ClearTrajectory();
    }

    private void CancelTrajectory()
    {
        Debug.Log("Trajectory cancelled");
        ClearTrajectory();
    }

    private void ClearTrajectory()
    {
        allTrajectorySegments.Clear();
        currentTrajectoryPoints.Clear();
        lineRenderer.positionCount = 0;
        hasUnsavedTrajectory = false;
        accumulatedDistance = 0f;
        
        // Clear head nod data
        selectedPointIndex = -1;
        waitingForDirectionPoint = false;
        hasHeadNodDirection = false;
        headNodPointMarker.SetActive(false);
        headNodDirectionMarker.SetActive(false);
        headNodLineRenderer.positionCount = 0;
    }

    public void LoadTrajectory(string filename)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Trajectories", filename);
        TrajectoryData data = JsonManager<TrajectoryData>.ReadJson(filePath);

        if (data != null && data.points != null)
        {
            ClearTrajectory();
            
            List<Vector3> loadedPoints = new List<Vector3>();
            foreach (var point in data.points)
            {
                loadedPoints.Add(point.position.ToVector3());
                
                // Visualize head nod if present
                if (point.headNod != null && point.headNod.Count > 0)
                {
                    Vector3 pos = point.position.ToVector3();
                    Vector3 dir = point.headNod[0].direction.ToVector3();
                    
                    headNodPointMarker.transform.position = pos + Vector3.up * 0.1f;
                    headNodPointMarker.SetActive(true);
                    
                    headNodDirectionMarker.transform.position = pos + dir + Vector3.up * 0.1f;
                    headNodDirectionMarker.SetActive(true);
                    
                    headNodLineRenderer.positionCount = 2;
                    headNodLineRenderer.SetPosition(0, pos + Vector3.up * 0.1f);
                    headNodLineRenderer.SetPosition(1, pos + dir + Vector3.up * 0.1f);
                }
            }
            
            allTrajectorySegments.Add(loadedPoints);
            UpdateLineRenderer();
            Debug.Log($"Loaded trajectory with {data.points.Count} points");
        }
    }
}

[System.Serializable]
public class TrajectoryData
{
    public List<TrajectoryPoint> points;
    public string timestamp;
    public int totalPoints;
}

[System.Serializable]
public class TrajectoryPoint
{
    public SerializableVector3 position;
    public List<HeadNodData> headNod;
}

[System.Serializable]
public class HeadNodData
{
    public int target_ped_id;
    public SerializableVector3 direction;
}

[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}