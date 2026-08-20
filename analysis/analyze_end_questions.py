import pandas as pd

import _paths

_PLOTS = _paths.output_dir('end_question_plots')
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.gridspec import GridSpec
from collections import Counter, defaultdict
import warnings
warnings.filterwarnings('ignore')

# ── Style ────────────────────────────────────────────────────────────────────
plt.rcParams.update({
    'font.family': 'DejaVu Sans',
    'axes.spines.top': False,
    'axes.spines.right': False,
    'axes.titlesize': 13,
    'axes.titleweight': 'bold',
    'axes.labelsize': 11,
    'xtick.labelsize': 10,
    'ytick.labelsize': 10,
    'figure.dpi': 150,
})

PALETTE = {
    'A': '#4C72B0',
    'B': '#DD8452',
    'C': '#55A868',
    'yes': '#4C72B0',
    'no':  '#E0E0E0',
    '+1': '#55A868',
    '0':  '#C7B8E8',
    '-1': '#DD8452',
}

# ── Load & clean ──────────────────────────────────────────────────────────────
df = pd.read_csv(_paths.require(_paths.EXIT_CSV))
df.columns = ['pid', 'Q1_3D', 'Q2_2D', 'Q3_chosen', 'Q4_corrected', 'Q5_cues', 'Q6_feedback']
N = len(df)
print(f"Participants: {N}")

# ── Parse Q5 cues ─────────────────────────────────────────────────────────────
def parse_cues(raw):
    cues = {}
    if pd.isna(raw):
        return cues
    for token in str(raw).replace(';', ',').split(','):
        token = token.strip()
        if ':' in token:
            parts = token.split(':')
            key = parts[0].strip()
            try:
                val = float(parts[1].strip())
                cues[key] = val
            except:
                pass
    return cues

df['Q5_parsed'] = df['Q5_cues'].apply(parse_cues)

# Aggregate cue scores
all_cue_keys = set()
for d in df['Q5_parsed']:
    all_cue_keys.update(d.keys())

cue_scores = {k: [] for k in all_cue_keys}
for d in df['Q5_parsed']:
    for k in all_cue_keys:
        if k in d:
            cue_scores[k].append(d[k])

cue_mean   = {k: np.mean(v) for k, v in cue_scores.items()}
cue_counts = {k: len(v)      for k, v in cue_scores.items()}
cue_pos    = {k: sum(1 for x in v if x > 0)  for k, v in cue_scores.items()}
cue_neu    = {k: sum(1 for x in v if x == 0) for k, v in cue_scores.items()}
cue_neg    = {k: sum(1 for x in v if x < 0)  for k, v in cue_scores.items()}

# ── Parse Q6 comments ────────────────────────────────────────────────────────
def parse_comments(raw):
    if pd.isna(raw):
        return []
    tags = []
    for token in str(raw).split(','):
        token = token.strip()
        if token.startswith('comment:'):
            tags.append(token.replace('comment:', '').strip())
    return tags

all_comments = []
for r in df['Q6_feedback']:
    all_comments.extend(parse_comments(r))
comment_counts = Counter(all_comments)


# ═════════════════════════════════════════════════════════════════════════════
#  FIGURE 1 – Perception Overview (Q1 & Q2)
# ═════════════════════════════════════════════════════════════════════════════
fig, axes = plt.subplots(1, 3, figsize=(14, 5))
fig.suptitle('Figure 1 – Behaviour Perception Overview', fontsize=15, fontweight='bold', y=1.02)

q1_yes = df['Q1_3D'].sum();  q1_no = N - q1_yes
q2_yes = df['Q2_2D'].sum();  q2_no = N - q2_yes

for ax, counts, title in zip(
        axes[:2],
        [(q1_yes, q1_no), (q2_yes, q2_no)],
        ['Q1: Behaviour Difference\n(3-D)', 'Q2: Behaviour Difference\n(2-D)']):
    wedges, texts, autotexts = ax.pie(
        counts, labels=['Noticed\ndifference', 'No\ndifference'],
        colors=[PALETTE['yes'], PALETTE['no']],
        autopct='%1.1f%%', startangle=90,
        wedgeprops=dict(edgecolor='white', linewidth=2),
        textprops={'fontsize': 10})
    for at in autotexts:
        at.set_fontweight('bold')
    ax.set_title(title)

