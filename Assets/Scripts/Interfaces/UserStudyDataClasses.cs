using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class StudyMetadata
{
    public string study_version;
    public string date_created;

    // Which row of the Williams square the participant was assigned, as written
    // by the config generator. 0 marks a config outside the design (e.g. DEMO001).
    public int counterbalancing_sequence;
}

[Serializable]
public class PedestrianConfig
{
    public int ped_id;
    public Vector3Data start_position;
    public Vector3Data start_orientation;
}

[Serializable]
public class TrajectoryReference
{
    public int trajectory_id;
    public string trajectory_label;
    public string trajectory_path;
}

[Serializable]
public class TrialConfig
{
    public int trial_id;
    public int trajectory_id;
    public string camera_type; // "pedestrian" or "top_down"
    public int camera_target_ped_id;
    public float duration_seconds;
}

[Serializable]
public class StudyConfiguration
{
    public string participant_id;
    public StudyMetadata study_metadata;
    public List<PedestrianConfig> pedestrians;
    public List<TrajectoryReference> robot_trajectories;
    public List<TrialConfig> trials;
    public string questionnaire_path;
    public bool show_2d_intro_dialog = true;
}

[Serializable]
public class Question
{
    public int question_id;
    public string question_text;
    public List<string> scale_labels;
    public int feedback_score = -1; // -1 means not answered
}

[Serializable]
public class Questionnaire
{
    public string study_title;
    public List<Question> questions;
}

[Serializable]
public class TrialFeedback
{
    public string participant_id;
    public int trial_id;
    public int trajectory_id;
    public string camera_type;
    public int camera_target_ped_id;
    public float feedback_duration_seconds;
    public List<Question> questions;
}
