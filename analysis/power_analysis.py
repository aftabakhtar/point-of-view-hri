import os

import numpy as np
import matplotlib.pyplot as plt
from statsmodels.stats.power import FTestAnovaPower

import _paths

# --- 1. SETUP YOUR PARAMETERS ---
alpha = 0.05        # 5% chance of False Positive
power = 0.80        # 80% chance of detecting true effect

# Effect sizes to check (Cohen's f)
# 0.10 = Small, 0.25 = Medium, 0.40 = Large
effect_sizes = [0.10, 0.25, 0.40]

# Your study design
n_trajectories = 3
n_viewpoints = 3
n_conditions = n_trajectories * n_viewpoints  # 9 conditions

# For within-subjects ANOVA, we need degrees of freedom
# Main effect of trajectory: df = 2 - 1 = 1
# Main effect of viewpoint: df = 3 - 1 = 2
# Interaction: df = (2-1) * (3-1) = 2

# Let's calculate for the interaction (usually most conservative)
df_effect_interaction = (n_trajectories - 1) * (n_viewpoints - 1)  # 2
df_effect_trajectory = n_trajectories - 1  # 2
df_effect_viewpoint = n_viewpoints - 1  # 2

# Correlation assumption for within-subjects (typical range 0.3-0.7)
# Higher correlation = more power benefit from within-subjects design
assumed_correlation = 0.5

# Adjust effect size for within-subjects design
# Within-subjects effectively reduces variance by factor of sqrt(1-r)
def adjust_effect_size_within(f, r, n_cond):
    """Adjust effect size for within-subjects correlation"""
    # This is a simplified adjustment
    # More accurate methods would use epsilon corrections
    return f / np.sqrt(1 - r)

# --- 2. CALCULATE SAMPLE SIZE ---
analysis = FTestAnovaPower()

print("=" * 70)
print("POWER ANALYSIS FOR 3×3 WITHIN-SUBJECTS ANOVA (REPEATED MEASURES)")
print("=" * 70)
print(f"\nDesign: {n_trajectories} Trajectories × {n_viewpoints} Viewpoints")
print(f"Assumed correlation between measures: {assumed_correlation}")
print(f"Target power: {power}, Alpha: {alpha}")
print(f"\nUsing William's Design (Balanced Latin Square) - multiple of 6")
print("\n" + "-" * 70)
print(f"{'Effect':<12} | {'Effect Size':<12} | {'Adjusted f':<12} | {'Sample Size':<12}")
print("-" * 70)

results = {}

for effect_name, df_effect in [
    ("Trajectory", df_effect_trajectory),
    ("Viewpoint", df_effect_viewpoint),
    ("Interaction", df_effect_interaction)
]:
    results[effect_name] = {}
    
    for f in effect_sizes:
        # Adjust effect size for within-subjects correlation
        f_adjusted = adjust_effect_size_within(f, assumed_correlation, n_conditions)
        
        # For within-subjects, we need to account for:
        # - k_groups = number of levels of the factor being tested
        # - Reduced error variance from repeated measures
        
        # Number of groups for this effect
        if effect_name == "Trajectory":
            k_groups = n_trajectories
        elif effect_name == "Viewpoint":
            k_groups = n_viewpoints
        else:  # Interaction
            k_groups = n_trajectories * n_viewpoints
        
        # Use iterative approach to find required N
        n_participants = 6  # Start with minimum for William's design
        current_power = 0
        
        while current_power < power and n_participants < 500:
            # For repeated measures ANOVA, effective sample size is reduced
            # We use the formula: nobs = n_participants * k_groups
            total_obs = n_participants * k_groups
            
            # Calculate power
            try:
                current_power = analysis.solve_power(
                    effect_size=f_adjusted,
                    nobs=total_obs,
                    alpha=alpha,
                    k_groups=k_groups,
                    power=None
                )
            except:
                # Fallback if solve_power has issues
                current_power = 0
            
            if current_power < power:
                n_participants += 6  # Increment by 6 for William's design
        
        results[effect_name][f] = n_participants
        print(f"{effect_name:<12} | {f:<12.2f} | {f_adjusted:<12.2f} | {n_participants:<12}")

# --- 3. PLOT THE POWER CURVES ---
fig, axes = plt.subplots(1, 3, figsize=(16, 5))
sample_sizes = np.arange(9, 100, 9)  # Multiples of 9 for the Williams design

effects = [
    ("Trajectory", df_effect_trajectory),
    ("Viewpoint", df_effect_viewpoint),
    ("Interaction", df_effect_interaction)
]

for idx, (effect_name, df_effect) in enumerate(effects):
    ax = axes[idx]
    
    # Number of groups for this effect
    if effect_name == "Trajectory":
        k_groups = n_trajectories
    elif effect_name == "Viewpoint":
        k_groups = n_viewpoints
    else:  # Interaction
        k_groups = n_trajectories * n_viewpoints
    
    for f in effect_sizes:
        f_adjusted = adjust_effect_size_within(f, assumed_correlation, n_conditions)
        powers = []
        
        for n in sample_sizes:
            total_obs = n * k_groups
            
            try:
                p = analysis.solve_power(
                    effect_size=f_adjusted,
                    nobs=total_obs,
                    alpha=alpha,
                    k_groups=k_groups,
                    power=None
                )
                powers.append(p)
            except:
                powers.append(np.nan)
        
        ax.plot(sample_sizes, powers, label=f'f={f}', marker='o', markersize=4)
    
    ax.axhline(y=0.8, color='r', linestyle='--', linewidth=2, label='Target Power')
    ax.set_title(f'{effect_name} Effect\n(df={df_effect})', fontsize=12, fontweight='bold')
    ax.set_xlabel('Number of Participants', fontsize=11)
    ax.set_ylabel('Statistical Power', fontsize=11)
    ax.grid(True, alpha=0.3)
    ax.legend(loc='lower right')
    ax.set_ylim([0, 1])

plt.suptitle('Power Analysis for Within-Subjects 3×3 ANOVA (Repeated Measures)', 
             fontsize=14, fontweight='bold', y=1.02)
plt.tight_layout()
_fig_path = os.path.join(_paths.output_dir(), 'power_curves.png')
plt.savefig(_fig_path, bbox_inches='tight', dpi=150)
print(f'\nPower curves saved to {_fig_path}')
plt.show()

# --- 4. SUMMARY RECOMMENDATIONS ---
print("\n" + "=" * 70)
print("RECOMMENDATIONS")
print("=" * 70)
print(f"\nFor CONSERVATIVE estimate (Interaction effect):")
print(f"  - Small effect (f=0.10): N = {results['Interaction'][0.10]} participants")
print(f"  - Medium effect (f=0.25): N = {results['Interaction'][0.25]} participants")
print(f"  - Large effect (f=0.40): N = {results['Interaction'][0.40]} participants")
print(f"\nNote: These estimates assume correlation between measures = {assumed_correlation}")
print("Lower correlation → need more participants")
print("Higher correlation → need fewer participants")
print("\nAll sample sizes are multiples of 6 for William's Design compatibility.")