# Side-by-side bar comparison
ax = axes[2]
cats = ['3-D\n(Q1)', '2-D\n(Q2)']
yes_pct = [q1_yes/N*100, q2_yes/N*100]
no_pct  = [q1_no/N*100,  q2_no/N*100]
x = np.arange(2)
b1 = ax.bar(x, yes_pct, color=PALETTE['yes'], label='Noticed difference', edgecolor='white')
b2 = ax.bar(x, no_pct, bottom=yes_pct, color=PALETTE['no'], label='No difference', edgecolor='white')
ax.set_xticks(x); ax.set_xticklabels(cats)
ax.set_ylabel('Participants (%)')
ax.set_ylim(0, 110)
ax.set_title('Q1 vs Q2 Comparison')
ax.legend(loc='upper right', fontsize=9)
for bar, pct in zip(b1, yes_pct):
    ax.text(bar.get_x() + bar.get_width()/2, pct/2, f'{pct:.0f}%',
            ha='center', va='center', color='white', fontweight='bold')

plt.tight_layout()
plt.savefig(_PLOTS + '/fig1_perception_overview.png', bbox_inches='tight', dpi=150)
plt.close()
print("Saved fig1")


# ═════════════════════════════════════════════════════════════════════════════
#  FIGURE 2 – Chosen vs Corrected Behaviour (Q3 vs Q4)
# ═════════════════════════════════════════════════════════════════════════════
fig, axes = plt.subplots(1, 3, figsize=(15, 5))
fig.suptitle('Figure 2 – Socially Acceptable Behaviour: Chosen vs Corrected', fontsize=15, fontweight='bold', y=1.02)

q3 = df['Q3_chosen'].value_counts().reindex(['A','B','C'], fill_value=0)
q4 = df['Q4_corrected'].value_counts().reindex(['A','B','C'], fill_value=0)

# Pie Q3
ax = axes[0]
ax.pie(q3, labels=[f'Behaviour {l}' for l in ['A','B','C']],
       colors=[PALETTE[l] for l in ['A','B','C']],
       autopct='%1.1f%%', startangle=90,
       wedgeprops=dict(edgecolor='white', linewidth=2))
ax.set_title('Q3: Initially Chosen\nBehaviour')

# Pie Q4
ax = axes[1]
ax.pie(q4, labels=[f'Behaviour {l}' for l in ['A','B','C']],
       colors=[PALETTE[l] for l in ['A','B','C']],
       autopct='%1.1f%%', startangle=90,
       wedgeprops=dict(edgecolor='white', linewidth=2))
ax.set_title('Q4: Corrected\nBehaviour')

# Grouped bar
ax = axes[2]
x = np.arange(3)
w = 0.35
b1 = ax.bar(x - w/2, q3.values/N*100, w, label='Chosen (Q3)',   color=[PALETTE[l] for l in ['A','B','C']], edgecolor='white')
b2 = ax.bar(x + w/2, q4.values/N*100, w, label='Corrected (Q4)', color=[PALETTE[l] for l in ['A','B','C']], edgecolor='white', alpha=0.55, hatch='//')
ax.set_xticks(x); ax.set_xticklabels(['Behaviour A','Behaviour B','Behaviour C'])
ax.set_ylabel('Participants (%)')
ax.set_title('Chosen vs Corrected\n(side-by-side)')
ax.legend()
for bar in list(b1) + list(b2):
    h = bar.get_height()
    if h > 0:
        ax.text(bar.get_x()+bar.get_width()/2, h+0.5, f'{h:.0f}%',
                ha='center', va='bottom', fontsize=8)

plt.tight_layout()
plt.savefig(_PLOTS + '/fig2_chosen_vs_corrected.png', bbox_inches='tight', dpi=150)
plt.close()
print("Saved fig2")


# ═════════════════════════════════════════════════════════════════════════════
#  FIGURE 3 – Behaviour Change Sankey-style alluvial (Q3 → Q4)
# ═════════════════════════════════════════════════════════════════════════════
from matplotlib.patches import FancyArrowPatch
import matplotlib.patheffects as pe

