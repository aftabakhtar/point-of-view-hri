# Setup

What a fresh clone gives you, and what you have to add yourself.

## What works immediately

Open the project in **Unity 6000.2.7f2** and it compiles with zero errors. The
Package Manager resolves everything automatically: Meta XR SDK 81.0.0, OpenXR,
URP/HDRP, Input System, AI Navigation, Newtonsoft JSON, and the URDF Importer
from its git URL.

Verified on a clean machine: a fresh import rebuilds `Library/` from scratch,
resolves all 43 packages, and compiles 171 assemblies including
`Assembly-CSharp.dll`. The only warnings are two unused private fields in
`TrajectoryPlayer.cs`.

### First launch: accept the API update prompt

Meta XR SDK 81.0.0 ships code written against `UnityEngine.PhysicMaterial`,
which Unity 6 renamed to `PhysicsMaterial`. On first import Unity shows an
**"API Update Required"** dialog. **Click "I Made a Backup, Go Ahead!"** — the
Script Updater patches the SDK's own files and everything compiles.

If you decline, the Meta audio assemblies fail with `CS0619`/`CS1503` errors and
the project will not build. Nothing in this repository is at fault, and no
first-party code is touched — only Meta's package cache, which is regenerated
and gitignored.

For CI or headless imports, pass `-accept-apiupdate` to skip the prompt:

```bash
Unity.exe -batchmode -quit -nographics \
  -projectPath /path/to/repo -logFile - -accept-apiupdate
```

The study *logic* is fully intact — trial sequencing, questionnaire UI,
trajectory playback, data output. What is missing is **art**.

## What is missing and why

`Assets/Scenes/SampleScene.unity` references assets across five commercial packs
that cannot be redistributed. On first open you will see roughly 50
`Missing Prefab` console errors where pedestrians, buildings and trees belong.
That is expected, not a broken clone.

