# Running a session

Operator's guide to collecting data with this platform.

## 1. Generate participant configurations

```bash
cd analysis
python generate_participant_configs.py --participants 27
```

Writes to `participant_configs/` (JSON) and `launch_scripts/` (`.bat`). Both are
`.gitignore`d — they are operator artefacts, not source.

Copy the JSONs to `Assets/StreamingAssets/User Study/ParticipantJsons/` before
building, or generate them there directly:

```bash
python generate_participant_configs.py \
  --output "../Assets/StreamingAssets/User Study/ParticipantJsons"
```

Useful flags: `--participants N` (multiples of 9 keep the design balanced),
`--trial-duration`, `--exe-name`, `--demo-only`, `--no-demo`, and `--date
YYYY-MM-DD` to pin `date_created` so a regenerated set matches a previous one
byte-for-byte.

The generator prints a self-check confirming every condition appears once in
every ordinal position.

## 2. Build

Windows x64, Direct3D11, OpenXR with the Meta XR feature enabled. The only
scene in Build Settings is `Assets/Scenes/SampleScene.unity`.

Copy the `.bat` launchers next to the built `.exe`.

## 3. Run a participant

```bat
robot-trajectory-pref-urp.exe -participantID P001
```

or double-click `Launch_P001.bat`.

**The participant ID is mandatory in a build.** Without it the player logs an
error and quits — a deliberate guard against unlabelled data. In the Editor it
falls back to `P001`.

### What the participant experiences

1. **Welcome dialog**, placed in front of the headset. They confirm to begin.
2. **Practice trial** — stationary robot, full questionnaire, so the UI is
   familiar before real data.
3. For each of the 9 trials: 3-2-1 countdown → trajectory plays for 10 s (16 s
   allocentric) → 8-item questionnaire.
4. Before their **first** allocentric trial, a one-time dialog explains the
   top-down view. Controlled by `show_2d_intro_dialog`.
5. **End dialog**, then the application quits.

A tooltip above the robot shows the condition letter (A/B/C) during each trial —
suppressed for the practice trial.

Answers are confirmed with a haptic pulse on both controllers. Participants
cannot skip an item: the Next button is hidden until a value is selected.

## 4. Collect the data

```
%USERPROFILE%\AppData\LocalLow\DefaultCompany\robot-trajectory-pref-urp\User Study\Results\<ID>\
```

Ten files per participant: `trial_intro_feedback.json` plus
`trial_0..8_feedback.json`. Copy them off the machine after each session — the
folder is the only copy and the demo launcher deletes its own subfolder on
every run.

Verify a session is complete by counting files before letting the participant
leave.

## 5. If something goes wrong

**The study is crash-resumable.** Relaunching with the same ID scans the results
folder and resumes at the first trial with no result file. Completed trials are
not repeated.

| Situation | Action |
|---|---|
| Crash or headset dropout mid-session | Relaunch with the same ID; it resumes at the interrupted trial |
| Need to redo one trial | Delete that trial's JSON, relaunch |
| Need a full restart | Delete the participant's folder |
| Participant withdraws | Delete the folder; the ID stays unused |

Because resume is keyed on files present, **never** hand-edit filenames in the
results folder.

## Demo mode

`DEMO001` is a ~3-minute walkthrough for open days and demos:

- 3 trials instead of 10 — trajectory A egocentric, C egocentric, A allocentric.
- 4 of the 8 questionnaire items (`questionnaire_demo.json`), keeping the
  original `question_id`s so demo data stays comparable.
- No practice trial, no trajectory B.
- `show_2d_intro_dialog: false` for a faster run.
- `counterbalancing_sequence: 0`, marking it outside the design.

`Launch_DEMO001.bat` **deletes `Results\DEMO001\` before launching** so each run
starts fresh instead of resuming. That deletion is scoped to the `DEMO001`
subfolder and does not touch participant data — but it does sit one path segment
away from it, so don't repoint it casually.

## Pre-session checklist

- [ ] Ethics approval and consent form in place, signed before the headset goes on
- [ ] Participant ID assigned and not previously used
- [ ] Config JSON present in `StreamingAssets/User Study/ParticipantJsons/`
- [ ] Results folder for this ID empty (or intentionally being resumed)
- [ ] Headset tracking, controllers charged, guardian configured
- [ ] Pre-study NARS and demographics collected
- [ ] Motion-sickness and withdrawal procedure explained
- [ ] Exit questionnaire ready for after the session
