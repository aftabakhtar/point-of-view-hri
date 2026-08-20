# Data formats

Every file the study reads or writes. All JSON I/O goes through
`JsonManager<T>` (Newtonsoft, indented output).

---

## Input: participant configuration

`Assets/StreamingAssets/User Study/ParticipantJsons/<ID>.json`

Loaded at startup from the `-participantID` command-line argument. Deserialized
into `StudyConfiguration` (`Assets/Scripts/Interfaces/UserStudyDataClasses.cs`).

```json
{
  "participant_id": "P001",
  "study_metadata": {
    "study_version": "1.0",
    "date_created": "2026-01-09",
    "counterbalancing_sequence": 1
  },
  "pedestrians": [
    { "ped_id": 0,
      "start_position":    { "x": -0.1072, "y": 0.0, "z": -1.58 },
      "start_orientation": { "x": 0.0, "y": -90.0, "z": 0.0 } }
  ],
  "robot_trajectories": [
    { "trajectory_id": 0, "trajectory_label": "A",
      "trajectory_path": "Trajectories/our_w_nod_0_02.json" }
  ],
  "trials": [
    { "trial_id": -1, "trajectory_id": 100, "camera_type": "pedestrian",
      "camera_target_ped_id": 4, "duration_seconds": 10 },
    { "trial_id": 0,  "trajectory_id": 0,   "camera_type": "pedestrian",
      "camera_target_ped_id": 3, "duration_seconds": 10 },
    { "trial_id": 2,  "trajectory_id": 2,   "camera_type": "top_down",
      "camera_target_ped_id": 100, "duration_seconds": 16 }
  ],
  "questionnaire_path": "User Study/questionnaire.json",
  "show_2d_intro_dialog": true
}
```

| Field | Type | Notes |
|---|---|---|
| `participant_id` | string | Must match the filename and the `-participantID` argument |
| `study_metadata.counterbalancing_sequence` | int | Williams-square row, 1–9. `0` marks a config outside the design (e.g. `DEMO001`) |
| `pedestrians[].ped_id` | int | Matched against GameObject **names** in the scene. Valid: 0–5, and 100 for the projection-room proxy |
| `pedestrians[].start_position` | Vector3 | Applied as `localPosition` relative to the pedestrian group parent, not world space |
| `pedestrians[].start_orientation` | Vector3 | Euler angles, applied as `localRotation` |
| `trials[].trial_id` | int | `-1` marks the practice trial; its result file is named `trial_intro_feedback.json` |
| `trials[].camera_type` | string | `"pedestrian"` or `"top_down"` — any other value logs a warning and leaves the camera where it was |
| `trials[].camera_target_ped_id` | int | Must be 0–5 or 100. **Any other value throws `KeyNotFoundException`** in `SetupPedCamera` |
| `trials[].duration_seconds` | float | Wall-clock trial length. The trajectory loops until this elapses |
| `questionnaire_path` | string | Relative to `StreamingAssets/` |
| `show_2d_intro_dialog` | bool | Defaults to `true` when absent. Set `false` to skip the one-time allocentric explainer |

## Input: questionnaire

`Assets/StreamingAssets/User Study/questionnaire.json`

```json
{
  "study_title": "Questionnaire",
  "questions": [
    {
      "question_id": 1,
      "question_text": "Using the scale provided, how closely is the word <b><u>Warm</u></b> \nassociated with the robot's behaviour?",
      "scale_labels": ["Not at\nAll", "Totally"]
    }
  ]
}
```

`question_text` is TextMeshPro rich text. `scale_labels` holds exactly **two**
anchors — the endpoints. The **number of scale points comes from the prefab**,
not from this file: `FeedbackManager` creates one Likert point per `Toggle` under
`ContentRoot/CanvasRoot/Buttons/TileButtons/ToggleButtons`, scoring them
`index + 1`. The shipped prefabs give a 7-point scale.

`feedback_score` is absent here and defaults to `-1` (unanswered).

