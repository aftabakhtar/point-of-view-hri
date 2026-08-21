"""Turn the raw survey exports into the de-identified dataset under data/.

The raw exports are Google Forms downloads containing a quasi-identifier set
(second-precision timestamp + age + gender + country of origin + primary
language + field of study) that is re-identifying in a cohort this small. They
are never committed; this script is the only sanctioned path from raw to
released data.

Usage:
    python deidentify.py --raw raw_data --out ../data

Expected inputs in --raw:
    demo.csv           demographics export
    nars.csv           NARS export
    end_user_all.csv   exit questionnaire, free text already coded into tags

Outputs in --out:
    demographics.csv
    nars.csv
    exit_questionnaire.csv

Per-trial response JSONs are copied separately, see collect_trial_responses().

Every transformation applied here is documented in docs/ETHICS.md.
"""

import argparse
import csv
import json
import os
import re
import shutil
import sys

# --- column names in the raw exports -----------------------------------------

COL_TIMESTAMP = "Timestamp"
COL_PID = "Participant ID (in numbers)"
COL_AGE = "Age (in numbers)"
COL_GENDER = "Gender"
COL_EDUCATION = (
    "Highest level of education completed "
    "(High school, Undergraduate, Graduate, Phd, other)"
)
COL_FIELD = "Field of Study (STEM, Arts, etc)"
COL_COUNTRY = "Country of Origin"
COL_LANGUAGE = "Primary Language"
COL_PRIOR_ROBOTS = "Prior experience interacting with robots"
COL_FAMILIARITY = (
    "Familiarity with mobile robots "
    "(eg. delivery robots, service robots, etc.)"
)
COL_VR = "Experience with Virtual/Augmented/Mixed Reality"

# --- disclosure-control mappings ---------------------------------------------

# Free-text field of study, collapsed to a binary. Several raw values ("Geography",
# "Ethology and AI") occur exactly once and are near-unique identifiers.
FIELD_TO_STEM = {
    "stem": "STEM",
    "steam": "STEM",
    "science": "STEM",
    "computer science": "STEM",
    "robotics": "STEM",
    "ethology and ai": "STEM",
    "geography": "STEM",          # natural science in this cohort's usage
    "arts": "non-STEM",
    "translation": "non-STEM",    # languages/humanities
}

# Free-text education, normalised to the three levels the form intended.
EDUCATION_NORMALISED = {
    "high school": "High school",
    "under graduate": "Undergraduate",
    "undergraduate": "Undergraduate",
    "graduate": "Graduate",
    "master": "Graduate",
    "master sc.": "Graduate",
    "master of science": "Graduate",
    "master of sciences": "Graduate",
    "master of science ": "Graduate",
}

VR_NORMALISED = {
    "never": "Never",
    "occassionally": "Occasionally",   # typo in the original form
    "occasionally": "Occasionally",
    "frequently": "Frequently",
}

# Rows whose participant ID was mistyped during collection. Keyed by the raw
# timestamp so the correction is auditable against the original export.
#
# The demographics export contains P006 twice and no P005. The second P006 row
# (2026-03-24 15:01:33) sits one minute after a P005 row in the NARS export
# (15:00:11) and is therefore the mislabelled one.
ID_CORRECTIONS_BY_TIMESTAMP = {
    "3/24/2026 15:01:33": "P005",
}

# Pilot, tester and demo runs. These are not study participants and are dropped
# from every released file, not merely annotated.
EXCLUDED_IDS = {"P100", "P013", "DEMO001"}

# Trials expected per participant: the practice trial plus the nine conditions.
EXPECTED_TRIALS = 10


def normalise_pid(raw):
    """P0xx, uppercased and zero-padded. Some rows were entered lower-case."""
    pid = (raw or "").strip().upper()
    m = re.fullmatch(r"P?0*(\d+)", pid)
    if not m:
        return pid
    return f"P{int(m.group(1)):03d}"


def exact_age(raw):
    """Exact age is retained deliberately.

    Banding it would make the mean and SD reported in the paper impossible to
    recompute from the released data, and reproducibility was judged to outweigh
    the additional disclosure risk. The risk is real but bounded: age plus gender
    singles out roughly a third of the cohort, whereas the fields that pushed
    that to 100% -- timestamp, country of origin, primary language -- remain
    dropped. See docs/ETHICS.md.
    """
    try:
        return str(int(str(raw).strip()))
    except (TypeError, ValueError):
        return ""


def lookup(table, value, label, path):
    key = (value or "").strip().lower()
    if key in table:
        return table[key]
    raise SystemExit(
        f"{path}: unmapped {label} value {value!r}.\n"
        f"Add it to the mapping in deidentify.py rather than guessing -- an "
        f"unreviewed value may be identifying."
    )


def read_csv(path):
    with open(path, newline="", encoding="utf-8-sig") as fh:
        return list(csv.DictReader(fh))


