# Third-party notices

The first-party code in this repository is MIT licensed (see `LICENSE`). This
file records everything else.

## ⚠️ The repository as a whole is not commercially usable

The bundled Toyota HSR meshes are licensed **CC BY-NC-ND 4.0**, whose
NonCommercial clause is incompatible with commercial use and with OSI-approved
licences generally. If you need a commercially usable derivative, remove
`Assets/Models/hsr_description_v2/hsr_meshes/` and substitute your own robot
model. Everything else in `Assets/Scripts/`, `analysis/` and `docs/` is MIT.

---

## Bundled in this repository

| Component | Path | Licence | Notes |
|---|---|---|---|
| Toyota HSR robot description (URDF, xacro, launch) | `Assets/Models/hsr_description_v2/` | **BSD 3-Clause Clear**, © 2017 Toyota Motor Corporation | See `Assets/Models/hsr_description_v2/LICENSE.txt`. Freely redistributable with notice. |
| Toyota HSR 3D meshes | `Assets/Models/hsr_description_v2/hsr_meshes/` | **CC BY-NC-ND 4.0** | See `hsr_meshes/LICENSE.txt`. Redistributed **verbatim and unmodified**, as the licence permits. NonCommercial and NoDerivatives both apply. |
| TextMesh Pro essentials | `Assets/TextMesh Pro/` | Unity Companion License | Includes Liberation Sans under SIL OFL (`Fonts/LiberationSans - OFL.txt`) and EmojiOne sprites under their own terms (`Sprites/EmojiOne Attribution.txt`). |
| Meta Interaction SDK UI themes | `Assets/UI Themes/` | Meta SDK licence | Sample theme assets copied from the Meta XR SDK. |

### On the HSR meshes and derivative works

The `.dae`, `.stl`, `.obj`, `.mtl` and `.png` files under `hsr_meshes/` are
Toyota's own originals and are shipped unchanged, which CC BY-NC-ND permits for
non-commercial use with attribution.

What the licence does **not** permit is redistributing *adapted* material. Unity's
URDF Importer produces exactly that — extracted `.asset` meshes, generated `.mat`
materials and `.prefab` hierarchies. Those artefacts are therefore **excluded**
from this repository and `.gitignore`d. You generate them locally in a few
seconds; see `docs/SETUP.md`.

---

## Fetched automatically by the Unity Package Manager

These are resolved from registries at project open and are **not** redistributed
here. Their licences apply to your local copies.

| Package | Version | Source |
|---|---|---|
| `com.meta.xr.sdk.all` (Core, Interaction, Platform, Voice, Audio, Haptics, MR Utility Kit, Simulator) | 81.0.0 | Unity registry — governed by the **Meta/Oculus SDK License** |
| `com.unity.render-pipelines.universal` / `.high-definition` | 17.2.0 | Unity |
| `com.unity.xr.openxr` | 1.15.1 | Unity |
| `com.unity.inputsystem` | 1.14.2 | Unity |
| `com.unity.ai.navigation` | 2.0.9 | Unity |
| `com.unity.robotics.urdf-importer` | v0.5.2 | [Unity-Technologies/URDF-Importer](https://github.com/Unity-Technologies/URDF-Importer) — Apache-2.0 |
| `com.unity.nuget.newtonsoft-json` | 3.2.1 | MIT |
| `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.shadergraph`, engine modules | various | Unity |

---

## NOT included — you must acquire these separately

The study scene references five commercial or otherwise non-redistributable
asset packs. They are `.gitignore`d and absent from this repository and from its
git history. `docs/SETUP.md` explains exactly where each must be installed.

| Asset pack | Expected path | Licence |
|---|---|---|
| Renderpeople rigged scans (`rp_manuel_rigged_001`, `rp_nathan_rigged_003`, `rp_sophia_rigged_003`) | `Assets/RP_Character/` | Renderpeople EULA — redistribution of models/textures prohibited |
| (HDRP) NYC-Like City Buildings Set (PBR) | `Assets/(HDRP) NYC-Like City Buildings Set (PBR)/` | Unity Asset Store EULA; embedded textures.com material has its own terms |
| Realistic Tree | `Assets/Realistic Tree/` | Unity Asset Store EULA |
| GrassFlowers | `Assets/GrassFlowers/` | Unity Asset Store EULA |
| Unity Terrain Tools Sample Asset Pack | `Assets/TerrainSampleAssets/` | Unity Asset Store EULA (free to acquire). Terrain data derived from OpenStreetMap (© OpenStreetMap contributors) and the U.S. Geological Survey |

Diffuse textures edited from Renderpeople scans are likewise excluded
(`Assets/Materials/Modified_Textures/rp_*.png`). The Unity material that
references them is kept so the shader setup survives; you re-supply the
textures. See `docs/SETUP.md`.

### Pre-rendered stimulus videos

`Assets/StreamingAssets/User Study/Trajectories/*.mp4` are the allocentric
condition's stimuli: top-down renders of the scene, which necessarily depict the
asset packs above. Renders and other "end products" are generally permitted by
these EULAs, whereas the source assets are not. If you intend to redistribute a
modified fork, re-render them from your own licensed copies.

---

## Citing this work

See `CITATION.cff`, or cite the paper directly:
<https://arxiv.org/abs/2603.28272>
