# Inferential statistics

**This directory is intentionally empty.**

The scripts in `analysis/` compute descriptives, Pearson correlations between
NARS subscales, and an a-priori power analysis. They do **not** contain the
inferential tests behind the significance claims in the paper — those were run
outside this repository and are not yet included here.

Concretely, nothing in this repo currently performs:

- a repeated-measures ANOVA over trajectory × viewpoint,
- non-parametric equivalents (Friedman, Wilcoxon signed-rank),
- a linear mixed-effects model with participant as a random effect,
- or any multiple-comparison correction.

## What is already in place

`analyze_nars.py` assembles the long-format frame such a model would consume and
writes it to `extended_nars_long_format.csv` with columns:

| Column | Meaning |
|---|---|
| `Participant` | pseudonymous ID (`P001`…) |
| `Trajectory` | `A` (ours + head nod), `B` (ours), `C` (DWA baseline) |
| `Camera` | `proximal`, `distal`, `allocentric` |
| `Subscale` | NARS S1/S2/S3, or the derived sociability / disturbance scores |
| `Score` | summed item score |

That file is the intended input for whatever is added here.

## Contributing the analysis

Drop the scripts in this directory, add any new dependencies to
`analysis/requirements.txt`, and describe in this README which table or figure of
the paper each script reproduces. Until then, please treat the paper as the
authoritative source for all reported statistics.
