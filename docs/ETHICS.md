# Ethics and data availability

> ## 🚧 THIS DOCUMENT IS INCOMPLETE — DO NOT PUBLISH THE REPOSITORY UNTIL IT IS FINISHED
>
> The placeholders below must be filled in by the study authors. No ethics
> approval, consent form or participant information sheet exists anywhere in the
> source repository, so these details could not be derived from the code.
>
> **In particular, no participant data may be published until someone confirms
> that the consent participants gave covers public release of an anonymised
> dataset.** If it does not, ship synthetic example data instead and say so
> here.

## Approval

- **Approving body:** `TODO`
- **Protocol / approval number:** `TODO`
- **Approval date:** `TODO`
- **Principal investigator:** `TODO`

## Recruitment and consent

- **Recruitment:** `TODO — how participants were recruited`
- **Compensation:** `TODO`
- **Written informed consent:** `TODO — confirm obtained before participation`
- **Right to withdraw:** `TODO — describe how it was communicated and honoured`
- **Consent covers open data:** `TODO — YES / NO. This gates everything below.`

Attach the participant information sheet and consent form to this directory
(with any institution-specific identifiers removed) so others can assess and
reuse the protocol.

## What was collected

| Instrument | When | Contains |
|---|---|---|
| Demographics | Pre-study | Age, gender, education, field of study, country of origin, primary language, prior robot experience, VR experience |
| NARS (Nomura et al. 2006) | Pre-study | 14 Likert items |
| Per-trial questionnaire | In VR | 8 Likert items × 10 trials |
| Exit questionnaire | Post-study | Forced-choice items on decision cues, plus free text |

The VR application records **only** the per-trial questionnaire answers and the
time spent answering. No head pose, gaze, controller input or video is captured
at any point.

## Risks

VR simulation sickness is the principal risk. Trials are short (10–16 s), the
participant does not locomote under their own control, and the camera is
parented to a walking rig — which can induce vection. `TODO: describe the
break/withdrawal protocol actually used.`

## De-identification applied to the released data

`analysis/deidentify.py` transforms the raw exports into the `data/` directory.
Raw exports are **not** in this repository and never should be.

| Field | Treatment | Why |
|---|---|---|
| Survey timestamp | **Dropped** | Second-precision, and it joins the demographics and NARS exports row-for-row, reconstructing the full quasi-identifier set |
| Age | **Banded** (18–24 / 25–34 / 35+) | Exact age plus gender plus origin is identifying at this sample size |
| Country of origin | **Dropped** | Several countries appear exactly once in the cohort |
| Primary language | **Dropped** | Same problem, and it correlates strongly with country |
| Field of study | **Coarsened** to STEM / non-STEM | Free-text fields like "Geography" are near-unique |
| Gender, education | Retained | Coarse categories, but note small cell counts |
| Participant ID | Retained as pseudonym (`P001`…) | Needed to join responses across instruments |

Per-trial response JSONs contain no personal data at all and are released
unmodified.

**This is pseudonymisation, not anonymisation.** With a sample of this size and
a known recruitment pool, re-identification cannot be ruled out. Anyone reusing
the data should treat it accordingly.

### Known data-quality corrections

`deidentify.py` fixes three defects found in the raw exports. They are corrected
in the release, and recorded here because they affect any previously computed
descriptives:

1. **Duplicate participant ID.** The demographics export contains `P006` twice
   and omits `P005`. The second `P006` row matches a `P005` timestamp in the
   NARS export and is relabelled accordingly. Scripts that reported `len(df)`
   uncritically produced wrong demographic summaries.
2. **Silent cue-parsing loss.** Two exit-questionnaire rows used `.` instead of
   `,` as a separator (e.g. `head_nod:-1.more_distance:+1`). The parser raised
   into a bare `except: pass`, dropping those cues entirely, so reported cue
   frequencies were computed on a reduced *n*.
3. **Inconsistent ID case.** Some IDs were lower-case (`p004`, `p011`); only one
   of the three analysis scripts normalised them, so joins silently missed rows.

## Contact

`TODO — corresponding author and contact address for data questions`
