using System.Collections.Generic;
using UnityEngine;

public class PedAnimationController : MonoBehaviour
{
    [SerializeField] private List<GameObject> pedestrianObjects;
    [SerializeField] private KeyCode startAnimationKey = KeyCode.T;
    [SerializeField] private KeyCode stopAnimationKey = KeyCode.Y;

    void Update()
    {
        if (Input.GetKeyDown(startAnimationKey))
        {
            StartPedestrianAnimations();
        }

        if (Input.GetKeyDown(stopAnimationKey))
        {
            StopPedestrianAnimations();
        }
    }

    private void StartPedestrianAnimations()
    {
        // Start animation for all pedestrian objects
        foreach (var pedObject in pedestrianObjects)
        {
            var animator = pedObject.GetComponent<AttributeAnimator>();
            if (animator != null)
            {
                animator.RandomizeAndStart();
            }
        }
    }

    private void StopPedestrianAnimations()
    {
        // Stop animation for all pedestrian objects
        foreach (var pedObject in pedestrianObjects)
        {
            var animator = pedObject.GetComponent<AttributeAnimator>();
            if (animator != null)
            {
                animator.ResetToIdleAndStop();
            }
        }
    }
}