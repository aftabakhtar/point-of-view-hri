# Analysis and study tooling

Python side of the project: participant configuration generation, descriptive
analysis, plotting, and power analysis.

```bash
python -m venv .venv
.venv/Scripts/activate          # source .venv/bin/activate on Unix
pip install -r requirements.txt
```

## Scripts

| Script | Purpose | Inputs |
|---|---|---|
| `generate_participant_configs.py` | Generates per-participant study configs and Windows launchers from a 9×9 Williams design. **Standard library only.** | none |
| `deidentify.py` | Turns raw survey exports into the released `data/` directory. See `docs/ETHICS.md`. | `raw_data/` (not in repo) |
| `analyze_demo.py` | Demographic descriptives: age, gender, robot familiarity. | `data/demographics.csv` |
| `analyze_nars.py` | NARS subscale scoring, descriptives, inter-subscale correlations; joins per-trial responses and emits the long-format frame. | `data/nars.csv`, `data/trial_responses/` |
| `analyze_end_questions.py` | Seven figures from the exit questionnaire, plus a printed summary. | `data/exit_questionnaire.csv` |
| `plot_trajectory.py` | Interactive Plotly animation of robot and pedestrian paths. | `trajectory_inputs/*.json` |
| `power_analysis.py` | A-priori power curves for the within-subjects design. | none |

## Reproducing the participant configurations

The design is deterministic — participant *N* always gets Williams row
*N* mod 9, with no RNG anywhere. Pinning the date reproduces a committed set
exactly:

```bash
python generate_participant_configs.py -n 27 --date 2026-01-09 -o /tmp/check
# byte-identical to Assets/StreamingAssets/User Study/ParticipantJsons/P*.json
```

Without `--date`, `study_metadata.date_created` is stamped with today's date and
that one field will differ.

## What is *not* here

**The paper's inferential statistics.** These scripts compute descriptives,
correlations and power only — no ANOVA, no non-parametric tests, no mixed
models, no multiple-comparison correction. See
[`inferential/README.md`](inferential/README.md).

## Known issues in the analysis code

Carried over from the original study code and left visible rather than silently
patched, because they affect how published numbers were produced:

- **`analyze_nars.py`** declares `reversed_items = ["Q3", "Q5", "Q6"]` but never
  applies the reversal, while printing a header that claims items were
  reverse-scored. A code comment argues the survey form already presented those
  items on a flipped scale. Verify against your own instrument before reuse.
- **`power_analysis.py`** prints a header describing a **2 × 3** design and
  steps *N* in multiples of 6 — leftovers from an earlier two-trajectory
  version. The condition count in the code (3 × 3 = 9) is current.
- **`analyze_end_questions.py`** parses cue strings inside a bare
  `except: pass`. `deidentify.py` repairs the two malformed rows that used to be
  dropped silently, but the fragile parser remains.

## Directories ignored by git

`raw_data/`, `participant_configs/`, `launch_scripts/`, `results/`,
`end_question_plots/` and generated `*.html` are operator or output artefacts,
not source. Raw survey exports must never be committed — see `docs/ETHICS.md`.
