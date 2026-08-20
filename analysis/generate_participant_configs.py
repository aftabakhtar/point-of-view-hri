import argparse
import json
import os
import sys
from datetime import datetime

# ============================================================================
# CONFIGURATION SECTION - EDIT THESE VARIABLES
# ============================================================================

# Study Settings
TOTAL_PARTICIPANTS = 27  # Must be multiple of 9 for William's Design with 9 conditions
STUDY_VERSION = "1.0"
OUTPUT_FOLDER = "participant_configs"
OUTPUT_FOLDER_BAT = "launch_scripts"

# Executable Settings (for batch file generation)
EXE_NAME = "robot-trajectory-pref-urp.exe"  # Name of your built executable

# Stamped into study_metadata.date_created. Overridable via --date so a
# regenerated config can be compared byte-for-byte against a committed one.
DATE_CREATED = datetime.now().strftime("%Y-%m-%d")

# Pedestrian Configurations (same for all participants)
PEDESTRIANS = [
    {
        "ped_id": 0,
        "start_position": {"x": -0.1072, "y": 0.00, "z": -1.58},
        "start_orientation": {"x": 0.0, "y": -90.0, "z": 0.0}
    },
    {
        "ped_id": 1,
        "start_position": {"x": 0.0, "y": 0.0, "z": -0.7973022},
        "start_orientation": {"x": 0.0, "y": -90.0, "z": 0.0}
    },
    {
        "ped_id": 2,
        "start_position": {"x": -0.04098, "y": 0.0, "z": 0.01169777},
        "start_orientation": {"x": 0.0, "y": -90, "z": 0.0}
    },
    {
        "ped_id": 3,
        "start_position": {"x": 1.367, "y": 0.0, "z": 0.4526},
        "start_orientation": {"x": 0.0, "y": 90, "z": 0.0}
    },
    {
        "ped_id": 4,
        "start_position": {"x": 1.26, "y": 0.0, "z": -0.33},
        "start_orientation": {"x": 0.0, "y": 90, "z": 0.0}
    },
    {
        "ped_id": 5,
        "start_position": {"x": 1.3, "y": 0.0, "z": -1.1389},
        "start_orientation": {"x": 0.0, "y": 90, "z": 0.0}
    },
    {
        "ped_id": 100,
        "start_position": {"x": -57.3, "y": 0.0, "z": -241.1},
        "start_orientation": {"x": 0.0, "y": 180, "z": 0.0}
    }
]

# Robot Trajectories (same for all participants) - NOW WITH 3 TRAJECTORIES
ROBOT_TRAJECTORIES = [
    {
        "trajectory_id": 0,
        "trajectory_label": "A",
        "trajectory_path": "Trajectories/our_w_nod_0_02.json"
    },
    {
        "trajectory_id": 1,
        "trajectory_label": "B",
        "trajectory_path": "Trajectories/our_wo_nod_0_02.json"
    },
    {
        "trajectory_id": 2,
        "trajectory_label": "C",
        "trajectory_path": "Trajectories/dwa_9s_0_02.json"
    },
    {
        "trajectory_id": 100,
        "trajectory_label": "Intro",
        "trajectory_path": "Trajectories/Intro.json"
    }
]

# Camera Viewpoints (pedestrian IDs to use as camera targets)
# 100 is the special ID for top-down view
CAMERA_VIEWPOINTS = [3, 5, 100]  # Three different viewpoints

# Camera type mapping
CAMERA_TYPE_MAP = {
    100: "top_down",  # Special case for top-down camera
}

# Trial duration in seconds
TRIAL_DURATION = 10

# Questionnaire path (same for all participants)
QUESTIONNAIRE_PATH = "User Study/questionnaire.json"

# ============================================================================
# WILLIAM'S DESIGN (BALANCED LATIN SQUARE) GENERATION - 9×9 MATRIX
# ============================================================================

