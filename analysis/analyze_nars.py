"""
NARS (Negative Attitudes toward Robots Scale) Analysis
Based on: Nomura et al. (2006) - Measurement of Negative Attitudes toward Robots

Scale structure (14 items, 1–5 Likert scale):
  S1 - Negative Attitudes toward Situations of Interaction with Robots (6 items)
  S2 - Negative Attitudes toward the Social Influence of Robots (5 items)
  S3 - Negative Attitudes toward Emotions in Interaction with Robots (3 items)

No reverse-scoring is applied. Standard NARS reverses Q3/Q5/Q6, but the form
used in this study already presented those three on a flipped scale, so the raw
values are keyed the same direction as the rest of the instrument. See the note
at section 3.
"""

import pandas as pd
import numpy as np

import _paths

# ---------------------------------------------------------------------------
# 1. Load data
# ---------------------------------------------------------------------------
df = pd.read_csv(_paths.require(_paths.NARS_CSV))

# Normalise participant IDs to uppercase for consistency (e.g. "p011" -> "P011")
df[_paths.PID] = df[_paths.PID].str.upper()
df = df.sort_values(_paths.PID).reset_index(drop=True)

# Shorter column aliases matching the 14 questionnaire items (Q1–Q14)
question_cols = [col for col in df.columns if col != _paths.PID]
q_labels = [f"Q{i+1}" for i in range(14)]
df.rename(columns=dict(zip(question_cols, q_labels)), inplace=True)

# ---------------------------------------------------------------------------
# 2. Define subscales
# ---------------------------------------------------------------------------
# From Table 1 in the paper (item numbers match Q1–Q14 order in the CSV):
#   Q1  – S2  | Q2  – S2  | Q3  – S3
#   Q4  – S1  | Q5  – S3  | Q6  – S3
#   Q7  – S1  | Q8  – S1  | Q9  – S1
#   Q10 – S1  | Q11 – S2  | Q12 – S1
#   Q13 – S2  | Q14 – S2

subscales = {
    "S1 – Situations of Interaction": ["Q4", "Q7", "Q8", "Q9", "Q10", "Q12"],
    "S2 – Social Influence":          ["Q1", "Q2", "Q11", "Q13", "Q14"],
    "S3 – Emotions in Interaction":   ["Q3", "Q5", "Q6"],
}

# ---------------------------------------------------------------------------
# 3. No reverse-scoring is applied.
#
#    Standard NARS reverses Q3, Q5 and Q6 because they are positively worded
#    ("I would feel relaxed talking with robots"). The form used in this study
#    already presented those three on a flipped scale -- "strongly agree" at 1,
#    "strongly disagree" at 5 -- so the raw values are keyed the same direction
#    as the rest of the instrument (higher = more negative attitude) and need no
#    transformation.
#
#    The inter-subscale correlations printed below corroborate this. All three
#    subscales measure negative attitudes and so must correlate positively, as
#    they do in Nomura et al. (2006): r(S1,S2)=+0.408, r(S1,S3)=+0.119,
#    r(S2,S3)=+0.165. Scored as-is, this sample reproduces that sign pattern
#    (+0.526, +0.328, +0.477). Applying the textbook reversal would flip S3 into
#    anti-correlation with S1 and S2, which is incoherent for subscales keyed
#    the same direction.
# ---------------------------------------------------------------------------
df_scored = df.copy()

# ---------------------------------------------------------------------------
# 4. Compute subscale scores per participant (sum of items in each subscale)
# ---------------------------------------------------------------------------
for name, items in subscales.items():
    df_scored[name] = df_scored[items].sum(axis=1)

# ---------------------------------------------------------------------------
# 5. Report results
# ---------------------------------------------------------------------------
subscale_names = list(subscales.keys())

print("=" * 70)
print("NARS ANALYSIS RESULTS")
print("=" * 70)

# --- Per-participant subscale scores ---
print("\n--- Per-Participant Subscale Scores ---\n")
participant_results = df_scored[[_paths.PID] + subscale_names].copy()
participant_results.columns = ["Participant", "S1 (max 30)", "S2 (max 25)", "S3 (max 15)"]
print(participant_results.to_string(index=False))

# --- Group-level descriptive statistics ---
print("\n\n--- Group-Level Descriptive Statistics (n={}) ---\n".format(len(df_scored)))

stats_rows = []
for name, items in subscales.items():
    scores = df_scored[name]
    n_items = len(items)
    max_score = n_items * 5
    min_score = n_items * 1
    stats_rows.append({
        "Subscale": name,
        "N items": n_items,
        "Possible range": f"{min_score}–{max_score}",
        "Mean": round(scores.mean(), 2),
        "SD":   round(scores.std(ddof=1), 2),
        "Min":  scores.min(),
        "Max":  scores.max(),
        "Median": scores.median(),
    })