def write_csv(path, fieldnames, rows):
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    with open(path, "w", newline="", encoding="utf-8") as fh:
        writer = csv.DictWriter(fh, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)
    print(f"  wrote {path} ({len(rows)} rows)")


# --- demographics -------------------------------------------------------------

def deidentify_demographics(raw_path, out_path):
    rows = read_csv(raw_path)
    out_fields = [
        "participant_id", "age", "gender", "education",
        "field_of_study", "prior_robot_experience",
        "mobile_robot_familiarity", "vr_experience",
    ]

    seen, out_rows, corrections = {}, [], 0
    for row in rows:
        timestamp = (row.get(COL_TIMESTAMP) or "").strip()
        pid = normalise_pid(row.get(COL_PID))

        if timestamp in ID_CORRECTIONS_BY_TIMESTAMP:
            pid = ID_CORRECTIONS_BY_TIMESTAMP[timestamp]
            corrections += 1

        if pid in EXCLUDED_IDS:
            continue
        if pid in seen:
            raise SystemExit(
                f"{raw_path}: duplicate participant id {pid} that no correction "
                f"resolves (timestamps {seen[pid]!r} and {timestamp!r}).\n"
                f"Resolve it in ID_CORRECTIONS_BY_TIMESTAMP before releasing."
            )
        seen[pid] = timestamp

        out_rows.append({
            "participant_id": pid,
            # Timestamp, country of origin and primary language are dropped
            # entirely -- see docs/ETHICS.md.
            "age": exact_age(row.get(COL_AGE)),
            "gender": (row.get(COL_GENDER) or "").strip(),
            "education": lookup(EDUCATION_NORMALISED, row.get(COL_EDUCATION),
                                "education", raw_path),
            "field_of_study": lookup(FIELD_TO_STEM, row.get(COL_FIELD),
                                     "field of study", raw_path),
            "prior_robot_experience": (row.get(COL_PRIOR_ROBOTS) or "").strip(),
            "mobile_robot_familiarity": (row.get(COL_FAMILIARITY) or "").strip(),
            "vr_experience": lookup(VR_NORMALISED, row.get(COL_VR),
                                    "VR experience", raw_path),
        })

    out_rows.sort(key=lambda r: r["participant_id"])
    write_csv(out_path, out_fields, out_rows)
    print(f"  applied {corrections} participant-id correction(s)")
    return {r["participant_id"] for r in out_rows}


# --- NARS ---------------------------------------------------------------------

def deidentify_nars(raw_path, out_path):
    rows = read_csv(raw_path)
    with open(raw_path, newline="", encoding="utf-8-sig") as fh:
        header = next(csv.reader(fh))

    # Everything after Timestamp and the ID is a NARS item, in form order.
    item_cols = [c for c in header if c not in (COL_TIMESTAMP, COL_PID)]

    out_fields = ["participant_id"] + [f"Q{i}" for i in range(1, len(item_cols) + 1)]
    out_rows = []
    for row in rows:
        pid = normalise_pid(row.get(COL_PID))
        if pid in EXCLUDED_IDS:
            continue
        out = {"participant_id": pid}
        for i, col in enumerate(item_cols, start=1):
            out[f"Q{i}"] = (row.get(col) or "").strip()
        out_rows.append(out)

    out_rows.sort(key=lambda r: r["participant_id"])
    write_csv(out_path, out_fields, out_rows)

    # The item wording is part of the instrument, not participant data, so it is
    # preserved alongside rather than in the data file.
    codebook = os.path.join(os.path.dirname(out_path), "nars_items.txt")
    with open(codebook, "w", encoding="utf-8") as fh:
        fh.write("NARS items in form order (Nomura et al., 2006).\n")
        fh.write("Subscales: S1 = Q4,7,8,9,10,12  S2 = Q1,2,11,13,14  S3 = Q3,5,6\n\n")
        for i, col in enumerate(item_cols, start=1):
            fh.write(f"Q{i}\t{col.strip()}\n")
    print(f"  wrote {codebook} ({len(item_cols)} items)")
    return {r["participant_id"] for r in out_rows}


# --- exit questionnaire -------------------------------------------------------

# Two rows used '.' instead of ',' between cue tokens. The original parser did
# float("-1.more_distance"), raised, and swallowed it in a bare except -- so
# those cues silently vanished from every reported frequency.
MALFORMED_CUE = re.compile(r"([+-]?\d)\.(?=[a-zA-Z_]+:)")


