using UnityEngine;

public class HapticsController : MonoBehaviour
{
    public OVRInput.Controller controllerType;

    /// <summary>
    /// Sends a single haptic pulse.
    /// </summary>
    /// <param name="strength">Intensity (0.0 to 1.0).</param>
    /// <param name="duration">Duration in seconds.</param>
    public void SendHapticPulse(float strength, float duration)
    {
        StartCoroutine(PlayHapticPulse(strength, duration));
    }

    private System.Collections.IEnumerator PlayHapticPulse(float strength, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            OVRInput.SetControllerVibration(0.5f, strength, controllerType);
            elapsed += Time.deltaTime;
            yield return null;
        }
        OVRInput.SetControllerVibration(0f, 0f, controllerType); 
    }

    /// <summary>
    /// Plays a custom pattern.
    /// </summary>
    /// <param name="pattern">Array of float values alternating strength and duration.</param>
    public void PlayHapticPattern(float[] pattern)
    {
        StartCoroutine(ExecutePattern(pattern));
    }

    private System.Collections.IEnumerator ExecutePattern(float[] pattern)
    {
        for (int i = 0; i < pattern.Length; i += 2)
        {
            float strength = pattern[i];
            float duration = pattern[i + 1];
            OVRInput.SetControllerVibration(0.5f, strength, controllerType); 
            yield return new WaitForSeconds(duration);
        }
        OVRInput.SetControllerVibration(0f, 0f, controllerType); 
    }
}
