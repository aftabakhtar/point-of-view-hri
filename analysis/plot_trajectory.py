import json
import plotly.graph_objects as go
import numpy as np
import os

# ============================================
# CONFIGURATION - Set these flags
# ============================================
PRIMARY_JSON = 'plot_trajectory/dwa_9s_0_02.json'
SECONDARY_JSON = 'plot_trajectory/dwa.json'
TERTIARY_JSON = 'plot_trajectory/dwa_9s.json'
PLOT_SECONDARY = False
PLOT_TERTIARY = False

BEHAVIOR_LABEL = 'C'

# Legend labels for the robot paths
PRIMARY_LABEL = 'Robot'
SECONDARY_LABEL = 'DWA'
TERTIARY_LABEL = 'DWA 9s'

# ============================================
# SPEEDUP CONFIGURATION
# ============================================
SPEEDUP_FACTOR = 1.0  # Increase this to make animation faster (e.g., 2.0 = 2x speed, 5.0 = 5x speed)
FRAME_SKIP = 3  # Skip every N frames to reduce total number of frames (higher = faster but choppier)

# ============================================
# Robot and pedestrian parameters
# ============================================
robot_speed = 0.8  # m/s
ped_speed = 1.05  # m/s

# Pedestrian definitions (same for all scenarios)
pedestrians = [
    # RIGHT side group (moving LEFT, y=-90)
    {"ped_id": 0, "start_position": {"x": 68.43069, "y": 0.0, "z": 34.5},   "start_orientation": {"x": 0.0, "y": -90.0, "z": 0.0}},
    {"ped_id": 1, "start_position": {"x": 68.5379,  "y": 0.0, "z": 35.2827}, "start_orientation": {"x": 0.0, "y": -90.0, "z": 0.0}},
    {"ped_id": 2, "start_position": {"x": 68.49692, "y": 0.0, "z": 36.0917}, "start_orientation": {"x": 0.0, "y": -90.0, "z": 0.0}},
    # Left side group (moving RIGHT, y=90)
    {"ped_id": 3, "start_position": {"x": 48.237, "y": 0.0, "z": 33.1126}, "start_orientation": {"x": 0.0, "y": 90.0, "z": 0.0}},
    {"ped_id": 4, "start_position": {"x": 48.13,  "y": 0.0, "z": 32.33},   "start_orientation": {"x": 0.0, "y": 90.0, "z": 0.0}},
    {"ped_id": 5, "start_position": {"x": 48.17,  "y": 0.0, "z": 31.5211}, "start_orientation": {"x": 0.0, "y": 90.0, "z": 0.0}},
]

# ============================================
# Helper Functions
# ============================================
def calculate_distance(x1, z1, x2, z2):
    return np.sqrt((x2 - x1)**2 + (z2 - z1)**2)

def load_robot_trajectory(json_path):
    """Load robot trajectory from JSON file"""
    with open(json_path, 'r') as f:
        data = json.load(f)
    
    robot_x = [point['position']['x'] for point in data['points']]
    robot_z = [point['position']['z'] for point in data['points']]
    
    distances = [calculate_distance(robot_x[i], robot_z[i], robot_x[i+1], robot_z[i+1]) 
                 for i in range(len(robot_x) - 1)]
    total_distance = sum(distances)
    
    cumulative_distances = [0] + [sum(distances[:i+1]) for i in range(len(distances))]
    frame_times = [d / robot_speed for d in cumulative_distances]
    total_time = frame_times[-1]
    
    return {
        'x': robot_x,
        'z': robot_z,
        'times': frame_times,
        'total_time': total_time,
        'total_distance': total_distance
    }

def get_ped_position(start_x, start_z, orientation_y, time_elapsed):
    """Calculate pedestrian position based on orientation and time"""
    direction_rad = np.radians(orientation_y)
    distance_traveled = ped_speed * time_elapsed
    new_x = start_x + distance_traveled * np.sin(direction_rad)
    new_z = start_z + distance_traveled * np.cos(direction_rad)
    return new_x, new_z

