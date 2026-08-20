using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine.Video;
using Unity.VisualScripting;
using TMPro;
using System.Text.RegularExpressions;

public class StudyManager : MonoBehaviour
{
    [Header("Configuration")]
    private string studyParticipantId;
    [SerializeField] private string defaultEditorParticipantID = "P001";

    [Header("References")]
    [SerializeField] private FeedbackManager feedbackManager;
    [SerializeField] private List<GameObject> pedestrianObjects;
    [SerializeField] private RobotMovement robotMovement;
    [SerializeField] private VRCameraAttacher vRCameraAttacher;
    [SerializeField] private DialogManager dialogManager;
    [SerializeField] private TooltipController tooltipController;

    [Header("Video References")]
    [SerializeField] private VideoPlayer videoPlayer;


    private string studyConfigPath;
    private StudyConfiguration studyConfig;
    private int currentTrialIndex = 0;
    private bool show2DIntroDialog;

    void Awake()
    {
        studyParticipantId = GetParticipantIDFromCommandLine();

        if (string.IsNullOrEmpty(studyParticipantId))
        {
            Debug.LogError("Participant ID is required to run the study.");

            if (Application.isEditor)
            {
                studyParticipantId = defaultEditorParticipantID;
                Debug.LogWarning($"Using default participant ID for editor: {studyParticipantId}");
            }
            else
            {
                Debug.LogError("Exiting application due to missing participant ID.");
                Application.Quit();
            }
        }
    }

    void Start()
    {
        studyConfigPath = Path.Combine(Application.streamingAssetsPath, "User Study", "ParticipantJsons", studyParticipantId + ".json");

        LoadStudyConfiguration();

        // Subscribe to feedback completion
        if (feedbackManager != null)
        {
            feedbackManager.OnQuestionsCompleted += OnFeedbackCompleted;
        }

        // Subscribe to dialog manager countdown finished
        if (dialogManager != null)
        {
            dialogManager.OnCountdownFinished += OnCountdownFinished;
            dialogManager.OnStudyStartConfirmed += OnWelcomeDialogConfirmed;
            dialogManager.OnStudyEndConfirmed += OnEndDialogConfirmed;
            dialogManager.OnTwoDimIntroConfirmed += OnTwoDimIntroDialogConfirmed;
        }

        // Stopping peds animation at start
        StopAnimationForPeds();

        // Start the study
        StartStudy();
    }

    void LoadStudyConfiguration()
    {
        studyConfig = JsonManager<StudyConfiguration>.ReadJson(studyConfigPath);

        if (studyConfig == null)
        {
            Debug.LogError("Failed to load study configuration!");
            return;
        }

        show2DIntroDialog = studyConfig.show_2d_intro_dialog;

        Debug.Log($"Study loaded for participant: {studyConfig.participant_id}");
        Debug.Log($"Total trials: {studyConfig.trials.Count}");

        // Load questionnaire into feedback manager
        if (feedbackManager != null)
        {
            string questionnairePath = Path.Combine(
                Application.streamingAssetsPath,
                studyConfig.questionnaire_path
            );
            feedbackManager.LoadQuestionnaire(questionnairePath);
        }
    }

    void SetupPedestrians()
    {
        foreach (var ped in studyConfig.pedestrians)
        {
            Vector3 position = ped.start_position.ToVector3();
            Vector3 rotation = ped.start_orientation.ToVector3();

            Debug.Log($"Setup Pedestrian {ped.ped_id}: Pos={position}, Rot={rotation}");

            GameObject pedObject = pedestrianObjects.Find(p => p.name == ped.ped_id.ToString());
            if (pedObject != null)
            {
                // transform only child respecting parents position
                pedObject.transform.localPosition = position;
                pedObject.transform.localRotation = Quaternion.Euler(rotation);

                Debug.Log($"Pedestrian {ped.ped_id} positioned.");
            }
            else
            {
                Debug.LogWarning($"Pedestrian object for ID {ped.ped_id} not found!");
            }
        }
    }

    public void StartStudy()
    {
        currentTrialIndex = GetStartingTrialIndex();

        if (currentTrialIndex == 0)
        {
            startGuidedStudy();
        }
        else
        {
            NextTrialWithCountdown();
        }
        // StartTrial(currentTrialIndex);
        // NextTrialWithCountdown();
    }

