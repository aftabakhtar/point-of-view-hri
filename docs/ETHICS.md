# Ethics and data availability

> ## 🚧 Placeholders below must be completed before publishing
>
> No ethics approval, consent form or participant information sheet exists in
> the source repository, so the approval details could not be derived from the
> code and are marked `TODO`. Fill them in — a data release without a stated
> approval basis is not reusable by others and will be queried by reviewers.
>
> The decision to release the full de-identified dataset was taken by the study
> authors after consultation within the lab.

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
| Age | **Retained exactly** | A deliberate trade-off: banding would make the mean and SD reported in the paper impossible to recompute from the released data. Reproducibility was judged to outweigh the added disclosure risk |
| Country of origin | **Dropped** | Several countries appear exactly once in the cohort |
| Primary language | **Dropped** | Same problem, and it correlates strongly with country |
| Field of study | **Coarsened** to STEM / non-STEM | Free-text fields like "Geography" are near-unique |
| Gender, education | Retained | Coarse categories, but note small cell counts |
| Participant ID | Retained as pseudonym (`P001`…) | Needed to join responses across instruments |

Per-trial response JSONs contain no personal data at all and are released
unmodified.

### Excluded sessions

Three IDs appear in the raw exports but not in the release. They are removed
from every file rather than merely annotated:

| ID | Reason |
|---|---|
| `P100` | Tester account, not a study participant |
| `P013` | Experimenter's own test run, not a real session |
| `DEMO001` | The short public-walkthrough configuration |

Applying these exclusions is what takes the raw exports to the **N = 24**
analysed in the paper. `P017` and `P022` were never assigned.

### Corrected identifier

The raw demographics export labels two rows `P006` and contains no `P005`,
while the NARS and exit-questionnaire exports each contain `P005` and no
duplicate — so one of the two `P006` rows must be `P005`. Which one is settled
by same-day pairing, the two sessions falling two months apart:

| Demographics row | Matching NARS submission |
|---|---|
| 2026-01-22 15:42:15 | `P006` at 2026-01-22 15:44:53 |
| 2026-03-24 15:01:33 | `P005` at 2026-03-24 15:00:11 |

The March row is therefore released as `P005`. Without this correction the
demographics table cannot be joined to the other instruments for those two
participants.

**This is pseudonymisation, not anonymisation.** With a sample of this size and
a known recruitment pool, re-identification cannot be ruled out. Anyone reusing
the data should treat it accordingly.

### Residual disclosure risk, measured

Replacing names with `P0xx` identifiers is *not* what protects participants —
the identifiers carry no risk on their own. Protection comes from the
transformations above. The table below reports how many of the 24 released
participants are uniquely singled out by a given attribute combination, i.e.
cells where k = 1 under k-anonymity.

**In the raw exports (never released):**

| Attribute combination | Uniquely identified |
|---|---|
| Country of origin alone | 8 / 24 (33%) |
| Age + gender | 9 / 24 (38%) |
| Age + gender + country | 19 / 24 (79%) |
| Age + gender + country + language | 20 / 24 (83%) |
| Full set (+ field, education) | **24 / 24 (100%)** |

Additionally all survey timestamps are distinct to the second, across 13
session days, 5 of which had a single participant — enough to pin a session
against a room-booking calendar or access log. The demographics and NARS
exports also share timestamps row-for-row, so releasing both raw would rebuild
the full identifier set even without participant IDs.

**In the released files, after transformation:**

| Attribute combination | Uniquely identified |
|---|---|
| Age alone | 6 / 24 (25%) |
| Gender alone | 0 / 24 (0%) |
| Age + gender | 9 / 24 (38%) |
| Age + gender + field | 9 / 24 (38%) |
| Age + gender + field + education | 11 / 24 (46%) |

So the release removes the timestamp and geographic vectors entirely, but a
reader who already knows an individual participated and knows their age, gender
and education may still be able to locate their row. The realistic adversary is
not a stranger — it is a colleague with background knowledge of who took part.

**The cost of retaining exact age is quantified.** Had age been banded into
18–24 / 25–34 / 35+, uniqueness would fall from 38% to 8% on age + gender, and
from 46% to 21% on the full released set. That reduction was traded away
knowingly: banding would make the M = 27.3, SD = 3.6 reported in the paper
impossible to recompute from the released data, and reproducibility was judged
the higher priority. Anyone reusing this data should understand that roughly
half the cohort is uniquely characterised by their released attributes.

Consequences for reuse:

- Do not attempt to re-identify participants, and do not link this dataset to
  other sources for that purpose.
- The `data/trial_responses/` files carry no demographic attributes at all and
  present no meaningful risk.
- The mapping from participant ID to real identity exists only on the signed
  consent forms. Those are held offline by the study team, are not in this
  repository, and must never be added to it.

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
