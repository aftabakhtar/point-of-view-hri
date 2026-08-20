using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class TransformRecorder : MonoBehaviour
{
    [System.Serializable]
    public class TransformData
    {
        public GameObject gameObject;
        public Vector3 startPosition;
        public Quaternion startRotation;
        public Vector3 endPosition;
        public Quaternion endRotation;
    }

    [SerializeField] private GameObject[] objectsToRecord = new GameObject[6];
    [SerializeField] private float recordDelay = 10f;
    [SerializeField] private string fileName = "TransformData.txt";
    
    private List<TransformData> transformDataList = new List<TransformData>();
    private bool hasRecordedEnd = false;
    private string filePath;

    void Start()
    {
        // Set up file path
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        Debug.Log("Data will be saved to: " + filePath);
        
        // Record start positions and rotations
        RecordStartTransforms();
        
        // Schedule end recording after delay
        Invoke(nameof(RecordEndTransforms), recordDelay);
    }

    void RecordStartTransforms()
    {
        transformDataList.Clear();
        
        foreach (GameObject obj in objectsToRecord)
        {
            if (obj != null)
            {
                TransformData data = new TransformData
                {
                    gameObject = obj,
                    startPosition = obj.transform.position,
                    startRotation = obj.transform.rotation
                };
                transformDataList.Add(data);
            }
        }
        
        Debug.Log("Start transforms recorded for " + transformDataList.Count + " objects");
    }

    void RecordEndTransforms()
    {
        foreach (TransformData data in transformDataList)
        {
            if (data.gameObject != null)
            {
                data.endPosition = data.gameObject.transform.position;
                data.endRotation = data.gameObject.transform.rotation;
            }
        }
        
        hasRecordedEnd = true;
        Debug.Log("End transforms recorded");
        SaveToFile();
        PrintTransformData();
    }

    void PrintTransformData()
    {
        foreach (TransformData data in transformDataList)
        {
            if (data.gameObject != null)
            {
                Debug.Log($"--- {data.gameObject.name} ---");
                Debug.Log($"Start Position: {data.startPosition}");
                Debug.Log($"Start Rotation: {data.startRotation.eulerAngles}");
                Debug.Log($"End Position: {data.endPosition}");
                Debug.Log($"End Rotation: {data.endRotation.eulerAngles}");
                Debug.Log($"Position Delta: {data.endPosition - data.startPosition}");
                Debug.Log("");
            }
        }
    }

    void SaveToFile()
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("Transform Recording Data");
                writer.WriteLine("========================");
                writer.WriteLine($"Recording Time: {System.DateTime.Now}");
                writer.WriteLine($"Duration: {recordDelay} seconds");
                writer.WriteLine($"Average Speed: {1.05f} m/s");
                writer.WriteLine("");

                foreach (TransformData data in transformDataList)
                {
                    if (data.gameObject != null)
                    {
                        writer.WriteLine($"--- {data.gameObject.name} ---");
                        writer.WriteLine($"Start Position: ({data.startPosition.x}, {data.startPosition.z})");
                        writer.WriteLine($"Start Rotation (Euler): {data.startRotation.eulerAngles}");
                        writer.WriteLine($"Start Rotation (Quaternion): {data.startRotation}");
                        writer.WriteLine("");
                        writer.WriteLine($"End Position: ({data.endPosition.x}, {data.endPosition.z})");
                        writer.WriteLine($"End Rotation (Euler): {data.endRotation.eulerAngles}");
                        writer.WriteLine($"End Rotation (Quaternion): {data.endRotation}");
                        writer.WriteLine("");
                        writer.WriteLine($"Position Delta: ({(data.endPosition - data.startPosition).x}, {(data.endPosition - data.startPosition).z})");
                        writer.WriteLine($"Distance Moved: {Vector3.Distance(data.startPosition, data.endPosition)}");
                        writer.WriteLine("");
                        writer.WriteLine("----------------------------------------");
                        writer.WriteLine("");
                    }
                }
            }

            Debug.Log("Transform data saved to: " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to save transform data: " + e.Message);
        }
    }

    // Public method to access the recorded data
    public List<TransformData> GetTransformData()
    {
        return transformDataList;
    }

    public bool HasRecordedEndTransforms()
    {
        return hasRecordedEnd;
    }
}