    void StartTrial(int trialIndex)
    {
        if (trialIndex >= studyConfig.trials.Count)
        {
            Debug.Log("All trials completed!");
            OnStudyCompleted();
            return;
        }

        TrialConfig trial = studyConfig.trials[trialIndex];
        Debug.Log($"Starting Trial {trial.trial_id}");


        // Setup pedestrians
        SetupPedestrians();

        // Setup camera
        SetupPedCamera(trial);

        // Animate pedestrians
        StartAnimationFoPeds();

        // Load robot trajectory
        string trajectoryPath = GetTrajectoryPath(trial.trajectory_id);
        if (trajectoryPath != null)
        {
            Debug.Log($"Loading trajectory: {trajectoryPath}");
            Debug.Log($"Trajectory label: {studyConfig.robot_trajectories.Find(t => t.trajectory_id == trial.trajectory_id)?.trajectory_label}");


            // Display tooltip
            if (tooltipController != null && trial.trial_id != -1)
            {
                tooltipController.ShowTooltip(studyConfig.robot_trajectories.Find(t => t.trajectory_id == trial.trajectory_id)?.trajectory_label ?? "Error");
            }

            // Load and play your robot trajectory
            robotMovement.PlayTrajectory(trajectoryPath, trial.camera_target_ped_id);
        }

        // Start trial simulation
        // After trial.duration_seconds, show questionnaire
        Invoke(nameof(ShowQuestionnaire), trial.duration_seconds);
    }

    void SetupPedCamera(TrialConfig trial)
    {
        Dictionary<int, int> pedIdToCameraId = new Dictionary<int, int>() {
            {0, 0},
            {1, 0},
            {2, 0},
            {3, 1},
            {4, 1},
            {5, 1},
            {100, 0}  // Projection Room
        };

        int groupId = pedIdToCameraId[trial.camera_target_ped_id];
        if (trial.camera_type == "pedestrian")
        {
            Debug.Log($"Camera -> Pedestrian {trial.camera_target_ped_id}");

            // Attach VR camera to pedestrian
            vRCameraAttacher.AttachToChild(groupId, trial.camera_target_ped_id % 3);

            // Hide the target ped
            HidePedMeshByName(trial.camera_target_ped_id.ToString());
        }
        else if (trial.camera_type == "top_down")
        {
            Debug.Log("Camera -> Projection Room");

            // hiding all ped meshes
            HideAllPedMeshes();

            // Move camera to projection room position/orientation
            vRCameraAttacher.AttachToChild(groupId, 3); // 3 is the fixed id for projection room

            // Play video in projection room
            PlayVideo(trial);
        }
        else
        {
            Debug.LogWarning($"Unknown camera type: {trial.camera_type}");
        }
    }

    void ShowQuestionnaire()
    {
        // Stop pedestrian animations
        StopAnimationForPeds();

        // Stop robot movement
        robotMovement.StopTrajectory();

        // hiding all ped meshes
        HideAllPedMeshes();

        // getting camera type
        int cameraType = getCameraTypeByTrailIdx(currentTrialIndex);

        TrialConfig trial = studyConfig.trials[currentTrialIndex];

        if (feedbackManager != null)
        {
            feedbackManager.ShowQuestionnaire(studyConfig.participant_id, trial, cameraType == 0 ? true : false); // is3Dtrial = cameraType 0 (pedestrian)
        }
    }

    void OnFeedbackCompleted()
    {
        Debug.Log($"Feedback completed for trial {currentTrialIndex}");

        // Showing ped meshes
        // ShowAllPedMeshes();

        // Move to next trial
        currentTrialIndex++;
        // StartTrial(currentTrialIndex);

        // Check for the end of the study before inspecting the next trial, so we
        // never index past the end of the list.
        if (currentTrialIndex >= studyConfig.trials.Count)
        {
            Debug.Log("All trials completed!");
            int previousCameraType = getCameraTypeByTrailIdx(currentTrialIndex - 1);

            endGuidedStudy(previousCameraType);
            return;
        }

        // pre-emptively checking camera type to show dialog if needed
        int cameraType = getCameraTypeByTrailIdx(currentTrialIndex);
        if (cameraType == 1 && show2DIntroDialog) // top_down
        {
            show2DIntroDialog = false;
            SetupPedestrians();
            SetupPedCamera(studyConfig.trials[currentTrialIndex]);
            StopVideo();
            showTwoDimIntroDialog();
            return;
        }

        NextTrialWithCountdown();
    }

    void OnStudyCompleted()
    {
        Debug.Log($"Study completed for participant {studyConfig.participant_id}");
        // Show completion message or return to main menu
    }

    string GetTrajectoryPath(int trajectoryId)
    {
        TrajectoryReference traj = studyConfig.robot_trajectories.Find(t => t.trajectory_id == trajectoryId);

        if (traj != null)
        {
            return Path.Combine(Application.streamingAssetsPath, "User Study", traj.trajectory_path);
        }

        Debug.LogError($"Trajectory ID {trajectoryId} not found!");
        return null;
    }

