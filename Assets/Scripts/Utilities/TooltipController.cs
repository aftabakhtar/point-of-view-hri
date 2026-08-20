using TMPro;
using UnityEngine;

public class TooltipController : MonoBehaviour
{
    [SerializeField] private GameObject tooltipObject;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private GameObject baseLink;
    [SerializeField] private MenuPlacement menuPlacement;


    // Update is called once per frame
    void Update()
    {
        if (tooltipObject.activeSelf)
        {
            Vector3 position = new Vector3(baseLink.transform.position.x, 1.3f, baseLink.transform.position.z);
            tooltipObject.transform.position = position;
            // Keep the tooltip facing the user
            Vector3 lookOrientation = menuPlacement.CalculateLookTarget(tooltipObject);
            tooltipObject.transform.LookAt(lookOrientation);
            tooltipObject.transform.Rotate(0, 180f, 0);
        }
    }

    public void ShowTooltip(string message)
    {
        // Position the tooltip in front of the user
        Vector3 position = new Vector3(baseLink.transform.position.x, 1.3f, baseLink.transform.position.z);
        tooltipObject.transform.position = position;
        Vector3 lookOrientation = menuPlacement.CalculateLookTarget(tooltipObject);
        tooltipObject.transform.LookAt(lookOrientation);
        tooltipObject.transform.Rotate(0, 180f, 0);

        // Set the message text
        if (tooltipText != null)
        {
            tooltipText.text = message;
        }

        // Activate the tooltip
        tooltipObject.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipObject.SetActive(false);
    }
}