fig, ax = plt.subplots(figsize=(10, 6))
ax.set_xlim(0, 10); ax.set_ylim(0, 10)
ax.axis('off')
fig.suptitle('Figure 3 – Individual Behaviour Shifts (Q3 → Q4)', fontsize=15, fontweight='bold')

transitions = df.groupby(['Q3_chosen','Q4_corrected']).size().reset_index(name='count')

# Draw a simple alluvial diagram
labels = ['A','B','C']
left_y  = {'A': 7.5, 'B': 5.0, 'C': 2.5}
right_y = {'A': 7.5, 'B': 5.0, 'C': 2.5}
col_map = {'A': PALETTE['A'], 'B': PALETTE['B'], 'C': PALETTE['C']}

for lbl in labels:
    cnt = q3[lbl]
    pct = cnt/N*100
    ax.text(1.2, left_y[lbl], f'Behaviour {lbl}\n({cnt}, {pct:.0f}%)',
            ha='center', va='center', fontsize=11, fontweight='bold',
            bbox=dict(boxstyle='round,pad=0.4', facecolor=col_map[lbl], alpha=0.8, edgecolor='white'))
    cnt2 = q4[lbl]
    pct2 = cnt2/N*100
    ax.text(8.8, right_y[lbl], f'Behaviour {lbl}\n({cnt2}, {pct2:.0f}%)',
            ha='center', va='center', fontsize=11, fontweight='bold',
            bbox=dict(boxstyle='round,pad=0.4', facecolor=col_map[lbl], alpha=0.8, edgecolor='white'))

ax.text(1.2, 9.3, 'Chosen (Q3)', ha='center', fontsize=12, fontweight='bold')
ax.text(8.8, 9.3, 'Corrected (Q4)', ha='center', fontsize=12, fontweight='bold')

for _, row in transitions.iterrows():
    src, dst, cnt = row['Q3_chosen'], row['Q4_corrected'], row['count']
    lw = max(0.5, cnt * 1.5)
    color = col_map[src] if src == dst else '#888888'
    style = '-' if src == dst else '--'
    ax.annotate('', xy=(8.2, right_y[dst]), xytext=(2.2, left_y[src]),
                arrowprops=dict(arrowstyle='->', color=color, lw=lw, connectionstyle='arc3,rad=0.1'))
    mid_x = 5.2
    mid_y = (left_y[src] + right_y[dst]) / 2 + (0.3 if src != dst else 0)
    ax.text(mid_x, mid_y, str(cnt), ha='center', va='center', fontsize=9,
            color=color, fontweight='bold')

# Stability annotation
stable = (df['Q3_chosen'] == df['Q4_corrected']).sum()
ax.text(5, 0.5, f'Agreement (no change): {stable}/{N} participants ({stable/N*100:.0f}%)',
        ha='center', fontsize=10, style='italic',
        bbox=dict(boxstyle='round', facecolor='lightyellow', edgecolor='gray'))

plt.tight_layout()
plt.savefig(_PLOTS + '/fig3_behaviour_shifts.png', bbox_inches='tight', dpi=150)
plt.close()
print("Saved fig3")


# ═════════════════════════════════════════════════════════════════════════════
#  FIGURE 4 – Social Cue Sentiment (Q5)
# ═════════════════════════════════════════════════════════════════════════════
sorted_keys = sorted(cue_mean.keys(), key=lambda k: cue_mean[k], reverse=True)

fig, axes = plt.subplots(1, 2, figsize=(14, 5))
fig.suptitle('Figure 4 – Robot Social Cue Implications (Q5)', fontsize=15, fontweight='bold', y=1.02)

# Mean score bar chart
ax = axes[0]
means = [cue_mean[k] for k in sorted_keys]
colors = [PALETTE['+1'] if m > 0 else (PALETTE['-1'] if m < 0 else PALETTE['0']) for m in means]
bars = ax.barh(sorted_keys, means, color=colors, edgecolor='white', height=0.55)
ax.axvline(0, color='black', lw=0.8, linestyle='--')
ax.set_xlabel('Mean Implication Score')
ax.set_title('Mean Implication Score per Cue\n(+1 positive, 0 neutral, −1 negative)')
for bar, val in zip(bars, means):
    ax.text(val + (0.03 if val >= 0 else -0.03),
            bar.get_y() + bar.get_height()/2,
            f'{val:.2f}', va='center',
            ha='left' if val >= 0 else 'right', fontsize=9)
