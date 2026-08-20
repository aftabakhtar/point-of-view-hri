using UnityEngine;

public class PedStudyPositions : MonoBehaviour
{
    public GameObject pedI;
    public GameObject pedJ;
    public GameObject pedK;

    // Population B, Size=3 values from Table 1
    public float angleLeft = 107.9f;   // α_ij for pedestrian i (left side)
    public float angleRight = 70.6f;   // α_jk for pedestrian k (right side)
    public float distanceLeft = 0.55f;  // d_ij
    public float distanceRight = 0.62f; // d_jk

    [SerializeField] private Vector3 pedIPosition;
    [SerializeField] private Vector3 pedJPosition;
    [SerializeField] private Vector3 pedKPosition;

    private Vector3 pedILastPosition;
    private Vector3 pedJLastPosition;
    private Vector3 pedKLastPosition;

    private float pedITotalSpeed = 0f;
    private int pedISpeedSamples = 0;

    private float pedJTotalSpeed = 0f;
    private int pedJSpeedSamples = 0;

    private float pedKTotalSpeed = 0f;
    private int pedKSpeedSamples = 0;

    public float pedIAverageSpeed;
    public float pedJAverageSpeed;
    public float pedKAverageSpeed;


    void Start()
    {
        pedIPosition = pedI.transform.localPosition;
        pedJPosition = pedJ.transform.localPosition;
        pedKPosition = pedK.transform.localPosition;

        // Initial positioning
        UpdatePedIPosition();
        UpdatePedKPosition();

        pedI.transform.localPosition = pedIPosition;
        pedJ.transform.localPosition = pedJPosition;
        pedK.transform.localPosition = pedKPosition;
    }

    void Update()
    {
        UpdateSpeed();

        pedILastPosition = pedI.transform.localPosition;
        pedJLastPosition = pedJ.transform.localPosition;
        pedKLastPosition = pedK.transform.localPosition;


        // UpdatePedIPosition();
        // UpdatePedKPosition();

        // pedI.transform.localPosition = pedIPosition;
        // pedJ.transform.localPosition = pedJPosition;
        // pedK.transform.localPosition = pedKPosition;
    }


    public void UpdatePedIPosition()
    {
        float theta = angleLeft * Mathf.Deg2Rad;
        float x = 0f;
        float z = 0f;

        Debug.Log("Ped J Y Rotation: " + pedJ.transform.localEulerAngles.y);

        if (pedJ.transform.localEulerAngles.y == 270f)
        {
            x = pedJ.transform.localPosition.x + (distanceLeft * Mathf.Cos(theta));
            z = pedJ.transform.localPosition.z - (distanceLeft * Mathf.Sin(theta));
        }
        else if (pedJ.transform.localEulerAngles.y == 90f)
        {
            x = pedJ.transform.localPosition.x - (distanceLeft * Mathf.Cos(theta));
            z = pedJ.transform.localPosition.z + (distanceLeft * Mathf.Sin(theta));
        }

        pedIPosition = new Vector3(x, pedIPosition.y, z);
    }

    public void UpdatePedKPosition()
    {
        float theta = angleRight * Mathf.Deg2Rad;
        float x = 0f;
        float z = 0f;

        if (pedJ.transform.localEulerAngles.y == 270f)
        {
            x = pedJ.transform.localPosition.x - (distanceRight * Mathf.Cos(theta));
            z = pedJ.transform.localPosition.z + (distanceRight * Mathf.Sin(theta));
        }
        else if (pedJ.transform.localEulerAngles.y == 90f)
        {
            x = pedJ.transform.localPosition.x + (distanceRight * Mathf.Cos(theta));
            z = pedJ.transform.localPosition.z - (distanceRight * Mathf.Sin(theta));
        }

        pedKPosition = new Vector3(x, pedKPosition.y, z);
    }

    public void UpdateSpeed()
    {
        float pedISpeed = Vector3.Distance(pedILastPosition, pedI.transform.localPosition) / Time.deltaTime;
        float pedJSpeed = Vector3.Distance(pedJLastPosition, pedJ.transform.localPosition) / Time.deltaTime;
        float pedKSpeed = Vector3.Distance(pedKLastPosition, pedK.transform.localPosition) / Time.deltaTime;

        pedITotalSpeed += pedISpeed;
        pedISpeedSamples++;
        pedIAverageSpeed = pedITotalSpeed / pedISpeedSamples;

        pedJTotalSpeed += pedJSpeed;
        pedJSpeedSamples++;
        pedJAverageSpeed = pedJTotalSpeed / pedJSpeedSamples;

        pedKTotalSpeed += pedKSpeed;
        pedKSpeedSamples++;
        pedKAverageSpeed = pedKTotalSpeed / pedKSpeedSamples;
    }
}