stats_df = pd.DataFrame(stats_rows)
print(stats_df.to_string(index=False))

# --- Reference means from the paper (Japanese student sample, n=240) ---
print("\n\n--- Comparison with Paper's Reference Norms (Nomura et al., 2006, n=240) ---\n")
ref = {
    "S1 – Situations of Interaction": {"Paper Mean": 13.2, "Paper SD": 3.9},
    "S2 – Social Influence":          {"Paper Mean": 15.4, "Paper SD": 4.0},
    "S3 – Emotions in Interaction":   {"Paper Mean": 10.4, "Paper SD": 2.3},
}
comp_rows = []
for name in subscale_names:
    our_mean = round(df_scored[name].mean(), 2)
    our_sd   = round(df_scored[name].std(ddof=1), 2)
    paper_mean = ref[name]["Paper Mean"]
    paper_sd   = ref[name]["Paper SD"]
    diff = round(our_mean - paper_mean, 2)
    comp_rows.append({
        "Subscale": name,
        "Our Mean (SD)":   f"{our_mean} ({our_sd})",
        "Paper Mean (SD)": f"{paper_mean} ({paper_sd})",
        "Difference":      f"{diff:+.2f}",
    })
print(pd.DataFrame(comp_rows).to_string(index=False))

# --- Inter-subscale correlations ---
print("\n\n--- Inter-Subscale Pearson Correlations ---\n")
corr_df = df_scored[subscale_names].corr(method="pearson").round(3)
corr_df.index   = ["S1", "S2", "S3"]
corr_df.columns = ["S1", "S2", "S3"]
print(corr_df.to_string())
print("\n  Reference from paper: r(S1,S2)=0.408***, r(S1,S3)=0.119†, r(S2,S3)=0.165**")

# --- Item-level means for diagnostics ---
print("\n\n--- Item-Level Means (raw, no reverse-scoring applied) ---\n")
item_info = {
    "Q1":  ("S2", "I would feel uneasy if robots really had emotions."),
    "Q2":  ("S2", "Something bad might happen if robots developed into living beings."),
    "Q3":  ("S3", "I would feel relaxed talking with robots. [flipped on form]"),
    "Q4":  ("S1", "I would feel uneasy if I was given a job where I had to use robots."),
    "Q5":  ("S3", "If robots had emotions, I would be able to make friends with them. [flipped on form]"),
    "Q6":  ("S3", "I feel comforted being with robots that have emotions. [flipped on form]"),
    "Q7":  ("S1", "The word 'robot' means nothing to me."),
    "Q8":  ("S1", "I would feel nervous operating a robot in front of other people."),
    "Q9":  ("S1", "I would hate the idea that robots/AI were making judgments about things."),
    "Q10": ("S1", "I would feel very nervous just standing in front of a robot."),
    "Q11": ("S2", "If I depend on robots too much, something bad might happen."),
    "Q12": ("S1", "I would feel paranoid talking with a robot."),
    "Q13": ("S2", "I am concerned that robots would be a bad influence on children."),
    "Q14": ("S2", "I feel that in the future society will be dominated by robots."),
}
item_rows = []
for q, (sub, desc) in item_info.items():
    item_rows.append({
        "Item": q,
        "Subscale": sub.split(" ")[0],
        "Mean": round(df_scored[q].mean(), 2),
        "SD":   round(df_scored[q].std(ddof=1), 2),
        "Description": desc[:60],
    })
print(pd.DataFrame(item_rows).to_string(index=False))

print("\n" + "=" * 70)
print("NOTE: Higher scores = MORE negative attitudes toward robots.")
print("      S1 range: 6–30 | S2 range: 5–25 | S3 range: 3–15")
print("=" * 70)

# ---------------------------------------------------------------------------
# 6. Additional Analysis: Trial-Based Sociability & Disturbance Scores
# ---------------------------------------------------------------------------
import os
import json
from collections import defaultdict

# Sociability and disturbance are each the mean of four 1-7 items, matching the
# convention used in the paper. Set STUDY_SCORE_AS_SUM=1 to emit 4-28 totals
# instead; this rescales coefficients by 4 but leaves r, R^2 and p unchanged.
SCORE_AS_SUM = os.environ.get("STUDY_SCORE_AS_SUM", "").strip() not in ("", "0")
SCALE_LABEL = "max 28" if SCORE_AS_SUM else "1-7 mean"