def generate_williams_design_9():
    """
    Generate a standard 9×9 Williams Design (Balanced Latin Square).
    
    For 9 conditions (3 trajectories × 3 viewpoints), this creates 9 sequences
    where each condition appears in each position once, and each condition
    follows every other condition exactly once across all sequences.
    
    Returns:
        List of 9 sequences, each containing the order [0,1,2,3,4,5,6,7,8] rearranged
    """
    # Standard 9×9 Williams Design Latin Square
    # Each row is a different participant sequence
    # Each column represents a trial position
    # Numbers 0-8 represent the 9 conditions (T0V0, T0V1, T0V2, T1V0, T1V1, T1V2, T2V0, T2V1, T2V2)
    
    williams_square = [
        [0, 1, 8, 2, 4, 7, 5, 3, 6],  # Sequence 1
        [1, 2, 0, 3, 5, 8, 6, 4, 7],  # Sequence 2
        [2, 3, 1, 4, 6, 0, 7, 5, 8],  # Sequence 3
        [3, 4, 2, 5, 7, 1, 8, 6, 0],  # Sequence 4
        [4, 5, 3, 6, 8, 2, 0, 7, 1],  # Sequence 5
        [5, 6, 4, 7, 0, 3, 1, 8, 2],  # Sequence 6
        [6, 7, 5, 8, 1, 4, 2, 0, 3],  # Sequence 7
        [7, 8, 6, 0, 2, 5, 3, 1, 4],  # Sequence 8
        [8, 0, 7, 1, 3, 6, 4, 2, 5],  # Sequence 9
    ]
    
    return williams_square

def get_camera_type(ped_id):
    """Determine camera type based on pedestrian ID."""
    return CAMERA_TYPE_MAP.get(ped_id, "pedestrian")

def create_condition_list():
    """
    Create the list of all 9 conditions: 3 trajectories × 3 viewpoints.
    
    Returns:
        List of 9 condition dictionaries
    """
    conditions = []
    trajectory_ids = [0, 1, 2]  # NOW THREE TRAJECTORIES
    
    # Create all combinations: T0V0, T0V1, T0V2, T1V0, T1V1, T1V2, T2V0, T2V1, T2V2
    for traj_id in trajectory_ids:
        for view_id in CAMERA_VIEWPOINTS:
            conditions.append({
                "trajectory_id": traj_id,
                "camera_target_ped_id": view_id,
                "camera_type": get_camera_type(view_id),
                "label": f"T{traj_id}V{view_id}"  # For debugging
            })
    
    return conditions

def create_trial_order_williams(participant_num):
    """
    Create trial order for a participant using William's Design.
    
    Args:
        participant_num: Participant number (0-indexed)
    
    Returns:
        List of trials with properly ordered conditions
    """
    # Get the Williams Design sequences (9 sequences for 9 conditions)
    williams_sequences = generate_williams_design_9()
    
    # Get all condition combinations
    conditions = create_condition_list()
    
    # Determine which sequence this participant gets (cycles every 9 participants)
    sequence_idx = participant_num % 9
    sequence = williams_sequences[sequence_idx]
    
    # Create trials in the order specified by the sequence
    trials = []

    # Adding introduction trial
    trials.append({
        "trial_id": -1,
        "trajectory_id": 100,
        "camera_type": "pedestrian",
        "camera_target_ped_id": 4,
        "duration_seconds": 10
    })

    for trial_idx, condition_idx in enumerate(sequence):
        condition = conditions[condition_idx]
        duration = TRIAL_DURATION if condition["camera_type"] != "top_down" else TRIAL_DURATION + 6 # Longer for top-down if needed

        trial = {
            "trial_id": trial_idx,
            "trajectory_id": condition["trajectory_id"],
            "camera_type": condition["camera_type"],
            "camera_target_ped_id": condition["camera_target_ped_id"],
            "duration_seconds": duration
        }
        trials.append(trial)
    
    return trials, sequence_idx

# ============================================================================
# JSON GENERATION
# ============================================================================

def generate_participant_json(participant_num):
    """
    Generate a complete JSON configuration for one participant.
    
    Args:
        participant_num: Participant number (0-indexed)
    
    Returns:
        Tuple of (config_dict, sequence_number)
    """
    participant_id = f"P{participant_num + 1:03d}"  # P001, P002, etc.
    
    # Create trials using William's Design
    trials, sequence_idx = create_trial_order_williams(participant_num)
    
    # Build the complete JSON structure
    participant_config = {
        "participant_id": participant_id,
        "study_metadata": {
            "study_version": STUDY_VERSION,
            "date_created": DATE_CREATED,
            "counterbalancing_sequence": sequence_idx + 1
        },
        "pedestrians": PEDESTRIANS,
        "robot_trajectories": ROBOT_TRAJECTORIES,
        "trials": trials,
        "questionnaire_path": QUESTIONNAIRE_PATH
    }
    
    return participant_config, sequence_idx

