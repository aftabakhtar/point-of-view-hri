# Point of View: Perspective and Perceived Robot Sociability

A Unity VR platform for studying how **viewing perspective** changes people's
social judgements of robot navigation, plus the trajectory data, study
configuration tooling and analysis scripts behind the paper.

> **Point of View: How Perspective Affects Perceived Robot Sociability**
> Subham Agrawal, Aftab Akhtar, Nils Dengler, Maren Bennewitz
> [arXiv:2603.28272](https://arxiv.org/abs/2603.28272)

Robot navigation policies are usually validated from a bird's-eye view. This
platform asks whether that view is misleading: it replays **identical** robot
trajectories from three perspectives and measures how the ratings shift.

| Viewpoint | What the participant experiences |
|---|---|
| **Allocentric** | Seated in a projection room, watching a top-down render of the trajectory on a screen |
| **Egocentric-proximal** | Standing in the crowd as the pedestrian the robot passes closest to |
| **Egocentric-distal** | Standing in the crowd as a pedestrian further from the robot's path |

Headline finding: trajectories that look sociable from above can be rated
significantly more disturbing when experienced up close in first person — and a
head-nod gesture measurably improves perceived sociability.

---

## ⚠️ Read this before cloning

**The scene will not render out of the box.** Five commercial asset packs cannot
legally be redistributed, so they are absent. The project *opens and compiles
cleanly* — the robot, the study logic, the trajectories and the questionnaire UI
all work — but the pedestrians and the environment are missing until you supply
them.

Two ways forward:

1. **Just want to see the study?** Download the pre-built Windows player from
   [Releases](../../releases) — assets are embedded, nothing to buy.
2. **Want to modify or extend it?** Acquire the packs below, then follow
   [`docs/SETUP.md`](docs/SETUP.md).

### Asset packs you need

Install each at exactly the path shown — the scene resolves them by GUID, so
folder names matter.

| Pack | Where to get it | Install to |
|---|---|---|
| **Renderpeople** rigged scans: `rp_manuel_rigged_001`, `rp_nathan_rigged_003`, `rp_sophia_rigged_003`, plus their walking/idling/standing animation FBXs | [renderpeople.com](https://renderpeople.com/) (commercial, per-model) | `Assets/RP_Character/` |
| **(HDRP) NYC-Like City Buildings Set (PBR)** | Unity Asset Store | `Assets/(HDRP) NYC-Like City Buildings Set (PBR)/` |
| **Realistic Tree** (Ash, Birch, Chestnut, Spruce, Weeping Willow) | Unity Asset Store | `Assets/Realistic Tree/` |
| **GrassFlowers** | Unity Asset Store | `Assets/GrassFlowers/` |
| **Terrain Tools Sample Asset Pack** | Unity Asset Store — **free** | `Assets/TerrainSampleAssets/` |

Only Renderpeople and the three Asset Store packs cost money; the terrain pack is
free. Exact model names and the texture-editing step are in
[`docs/SETUP.md`](docs/SETUP.md).

### What a clone *without* the packs looks like

Opening `SampleScene` prints around 50 `Missing Prefab` errors. **This is
expected, not a broken clone.** Every one falls into these groups:

| Missing prefab names | From |
|---|---|
| `0`, `1`, `2`, `3`, `4`, `5` | Renderpeople — the six pedestrians |
| `building_1`, `building_2`, `building_3 (1)` | NYC buildings pack |
| `Ash *`, `Birch *`, `Chestnut *`, `Spruce *`, `Spruce Group *`, `Weeping_Willow *` | Realistic Tree pack |

You will also see `VR Camera attached to: 0 (Missing Prefab…)` — that is
`VRCameraAttacher` validating against an absent pedestrian rig, and it clears
once Renderpeople is installed.

Anything **not** in that list is a genuine problem worth
[opening an issue](../../issues) about. In particular, the Toyota HSR robot
parts (`base`, `torso`, `head_pan`, `arm_flex`, `palm`, `laser`, `rgbd`…) ship
with this repository and must **not** appear as missing.

Also note the bundled Toyota HSR meshes are **CC BY-NC-ND 4.0**, so this
repository as a whole is not commercially usable. See
[`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

---

## Repository map

```
Assets/                     Unity project (Unity 6000.2.7f2, URP, OpenXR + Meta XR SDK)
  Scenes/SampleScene.unity  The one study scene — all three viewpoints live here
  Scripts/
    Interfaces/             Serializable DTOs for config, questionnaire, results
    Robot/                  Trajectory playback and the HSR head-nod animation
    Utilities/              Study orchestration, questionnaire UI, camera switching,
                            and the trajectory authoring/import tools
  StreamingAssets/
    User Study/
      ParticipantJsons/     P001–P027 + DEMO001 — one config per participant
      Trajectories/         Robot paths (.json) and allocentric stimuli (.mp4)
      questionnaire.json    The 8 per-trial items
  Models/hsr_description_v2 Toyota HSR robot description (see licence notes)

analysis/                   Python: config generation, plotting, power analysis
  inferential/              Intentionally empty — see the note below
data/                       De-identified study data — see docs/ETHICS.md
  demographics.csv          Coarsened: age banded, timestamps/country/language dropped
  nars.csv                  NARS item responses + nars_items.txt codebook
  exit_questionnaire.csv    Post-study coded responses
  trial_responses/          Per-trial ratings, one JSON per trial
docs/                       Setup, study design, data formats, session protocol
```

## How it works

**One scene, one camera.** Viewpoint is switched by *re-parenting* the VR camera
onto a target transform at eye height —
`VRCameraAttacher.AttachToChild()`. In egocentric conditions the participant
literally rides on a walking pedestrian rig, with their own avatar mesh hidden.

**Allocentric is not a camera angle.** The participant is teleported to a
separate projection room and shown a pre-rendered MP4 of the same trajectory. The
top-down condition is therefore a *video*, not a live render — which is what
makes it a faithful stand-in for how policies are normally reviewed.

**Nothing is randomised at runtime.** Trial order is baked into each
participant's JSON by a 9×9 Williams design (balanced Latin square) over 3
trajectories × 3 viewpoints. `StudyManager` simply walks the list. Regenerating a
participant's config always produces the same sequence.

**The robot follows a fixed path.** Trajectories are dense polylines (0.02 m
spacing) interpolated onto an `ArticulationBody`. There is no planner, no
NavMesh and no crowd simulation at runtime — the trajectories were generated
offline and are replayed identically for every participant.

**One JSON per trial is written.** Questionnaire answers land in
`persistentDataPath/User Study/Results/<ID>/trial_<n>_feedback.json`. No head
pose, gaze or telemetry is recorded. Sessions resume after a crash by scanning
that folder.

See [`docs/STUDY_DESIGN.md`](docs/STUDY_DESIGN.md) for the full design and
[`docs/DATA_FORMAT.md`](docs/DATA_FORMAT.md) for every schema.

## Quick start

### Run a session

```bash
# Generate participant configurations (deterministic)
cd analysis
python generate_participant_configs.py --participants 27

# Then launch the built player with a participant ID
robot-trajectory-pref-urp.exe -participantID P001
```

The ID is mandatory in a build; without it the player logs an error and quits.
In the Editor it falls back to `P001`. `DEMO001` is a 3-trial, ~3-minute
walkthrough suitable for demos and open days.

Full protocol: [`docs/RUNNING_A_SESSION.md`](docs/RUNNING_A_SESSION.md).

### Reproduce the analysis

```bash
cd analysis
python -m venv .venv && .venv/Scripts/activate    # source .venv/bin/activate on Unix
pip install -r requirements.txt
python analyze_demo.py          # demographics
python analyze_nars.py          # NARS subscale scoring
python analyze_end_questions.py # exit questionnaire figures
python power_analysis.py        # a-priori power curves
```

**The paper's inferential statistics are not in this repository.** What ships
here is descriptive: means, SDs, correlations and a power analysis. The
repeated-measures tests behind the significance claims were run separately —
see [`analysis/inferential/README.md`](analysis/inferential/README.md). Treat
the paper as authoritative for reported statistics.

## Reusing this for your own study

The study logic is data-driven and largely independent of the specific scenario.
To run a different experiment you mostly edit JSON, not C#:

- **Different questions** — edit `questionnaire.json`; the UI builds itself from
  the item list and the scale length comes from the prefab.
- **Different conditions or order** — edit the trajectory/viewpoint tables in
  `analysis/generate_participant_configs.py`.
- **Different robot paths** — draw one with `TrajectoryGenerator` (mouse, in
  play mode) or import a planner's `x,z` output with `TrajectoryTxtProcessor`.
- **Different robot** — replace the URDF and re-run the URDF Importer.

VR coupling is thin: `OVRInput` appears only in `HapticsController`. The
questionnaire prefabs use Meta Interaction SDK poke/ray interactors, so a
desktop (non-VR) mode would mean swapping those for a standard input module and
pointing `MenuPlacement.centerEyeAnchor` at a desktop camera.

## Citing

```bibtex
@article{agrawal2026pointofview,
  title   = {Point of View: How Perspective Affects Perceived Robot Sociability},
  author  = {Agrawal, Subham and Akhtar, Aftab and Dengler, Nils and Bennewitz, Maren},
  journal = {arXiv preprint arXiv:2603.28272},
  year    = {2026}
}
```

## Licence

First-party code and documentation: **MIT** ([`LICENSE`](LICENSE)).
Bundled and required third-party content is licensed separately, and some of it
restricts commercial use — read
[`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

## Acknowledgements

Developed at the [Humanoid Robots Lab](https://www.hrl.uni-bonn.de/),
University of Bonn. The robot model is the Toyota Human Support Robot (HSR).