def compute_ped_trajectories(frame_times):
    """Precompute all pedestrian trajectories"""
    ped_trajectories = []
    for ped in pedestrians:
        ped_id = ped["ped_id"]
        start_x = ped["start_position"]["x"]
        start_z = ped["start_position"]["z"]
        orientation_y = ped["start_orientation"]["y"]

        ped_x = []
        ped_z = []

        for t in frame_times:
            x, z = get_ped_position(start_x, start_z, orientation_y, t)
            ped_x.append(x)
            ped_z.append(z)

        ped_trajectories.append({
            "id": ped_id,
            "x": ped_x,
            "z": ped_z,
            "orientation": orientation_y,
            "group": "Left" if orientation_y == 90 else "Right"
        })
    return ped_trajectories

# ============================================
# Load trajectories
# ============================================
print(f"Loading primary trajectory: {PRIMARY_JSON}")
trajectory_1 = load_robot_trajectory(PRIMARY_JSON)

trajectory_2 = None
if PLOT_SECONDARY and SECONDARY_JSON:
    print(f"Loading secondary trajectory: {SECONDARY_JSON}")
    trajectory_2 = load_robot_trajectory(SECONDARY_JSON)

trajectory_3 = None
if PLOT_TERTIARY and TERTIARY_JSON:
    print(f"Loading tertiary trajectory: {TERTIARY_JSON}")
    trajectory_3 = load_robot_trajectory(TERTIARY_JSON)

# Use the longest trajectory for timing if comparing
trajectories = [trajectory_1]
if trajectory_2:
    trajectories.append(trajectory_2)
if trajectory_3:
    trajectories.append(trajectory_3)

max_time = max(t['total_time'] for t in trajectories)
frame_times = max(trajectories, key=lambda t: len(t['times']))['times']

# Apply frame skipping to reduce number of frames
frame_times = frame_times[::FRAME_SKIP]

# Compute pedestrian trajectories
ped_trajectories = compute_ped_trajectories(frame_times)

# ============================================
# Build traces
# ============================================
colors = ['#FF4444', '#FF8844', '#FFCC44', '#44FF44', '#4444FF', '#FF44FF']
initial_traces = []

# Primary robot trace (cyan)
initial_traces.append(
    go.Scatter(
        x=[trajectory_1['x'][0]],
        y=[trajectory_1['z'][0]],
        mode='lines',
        line=dict(color='cyan', width=2),
        name=PRIMARY_LABEL,
        showlegend=False
    )
)

initial_traces.append(
    go.Scatter(
        x=[trajectory_1['x'][0]],
        y=[trajectory_1['z'][0]],
        mode='markers',
        marker=dict(size=14, color='cyan', symbol='circle', line=dict(color='white', width=1)),
        name=f'🤖 {PRIMARY_LABEL}',
        showlegend=True
    )
)

# Secondary robot trace (orange/yellow) if enabled
if trajectory_2:
    initial_traces.append(
        go.Scatter(
            x=[trajectory_2['x'][0]],
            y=[trajectory_2['z'][0]],
            mode='lines',
            line=dict(color='orange', width=2, dash='dash'),
            name=SECONDARY_LABEL
        )
    )
    
    initial_traces.append(
        go.Scatter(
            x=[trajectory_2['x'][0]],
            y=[trajectory_2['z'][0]],
            mode='markers',
            marker=dict(size=14, color='orange', symbol='square', line=dict(color='white', width=1)),
            name=f'🤖 {SECONDARY_LABEL}',
            showlegend=True
        )
    )

# Tertiary robot trace (lime green) if enabled
if trajectory_3:
    initial_traces.append(
        go.Scatter(
            x=[trajectory_3['x'][0]],
            y=[trajectory_3['z'][0]],
            mode='lines',
            line=dict(color='limegreen', width=2, dash='dot'),
            name=TERTIARY_LABEL
        )
    )
    
    initial_traces.append(
        go.Scatter(
            x=[trajectory_3['x'][0]],
            y=[trajectory_3['z'][0]],
            mode='markers',
            marker=dict(size=14, color='limegreen', symbol='diamond', line=dict(color='white', width=1)),
            name=f'🤖 {TERTIARY_LABEL}',
            showlegend=True
        )
    )

