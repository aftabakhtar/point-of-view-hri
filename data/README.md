# Dataset

> ## ⚠️ This directory is empty in the repository as shipped
>
> The de-identified dataset is **not committed yet**. Releasing human-subject
> data requires confirming that the consent participants gave covers public
> release — see [`../docs/ETHICS.md`](../docs/ETHICS.md), which still has
> unfilled placeholders.
>
> Once that is confirmed, generate this directory with one command:
>
> ```bash
> cd analysis
> python deidentify.py --raw raw_data --out ../data
> ```
>
> `raw_data/` holds the original survey exports and is gitignored. It must never
> be committed: the raw demographics export carries a second-precision
> timestamp, country of origin and primary language, which together
> re-identify participants in a cohort this size.

## Contents once generated

| File | Rows | Description |
|---|---|---|
| `demographics.csv` | one per participant | Coarsened demographics |
| `nars.csv` | one per participant | NARS item responses, `Q1`–`Q14` |
| `nars_items.txt` | — | Item wording and subscale mapping |
| `exit_questionnaire.csv` | one per participant | Post-study coded responses |
| `trial_responses/<ID>/trial_<n>_feedback.json` | 10 per participant | Per-trial ratings straight from the VR application |

## demographics.csv

| Column | Values |
|---|---|
| `participant_id` | `P001`… — pseudonym, joins across all files |
| `age_band` | `18-24`, `25-34`, `35+` |
| `gender` | `Female`, `Male` |
| `education` | `High school`, `Undergraduate`, `Graduate` |
| `field_of_study` | `STEM`, `non-STEM` |
| `prior_robot_experience` | `None`, `Very Limited`, `Some`, `Extensive` |
| `mobile_robot_familiarity` | 1–5 (1 = not familiar) |
| `vr_experience` | `Never`, `Occasionally`, `Frequently` |

Timestamp, country of origin and primary language are **dropped**; age is
**banded**; field of study is **collapsed** to a binary. Rationale in
`docs/ETHICS.md`.

## nars.csv

Fourteen items, 5-point Likert, in form order. Subscales (Nomura et al., 2006):

| Subscale | Items | Construct |
|---|---|---|
| S1 | Q4, Q7, Q8, Q9, Q10, Q12 | Situations and interactions with robots |
| S2 | Q1, Q2, Q11, Q13, Q14 | Social influence of robots |
| S3 | Q3, Q5, Q6 | Emotions in interaction with robots |

> **Reverse scoring:** `analyze_nars.py` names Q3, Q5 and Q6 as reversed but
> does not apply the transformation, on the argument that the form already
> presented them on a flipped scale. Confirm against `nars_items.txt` before
> comparing to published norms.

## exit_questionnaire.csv

| Column | Content |
|---|---|
| `Q1: Behaviour Difference 3D` | Whether behaviour was perceived differently in egocentric view |
| `Q2: Behaviour DIfference 2D` | Same, allocentric view (column name typo preserved from the raw export) |
| `Q3: Chosen Socially Acceptable Behaviour` | Which trajectory was judged most acceptable |
| `Q4: Corrected Behaviour` | Choice after seeing all viewpoints |
| `Q5: Robot Movement and Social Cues Implications` | Coded cue tags, e.g. `head_nod:+1,close_distance:-1` |
| `Q6: Additional Feedback Cues` | Coded free-text themes, e.g. `comment:pay_for_study` |

Free text was coded into tags during analysis; the original free text is not
released, since open responses can be self-identifying.

Cue tags are `name:weight` pairs, comma-separated, where weight is `+1` or `-1`.
Two rows in the raw export used `.` instead of `,`; `deidentify.py` repairs them
(the original parser dropped those cues silently).

## trial_responses/

One JSON per trial, exactly as written by the VR application — no
transformation applied, since they contain no personal data. Schema in
[`../docs/DATA_FORMAT.md`](../docs/DATA_FORMAT.md).

Files are `trial_intro_feedback.json` (practice) and `trial_0`…`trial_8`. Map
conditions with:

| `camera_target_ped_id` | Viewpoint |
|---|---|
| 3 | egocentric-proximal |
| 5 | egocentric-distal |
| 100 | allocentric |

| `trajectory_id` | Trajectory |
|---|---|
| 0 | A — proposed policy with head nod |
| 1 | B — proposed policy, no gesture |
| 2 | C — DWA baseline |
| 100 | practice (stationary) |

## Known completeness gaps

Confirm these against your own regeneration; they reflect the collected data,
not a processing error.

- **25 participants** have complete VR trial data (10 files each).
- **23** have demographics and NARS.
- **24** have exit-questionnaire responses; `P010` has an exit response but no
  demographics or NARS record.
- The raw demographics export listed `P006` twice and omitted `P005`;
  `deidentify.py` relabels the second row to `P005` based on a matching NARS
  timestamp. See `docs/ETHICS.md`.

Twenty-seven configurations were generated, so participant IDs do not imply
twenty-seven completed sessions.

## Licence

Data: **CC BY 4.0** (see `LICENSE-DATA`). Code: MIT (see `../LICENSE`).
Please cite the paper — `../CITATION.cff`.