ax.set_xlim(-1.3, 1.3)

# Stacked sentiment breakdown
ax = axes[1]
pos_vals = [cue_pos[k] for k in sorted_keys]
neu_vals = [cue_neu[k] for k in sorted_keys]
neg_vals = [cue_neg[k] for k in sorted_keys]
y = np.arange(len(sorted_keys))
ax.barh(y, pos_vals, color=PALETTE['+1'], label='Positive (+1)', edgecolor='white')
ax.barh(y, neu_vals, left=pos_vals, color=PALETTE['0'], label='Neutral (0)', edgecolor='white')
ax.barh(y, neg_vals, left=[p+n for p,n in zip(pos_vals, neu_vals)],
        color=PALETTE['-1'], label='Negative (−1)', edgecolor='white')
ax.set_yticks(y); ax.set_yticklabels(sorted_keys)
ax.set_xlabel('Number of Participants')
ax.set_title('Sentiment Distribution per Cue')
ax.legend(loc='lower right', fontsize=9)

plt.tight_layout()
plt.savefig(_PLOTS + '/fig4_social_cues.png', bbox_inches='tight', dpi=150)
plt.close()
print("Saved fig4")


# ═════════════════════════════════════════════════════════════════════════════
#  FIGURE 5 – Perception vs Behaviour Agreement
# ═════════════════════════════════════════════════════════════════════════════
fig, axes = plt.subplots(1, 2, figsize=(12, 5))
fig.suptitle('Figure 5 – Cross-Question Analysis', fontsize=15, fontweight='bold', y=1.02)

# Heatmap: Q3 × Q4 confusion-style matrix
ax = axes[0]
conf = pd.crosstab(df['Q3_chosen'], df['Q4_corrected']).reindex(index=['A','B','C'], columns=['A','B','C'], fill_value=0)
im = ax.imshow(conf.values, cmap='Blues', aspect='auto', vmin=0)
ax.set_xticks([0,1,2]); ax.set_xticklabels(['A','B','C'])
ax.set_yticks([0,1,2]); ax.set_yticklabels(['A','B','C'])
ax.set_xlabel('Corrected Behaviour (Q4)')
ax.set_ylabel('Chosen Behaviour (Q3)')
ax.set_title('Q3 → Q4 Transition Matrix\n(counts)')
for i in range(3):
    for j in range(3):
        val = conf.values[i, j]
        ax.text(j, i, str(val), ha='center', va='center',
                color='white' if val > 2 else 'black', fontsize=14, fontweight='bold')
plt.colorbar(im, ax=ax, label='Count')

# Q1/Q2 perception vs agreement
ax = axes[1]
df['perceived_both'] = (df['Q1_3D'] == 1) & (df['Q2_2D'] == 1)
df['agreed']         = df['Q3_chosen'] == df['Q4_corrected']

groups = {
    'Both perceived\ndifference': df['perceived_both'],
    'Only 3D\nperceived':         (df['Q1_3D']==1) & (df['Q2_2D']==0),
    'Only 2D\nperceived':         (df['Q1_3D']==0) & (df['Q2_2D']==1),
    'Neither\nperceived':         (df['Q1_3D']==0) & (df['Q2_2D']==0),
}
labels2, agree_pcts, counts2 = [], [], []
for lbl, mask in groups.items():
    sub = df[mask]
    if len(sub) > 0:
        labels2.append(lbl)
        agree_pcts.append(sub['agreed'].mean()*100)
        counts2.append(len(sub))

x2 = np.arange(len(labels2))
bars2 = ax.bar(x2, agree_pcts, color=['#4C72B0','#DD8452','#55A868','#C7B8E8'], edgecolor='white')
ax.set_xticks(x2); ax.set_xticklabels(labels2, fontsize=9)
ax.set_ylabel('Q3=Q4 Agreement (%)')
ax.set_title('Behaviour Agreement Rate\nby Perception Group')
ax.set_ylim(0, 115)
for bar, cnt, pct in zip(bars2, counts2, agree_pcts):
    ax.text(bar.get_x()+bar.get_width()/2, bar.get_height()+1.5,
            f'{pct:.0f}%\n(n={cnt})', ha='center', va='bottom', fontsize=9)