# Pedestrian traces: trail + marker for each
for idx, ped in enumerate(ped_trajectories):
    initial_traces.append(
        go.Scatter(
            x=[ped['x'][0]],
            y=[ped['z'][0]],
            mode='lines',
            line=dict(color=colors[idx], width=1, dash='dot'),
            opacity=0.3,
            name=f"Ped{ped['id']}_Trail",
            showlegend=False
        )
    )
    
    arrow_symbol = 'triangle-right' if ped['orientation'] == 90 else 'triangle-left'
    initial_traces.append(
        go.Scatter(
            x=[ped['x'][0]],
            y=[ped['z'][0]],
            mode='markers+text',
            marker=dict(size=18, color=colors[idx], symbol=arrow_symbol),
            text=[f"P{ped['id']}"],
            textposition='top center',
            name=f"Ped{ped['id']}",
        )
    )

# ============================================
# Build frames
# ============================================
frames = []
num_frames = len(frame_times)

for i in range(1, num_frames):
    frame_data = []
    
    # Primary robot path and marker
    idx_1 = min(i * FRAME_SKIP, len(trajectory_1['x']) - 1)
    sample_indices_1 = list(range(0, idx_1 + 1, max(1, FRAME_SKIP)))
    frame_data.append(dict(x=[trajectory_1['x'][j] for j in sample_indices_1], 
                          y=[trajectory_1['z'][j] for j in sample_indices_1]))
    frame_data.append(dict(x=[trajectory_1['x'][idx_1]], y=[trajectory_1['z'][idx_1]]))
    
    # Secondary robot path and marker if enabled
    if trajectory_2:
        idx_2 = min(i * FRAME_SKIP, len(trajectory_2['x']) - 1)
        sample_indices_2 = list(range(0, idx_2 + 1, max(1, FRAME_SKIP)))
        frame_data.append(dict(x=[trajectory_2['x'][j] for j in sample_indices_2], 
                              y=[trajectory_2['z'][j] for j in sample_indices_2]))
        frame_data.append(dict(x=[trajectory_2['x'][idx_2]], y=[trajectory_2['z'][idx_2]]))
    
    # Tertiary robot path and marker if enabled
    if trajectory_3:
        idx_3 = min(i * FRAME_SKIP, len(trajectory_3['x']) - 1)
        sample_indices_3 = list(range(0, idx_3 + 1, max(1, FRAME_SKIP)))
        frame_data.append(dict(x=[trajectory_3['x'][j] for j in sample_indices_3], 
                              y=[trajectory_3['z'][j] for j in sample_indices_3]))
        frame_data.append(dict(x=[trajectory_3['x'][idx_3]], y=[trajectory_3['z'][idx_3]]))
    
    # Pedestrian trails and markers
    for ped in ped_trajectories:
        frame_data.append(dict(x=ped['x'][:i], y=ped['z'][:i]))
        frame_data.append(dict(x=[ped['x'][i]], y=[ped['z'][i]]))
    
    frames.append(go.Frame(data=frame_data, name=str(i)))

# ============================================
# Calculate bounds
# ============================================
all_x = trajectory_1['x'] + [x for p in ped_trajectories for x in p['x']]
all_z = trajectory_1['z'] + [z for p in ped_trajectories for z in p['z']]

if trajectory_2:
    all_x.extend(trajectory_2['x'])
    all_z.extend(trajectory_2['z'])

if trajectory_3:
    all_x.extend(trajectory_3['x'])
    all_z.extend(trajectory_3['z'])

x_margin = (max(all_x) - min(all_x)) * 0.1
z_margin = (max(all_z) - min(all_z)) * 0.1

# ============================================
# Create figure with speedup applied
# ============================================
# Calculate frame duration based on speedup factor
base_frame_duration = 80  # Original duration in ms
adjusted_frame_duration = int(base_frame_duration / SPEEDUP_FACTOR)

title_text = f"Behavior Label: {BEHAVIOR_LABEL}"
num_trajectories = sum([1, 1 if trajectory_2 else 0, 1 if trajectory_3 else 0])
if num_trajectories > 1:
    title_text += " (Comparison)"

