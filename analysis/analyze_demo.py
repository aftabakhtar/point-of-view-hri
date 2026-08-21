"""Demographic descriptives for the released (de-identified) sample."""

import pandas as pd

import _paths

# keep_default_na=False matters here: "None" is a valid answer to the prior-robot
# -experience item, and pandas would otherwise read it as NaN and drop those
# participants from the counts.
df = pd.read_csv(_paths.require(_paths.DEMOGRAPHICS_CSV), keep_default_na=False)

print("=" * 60)
print("SAMPLE DEMOGRAPHICS")
print("=" * 60)
print(f"Participants: {len(df)}")

age = pd.to_numeric(df["age"], errors="coerce")
print(f"\nAge: Mean {age.mean():.1f}, SD {age.std(ddof=1):.1f}, "
      f"range {int(age.min())}-{int(age.max())}")

print("\nGender:")
for value, count in df["gender"].value_counts().items():
    print(f"  {value:<12} {count:>3}  ({count / len(df):.0%})")

print("\nEducation:")
for value, count in df["education"].value_counts().items():
    print(f"  {value:<14} {count:>3}")

print("\nField of study:")
for value, count in df["field_of_study"].value_counts().items():
    print(f"  {value:<10} {count:>3}")

# Familiarity is a 1-5 ordinal, so mean and SD survive de-identification.
fam = pd.to_numeric(df["mobile_robot_familiarity"], errors="coerce")
fam_mean, fam_sd = fam.mean(), fam.std(ddof=1)
level = "low" if fam_mean < 2.5 else "moderate" if fam_mean < 3.5 else "high"

print("\nMobile-robot familiarity (1-5):")
print(f"  Mean {fam_mean:.1f}, SD {fam_sd:.1f}  ({level})")

print("\nPrior robot experience:")
order = ["None", "Very Limited", "Some", "Extensive"]
counts = df["prior_robot_experience"].value_counts()
for value in order:
    if value in counts:
        print(f"  {value:<14} {counts[value]:>3}")

print("\nVR/AR/MR experience:")
counts = df["vr_experience"].value_counts()
for value in ["Never", "Occasionally", "Frequently"]:
    if value in counts:
        print(f"  {value:<14} {counts[value]:>3}")

print(
    f"\nSummary: {len(df)} participants "
    f"(M_age = {age.mean():.1f}, SD = {age.std(ddof=1):.1f}; "
    f"{int((df['gender'] == 'Male').sum())} male, "
    f"{int((df['gender'] == 'Female').sum())} female), "
    f"reporting {level} familiarity with mobile robots "
    f"(M = {fam_mean:.1f}, SD = {fam_sd:.1f})."
)