def deidentify_exit(raw_path, out_path):
    rows = read_csv(raw_path)
    with open(raw_path, newline="", encoding="utf-8-sig") as fh:
        header = next(csv.reader(fh))

    pid_col = header[0]
    out_rows, repairs = [], 0
    for row in rows:
        pid = normalise_pid(row.get(pid_col))
        if pid in EXCLUDED_IDS:
            continue
        out = {"participant_id": pid}
        for col in header[1:]:
            value = (row.get(col) or "").strip()
            fixed = MALFORMED_CUE.sub(r"\1,", value)
            if fixed != value:
                repairs += 1
            out[col] = fixed
        out_rows.append(out)

    out_rows.sort(key=lambda r: r["participant_id"])
    write_csv(out_path, ["participant_id"] + header[1:], out_rows)
    print(f"  repaired {repairs} malformed cue string(s)")
    return {r["participant_id"] for r in out_rows}


# --- per-trial responses ------------------------------------------------------

def collect_trial_responses(results_dir, out_dir):
    """Copy per-trial response JSONs. These contain no personal data."""
    if not os.path.isdir(results_dir):
        print(f"  skipped: {results_dir} not found")
        return 0

    copied, incomplete = 0, []
    for pid in sorted(os.listdir(results_dir)):
        clean = normalise_pid(pid)
        if clean in EXCLUDED_IDS or pid.strip().upper() in EXCLUDED_IDS:
            continue
        src = os.path.join(results_dir, pid)
        if not os.path.isdir(src):
            continue
        names = sorted(n for n in os.listdir(src) if n.endswith(".json"))
        if not names:
            continue
        dst = os.path.join(out_dir, clean)
        os.makedirs(dst, exist_ok=True)
        for name in names:
            shutil.copy2(os.path.join(src, name), os.path.join(dst, name))
            copied += 1
        if len(names) != EXPECTED_TRIALS:
            incomplete.append((clean, len(names)))

    print(f"  copied {copied} trial response file(s) to {out_dir}")
    if incomplete:
        detail = ", ".join(f"{p} ({n}/{EXPECTED_TRIALS})" for p, n in incomplete)
        print(f"  WARNING incomplete sessions: {detail}")
    return copied


def default_results_dir():
    return os.path.join(
        os.path.expanduser("~"), "AppData", "LocalLow", "DefaultCompany",
        "robot-trajectory-pref-urp", "User Study", "Results",
    )


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="De-identify the raw survey exports into the released dataset.")
    parser.add_argument("--raw", default="raw_data",
                        help="directory holding the raw exports (default: %(default)s)")
    parser.add_argument("--out", default=os.path.join("..", "data"),
                        help="output directory (default: %(default)s)")
    parser.add_argument("--results", default=default_results_dir(),
                        help="per-participant trial results directory")
    parser.add_argument("--skip-trials", action="store_true",
                        help="do not copy per-trial response JSONs")
    parser.add_argument("--age-stats", action="store_true",
                        help="print age mean/SD computed from the raw export, "
                             "as a cross-check that the release reproduces the "
                             "figures reported in the paper")
    args = parser.parse_args(argv)

    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    if not os.path.isdir(args.raw):
        raise SystemExit(
            f"raw directory {args.raw!r} not found.\n"
            f"Place demo.csv, nars.csv and end_user_all.csv there. Raw exports "
            f"are gitignored and must never be committed."
        )

    os.makedirs(args.out, exist_ok=True)

    print("demographics:")
    demo_ids = deidentify_demographics(
        os.path.join(args.raw, "demo.csv"),
        os.path.join(args.out, "demographics.csv"))

    print("NARS:")
    nars_ids = deidentify_nars(
        os.path.join(args.raw, "nars.csv"),
        os.path.join(args.out, "nars.csv"))

    print("exit questionnaire:")
    exit_ids = deidentify_exit(
        os.path.join(args.raw, "end_user_all.csv"),
        os.path.join(args.out, "exit_questionnaire.csv"))

    if not args.skip_trials:
        print("trial responses:")
        collect_trial_responses(args.results,
                                os.path.join(args.out, "trial_responses"))

    if args.age_stats:
        ages = []
        for row in read_csv(os.path.join(args.raw, "demo.csv")):
            if normalise_pid(row.get(COL_PID)) in EXCLUDED_IDS:
                continue
            try:
                ages.append(int(str(row.get(COL_AGE)).strip()))
            except (TypeError, ValueError):
                pass
        if ages:
            mean = sum(ages) / len(ages)
            var = sum((a - mean) ** 2 for a in ages) / (len(ages) - 1)
            print(f"\nage: n={len(ages)} mean={mean:.1f} sd={var ** 0.5:.1f} "
                  f"range={min(ages)}-{max(ages)}")
            print(f"     paper reports: N=24 mean=27.3 sd=3.6")

    # Coverage report -- mismatches usually mean a mislabelled ID upstream.
    print("\ncoverage:")
    print(f"  demographics only: {sorted(demo_ids - nars_ids - exit_ids)}")
    print(f"  NARS only:         {sorted(nars_ids - demo_ids - exit_ids)}")
    print(f"  exit only:         {sorted(exit_ids - demo_ids - nars_ids)}")
    print(f"  in all three:      {len(demo_ids & nars_ids & exit_ids)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
