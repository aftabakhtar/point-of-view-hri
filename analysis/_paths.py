"""Shared path resolution for the analysis scripts.

Keeps every script working regardless of the directory it is invoked from, and
gives a single place to repoint at a different dataset.

Override with environment variables:
    STUDY_DATA_DIR      dataset location   (default: <repo>/data)
    STUDY_OUTPUT_DIR    generated figures  (default: <repo>/analysis/output)
"""

import os
import sys

# These scripts print box-drawing characters, check marks and en dashes, which
# the default cp1252 console on Windows cannot encode. Importing this module is
# enough to make that safe.
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

_HERE = os.path.dirname(os.path.abspath(__file__))
_REPO = os.path.dirname(_HERE)

DATA_DIR = os.environ.get("STUDY_DATA_DIR", os.path.join(_REPO, "data"))
OUTPUT_DIR = os.environ.get("STUDY_OUTPUT_DIR", os.path.join(_HERE, "output"))

DEMOGRAPHICS_CSV = os.path.join(DATA_DIR, "demographics.csv")
NARS_CSV = os.path.join(DATA_DIR, "nars.csv")
EXIT_CSV = os.path.join(DATA_DIR, "exit_questionnaire.csv")
TRIAL_RESPONSES_DIR = os.path.join(DATA_DIR, "trial_responses")

PID = "participant_id"


def require(path):
    """Fail with a pointer to the generation step rather than a bare IOError."""
    if not os.path.exists(path):
        raise SystemExit(
            f"missing: {path}\n\n"
            f"The dataset is not in the repository by default. Generate it with:\n"
            f"    cd analysis && python deidentify.py --raw raw_data --out ../data\n"
            f"See data/README.md and docs/ETHICS.md."
        )
    return path


def output_dir(name=""):
    """Create and return a directory under OUTPUT_DIR."""
    path = os.path.join(OUTPUT_DIR, name) if name else OUTPUT_DIR
    os.makedirs(path, exist_ok=True)
    return path
