using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TrajectoryTxtProcessor : MonoBehaviour
{
    [Header("File Settings")]
    [SerializeField] private string txtFilename = "trajectory.txt";
    [SerializeField] private bool loadOnStart = false;
    
    [Header("Transform Settings")]
    [SerializeField] private Vector2 desiredStartPosition = new Vector2(60f, 35f);
    [SerializeField] private bool applyTransform = true;
    
    [Header("Uniform Distance Settings")]
    [SerializeField] private float uniformPointDistance = 0.05f;
    [SerializeField] private bool applyUniformSampling = true;
    
    [Header("Visualization")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color trajectoryColor = Color.cyan;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Material lineMaterial;
    
    [Header("Head Nod Settings")]
    [SerializeField] private int targetPedId1 = 0;
    [SerializeField] private int targetPedId2 = 1;
    [SerializeField] private Color headNodPointColor = Color.yellow;
    [SerializeField] private Color headNodDirection1Color = Color.red;
    [SerializeField] private Color headNodDirection2Color = Color.blue;
    [SerializeField] private float headNodMarkerSize = 0.3f;
    [SerializeField] private float pointSelectionRadius = 0.5f;
    [SerializeField] private LayerMask floorLayerMask;
    
    [Header("UI")]
    [SerializeField] private KeyCode saveKey = KeyCode.S;
    [SerializeField] private KeyCode loadKey = KeyCode.L;
    [SerializeField] private Camera mainCamera;
    
    private List<Vector3> trajectoryPoints = new List<Vector3>();
    private Vector2 originalStartPosition;
    private bool trajectoryLoaded = false;
    
    // Head nod functionality - enhanced for two pedestrians
    private int selectedPointIndex = -1;
    private Vector3 selectedPointPosition;
    private bool waitingForDirectionPoint = false;
    private int currentPedIndex = 0; // 0 for first ped, 1 for second ped
    private GameObject headNodPointMarker;
    private GameObject headNodDirectionMarker1;
    private GameObject headNodDirectionMarker2;
    private LineRenderer headNodLineRenderer1;
    private LineRenderer headNodLineRenderer2;
    private LineRenderer previewLineRenderer;
    private Vector3 headNodDirection1;
    private Vector3 headNodDirection2;
    private bool hasHeadNodDirection1 = false;
    private bool hasHeadNodDirection2 = false;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found!");
                return;
            }
        }
        
        SetupLineRenderer();
        SetupHeadNodVisualization();
        
        if (loadOnStart)
        {
            LoadAndProcessTrajectory();
        }
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            GameObject lineObj = new GameObject("TrajectoryLine_TxtProcessor");
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

        // Create direction marker 1 (for first pedestrian)
        headNodDirectionMarker1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headNodDirectionMarker1.transform.localScale = Vector3.one * (headNodMarkerSize * 0.7f);
        headNodDirectionMarker1.GetComponent<Renderer>().material.color = headNodDirection1Color;
        Destroy(headNodDirectionMarker1.GetComponent<Collider>());
        headNodDirectionMarker1.SetActive(false);

        // Create direction marker 2 (for second pedestrian)
        headNodDirectionMarker2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        headNodDirectionMarker2.transform.localScale = Vector3.one * (headNodMarkerSize * 0.7f);
        headNodDirectionMarker2.GetComponent<Renderer>().material.color = headNodDirection2Color;
        Destroy(headNodDirectionMarker2.GetComponent<Collider>());
        headNodDirectionMarker2.SetActive(false);

        // Create line renderer for direction 1
        GameObject lineObj1 = new GameObject("HeadNodDirectionLine1_TxtProcessor");
        lineObj1.transform.parent = transform;
        headNodLineRenderer1 = lineObj1.AddComponent<LineRenderer>();
        headNodLineRenderer1.startWidth = lineWidth * 0.5f;
        headNodLineRenderer1.endWidth = lineWidth * 0.5f;
        headNodLineRenderer1.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        headNodLineRenderer1.startColor = headNodDirection1Color;
        headNodLineRenderer1.endColor = headNodDirection1Color;
        headNodLineRenderer1.positionCount = 0;
        headNodLineRenderer1.useWorldSpace = true;

        // Create line renderer for direction 2
        GameObject lineObj2 = new GameObject("HeadNodDirectionLine2_TxtProcessor");
        lineObj2.transform.parent = transform;
        headNodLineRenderer2 = lineObj2.AddComponent<LineRenderer>();
        headNodLineRenderer2.startWidth = lineWidth * 0.5f;
        headNodLineRenderer2.endWidth = lineWidth * 0.5f;
        headNodLineRenderer2.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        headNodLineRenderer2.startColor = headNodDirection2Color;
        headNodLineRenderer2.endColor = headNodDirection2Color;
        headNodLineRenderer2.positionCount = 0;
        headNodLineRenderer2.useWorldSpace = true;

        // Create preview line renderer (shows while waiting for second click)
        GameObject previewObj = new GameObject("HeadNodPreviewLine_TxtProcessor");
        previewObj.transform.parent = transform;
        previewLineRenderer = previewObj.AddComponent<LineRenderer>();
        previewLineRenderer.startWidth = lineWidth * 0.3f;
        previewLineRenderer.endWidth = lineWidth * 0.3f;
        previewLineRenderer.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        previewLineRenderer.startColor = new Color(1f, 1f, 1f, 0.5f); // White semi-transparent
        previewLineRenderer.endColor = new Color(1f, 1f, 1f, 0.5f);
        previewLineRenderer.positionCount = 0;
        previewLineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(loadKey))
        {
            LoadAndProcessTrajectory();
        }

        if (trajectoryLoaded)
        {
            if (Input.GetKeyDown(saveKey))
            {
                SaveTrajectory();
            }

            HandleHeadNodSelection();
            UpdatePreviewLine();
        }
    }

    private void HandleHeadNodSelection()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 worldPos;
            if (GetMouseWorldPosition(out worldPos))
            {
                if (!waitingForDirectionPoint)
                {
                    // First click - select point on trajectory
                    TrySelectTrajectoryPoint(worldPos);
                }
                else if (currentPedIndex == 0)
                {
                    // Second click - set direction for first pedestrian
                    SetHeadNodDirection1(worldPos);
                }
                else if (currentPedIndex == 1)
                {
                    // Third click - set direction for second pedestrian
                    SetHeadNodDirection2(worldPos);
                }
            }
        }
    }

    private void UpdatePreviewLine()
    {
        if (waitingForDirectionPoint && currentPedIndex < 2)
        {
            Vector3 worldPos;
            if (GetMouseWorldPosition(out worldPos))
            {
                previewLineRenderer.positionCount = 2;
                previewLineRenderer.SetPosition(0, selectedPointPosition + Vector3.up * 0.1f);
                previewLineRenderer.SetPosition(1, worldPos + Vector3.up * 0.1f);
                
                // Change preview color based on which ped we're setting
                if (currentPedIndex == 0)
                {
                    previewLineRenderer.startColor = new Color(headNodDirection1Color.r, headNodDirection1Color.g, headNodDirection1Color.b, 0.5f);
                    previewLineRenderer.endColor = new Color(headNodDirection1Color.r, headNodDirection1Color.g, headNodDirection1Color.b, 0.5f);
                }
                else
                {
                    previewLineRenderer.startColor = new Color(headNodDirection2Color.r, headNodDirection2Color.g, headNodDirection2Color.b, 0.5f);
                    previewLineRenderer.endColor = new Color(headNodDirection2Color.r, headNodDirection2Color.g, headNodDirection2Color.b, 0.5f);
                }
            }
        }
        else
        {
            previewLineRenderer.positionCount = 0;
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

    public void LoadAndProcessTrajectory()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, txtFilename);

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Trajectory file not found: {filePath}");
            return;
        }

        // Read raw points from txt file
        List<Vector3> rawPoints = ReadTxtFile(filePath);

        if (rawPoints.Count == 0)
        {
            Debug.LogError("No valid points read from file!");
            return;
        }

        Debug.Log($"Read {rawPoints.Count} points from {txtFilename}");

        // Store original start position
        originalStartPosition = new Vector2(rawPoints[0].x, rawPoints[0].z);
        Debug.Log($"Original start position: ({originalStartPosition.x:F2}, {originalStartPosition.y:F2})");

        // Apply position transform if enabled
        if (applyTransform)
        {
            rawPoints = TransformTrajectory(rawPoints);
            Debug.Log($"Transformed to new start position: ({desiredStartPosition.x:F2}, {desiredStartPosition.y:F2})");
        }

        // Apply uniform sampling if enabled
        if (applyUniformSampling)
        {
            trajectoryPoints = ApplyUniformSampling(rawPoints);
            Debug.Log($"Applied uniform sampling: {rawPoints.Count} points -> {trajectoryPoints.Count} points (spacing: {uniformPointDistance}m)");
        }
        else
        {
            trajectoryPoints = rawPoints;
        }

        // Visualize trajectory
        UpdateLineRenderer();
        trajectoryLoaded = true;

        Debug.Log($"Trajectory loaded and processed. Total points: {trajectoryPoints.Count}");
        Debug.Log($"Right-click on a trajectory point to set head nod directions for {targetPedId1} and {targetPedId2}.");
        Debug.Log($"Press '{saveKey}' to save trajectory to JSON.");
    }

    private List<Vector3> ReadTxtFile(string filePath)
    {
        List<Vector3> points = new List<Vector3>();

        try
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] values = line.Split(',');

                if (values.Length >= 2)
                {
                    if (float.TryParse(values[0].Trim(), out float x) &&
                        float.TryParse(values[1].Trim(), out float z))
                    {
                        // Create point with y = 0 (ground level)
                        points.Add(new Vector3(x, 0f, z));
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading file: {e.Message}");
        }

        return points;
    }

    private List<Vector3> TransformTrajectory(List<Vector3> points)
    {
        if (points.Count == 0)
            return points;

        List<Vector3> transformedPoints = new List<Vector3>();

        // Calculate offset
        Vector2 offset = desiredStartPosition + originalStartPosition;

        foreach (Vector3 point in points)
        {
            Vector3 transformed = new Vector3(
                -point.x + offset.x,
                point.y,
                -point.z + offset.y
            );
            transformedPoints.Add(transformed);
        }

        return transformedPoints;
    }

    private List<Vector3> ApplyUniformSampling(List<Vector3> points)
    {
        if (points.Count < 2)
            return points;

        List<Vector3> uniformPoints = new List<Vector3>();
        uniformPoints.Add(points[0]); // Always keep first point

        float accumulatedDistance = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 segmentStart = uniformPoints[uniformPoints.Count - 1]; // Last added point
            Vector3 segmentEnd = points[i];
            
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            accumulatedDistance += segmentLength;

            // If this segment is long enough, add interpolated points
            if (segmentLength >= uniformPointDistance)
            {
                Vector3 direction = (segmentEnd - segmentStart).normalized;
                int numPointsToAdd = Mathf.FloorToInt(segmentLength / uniformPointDistance);

                for (int j = 1; j <= numPointsToAdd; j++)
                {
                    Vector3 newPoint = segmentStart + direction * (uniformPointDistance * j);
                    uniformPoints.Add(newPoint);
                }
                
                // Reset accumulated distance with remainder
                accumulatedDistance = segmentLength - (numPointsToAdd * uniformPointDistance);
            }
            else if (accumulatedDistance >= uniformPointDistance)
            {
                // Accumulated enough distance across multiple small segments
                uniformPoints.Add(segmentEnd);
                accumulatedDistance = 0f;
            }
            // else: segment too short, skip this point and accumulate distance
        }

        // Always add the last point if it's not already close to the last added point
        Vector3 lastOriginalPoint = points[points.Count - 1];
        Vector3 lastAddedPoint = uniformPoints[uniformPoints.Count - 1];
        if (Vector3.Distance(lastAddedPoint, lastOriginalPoint) > uniformPointDistance * 0.3f)
        {
            uniformPoints.Add(lastOriginalPoint);
        }

        return uniformPoints;
    }

    private void TrySelectTrajectoryPoint(Vector3 clickPosition)
    {
        if (trajectoryPoints.Count == 0)
        {
            Debug.LogWarning("No trajectory loaded!");
            return;
        }

        // Find closest point within selection radius
        float closestDistance = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            float distance = Vector3.Distance(clickPosition, trajectoryPoints[i]);
            if (distance < closestDistance && distance <= pointSelectionRadius)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex != -1)
        {
            selectedPointIndex = closestIndex;
            selectedPointPosition = trajectoryPoints[closestIndex];
            waitingForDirectionPoint = true;
            currentPedIndex = 0;
            hasHeadNodDirection1 = false;
            hasHeadNodDirection2 = false;

            // Show marker at selected point
            headNodPointMarker.transform.position = selectedPointPosition + Vector3.up * 0.1f;
            headNodPointMarker.SetActive(true);

            // Clear previous direction markers
            headNodDirectionMarker1.SetActive(false);
            headNodDirectionMarker2.SetActive(false);
            headNodLineRenderer1.positionCount = 0;
            headNodLineRenderer2.positionCount = 0;

            Debug.Log($"Point {closestIndex} selected at position {selectedPointPosition}.");
            Debug.Log($"Right-click to set head nod direction for Ped {targetPedId1} (Red)");
        }
        else
        {
            Debug.LogWarning($"No trajectory point found within {pointSelectionRadius}m of click position. Try clicking closer to the path.");
        }
    }

    private void SetHeadNodDirection1(Vector3 directionPoint)
    {
        headNodDirection1 = (directionPoint - selectedPointPosition).normalized;
        hasHeadNodDirection1 = true;
        currentPedIndex = 1;

        // Show direction marker and line for first ped
        headNodDirectionMarker1.transform.position = directionPoint + Vector3.up * 0.1f;
        headNodDirectionMarker1.SetActive(true);

        headNodLineRenderer1.positionCount = 2;
        headNodLineRenderer1.SetPosition(0, selectedPointPosition + Vector3.up * 0.1f);
        headNodLineRenderer1.SetPosition(1, directionPoint + Vector3.up * 0.1f);

        Debug.Log($"Head nod direction 1 set for Ped {targetPedId1}: {headNodDirection1}");
        Debug.Log($"Right-click again to set head nod direction for Ped {targetPedId2} (Blue)");
    }

    private void SetHeadNodDirection2(Vector3 directionPoint)
    {
        headNodDirection2 = (directionPoint - selectedPointPosition).normalized;
        hasHeadNodDirection2 = true;
        waitingForDirectionPoint = false;
        currentPedIndex = 0;

        // Show direction marker and line for second ped
        headNodDirectionMarker2.transform.position = directionPoint + Vector3.up * 0.1f;
        headNodDirectionMarker2.SetActive(true);

        headNodLineRenderer2.positionCount = 2;
        headNodLineRenderer2.SetPosition(0, selectedPointPosition + Vector3.up * 0.1f);
        headNodLineRenderer2.SetPosition(1, directionPoint + Vector3.up * 0.1f);

        Debug.Log($"Head nod direction 2 set for Ped {targetPedId2}: {headNodDirection2}");
        Debug.Log($"Both head nod directions set! Press '{saveKey}' to save.");
    }

    private void UpdateLineRenderer()
    {
        lineRenderer.positionCount = trajectoryPoints.Count;

        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, trajectoryPoints[i]);
        }
    }

    private void SaveTrajectory()
    {
        if (trajectoryPoints.Count == 0)
        {
            Debug.LogWarning("No trajectory to save!");
            return;
        }

        List<TrajectoryPoint> points = new List<TrajectoryPoint>();
        for (int i = 0; i < trajectoryPoints.Count; i++)
        {
            TrajectoryPoint point = new TrajectoryPoint
            {
                position = new SerializableVector3(trajectoryPoints[i])
            };

            // Add head nod directions if this is the selected point
            if ((hasHeadNodDirection1 || hasHeadNodDirection2) && i == selectedPointIndex)
            {
                point.headNod = new List<HeadNodData>();

                if (hasHeadNodDirection1)
                {
                    point.headNod.Add(new HeadNodData
                    {
                        target_ped_id = targetPedId1,
                        direction = new SerializableVector3(headNodDirection1)
                    });
                }

                if (hasHeadNodDirection2)
                {
                    point.headNod.Add(new HeadNodData
                    {
                        target_ped_id = targetPedId2,
                        direction = new SerializableVector3(headNodDirection2)
                    });
                }
            }

            points.Add(point);
        }

        TrajectoryData trajectoryData = new TrajectoryData
        {
            points = points,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"),
            totalPoints = trajectoryPoints.Count
        };

        string directory = Path.Combine(Application.streamingAssetsPath, "Trajectories");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"Created directory: {directory}");
        }

        string filename = $"trajectory_controller_generated_{trajectoryData.timestamp}.json";
        string filePath = Path.Combine(directory, filename);

        JsonManager<TrajectoryData>.WriteJson(filePath, trajectoryData);

        Debug.Log($"Trajectory saved with {trajectoryPoints.Count} points to: {filePath}");
        if (hasHeadNodDirection1 || hasHeadNodDirection2)
        {
            Debug.Log($"Head nod directions saved at point {selectedPointIndex}:");
            if (hasHeadNodDirection1) Debug.Log($"  - Ped {targetPedId1}: {headNodDirection1}");
            if (hasHeadNodDirection2) Debug.Log($"  - Ped {targetPedId2}: {headNodDirection2}");
        }
        
        Debug.Log($"Original file: {txtFilename}");
        Debug.Log($"Transform applied: {applyTransform} (Start: {desiredStartPosition})");
        Debug.Log($"Uniform sampling: {applyUniformSampling} (Distance: {uniformPointDistance}m)");
    }

    // Public methods for runtime control
    public void SetDesiredStartPosition(float x, float z)
    {
        desiredStartPosition = new Vector2(x, z);
    }

    public void SetUniformDistance(float distance)
    {
        uniformPointDistance = distance;
    }

    public void SetTxtFilename(string filename)
    {
        txtFilename = filename;
    }

    public void SetTargetPedIds(int ped1, int ped2)
    {
        targetPedId1 = ped1;
        targetPedId2 = ped2;
    }
}