results_dir = _paths.require(_paths.TRIAL_RESPONSES_DIR)

trajectory_map = {0: "A", 1: "B", 2: "C"}
camera_map = {3: "proximal", 5: "distal", 100: "allocentric"}

# Prepare structure
participant_trial_data = {}

for participant in os.listdir(results_dir):
    participant_path = os.path.join(results_dir, participant)

    if not os.path.isdir(participant_path):
        continue

    # Initialize all columns to 0
    data = defaultdict(float)

    for traj in ["A", "B", "C"]:
        for cam in ["proximal", "distal", "allocentric"]:
            data[f"{traj}-{cam}-sociability"] = 0
            data[f"{traj}-{cam}-disturbance"] = 0

    # Read all trial files
    for file in os.listdir(participant_path):
        if not file.endswith(".json") or "intro" in file:
            continue

        file_path = os.path.join(participant_path, file)

        with open(file_path, "r") as f:
            trial = json.load(f)

        traj = trajectory_map.get(trial["trajectory_id"])
        cam = camera_map.get(trial["camera_target_ped_id"])

        if traj is None or cam is None:
            continue

        questions = trial["questions"]

        # First 4 → sociability (Warm, Trustworthy, Likeable, Friendly),
        # last 4 → disturbance (Scary, Creepy, Uncanny, Weird).
        #
        # Reported as the MEAN of the four items, so scores stay on the 1-7
        # response scale. This is the convention the paper uses; scoring by sum
        # instead multiplies every coefficient by 4 (a regression slope of
        # -0.1527 becomes -0.6109) while leaving r, R^2 and p unchanged. Set
        # SCORE_AS_SUM to recover the 4-28 totals.
        sociability_items = [q["feedback_score"] for q in questions[:4]]
        disturbance_items = [q["feedback_score"] for q in questions[4:]]

        if SCORE_AS_SUM:
            sociability_score = sum(sociability_items)
            disturbance_score = sum(disturbance_items)
        else:
            sociability_score = sum(sociability_items) / len(sociability_items)
            disturbance_score = sum(disturbance_items) / len(disturbance_items)

        data[f"{traj}-{cam}-sociability"] += sociability_score
        data[f"{traj}-{cam}-disturbance"] += disturbance_score

    participant_trial_data[participant] = data

# ---------------------------------------------------------------------------
# Merge with NARS results
# ---------------------------------------------------------------------------
extended_rows = []

for _, row in df_scored.iterrows():
    pid = row[_paths.PID]

    base = {
        "Participant": pid,
        "S1 (max 30)": row[subscale_names[0]],
        "S2 (max 25)": row[subscale_names[1]],
        "S3 (max 15)": row[subscale_names[2]],
    }

    trial_data = participant_trial_data.get(pid, {})

    # Add all combinations
    for traj in ["A", "B", "C"]:
        for cam in ["proximal", "distal", "allocentric"]:
            base[f"{traj}-{cam}-sociability ({SCALE_LABEL})"] = trial_data.get(f"{traj}-{cam}-sociability", 0)
            base[f"{traj}-{cam}-disturbance ({SCALE_LABEL})"] = trial_data.get(f"{traj}-{cam}-disturbance", 0)

    extended_rows.append(base)

extended_df = pd.DataFrame(extended_rows)

# ---------------------------------------------------------------------------
# Print results
# ---------------------------------------------------------------------------
print("\n\n" + "=" * 70)
print("EXTENDED NARS + TRAJECTORY ANALYSIS")
print("=" * 70)
print("\n--- Per-Participant Extended Scores ---\n")
print(extended_df.to_string(index=False))

# ---------------------------------------------------------------------------
# Convert to LONG FORMAT (for statistics)
# ---------------------------------------------------------------------------
long_rows = []

for _, row in extended_df.iterrows():
    participant = row["Participant"]

    for traj in ["A", "B", "C"]:
        for cam in ["proximal", "distal", "allocentric"]:
            for sub in ["sociability", "disturbance"]:
                
                col_name = f"{traj}-{cam}-{sub} ({SCALE_LABEL})"
                
                long_rows.append({
                    "Participant": participant,
                    "Trajectory": traj,
                    "Camera": cam,
                    "Subscale": sub,
                    "Score": row[col_name]
                })

long_df = pd.DataFrame(long_rows)
extended_df.to_csv(os.path.join(_paths.output_dir(), "extended_nars_results.csv"), index=False)
long_df.to_csv(os.path.join(_paths.output_dir(), "extended_nars_long_format.csv"), index=False)