def generate_batch_file(participant_id):
    """
    Generate a Windows batch file to launch the study for a specific participant.
    
    Args:
        participant_id: Participant ID string (e.g., "P001")
    
    Returns:
        String containing the batch file content
    """
    batch_content = f"""@echo off
title VR Study - Participant {participant_id}
echo Starting VR Study for Participant {participant_id}...
echo.
echo Please wait while the application loads...
echo.

"{EXE_NAME}" -participantID {participant_id}

if errorlevel 1 (
    echo.
    echo Application exited with an error.
    pause
)
"""
    return batch_content

def save_participant_configs():
    """Generate and save JSON files for all participants."""
    
    # Create output folders if they don't exist
    os.makedirs(OUTPUT_FOLDER, exist_ok=True)
    os.makedirs(OUTPUT_FOLDER_BAT, exist_ok=True)
    
    # Generate and save each participant's JSON and BAT file
    print(f"Generating {TOTAL_PARTICIPANTS} participant configurations...")
    print(f"Counterbalancing {len([t for t in ROBOT_TRAJECTORIES if t['trajectory_id'] != 100])} trajectories × {len(CAMERA_VIEWPOINTS)} viewpoints")
    print(f"= 9 conditions using Williams Design (9 unique sequences)\n")
    
    if TOTAL_PARTICIPANTS % 9 != 0:
        print(f"WARNING: Total participants ({TOTAL_PARTICIPANTS}) is not a multiple of 9.")
        print("For perfect counterbalancing, use a multiple of 9 (e.g., 9, 18, 27, 36, 45).")
        print("Some sequences will be used more than others.\n")
    
    for i in range(TOTAL_PARTICIPANTS):
        config, seq_idx = generate_participant_json(i)
        participant_id = config['participant_id']
        
        # Save JSON file
        json_filename = f"{participant_id}.json"
        json_filepath = os.path.join(OUTPUT_FOLDER, json_filename)
        
        with open(json_filepath, 'w', encoding='utf-8') as f:
            json.dump(config, f, indent=2, ensure_ascii=False)
        
        # Save BAT file
        bat_filename = f"Launch_{participant_id}.bat"
        bat_filepath = os.path.join(OUTPUT_FOLDER_BAT, bat_filename)
        
        with open(bat_filepath, 'w', encoding='utf-8') as f:
            f.write(generate_batch_file(participant_id))
        
        if i < 3 or i >= TOTAL_PARTICIPANTS - 1:  # Print first 3 and last
            print(f"✓ Generated: {json_filename} + {bat_filename} (Sequence {seq_idx + 1})")
        elif i == 3:
            print(f"  ... (generating {TOTAL_PARTICIPANTS - 4} more) ...")
    
    print(f"\n✓ All {TOTAL_PARTICIPANTS} JSON files saved to '{OUTPUT_FOLDER}/' folder")
    print(f"✓ All {TOTAL_PARTICIPANTS} batch files saved to '{OUTPUT_FOLDER_BAT}/' folder")
    
    # Print summary of counterbalancing
    print("\n" + "="*80)
    print("COUNTERBALANCING SUMMARY")
    print("="*80)
    print("9 Conditions:")
    conditions = create_condition_list()
    for i, cond in enumerate(conditions):
        print(f"  {i}: Trajectory {cond['trajectory_id']} × Viewpoint {cond['camera_target_ped_id']} ({cond['camera_type']})")
    
    print(f"\nTotal sequences: 9")
    print(f"Each sequence used: {TOTAL_PARTICIPANTS // 9} times")
    if TOTAL_PARTICIPANTS % 9 != 0:
        print(f"Extra participants: {TOTAL_PARTICIPANTS % 9} (will repeat first {TOTAL_PARTICIPANTS % 9} sequences)")
    
    # Show trial order for first 9 participants (all unique sequences)
    print("\n" + "="*80)
    print("TRIAL ORDERS FOR ALL 9 SEQUENCES")
    print("="*80)
    
    for p in range(min(9, TOTAL_PARTICIPANTS)):
        config, seq_idx = generate_participant_json(p)
        print(f"\n{config['participant_id']} (Sequence {seq_idx + 1}):")
        trials = [t for t in config['trials'] if t['trial_id'] >= 0]  # Exclude intro trial
        for trial in trials:
            traj = trial['trajectory_id']
            view = trial['camera_target_ped_id']
            cam_type = trial['camera_type']
            print(f"  Trial {trial['trial_id']}: T{traj}V{view} (Trajectory {traj}, Viewpoint {view}, {cam_type})")
    
    # Verify counterbalancing
    print("\n" + "="*80)
    print("VERIFICATION: Each condition appears in each position across sequences")
    print("="*80)
    
    # Create position analysis
    williams_sequences = generate_williams_design_9()
    all_verified = True
    for position in range(9):
        conditions_in_position = [williams_sequences[seq][position] for seq in range(9)]
        is_complete = sorted(conditions_in_position) == list(range(9))
        all_verified = all_verified and is_complete
        print(f"Position {position}: {sorted(conditions_in_position)} (all conditions present: {is_complete})")
    
    if all_verified:
        print("\n✓ VERIFICATION PASSED: Perfect counterbalancing confirmed!")
    else:
        print("\n✗ VERIFICATION FAILED: Counterbalancing issue detected!")
    
    # Batch file instructions
    print("\n" + "="*80)
    print("BATCH FILE USAGE INSTRUCTIONS")
    print("="*80)
    print(f"1. Copy all .bat files from '{OUTPUT_FOLDER_BAT}/' to your built project folder")
    print(f"   (the folder containing '{EXE_NAME}')")
    print(f"2. Double-click 'Launch_P001.bat' to start the study for participant P001")
    print(f"3. The batch file will automatically pass the participant ID to the executable")
    print(f"4. If there's an error, the window will stay open so you can see the message")
    print(f"\nExample batch files created:")
    print(f"  - Launch_P001.bat")
    print(f"  - Launch_P002.bat")
    print(f"  - ... (up to Launch_P{TOTAL_PARTICIPANTS:03d}.bat)")

