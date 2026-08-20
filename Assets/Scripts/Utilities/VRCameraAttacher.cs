using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ObjectGroup
{
    public string groupName = "Group";
    public Transform parentGroup;
    public int selectedChildIndex = -1; // -1 means none selected
    [HideInInspector] public List<string> childNames = new List<string>();
    [HideInInspector] public List<Transform> children = new List<Transform>();
}

public class VRCameraAttacher : MonoBehaviour
{
    [Header("VR Camera")]
    [SerializeField] private GameObject cameraVR;

    [Header("Eye Position Offset")]
    [SerializeField] private float eyeHeightY = 1.6f;
    [SerializeField] private float eyeDistanceZ = 0.1f;

    [Header("Object Groups")]
    [SerializeField] private List<ObjectGroup> objectGroups = new List<ObjectGroup>();

    [Header("Selection")]
    [Tooltip("Select which group to use (-1 for none)")]
    [SerializeField] private int activeGroupIndex = -1;

    private Transform previousParent;
    private Vector3 previousLocalPosition;
    private Quaternion previousLocalRotation;
    private Transform currentAttachedChild;
    private int lastActiveGroup = -1;
    private int lastSelectedChild = -1;

    private void OnValidate()
    {
        if (cameraVR == null || objectGroups == null)
            return;

        // Update children lists for each group
        for (int i = 0; i < objectGroups.Count; i++)
        {
            var group = objectGroups[i];
            if (group.parentGroup != null)
            {
                UpdateGroupChildren(group);
            }
        }

        // Validate active group index
        if (activeGroupIndex < -1)
            activeGroupIndex = -1;
        if (activeGroupIndex >= objectGroups.Count)
            activeGroupIndex = objectGroups.Count - 1;

        // Check if selection changed
        bool selectionChanged = false;
        Transform newSelectedChild = null;

        if (activeGroupIndex >= 0 && activeGroupIndex < objectGroups.Count)
        {
            var activeGroup = objectGroups[activeGroupIndex];

            // Validate child index
            if (activeGroup.selectedChildIndex < -1)
                activeGroup.selectedChildIndex = -1;
            if (activeGroup.selectedChildIndex >= activeGroup.children.Count)
                activeGroup.selectedChildIndex = activeGroup.children.Count - 1;

            // Check if group or child selection changed
            if (activeGroupIndex != lastActiveGroup ||
                activeGroup.selectedChildIndex != lastSelectedChild)
            {
                selectionChanged = true;
                lastActiveGroup = activeGroupIndex;
                lastSelectedChild = activeGroup.selectedChildIndex;

                if (activeGroup.selectedChildIndex >= 0 &&
                    activeGroup.selectedChildIndex < activeGroup.children.Count)
                {
                    newSelectedChild = activeGroup.children[activeGroup.selectedChildIndex];
                }
            }
        }
        else if (activeGroupIndex == -1 && lastActiveGroup != -1)
        {
            // Deselected
            selectionChanged = true;
            lastActiveGroup = -1;
            lastSelectedChild = -1;
        }

        // Attach camera if selection changed
        if (selectionChanged)
        {
            if (newSelectedChild != null)
            {
                AttachCameraToChild(newSelectedChild);
            }
            else
            {
                // No selection - could optionally reset camera here
                currentAttachedChild = null;
            }
        }
    }

    private void UpdateGroupChildren(ObjectGroup group)
    {
        // Clear existing lists
        group.children.Clear();
        group.childNames.Clear();

        // Get all current children from the parent
        foreach (Transform child in group.parentGroup)
        {
            group.children.Add(child);
            group.childNames.Add(child.name);
        }

        // Validate selected index
        if (group.selectedChildIndex >= group.children.Count)
        {
            group.selectedChildIndex = -1;
        }
    }

    private void AttachCameraToChild(Transform newParent)
    {
        if (cameraVR == null || newParent == null)
            return;

        Transform cameraTransform = cameraVR.transform;

        // Store original state if this is the first attachment
        if (previousParent == null)
        {
            previousParent = cameraTransform.parent;
            previousLocalPosition = cameraTransform.localPosition;
            previousLocalRotation = cameraTransform.localRotation;
        }

        // Set the parent first
        cameraTransform.SetParent(newParent);

        // Calculate the local position with eye offset
        // The offset is relative to the parent's local space
        Vector3 localOffset = new Vector3(0, eyeHeightY, eyeDistanceZ);
        cameraTransform.localPosition = localOffset;

        // Reset local rotation to face forward relative to parent
        cameraTransform.localRotation = Quaternion.identity;

        currentAttachedChild = newParent;

        Debug.Log($"VR Camera attached to: {newParent.name} at local offset Y:{eyeHeightY}, Z:{eyeDistanceZ}");
    }

    public void ResetCamera()
    {
        if (cameraVR != null && previousParent != null)
        {
            Transform cameraTransform = cameraVR.transform;
            cameraTransform.SetParent(previousParent);
            cameraTransform.localPosition = previousLocalPosition;
            cameraTransform.localRotation = previousLocalRotation;

            currentAttachedChild = null;
            activeGroupIndex = -1;
            lastActiveGroup = -1;
            lastSelectedChild = -1;

            Debug.Log("VR Camera reset to original parent");
        }
    }

    // Helper method to attach via code
    public void AttachToChild(int groupIndex, int childIndex)
    {
        if (groupIndex < 0 || groupIndex >= objectGroups.Count)
            return;

        var group = objectGroups[groupIndex];
        if (childIndex < 0 || childIndex >= group.children.Count)
            return;

        activeGroupIndex = groupIndex;
        group.selectedChildIndex = childIndex;
        AttachCameraToChild(group.children[childIndex]);
    }

    public void AttachToChildByName(string groupName, string childName)
    {
        for (int i = 0; i < objectGroups.Count; i++)
        {
            var group = objectGroups[i];
            if (group.groupName == groupName || group.parentGroup.name == groupName)
            {
                for (int j = 0; j < group.children.Count; j++)
                {
                    if (group.children[j].name == childName)
                    {
                        AttachToChild(i, j);
                        return;
                    }
                }
            }
        }
    }
}