subtitle = (f"<sub>Speed: {SPEEDUP_FACTOR}x ")
if trajectory_2 or trajectory_3:
    subtitle += f" | {PRIMARY_LABEL}: {trajectory_1['total_distance']:.1f}m"
    if trajectory_2:
        subtitle += f" | {SECONDARY_LABEL}: {trajectory_2['total_distance']:.1f}m"
    if trajectory_3:
        subtitle += f" | {TERTIARY_LABEL}: {trajectory_3['total_distance']:.1f}m"
subtitle += "</sub>"

fig = go.Figure(
    data=initial_traces,
    layout=go.Layout(
        title=dict(
            text=title_text, # + "<br>" + subtitle,
            x=0.5,
            xanchor='center',
            font=dict(size=22, color='white')
        ),
        xaxis=dict(
            title="X Position (m)",
            gridcolor='rgba(150,150,170,0.15)',
            zerolinecolor='rgba(255,255,255,0.2)',
            showgrid=True,
            range=[min(all_x)-x_margin, max(all_x)+x_margin]
        ),
        yaxis=dict(
            title="Z Position (m)",
            gridcolor='rgba(150,150,170,0.15)',
            zerolinecolor='rgba(255,255,255,0.2)',
            showgrid=True,
            scaleanchor="x",
            scaleratio=1,
            range=[min(all_z)-z_margin, max(all_z)+z_margin]
        ),
        plot_bgcolor='rgba(15, 15, 25, 1)',
        paper_bgcolor='rgba(10, 10, 18, 1)',
        font=dict(color='white', size=13),
        updatemenus=[{
            'type': 'buttons',
            'showactive': False,
            'buttons': [
                {
                    'label': '▶ Play',
                    'method': 'animate',
                    'args': [None, {
                        'frame': {'duration': adjusted_frame_duration, 'redraw': True},
                        'fromcurrent': True,
                        'mode': 'immediate'
                    }]
                },
                {
                    'label': '⏸ Pause',
                    'method': 'animate',
                    'args': [[None], {
                        'frame': {'duration': 0, 'redraw': False},
                        'mode': 'immediate'
                    }]
                }
            ],
            'x': 0.05,
            'y': 1.1,
            'bgcolor': 'rgba(40,40,55,0.7)',
            'bordercolor': 'white',
            'borderwidth': 1
        }],
        sliders=[{
            'active': 0,
            'y': -0.08,
            'x': 0.1,
            'len': 0.8,
            'pad': {'t': 30},
            'currentvalue': {
                'prefix': 'Time: ',
                'font': {'size': 14, 'color': 'white'}
            },
            'steps': [
                {
                    'args': [[f.name], {
                        'frame': {'duration': 0, 'redraw': True},
                        'mode': 'immediate'
                    }],
                    'label': f"{frame_times[int(f.name)]:.2f}s",
                    'method': 'animate'
                }
                for f in frames
            ]
        }],
        legend=dict(
            title="Entities",
            orientation='v',
            x=1.02,
            y=1,
            bgcolor='rgba(20,20,30,0.85)',
            bordercolor='white',
            borderwidth=1,
            font=dict(size=12)
        )
    ),
    frames=frames
)

output_file = "unity_animation_fixed.html"
fig.write_html(output_file)

print(f"✓ Animation saved: {output_file}")
print(f"  - Speedup factor: {SPEEDUP_FACTOR}x")
print(f"  - Frame skip: {FRAME_SKIP} (total frames: {num_frames})")
print(f"  - Frame duration: {adjusted_frame_duration}ms")
if trajectory_2 or trajectory_3:
    print(f"  - {PRIMARY_LABEL}: {trajectory_1['total_distance']:.2f}m in {trajectory_1['total_time']:.2f}s")
    if trajectory_2:
        print(f"  - {SECONDARY_LABEL}: {trajectory_2['total_distance']:.2f}m in {trajectory_2['total_time']:.2f}s")
    if trajectory_3:
        print(f"  - {TERTIARY_LABEL}: {trajectory_3['total_distance']:.2f}m in {trajectory_3['total_time']:.2f}s")
else:
    print(f"  - Single path: {trajectory_1['total_distance']:.2f}m in {trajectory_1['total_time']:.2f}s")