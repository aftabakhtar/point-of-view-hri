using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    public Action OnCountdownFinished;
    public Action OnStudyStartConfirmed;
    public Action OnStudyEndConfirmed;
    public Action OnTwoDimIntroConfirmed;
    [SerializeField] private GameObject countdownDialog;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject studyStartDialog;
    [SerializeField] private GameObject studyEndDialog;
    [SerializeField] private GameObject twoDimIntroDialog;
    [SerializeField] private int countdownDuration = 3;
    [SerializeField] private MenuPlacement menuPlacement;
    [SerializeField] private HapticsManager hapticsManager;
    [Header("Position Anchors")]
    [SerializeField] private Vector3 positionAnchor3D = new Vector3(58.49426f, 1.7f, 32.41912f);
    [SerializeField] private Vector3 positionAnchor2D = new Vector3(11.3057f, 1.7f, -207.2189f);
    private Coroutine countdownCoroutine;

    public void StartCountdown()
    {
        if (countdownDialog == null)
        {
            Debug.LogError("Countdown dialog GameObject is not assigned.");
            return;
        }

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    public IEnumerator CountdownRoutine()
    {
        Vector3 position = menuPlacement.GetMainMenuOptimalPosition();
        countdownDialog.transform.position = position;
        Vector3 lookOrientation = menuPlacement.CalculateLookTarget(countdownDialog);
        countdownDialog.transform.LookAt(lookOrientation);
        countdownDialog.transform.Rotate(0, 180f, 0);
        countdownDialog.SetActive(true);

        for (int i = countdownDuration; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        countdownDialog.SetActive(false);
        OnCountdownFinished?.Invoke();
    }

    public void StartStudyConfirmation()
    {
        studyStartDialog.SetActive(false);
        if (hapticsManager != null)
        {
            hapticsManager.TriggerHapticsOnBothControllers();
        }
        OnStudyStartConfirmed?.Invoke();
    }

    public void EndStudyConfirmation()
    {
        studyEndDialog.SetActive(false);
        if (hapticsManager != null)
        {
            hapticsManager.TriggerHapticsOnBothControllers();
        }
        OnStudyEndConfirmed?.Invoke();
    }

    public void ShowStartDialog()
    {
        if (menuPlacement == null)
        {
            Debug.LogError("MenuPlacement reference is missing in DialogManager.");
            return;
        }
        Vector3 position = menuPlacement.GetMainMenuOptimalPosition();
        studyStartDialog.transform.position = position;
        Vector3 lookOrientation = menuPlacement.CalculateLookTarget(studyStartDialog);
        studyStartDialog.transform.LookAt(lookOrientation);
        studyStartDialog.transform.Rotate(0, 180f, 0);
        studyStartDialog.SetActive(true);
    }

    public void ShowEndDialog(bool use3DPosition)
    {
        if (menuPlacement == null)
        {
            Debug.LogError("MenuPlacement reference is missing in DialogManager.");
            return;
        }
        if (use3DPosition)
        {
            studyEndDialog.transform.position = positionAnchor3D;
        }
        else
        {
            studyEndDialog.transform.position = positionAnchor2D;
        }
        Vector3 lookOrientation = menuPlacement.CalculateLookTarget(studyEndDialog);
        studyEndDialog.transform.LookAt(lookOrientation);
        studyEndDialog.transform.Rotate(0, 180f, 0);
        studyEndDialog.SetActive(true);
    }

    public void ShowTwoDimIntroDialog(bool use2DPosition)
    {
        if (menuPlacement == null)
        {
            Debug.LogError("MenuPlacement reference is missing in DialogManager.");
            return;
        }
        if (use2DPosition)
        {
            twoDimIntroDialog.transform.position = positionAnchor2D;
        }
        else
        {
            Vector3 position = menuPlacement.GetMainMenuOptimalPosition();
            twoDimIntroDialog.transform.position = position;
        }

        Vector3 lookOrientation = menuPlacement.CalculateLookTarget(twoDimIntroDialog);
        twoDimIntroDialog.transform.LookAt(lookOrientation);
        twoDimIntroDialog.transform.Rotate(0, 180f, 0);
        twoDimIntroDialog.SetActive(true);
    }

    public void TwoDimIntroConfirmation()
    {
        twoDimIntroDialog.SetActive(false);
        if (hapticsManager != null)
        {
            hapticsManager.TriggerHapticsOnBothControllers();
        }
        OnTwoDimIntroConfirmed?.Invoke();
    }

}