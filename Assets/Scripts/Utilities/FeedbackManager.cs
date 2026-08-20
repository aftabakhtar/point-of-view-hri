using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class FeedbackManager : MonoBehaviour
{
    public Action OnQuestionsCompleted;
    
    [Header("UI Prefabs")]
    [SerializeField] private GameObject startingQuestion;
    [SerializeField] private GameObject inbetweenQuestion;
    [SerializeField] private GameObject endingQuestion;
    [SerializeField] private GameObject singleQuestion;
    [SerializeField] private Transform parentContainer;
    [SerializeField] private MenuPlacement menuPlacement;

    [Header("Haptic Manager")]
    [SerializeField] private HapticsManager hapticsManager;

    [Header("Position Anchors")]
    [SerializeField] private Vector3 positionAnchor3D = new Vector3(58.49426f, 1.7f, 32.41912f);
    [SerializeField] private Vector3 positionAnchor2D = new Vector3(11.3057f, 1.7f, -207.2189f);

    private Questionnaire questionnaire;
    private List<GameObject> activeQuestions = new List<GameObject>();
    private int currentQuestionIndex = 0;
    private Dictionary<int, int> feedbackResponses = new Dictionary<int, int>();
    
    // Current trial info
    private string currentParticipantId;
    private TrialConfig currentTrial;
    private float feedbackStartTime = 0f;

    public void LoadQuestionnaire(string questionnairePath)
    {
        questionnaire = JsonManager<Questionnaire>.ReadJson(questionnairePath);

        if (questionnaire == null)
        {
            Debug.LogError($"Failed to load questionnaire from: {questionnairePath}");
        }
        else
        {
            Debug.Log($"Loaded questionnaire: {questionnaire.study_title} with {questionnaire.questions.Count} questions");
        }
    }

    public void ShowQuestionnaire(string participantId, TrialConfig trial, bool is3Dtrial)
    {
        if (questionnaire == null)
        {
            Debug.LogError("Questionnaire not loaded! Call LoadQuestionnaire first.");
            return;
        }

        currentParticipantId = participantId;
        currentTrial = trial;
        feedbackResponses.Clear();
        
        // Clear previous questions
        foreach (var obj in activeQuestions)
        {
            Destroy(obj);
        }
        activeQuestions.Clear();
        currentQuestionIndex = 0;

        // Start timing
        feedbackStartTime = Time.time;

        // Create question UI
        List<Question> questions = questionnaire.questions;
        int totalQuestions = questions.Count;

        if (totalQuestions == 1)
        {
            // Single question case
            GameObject questionObj = Instantiate(singleQuestion, parentContainer);
            UpdateQuestionUI(questionObj, questions[0]);
            AttachButtonListeners(questionObj, false, true);
            activeQuestions.Add(questionObj);
        }
        else
        {
            for (int i = 0; i < totalQuestions; i++)
            {
                GameObject questionObj;

                if (i == 0)
                {
                    questionObj = Instantiate(startingQuestion, parentContainer);
                    AttachButtonListeners(questionObj, false, true);
                }
                else if (i == totalQuestions - 1)
                {
                    questionObj = Instantiate(endingQuestion, parentContainer);
                    AttachButtonListeners(questionObj, true, true);
                }
                else
                {
                    questionObj = Instantiate(inbetweenQuestion, parentContainer);
                    AttachButtonListeners(questionObj, true, true);
                }

                UpdateQuestionUI(questionObj, questions[i]);
                activeQuestions.Add(questionObj);
                questionObj.SetActive(false);
            }
        }

        SetPositionAndOrientation(is3Dtrial);

        // Show first question
        if (activeQuestions.Count > 0)
        {
            activeQuestions[0].SetActive(true);
        }
    }

    private void UpdateQuestionUI(GameObject questionObj, Question question)
    {
        // Update question text
        Transform questionTextTransform = questionObj.transform.Find("ContentRoot/CanvasRoot/Buttons/TileButtons/Question");
        if (questionTextTransform != null)
        {
            TextMeshProUGUI questionTMP = questionTextTransform.GetComponent<TextMeshProUGUI>();
            if (questionTMP != null)
            {
                questionTMP.text = question.question_text;
            }
        }

        // Get next button content
        Transform nextContent = questionObj.transform.Find("ContentRoot/CanvasRoot/Buttons/TileButtons/PrimaryButtons/Next/Content");
        if (nextContent == null)
        {
            Debug.LogError("Next button content not found");
        }

        // Update scale labels and attach toggle listeners
        Transform toggleButtons = questionObj.transform.Find("ContentRoot/CanvasRoot/Buttons/TileButtons/ToggleButtons");

        // Toggle group reference
        ToggleGroup toggleGroup = toggleButtons.GetComponent<ToggleGroup>();

        if (toggleButtons != null)
        {
            TextMeshProUGUI[] labels = toggleButtons.GetComponentsInChildren<TextMeshProUGUI>();
            if (labels.Length >= 2)
            {
                labels[0].text = question.scale_labels[0];
                labels[labels.Length - 1].text = question.scale_labels[1];
            }

            // Attach toggle listeners (scale 1-7)
            Toggle[] toggles = toggleButtons.GetComponentsInChildren<Toggle>();
            for (int i = 0; i < toggles.Length; i++)
            {
                int score = i + 1;
                int questionId = question.question_id;
                
                toggles[i].onValueChanged.AddListener((bool isSelected) =>
                {
                    if (isSelected)
                    {
                        feedbackResponses[questionId] = score;
                        if (nextContent != null)
                        {
                            nextContent.gameObject.SetActive(true);
                        }
                        
                        // Trigger haptic feedback
                        if (hapticsManager != null)
                        {
                            hapticsManager.TriggerHapticsOnBothControllers();
                        }

                        // if selected, disable the toggle group switch off to prevent turning off
                        if (toggleGroup != null && toggleGroup.allowSwitchOff)
                        {
                            toggleGroup.allowSwitchOff = false;
                        }
                    }
                });
            }
        }
    }

    private void AttachButtonListeners(GameObject questionObj, bool attachBack, bool attachNext)
    {
        if (attachBack)
        {
            Transform backParent = questionObj.transform.Find("ContentRoot/CanvasRoot/Buttons/TileButtons/PrimaryButtons/Back");
            if (backParent != null)
            {
                Toggle backButton = backParent.GetComponent<Toggle>();
                backButton.onValueChanged.AddListener(PreviousQuestion);
            }
            else
            {
                Debug.LogError("Back toggle component is missing in Question UI object");
            }
        }
        
        if (attachNext)
        {
            Transform nextParent = questionObj.transform.Find("ContentRoot/CanvasRoot/Buttons/TileButtons/PrimaryButtons/Next");
            if (nextParent != null)
            {
                Toggle nextButton = nextParent.GetComponent<Toggle>();
                nextButton.onValueChanged.AddListener(NextQuestion);
            }
            else
            {
                Debug.LogError("Next toggle component is missing in Question UI object");
            }
        }
    }

    public void NextQuestion(bool state)
    {
        // Trigger haptic feedback
        if (hapticsManager != null)
        {
            hapticsManager.TriggerHapticsOnBothControllers();
        }

        if (currentQuestionIndex < activeQuestions.Count - 1)
        {
            activeQuestions[currentQuestionIndex].SetActive(false);
            currentQuestionIndex++;
            activeQuestions[currentQuestionIndex].SetActive(true);
        }
        else
        {
            // All questions completed
            float feedbackDuration = Time.time - feedbackStartTime;
            activeQuestions[currentQuestionIndex].SetActive(false);
            SaveFeedbackToJson(feedbackDuration);
            OnQuestionsCompleted?.Invoke();
        }
    }

    public void PreviousQuestion(bool state)
    {
        // Trigger haptic feedback
        if (hapticsManager != null)
        {
            hapticsManager.TriggerHapticsOnBothControllers();
        }

        if (currentQuestionIndex > 0)
        {
            activeQuestions[currentQuestionIndex].SetActive(false);
            currentQuestionIndex--;
            activeQuestions[currentQuestionIndex].SetActive(true);
        }
        else
        {
            Debug.Log("Already at first question.");
        }
    }

    private void SaveFeedbackToJson(float feedbackDuration)
    {
        // Create folder structure: Assets/User Study/Results/{participant_id}/
        string resultsFolder = Path.Combine(Application.persistentDataPath, "User Study", "Results", currentParticipantId);
        
        if (!Directory.Exists(resultsFolder))
        {
            Directory.CreateDirectory(resultsFolder);
        }

        // Filename: trial_{trial_id}_feedback.json (if intro trial, set trial_id to intro)
        string trialIdStr = currentTrial.trial_id == -1 ? "intro" : currentTrial.trial_id.ToString();
        string outputPath = Path.Combine(resultsFolder, $"trial_{trialIdStr}_feedback.json");

        // Prepare feedback data
        List<Question> feedbackQuestions = new List<Question>();
        
        foreach (var question in questionnaire.questions)
        {
            Question feedbackQuestion = new Question
            {
                question_id = question.question_id,
                question_text = question.question_text,
                scale_labels = question.scale_labels,
                feedback_score = feedbackResponses.ContainsKey(question.question_id) 
                    ? feedbackResponses[question.question_id] 
                    : -1
            };
            feedbackQuestions.Add(feedbackQuestion);
        }

        // Create trial feedback object
        TrialFeedback feedback = new TrialFeedback
        {
            participant_id = currentParticipantId,
            trial_id = currentTrial.trial_id,
            trajectory_id = currentTrial.trajectory_id,
            camera_type = currentTrial.camera_type,
            camera_target_ped_id = currentTrial.camera_target_ped_id,
            feedback_duration_seconds = feedbackDuration,
            questions = feedbackQuestions
        };

        // Save to file
        JsonManager<TrialFeedback>.WriteJson(outputPath, feedback);
        Debug.Log($"Feedback saved: {outputPath}");
    }

    private void SetPositionAndOrientation(bool is3Dtrial)
    {
        if (menuPlacement != null)
        {
            Vector3 position = is3Dtrial ? positionAnchor3D : positionAnchor2D;
            transform.position = position;
            Vector3 lookOrientation = menuPlacement.CalculateLookTarget(gameObject);
            transform.LookAt(lookOrientation);
            transform.Rotate(0, 180f, 0);
        }
    }
}