plt.tight_layout()
plt.savefig(_PLOTS + '/fig5_cross_analysis.png', bbox_inches='tight', dpi=150)
plt.close()
print("Saved fig5")


# ═════════════════════════════════════════════════════════════════════════════
#  FIGURE 6 – Additional Feedback Themes (Q6)
# ═════════════════════════════════════════════════════════════════════════════
if comment_counts:
    fig, axes = plt.subplots(1, 2, figsize=(13, 5))
    fig.suptitle('Figure 6 – Additional Feedback Themes (Q6)', fontsize=15, fontweight='bold', y=1.02)

    ax = axes[0]
    labels_c = [l.replace('_', '\n') for l in comment_counts.keys()]
    vals_c   = list(comment_counts.values())
    order    = np.argsort(vals_c)[::-1]
    bars_c   = ax.barh([labels_c[i] for i in order], [vals_c[i] for i in order],
                       color='#7B9EC8', edgecolor='white')
    ax.set_xlabel('Frequency')
    ax.set_title('Feedback Theme Frequency')
    for bar in bars_c:
        ax.text(bar.get_width()+0.05, bar.get_y()+bar.get_height()/2,
                str(int(bar.get_width())), va='center', fontsize=9)

    ax2 = axes[1]
    pcts_c = [v/N*100 for v in vals_c]
    wedges, texts, autotexts = ax2.pie(
        vals_c, labels=[labels_c[i] for i in order],
        colors=plt.cm.Set2(np.linspace(0, 1, len(vals_c))),
        autopct='%1.1f%%', startangle=90,
        wedgeprops=dict(edgecolor='white', linewidth=1.5))
    ax2.set_title('Feedback Theme Proportion')

    plt.tight_layout()
    plt.savefig(_PLOTS + '/fig6_feedback_themes.png', bbox_inches='tight', dpi=150)
    plt.close()
    print("Saved fig6")


# ═════════════════════════════════════════════════════════════════════════════
#  FIGURE 7 – Summary Dashboard
# ═════════════════════════════════════════════════════════════════════════════
fig = plt.figure(figsize=(16, 10))
fig.patch.set_facecolor('#F8F9FA')
gs  = GridSpec(2, 4, figure=fig, hspace=0.45, wspace=0.4)
fig.suptitle('User Study – Summary Dashboard', fontsize=18, fontweight='bold', y=1.01)

# 1) Q1/Q2 Perception
ax1 = fig.add_subplot(gs[0, 0])
q_labels = ['3-D (Q1)', '2-D (Q2)']
pcts     = [q1_yes/N*100, q2_yes/N*100]
b = ax1.bar(q_labels, pcts, color=['#4C72B0','#DD8452'], width=0.5, edgecolor='white')
ax1.set_ylim(0, 100); ax1.set_ylabel('%')
ax1.set_title('% Perceived\nBehaviour Diff.')
for bar, p in zip(b, pcts):
    ax1.text(bar.get_x()+bar.get_width()/2, p+1.5, f'{p:.0f}%', ha='center', fontsize=10, fontweight='bold')

# 2) Q3 pie
ax2 = fig.add_subplot(gs[0, 1])
ax2.pie(q3, labels=['A','B','C'],
        colors=[PALETTE[l] for l in ['A','B','C']],
        autopct='%1.0f%%', startangle=90,
        wedgeprops=dict(edgecolor='white', linewidth=2))
ax2.set_title('Chosen Behaviour\n(Q3)')

# 3) Q4 pie
ax3 = fig.add_subplot(gs[0, 2])
ax3.pie(q4, labels=['A','B','C'],
        colors=[PALETTE[l] for l in ['A','B','C']],
        autopct='%1.0f%%', startangle=90,
        wedgeprops=dict(edgecolor='white', linewidth=2))
ax3.set_title('Corrected Behaviour\n(Q4)')