    void PlayVideo(TrialConfig trial)
    {
        string videoPath = "file://" + GetTrajectoryPath(trial.trajectory_id).Replace("json", "mp4");

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoPath;
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += (source) =>
            {
                videoPlayer.Play();
            };
        }
    }

    void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }

    int GetStartingTrialIndex()
    {
        HashSet<int> completedTrials = new HashSet<int>();

        // checking if directory exists
        string feedbackDir = Path.Combine(
            Application.persistentDataPath,
            "User Study",
            "Results",
            studyConfig.participant_id
        );

        if (!Directory.Exists(feedbackDir))
        {
            Debug.Log("No previous results found, starting from the beginning.");
            return 0;
        }

        // Logic to determine starting trial index if resuming
        string[] files = Directory.GetFiles(Path.Combine(
            Application.persistentDataPath,
            "User Study",
            "Results",
            studyConfig.participant_id
        ), "*.json");

        foreach (string file in files)
        {
            int trialId = ExtractTrialId(file);
            if (trialId != -999) // -999 indicates extraction worked (including -1 for intro)
            {
                completedTrials.Add(trialId);
            }
        }

        int trialIndex;
        for (int i = 0; i < studyConfig.trials.Count; i++)
        {
            if (!completedTrials.Contains(studyConfig.trials[i].trial_id))
            {
                trialIndex = i;
                Debug.Log($"Resuming from trial index: {trialIndex}");
                return trialIndex;
            }
        }

        trialIndex = studyConfig.trials.Count; // All trials completed
        return trialIndex;
    }

    int ExtractTrialId(string filename)
    {
        var name = Path.GetFileNameWithoutExtension(filename);

        // Check for intro trial first
        if (name.Contains("intro"))
        {
            return -1;
        }

        // Extract last number in file name before extension
        var digits = Regex.Matches(name, @"\d+");
        if (digits.Count == 0) return -999;  // no number found (use -999 as error flag)
        return int.Parse(digits[^1].Value); // last number
    }

    private void StartAnimationFoPeds()
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

    private void StopAnimationForPeds()
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

    private string GetParticipantIDFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("-participantID", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        Debug.LogError($"No participant ID specified");
        return null;
    }


    public void HideAllPedMeshes()
    {
        foreach (var pedObject in pedestrianObjects)
        {
            var animator = pedObject.GetComponent<AttributeAnimator>();
            if (animator != null)
            {
                animator.HidePedMesh();
            }
        }
    }

    public void ShowAllPedMeshes()
    {
        foreach (var pedObject in pedestrianObjects)
        {
            var animator = pedObject.GetComponent<AttributeAnimator>();
            if (animator != null)
            {
                animator.ShowPedMesh();
            }
        }
    }

    public void HidePedMeshByName(string pedName)
    {
        var pedObject = pedestrianObjects.Find(p => p.name == pedName);
        if (pedObject != null)
        {
            var animator = pedObject.GetComponent<AttributeAnimator>();
            if (animator != null)
            {
                animator.HidePedMesh();
            }
        }
    }

    public void OnCountdownFinished()
    {
        // showing meshes after countdown
        ShowAllPedMeshes();

        StartTrial(currentTrialIndex);
    }

    public void NextTrialWithCountdown()
    {
        if (dialogManager != null)
        {
            dialogManager.StartCountdown();
        }
    }

    public void OnWelcomeDialogConfirmed()
    {
        // ShowAllPedMeshes();
        NextTrialWithCountdown();
    }

    public void OnEndDialogConfirmed()
    {
        Debug.Log("Study completed by user.");
        // Handle end of study actions here
        // Quit application for now
        Application.Quit();
    }

    public void OnTwoDimIntroDialogConfirmed()
    {
        NextTrialWithCountdown();
    }

    private void startGuidedStudy()
    {
        if (dialogManager != null)
        {
            // Hide all ped meshes
            HideAllPedMeshes();
            dialogManager.ShowStartDialog();
        }
    }

    private void endGuidedStudy(int previousCameraType)
    {
        if (dialogManager != null)
        {
            dialogManager.ShowEndDialog(previousCameraType == 0 ? true : false); // is3Dtrial = cameraType 0 (pedestrian)
        }
    }

    private void showTwoDimIntroDialog()
    {
        if (dialogManager != null)
        {
            dialogManager.ShowTwoDimIntroDialog(true);
        }
    }

    private int getCameraTypeByTrailIdx(int trialIndex)
    {
        if (trialIndex >= studyConfig.trials.Count)
        {
            Debug.LogError("Trial index out of range for camera type retrieval.");
            return -2; // Invalid index
        }
        if (trialIndex < -1 || trialIndex >= studyConfig.trials.Count) // trialIndex can be -1 when checking before starting first trial
        {
            Debug.LogError("Invalid trial index for camera type retrieval.");
            return -1; // Invalid index
        }

        TrialConfig trial = studyConfig.trials[trialIndex];
        if (trial.camera_type == "pedestrian")
        {
            return 0;
        }
        else if (trial.camera_type == "top_down")
        {
            return 1;
        }
        else
        {
            Debug.LogWarning($"Unknown camera type: {trial.camera_type}");
            return -1; // Unknown type
        }
    }


    // Helper methods
    public int GetTotalTrials() => studyConfig.trials.Count;
    public int GetCurrentTrialIndex() => currentTrialIndex;
    public TrialConfig GetCurrentTrial() => studyConfig.trials[currentTrialIndex];
}