The complete expected-missing list is in the
[README](../README.md#what-a-clone-without-the-packs-looks-like). In short:
pedestrians `0`–`5`, `building_*`, and the tree species. **The Toyota HSR robot
parts ship with this repository** — if `base`, `torso`, `head_pan`, `arm_flex`,
`palm`, `laser` or `rgbd` appear as missing, something is genuinely wrong and is
worth reporting.

### The five asset packs

Install each at the exact path below — the scene resolves them by GUID, so the
folder names matter.

| Pack | Install to | Where to get it |
|---|---|---|
| **Renderpeople** rigged scans (3 models, see below) | `Assets/RP_Character/` | [renderpeople.com](https://renderpeople.com/) (commercial, per-model) |
| **(HDRP) NYC-Like City Buildings Set (PBR)** | `Assets/(HDRP) NYC-Like City Buildings Set (PBR)/` | Unity Asset Store |
| **Realistic Tree** | `Assets/Realistic Tree/` | Unity Asset Store |
| **GrassFlowers** | `Assets/GrassFlowers/` | Unity Asset Store |
| **Terrain Tools Sample Asset Pack** | `Assets/TerrainSampleAssets/` | Unity Asset Store (**free**) |

#### Renderpeople contents

Three rigged scans plus their animation clips. Six pedestrians are built from
three models by recolouring clothing (see the texture note below).

```
Assets/RP_Character/
├── rp_manuel_rigged_001/      rp_manuel_rigged_001_u3d.fbx
├── rp_nathan_rigged_003/      rp_nathan_rigged_003_u3d.fbx
├── rp_sophia_rigged_003/      rp_sophia_rigged_003_u3d.fbx
├── 00_Animations/             rp_nathan_animated_003_walking_u3d.fbx
│                              rp_sophia_animated_003_idling_facial_u3d.fbx
│                              rp_sophia_animated_003_standing_u3d.fbx
│                              (+ manuel dancing variants, unused by the study)
├── 00_Animations_Prefabs/     animator controllers
└── 00_rp_master/              RP_Rigged_MasterShader.shader
```

The walking clip drives all six pedestrians; `AttributeAnimator` randomises each
one's cycle phase so the crowd does not move in lockstep.

#### Realistic Tree contents

The scene places Ash, Birch, Chestnut, Spruce and Weeping Willow prefabs from
`Prefabs/URP/`. Install the URP variants, not HDRP or Standard.

> The NYC pack is the only reason HDRP 17.2.0 is a dependency of this URP
> project. If you substitute a different environment, you can drop
> `com.unity.render-pipelines.high-definition` from `Packages/manifest.json`.

### The edited pedestrian textures

The study used Renderpeople diffuse maps with clothing recoloured so the three
scanned humans could be reused as six visually distinct pedestrians. Those
edited PNGs are derivative works of a commercial licence and are **not**
included.

`Assets/Materials/Modified_Textures/rp_sophia_rigged_003_mat 1.mat` *is*
included, so the shader and its parameters survive — the texture slot will
simply be empty. To reproduce the original look, take the `dif` maps from your
own Renderpeople purchase, recolour the clothing regions, and assign them to
that material. Exact appearance is not required to replicate the study design;
only pedestrian distinguishability matters.

### The robot — nothing to do

`Assets/Models/hsr_description_v2/` is included in full: the BSD-licensed URDF
and xacro files, Toyota's original meshes redistributed unmodified, **and** the
Unity import output under `robots/` (per-link prefabs, extracted meshes,
materials).

The import output ships deliberately. The study scene addresses those per-link
prefabs by GUID, and re-running the URDF Importer generates fresh GUIDs that
would not reconnect — you would get a scene with ~30 missing prefab references
and no visible robot. See `THIRD_PARTY_NOTICES.md` for why redistributing the
format-converted meshes is permitted under CC BY-NC-ND §2(a)(4).

If you want to re-import anyway — to change the robot, or regenerate against a
different Unity version — the URDF Importer is already a project dependency:
select `Assets/Models/hsr_description_v2/urdf/hsr_v4.urdf`, right-click →
**Import Robot from Selected URDF file**, and choose **Articulation Body** as
the physics representation (`TrajectoryPlayer` teleports the articulation root,
so this is required). You will then need to re-wire the `RobotMovement` and
`HSRAnimateHead` references in `SampleScene` by hand.

## Hardware and XR configuration

Built and validated for **PC VR**, not standalone Quest:

- XR plug-in: **OpenXR** only, Standalone build target, initialised on start.
- The single enabled OpenXR feature is **MetaXRFeature (Standalone)**.
- Stereo rendering: Single Pass Instanced. Graphics API: Direct3D11.
- Target devices declared in `Assets/Oculus/OculusProjectConfig.asset`:
  Quest 2, Quest Pro, Quest 3, Quest 3S — tethered via Link/Air Link.

Android/Quest settings exist (IL2CPP, ARM64, Vulkan) but are not the shipped
configuration and have not been validated.

Hand, body, face and eye tracking are all disabled. The study needs only head
pose and two controllers.

## Windows: enable long paths before cloning

Windows limits paths to 260 characters by default. This repository's deepest
file is 99 characters, so a clone into a normal location is fine — but Unity's
generated `Library/` folder routinely exceeds the limit, and cloning into a
deeply nested directory will fail with `Filename too long` and an incomplete
checkout.

Enable long-path support once:

```bash
git config --global core.longpaths true
```

On Windows 10 1607+ you may also need the OS-level setting: Group Policy →
Computer Configuration → Administrative Templates → System → Filesystem →
**Enable Win32 long paths**.

If a clone already failed this way, `git restore --source=HEAD :/` completes the
checkout after enabling the setting.

## Recommended git configuration

The repository ships a `.gitattributes` that routes Unity YAML through Unity's
own merge tool. Enable it once per clone:

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

Adjust the path to your Unity install. Without this, concurrent scene edits
produce conflicts that are painful to resolve by hand.

### Expected churn on first open

Unity writes a few things on first import. These are machine-local and safe to
ignore or discard:

| What | Why |
|---|---|
| `Assets/HDRPDefaultResources/` appears | HDRP's global settings normally ship inside the NYC buildings pack. Without that pack Unity regenerates them. Gitignored. |
| `ProjectSettings/GraphicsSettings.asset` shows as modified | Unity repoints the HDRP settings reference at the folder above. Discard it — the committed value points at the pack's copy, which resolves correctly once you install the pack. |
| `Assets/Resources/OculusRuntimeSettings.asset` shows as modified | The Meta SDK stamps a per-install `telemetryProjectGuid`. Discard it; the repository ships it blank deliberately. |

`git checkout -- ProjectSettings Assets/Resources` clears all of these. None
affect how the project builds or runs.

HDRP is in `Packages/manifest.json` only because the NYC pack was authored for
it; this project renders with URP. If you substitute a different environment you
can drop the HDRP dependency and this whole section stops applying.

## Verifying your setup

1. Project compiles, Console shows no errors.
2. Open `Assets/Scenes/SampleScene.unity` — after installing the packs, no
   missing-mesh placeholders remain.
3. Press Play. Without a headset you get the questionnaire flow but no tracked
   camera; with a headset connected you should see the welcome dialog.
4. In the Editor the participant ID defaults to `P001`.
5. Complete one trial and confirm a JSON appears under
   `%USERPROFILE%\AppData\LocalLow\DefaultCompany\robot-trajectory-pref-urp\User Study\Results\P001\`.

If step 5 works, the pipeline is intact.

## Note on the data path

`companyName` is deliberately left at Unity's `DefaultCompany` default, because
`Application.persistentDataPath` is derived from it and the generated `.bat`
launchers hard-code that path. Changing it in Project Settings means updating
`analysis/generate_participant_configs.py` to match, or the demo launcher will
clear the wrong directory.