## Input: robot trajectory

`Assets/StreamingAssets/User Study/Trajectories/<name>.json`

```json
{
  "points": [
    {
      "position": { "x": 56.11, "y": 0.0, "z": 29.54 },
      "headNod": [
        { "target_ped_id": 3,
          "direction": { "x": -0.995188951, "y": -2.35013459E-07, "z": -0.09797429 } },
        { "target_ped_id": 5,
          "direction": { "x": -0.887977839, "y": 0.0, "z": -0.459886342 } }
      ]
    }
  ],
  "timestamp": "2025-11-04_17-03-47",
  "totalPoints": 807
}
```

| Field | Notes |
|---|---|
| `points[].position` | World-space waypoint. Spacing is uniform — 0.02 m in the shipped trajectories |
| `points[].headNod` | `null` on most points. Where present, one entry per pedestrian the nod can target; the entry matching the trial's `camera_target_ped_id` is chosen at load |
| `headNod[].direction` | Unit vector the head turns toward |
| `totalPoints` | Informational; playback uses `points.length` |

Trajectory A carries a nod on exactly one point; B and C carry none.

Each `<name>.json` used in an allocentric trial needs a sibling `<name>.mp4`.
The player derives the video path by string-replacing `json` with `mp4` in the
full path — so avoid installing the project under a directory containing "json".

## Output: trial feedback

`%USERPROFILE%\AppData\LocalLow\DefaultCompany\robot-trajectory-pref-urp\User Study\Results\<ID>\trial_<n>_feedback.json`

The **only** file the study writes. One per trial, written when the last
question is answered.

```json
{
  "participant_id": "P001",
  "trial_id": 0,
  "trajectory_id": 0,
  "camera_type": "pedestrian",
  "camera_target_ped_id": 3,
  "feedback_duration_seconds": 57.289135,
  "questions": [
    {
      "question_id": 1,
      "question_text": "...",
      "scale_labels": ["Not at\nAll", "Totally"],
      "feedback_score": 7
    }
  ]
}
```

`feedback_duration_seconds` is time spent on the questionnaire (`Time.time`
delta), not trial duration.

**Not recorded:** head pose, gaze, controller input, per-frame robot or
pedestrian state, absolute timestamps, or trial start/end times. If you need
these, `TransformRecorder.cs` is a starting point — it exists but is disabled in
the scene.

### Resume behaviour

On startup `GetStartingTrialIndex()` scans the results folder and resumes at the
first trial with no result file. Filenames are parsed for their trailing number,
with `intro` mapping to `-1`. Deleting a participant's folder restarts them from
the beginning; deleting one file re-runs that trial.

## Derived: analysis CSVs

Produced by `analyze_nars.py` from the per-trial JSONs joined with NARS
responses.

`extended_nars_long_format.csv` — one row per participant × condition ×
subscale:

| Column | Values |
|---|---|
| `Participant` | `P001`… |
| `Trajectory` | `A`, `B`, `C` |
| `Camera` | `proximal` (ped 3), `distal` (ped 5), `allocentric` (100) |
| `Subscale` | NARS `S1`/`S2`/`S3`, or `sociability` / `disturbance` |
| `Score` | Summed item score |

This is the frame a repeated-measures model would consume. See
`analysis/inferential/README.md`.

## Schema reference

All C# types live in
[`Assets/Scripts/Interfaces/UserStudyDataClasses.cs`](../Assets/Scripts/Interfaces/UserStudyDataClasses.cs):
`StudyConfiguration`, `StudyMetadata`, `PedestrianConfig`, `TrajectoryReference`,
`TrialConfig`, `Questionnaire`, `Question`, `TrialFeedback`, `Vector3Data`.

Trajectory types are defined in
[`Assets/Scripts/Utilities/TrajectoryGenerator.cs`](../Assets/Scripts/Utilities/TrajectoryGenerator.cs):
`TrajectoryData`, `TrajectoryPoint`, `HeadNodData`, `SerializableVector3`.