# 4) Agreement
ax4 = fig.add_subplot(gs[0, 3])
agreed_n     = (df['Q3_chosen'] == df['Q4_corrected']).sum()
not_agreed_n = N - agreed_n
ax4.pie([agreed_n, not_agreed_n], labels=['Same', 'Changed'],
        colors=['#55A868','#E0E0E0'], autopct='%1.0f%%', startangle=90,
        wedgeprops=dict(edgecolor='white', linewidth=2))
ax4.set_title(f'Q3=Q4 Agreement\n({agreed_n}/{N} unchanged)')

# 5) Cue mean scores (bottom span)
ax5 = fig.add_subplot(gs[1, :])
sorted_keys2 = sorted(cue_mean.keys(), key=lambda k: cue_mean[k], reverse=True)
means2  = [cue_mean[k] for k in sorted_keys2]
colors2 = [PALETTE['+1'] if m > 0 else (PALETTE['-1'] if m < 0 else PALETTE['0']) for m in means2]
bars5 = ax5.bar(sorted_keys2, means2, color=colors2, edgecolor='white', width=0.6)
ax5.axhline(0, color='black', lw=0.8, linestyle='--')
ax5.set_ylabel('Mean Implication Score')
ax5.set_title('Social Cue Mean Implication Scores (Q5)')
ax5.set_ylim(-1.3, 1.3)
for bar, val in zip(bars5, means2):
    ax5.text(bar.get_x()+bar.get_width()/2,
             val + (0.04 if val >= 0 else -0.04),
             f'{val:.2f}', ha='center',
             va='bottom' if val >= 0 else 'top', fontsize=9, fontweight='bold')

legend_patches = [
    mpatches.Patch(color=PALETTE['+1'], label='Positive (+1)'),
    mpatches.Patch(color=PALETTE['0'],  label='Neutral (0)'),
    mpatches.Patch(color=PALETTE['-1'], label='Negative (−1)'),
]
ax5.legend(handles=legend_patches, loc='upper right', fontsize=9)

plt.savefig(_PLOTS + '/fig7_dashboard.png', bbox_inches='tight', dpi=150)
plt.close()
print("Saved fig7")


# ═════════════════════════════════════════════════════════════════════════════
#  PRINT STATISTICS TABLE
# ═════════════════════════════════════════════════════════════════════════════
print("\n" + "="*60)
print("  STATISTICAL SUMMARY")
print("="*60)
print(f"\nTotal participants (N): {N}")

print(f"\n── Q1 (3-D Perception) ──")
print(f"  Noticed difference : {q1_yes} ({q1_yes/N*100:.1f}%)")
print(f"  No difference      : {q1_no} ({q1_no/N*100:.1f}%)")

print(f"\n── Q2 (2-D Perception) ──")
print(f"  Noticed difference : {q2_yes} ({q2_yes/N*100:.1f}%)")
print(f"  No difference      : {q2_no} ({q2_no/N*100:.1f}%)")

print(f"\n── Q3 Chosen Behaviour ──")
for l in ['A','B','C']:
    print(f"  {l}: {q3[l]} ({q3[l]/N*100:.1f}%)")

print(f"\n── Q4 Corrected Behaviour ──")
for l in ['A','B','C']:
    print(f"  {l}: {q4[l]} ({q4[l]/N*100:.1f}%)")

print(f"\n── Q3 → Q4 Behaviour Shifts ──")
for _, row in transitions.iterrows():
    tag = " ← unchanged" if row['Q3_chosen']==row['Q4_corrected'] else ""
    print(f"  {row['Q3_chosen']} → {row['Q4_corrected']} : {row['count']}{tag}")
print(f"  Total unchanged (Q3=Q4): {agreed_n}/{N} ({agreed_n/N*100:.1f}%)")

print(f"\n── Q5 Social Cue Scores ──")
for k in sorted_keys:
    print(f"  {k:25s}  mean={cue_mean[k]:+.2f}  n={cue_counts[k]}  (+:{cue_pos[k]}, 0:{cue_neu[k]}, -:{cue_neg[k]})")

if comment_counts:
    print(f"\n── Q6 Feedback Themes ──")
    for tag, cnt in comment_counts.most_common():
        print(f"  {tag:45s}: {cnt} ({cnt/N*100:.1f}%)")

print("\nAll figures saved to end_question_plots/")