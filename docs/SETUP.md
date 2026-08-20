# Setup

What a fresh clone gives you, and what you have to add yourself.

## What works immediately

Open the project in **Unity 6000.2.7f2** and it compiles with zero errors. The
Package Manager resolves everything automatically: Meta XR SDK 81.0.0, OpenXR,
URP/HDRP, Input System, AI Navigation, Newtonsoft JSON, and the URDF Importer
from its git URL.

The study *logic* is fully intact — trial sequencing, questionnaire UI,
trajectory playback, data output. What is missing is **art**.

## What is missing and why

`Assets/Scenes/SampleScene.unity` references 23 assets across five commercial
packs that cannot be redistributed. On first open you will see missing-mesh
placeholders where pedestrians, buildings, trees and terrain detail belong.
That is expected, not a broken clone.

### The five asset packs

Install each at the exact path below — the scene resolves them by GUID, so the
folder names matter.

| Pack | Install to | Where to get it |
|---|---|---|
| **Renderpeople** rigged scans: `rp_manuel_rigged_001`, `rp_nathan_rigged_003`, `rp_sophia_rigged_003` | `Assets/RP_Character/` | [renderpeople.com](https://renderpeople.com/) (commercial) |
| **(HDRP) NYC-Like City Buildings Set (PBR)** | `Assets/(HDRP) NYC-Like City Buildings Set (PBR)/` | Unity Asset Store |
| **Realistic Tree** | `Assets/Realistic Tree/` | Unity Asset Store |
| **GrassFlowers** | `Assets/GrassFlowers/` | Unity Asset Store |
| **Terrain Tools Sample Asset Pack** | `Assets/TerrainSampleAssets/` | Unity Asset Store (**free**) |

The Renderpeople pack also needs six animation FBXs (walking, idling) and their
animator controllers, which ship with the rigged models.

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

### The robot

`Assets/Models/hsr_description_v2/` **is** included: the BSD-licensed URDF and
xacro files, plus Toyota's original meshes, redistributed unmodified as
CC BY-NC-ND permits.

What is *not* included is the Unity import output — extracted meshes, generated
materials and the robot prefab — because NoDerivatives forbids sharing adapted
material. Regenerate it locally:

1. The URDF Importer package is already a project dependency.
2. In the Project window, select
   `Assets/Models/hsr_description_v2/urdf/hsr_v4.urdf`.
3. Right-click → **Import Robot from Selected URDF file**.
4. Accept the defaults; choose **Articulation Body** as the physics
   representation (`TrajectoryPlayer` teleports the articulation root, so this
   is required).
5. Save the generated hierarchy as a prefab at `Assets/HSR/hsr.prefab`.
6. In `SampleScene`, assign it to the `RobotMovement` and `HSRAnimateHead`
   references, and confirm the head pan/tilt joints are wired.

Takes about a minute. Both `Assets/HSR/` and
`Assets/Models/hsr_description_v2/robots/` are `.gitignore`d so the output never
gets committed back.

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

## Recommended git configuration

The repository ships a `.gitattributes` that routes Unity YAML through Unity's
own merge tool. Enable it once per clone:

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

Adjust the path to your Unity install. Without this, concurrent scene edits
produce conflicts that are painful to resolve by hand.

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
