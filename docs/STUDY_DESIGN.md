# Study design

## Question

Robot navigation policies are typically judged from a bird's-eye view. Does that
view predict how the same behaviour feels to a pedestrian standing in it?

## Design

**3 × 3 fully within-subjects**: 3 robot trajectories × 3 viewpoints = 9
conditions. Every participant sees all nine.

### Trajectories

| ID | Label | File | Behaviour |
|---|---|---|---|
| 0 | **A** | `our_w_nod_0_02.json` | Proposed policy, **with** a head-nod gesture toward the participant |
| 1 | **B** | `our_wo_nod_0_02.json` | Proposed policy, no gesture |
| 2 | **C** | `dwa_9s_0_02.json` | Dynamic Window Approach baseline |
| 100 | Intro | `Intro.json` | Practice trial — a single stationary waypoint, robot does not move |

A and B isolate the head-nod manipulation; C is the comparison policy that tests
whether perspective effects generalise beyond one trajectory type.

Paths are dense polylines sampled at **0.02 m**. `our_w_nod_0_02.json` has 807
points. Replay is deterministic: identical geometry for every participant, in
every viewpoint.

### Viewpoints

| `camera_target_ped_id` | Name | Implementation |
|---|---|---|
| 3 | Egocentric-**proximal** | VR camera parented to the pedestrian the robot passes closest to |
| 5 | Egocentric-**distal** | VR camera parented to a pedestrian further from the path |
| 100 | **Allocentric** | Camera teleported to a projection room; a pre-rendered top-down MP4 of the same trajectory plays on a screen |

In egocentric conditions the participant's own avatar mesh is hidden so they
don't see a body from inside it. In the allocentric condition **all** pedestrian
meshes are hidden, since the crowd is visible in the video instead.

The allocentric condition being a *video* is deliberate: it reproduces how
trajectories are normally reviewed in papers and demos.

### Counterbalancing

Order is fixed offline by a hard-coded **9×9 Williams design** (balanced Latin
square) in `analysis/generate_participant_configs.py`:

```python
williams_square = [
    [0, 1, 8, 2, 4, 7, 5, 3, 6],  # sequence 1
    [1, 2, 0, 3, 5, 8, 6, 4, 7],  # sequence 2
    ...
]
sequence_idx = participant_num % 9
```

Each condition appears once in every ordinal position across the nine sequences,
and each condition follows every other exactly once — controlling both order and
first-order carryover effects. With 27 participants each sequence is used
exactly three times.

**There is no runtime randomisation.** `StudyManager` walks `studyConfig.trials`
in array order, so a participant's sequence is reproducible from their ID alone.
The only stochastic element is cosmetic: each pedestrian's walk-cycle phase is
randomised so the crowd doesn't march in lockstep.

### Trial structure

Every session begins with a **practice trial** (`trial_id: -1`) using the
stationary Intro trajectory from pedestrian 4's viewpoint, to acclimatise
participants to VR and the questionnaire UI before any data counts.

| | Duration |
|---|---|
| Egocentric trials | 10 s |
| Allocentric trials | 16 s |

The extra 6 s covers the video's lead-in. Trials end on a fixed timer, not on
trajectory completion — the robot path **loops** until the timer fires.

Per trial: countdown (3-2-1) → trajectory plays → questionnaire → next.

### Scene layout

Six pedestrians (IDs 0–5) stand in two groups of three, positioned from a
literature table of pedestrian spacing (`PedStudyPositions.cs`, "Population B,
Size = 3"). ID 100 is a proxy transform parked in the projection room. The
robot walks between the groups at ~0.8 m/s; pedestrians animate in place at
~1.05 m/s walk-cycle speed.

The head nod slows the robot to 60 % speed and tilts the head 40°, aimed at
whichever pedestrian the participant currently embodies — the nod direction is
selected at trajectory load time from the `headNod` entry's per-pedestrian
vectors.

## Measures

### Per trial (in VR)

Eight single-word semantic-differential items, each rated on a 7-point scale
anchored "Not at All" → "Totally":

> "Using the scale provided, how closely is the word **X** associated with the
> robot's behaviour?"

| Positive (sociability) | Negative (disturbance) |
|---|---|
| Warm, Trustworthy, Likeable, Friendly | Scary, Creepy, Uncanny, Weird |

`analyze_nars.py` sums items 1–4 as a **sociability** score and items 5–8 as a
**disturbance** score.

The Next button stays hidden until an answer is selected, and once selected the
toggle group prevents deselection — so no item can be skipped.

### Pre-study

- **NARS** (Negative Attitudes toward Robots Scale, Nomura et al. 2006) — 14
  items, 5-point, three subscales: S1 situations/interactions (items 4, 7, 8, 9,
  10, 12), S2 social influence (1, 2, 11, 13, 14), S3 emotions (3, 5, 6).
- Demographics: age, gender, education, field of study, prior robot experience,
  VR/AR/MR experience.

### Post-study

An exit questionnaire on which cues drove judgements (passing distance, head
nod, speed), whether ratings changed between viewpoints, and free-text feedback.
Free text was coded into tags before analysis.

> **Note on NARS reverse scoring.** `analyze_nars.py` declares
> `reversed_items = ["Q3", "Q5", "Q6"]` but never applies the transformation; a
> comment argues the survey already presented those items on a flipped scale.
> The printed header nonetheless says "after reverse-scoring". Verify this
> against your own form before reusing the scoring code.

## Sample

27 participant configurations were generated (3 × 9 sequences). See
`docs/ETHICS.md` for what was actually collected and released.

`power_analysis.py` computes a-priori power for the 3 × 3 within-subjects design
using `FTestAnovaPower`, inflating Cohen's *f* for a within-subject correlation
of r = 0.5. Note its printed header still describes an earlier **2 × 3** design
and increments N in steps of 6 — leftovers from a two-trajectory version. The
condition count in the code (3 × 3 = 9) is current.