# ============================================================================
# DEMO CONFIGURATION GENERATION
# ============================================================================

# Demo Settings
DEMO_PARTICIPANT_ID = "DEMO001"
DEMO_QUESTIONNAIRE_PATH = "User Study/questionnaire_demo.json"

# 3 trials: A (ped 3, 3D), C (ped 3, 3D), A (top-down)
DEMO_TRIALS = [
    {
        "trial_id": 0,
        "trajectory_id": 0,
        "camera_type": "pedestrian",
        "camera_target_ped_id": 3,
        "duration_seconds": TRIAL_DURATION
    },
    {
        "trial_id": 1,
        "trajectory_id": 2,
        "camera_type": "pedestrian",
        "camera_target_ped_id": 3,
        "duration_seconds": TRIAL_DURATION
    },
    {
        "trial_id": 2,
        "trajectory_id": 0,
        "camera_type": "top_down",
        "camera_target_ped_id": 100,
        "duration_seconds": TRIAL_DURATION + 6
    }
]

# Demo uses only trajectories A and C (no B, no Intro)
DEMO_ROBOT_TRAJECTORIES = [t for t in ROBOT_TRAJECTORIES if t["trajectory_id"] in (0, 2)]

# Set to False to skip the 2D intro dialog in the demo; True to show it
DEMO_SHOW_2D_INTRO_DIALOG = False


def generate_demo_config():
    """Generate the JSON configuration for the demo participant."""
    return {
        "participant_id": DEMO_PARTICIPANT_ID,
        "study_metadata": {
            "study_version": STUDY_VERSION,
            "date_created": DATE_CREATED,
            "counterbalancing_sequence": 0
        },
        "pedestrians": PEDESTRIANS,
        "robot_trajectories": DEMO_ROBOT_TRAJECTORIES,
        "trials": DEMO_TRIALS,
        "questionnaire_path": DEMO_QUESTIONNAIRE_PATH,
        "show_2d_intro_dialog": DEMO_SHOW_2D_INTRO_DIALOG
    }


def generate_demo_batch_file():
    """Generate the batch file content for the demo."""
    return f"""@echo off
title VR Study - Demo

:: Clear ONLY the DEMO001 results subfolder so every demo run starts fresh.
:: This path is scoped to \\Results\\DEMO001 and will not touch any P001-P027 data.
set RESULTS_PATH=%USERPROFILE%\\AppData\\LocalLow\\DefaultCompany\\{EXE_NAME.replace('.exe', '')}\\User Study\\Results\\{DEMO_PARTICIPANT_ID}
if exist "%RESULTS_PATH%" (
    echo Clearing previous demo results at:
    echo   %RESULTS_PATH%
    rmdir /s /q "%RESULTS_PATH%"
    echo Done.
    echo.
)

echo Starting VR Study Demo...
echo.
echo Please wait while the application loads...
echo.

"{EXE_NAME}" -participantID {DEMO_PARTICIPANT_ID}

if errorlevel 1 (
    echo.
    echo Application exited with an error.
    pause
)
"""


