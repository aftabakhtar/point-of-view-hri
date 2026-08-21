# Dataset

De-identified data from the user study: demographics, NARS, exit questionnaire
and the per-trial responses.

Generated from the raw survey exports by
[`../analysis/deidentify.py`](../analysis/deidentify.py). The raw exports live
in `analysis/raw_data/`, are gitignored, and must never be committed — they
carry a second-precision timestamp, country of origin and primary language,
which together single out every participant in a cohort this size.

To regenerate:

```bash
cd analysis
python deidentify.py --raw raw_data --out ../data
```

> **Please read [`../docs/ETHICS.md`](../docs/ETHICS.md) before reusing this
> data.** It records the approval this was collected under, every
> disclosure-control transformation applied, and the residual re-identification
> risk, which is not zero.

## Contents

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
| `age` | Exact age in years. Retained so the paper's M/SD stays recomputable; see `docs/ETHICS.md` for the trade-off |
| `gender` | `Female`, `Male` |
| `education` | `High school`, `Undergraduate`, `Graduate` |
| `field_of_study` | `STEM`, `non-STEM` |
| `prior_robot_experience` | `None`, `Very Limited`, `Some`, `Extensive` |
| `mobile_robot_familiarity` | 1–5 (1 = not familiar) |
| `vr_experience` | `Never`, `Occasionally`, `Frequently` |

Timestamp, country of origin and primary language are **dropped**; field of
study is **collapsed** to a binary; exact age is **retained** so the paper's
reported M/SD remains recomputable. Rationale and the measured residual risk are
in `docs/ETHICS.md`.

## nars.csv

Fourteen items, 5-point Likert, in form order. Subscales (Nomura et al., 2006):

| Subscale | Items | Construct |
|---|---|---|
| S1 | Q4, Q7, Q8, Q9, Q10, Q12 | Situations and interactions with robots |
| S2 | Q1, Q2, Q11, Q13, Q14 | Social influence of robots |
| S3 | Q3, Q5, Q6 | Emotions in interaction with robots |

> **Reverse scoring: none is applied, and none is needed.** Standard NARS
> reverses Q3, Q5 and Q6 because they are positively worded. The form used here
> already presented those three on a flipped scale ("strongly agree" at 1), so
> the released values are keyed the same direction as the rest of the
> instrument — higher = more negative attitude — and must **not** be reversed
> again.
>
> The correlation structure confirms this. All three subscales measure negative
> attitudes and so correlate positively in the original instrument
> (Nomura et al. 2006: r(S1,S2)=+0.408, r(S1,S3)=+0.119, r(S2,S3)=+0.165). This
> sample reproduces those signs as released (+0.526, +0.328, +0.477). Reversing
> Q3/Q5/Q6 would flip S3 to −0.328 and −0.477 against S1 and S2, contradicting
> the published structure.

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

The release is complete and internally consistent: **24 participants**, each
appearing in all four sources — demographics, NARS, exit questionnaire, and 10
trial response files (240 files total). This matches the N = 24 analysed in the
paper.

Getting there required two corrections, both documented in `docs/ETHICS.md`:

- **Excluded** `P100` (tester), `P013` (experimenter's test run) and `DEMO001`
  (public walkthrough config) from every file.
- **Relabelled** the second of two `P006` demographics rows to `P005`, resolved
  by same-day pairing against the NARS export.

Note that participant IDs are not contiguous and do not imply sample size:
`P013`, `P017` and `P022` are absent, and 27 configurations were generated
against 24 completed sessions.

## Licence

Data: **CC BY 4.0** (see `LICENSE-DATA`). Code: MIT (see `../LICENSE`).
Please cite the paper — `../CITATION.cff`.
