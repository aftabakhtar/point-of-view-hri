using UnityEngine;

public class HapticsManager : MonoBehaviour
{
    [Header("Haptics controller - for vibrations")]
    [SerializeField] private HapticsController leftControllerHapticsManager;
    [SerializeField] private HapticsController rightControllerHapticsManager;

    // Controllers are absent when the scene runs without a headset (editor
    // preview, desktop mode), so every entry point tolerates a null reference.

    public void TriggerHapticsOnBothControllers()
    {
        PlayHpaticsOnLeftController(1.0f, 0.1f);
        PlayHpaticsOnRightController(1.0f, 0.1f);
    }

    public void PlayHpaticsOnLeftController(float strength, float duration)
    {
        if (leftControllerHapticsManager == null) return;
        leftControllerHapticsManager.SendHapticPulse(strength, duration);
    }


    public void PlayHpaticsOnRightController(float strength, float duration)
    {
        if (rightControllerHapticsManager == null) return;
        rightControllerHapticsManager.SendHapticPulse(strength, duration);
    }
}