def save_demo_configs():
    """Generate and save the demo JSON and batch file."""
    os.makedirs(OUTPUT_FOLDER, exist_ok=True)
    os.makedirs(OUTPUT_FOLDER_BAT, exist_ok=True)

    config = generate_demo_config()

    json_filepath = os.path.join(OUTPUT_FOLDER, f"{DEMO_PARTICIPANT_ID}.json")
    with open(json_filepath, 'w', encoding='utf-8') as f:
        json.dump(config, f, indent=2, ensure_ascii=False)

    bat_filepath = os.path.join(OUTPUT_FOLDER_BAT, f"Launch_{DEMO_PARTICIPANT_ID}.bat")
    with open(bat_filepath, 'w', encoding='utf-8') as f:
        f.write(generate_demo_batch_file())

    print(f"✓ Generated: {DEMO_PARTICIPANT_ID}.json + Launch_{DEMO_PARTICIPANT_ID}.bat")
    print(f"  Trials: {len(DEMO_TRIALS)} (3D pedestrian only, no intro)")
    print(f"  Questionnaire: {DEMO_QUESTIONNAIRE_PATH}")
    for trial in DEMO_TRIALS:
        traj_label = next(t["trajectory_label"] for t in DEMO_ROBOT_TRAJECTORIES if t["trajectory_id"] == trial["trajectory_id"])
        print(f"  Trial {trial['trial_id']}: Trajectory {traj_label}, Ped {trial['camera_target_ped_id']}, {trial['duration_seconds']}s")


# ============================================================================
# MAIN EXECUTION
# ============================================================================

def build_arg_parser():
    parser = argparse.ArgumentParser(
        description=(
            "Generate per-participant study configurations and Windows launch "
            "scripts. Trial order comes from a 9x9 Williams design (balanced "
            "Latin square) over 3 trajectories x 3 viewpoints, so the sequence "
            "is deterministic: participant N always receives row N mod 9."
        ),
        epilog=(
            "To regenerate the configs Unity actually reads, point --output at "
            "the StreamingAssets folder:\n"
            "  python generate_participant_configs.py "
            '--output "../Assets/StreamingAssets/User Study/ParticipantJsons"'
        ),
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "-n", "--participants", type=int, default=TOTAL_PARTICIPANTS,
        help="number of participant configs to generate; a multiple of 9 keeps "
             "the design balanced (default: %(default)s)")
    parser.add_argument(
        "-o", "--output", default=OUTPUT_FOLDER,
        help="directory for the participant JSON files (default: %(default)s)")
    parser.add_argument(
        "--launch-output", default=OUTPUT_FOLDER_BAT,
        help="directory for the .bat launchers (default: %(default)s)")
    parser.add_argument(
        "--trial-duration", type=int, default=TRIAL_DURATION,
        help="seconds per egocentric trial; top-down trials get 6 s more "
             "(default: %(default)s)")
    parser.add_argument(
        "--study-version", default=STUDY_VERSION,
        help="value written to study_metadata.study_version (default: %(default)s)")
    parser.add_argument(
        "--exe-name", default=EXE_NAME,
        help="executable the launchers should invoke (default: %(default)s)")
    parser.add_argument(
        "--date", default=DATE_CREATED, metavar="YYYY-MM-DD",
        help="value written to study_metadata.date_created; pin it to "
             "reproduce a previously generated set (default: today)")
    parser.add_argument(
        "--no-demo", action="store_true",
        help="skip the short DEMO001 walkthrough config")
    parser.add_argument(
        "--demo-only", action="store_true",
        help="generate only the DEMO001 walkthrough config")
    return parser


def main(argv=None):
    global TOTAL_PARTICIPANTS, OUTPUT_FOLDER, OUTPUT_FOLDER_BAT
    global TRIAL_DURATION, STUDY_VERSION, EXE_NAME, DATE_CREATED

    args = build_arg_parser().parse_args(argv)

    # The progress output uses check marks and a multiplication sign, which the
    # default cp1252 console on Windows cannot encode.
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    # The generator functions read these as module-level constants, so bind the
    # parsed values here rather than threading an options object through all of
    # them.
    TOTAL_PARTICIPANTS = args.participants
    OUTPUT_FOLDER = args.output
    OUTPUT_FOLDER_BAT = args.launch_output
    TRIAL_DURATION = args.trial_duration
    STUDY_VERSION = args.study_version
    EXE_NAME = args.exe_name
    DATE_CREATED = args.date

    if not args.demo_only:
        save_participant_configs()
        print()
    if not args.no_demo:
        save_demo_configs()
    return 0


if __name__ == "__main__":
    sys.exit(main())