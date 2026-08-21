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

### On the HSR meshes and format conversion

The `.dae`, `.stl`, `.obj`, `.mtl` and `.png` files under `hsr_meshes/` are
Toyota's own originals and are shipped unchanged, which CC BY-NC-ND permits for
non-commercial use with attribution.

The repository also ships Unity's URDF-Importer output under
`hsr_description_v2/robots/` — extracted `.asset` meshes, generated `.mat`
materials and per-link `.prefab` files. These are **format conversions, not
adaptations**: the geometry is unaltered, and CC BY-NC-ND 4.0 §2(a)(4)
explicitly authorises "technical modifications necessary" to use a work in a
given medium, stating that such modifications "never produce Adapted Material".
Importing a COLLADA mesh so a game engine can render it falls squarely within
that.

This matters practically as well as legally: the study scene addresses those
per-link prefabs by GUID. Regenerating them locally produces fresh GUIDs that
would not reconnect, leaving the robot missing from the scene. They have to ship
for the scene to work.

The NonCommercial term still applies to all of it — see the warning at the top
of this file.

### Attribution and modification notice (CC BY-NC-ND 4.0 §3(a))

**Licensed Material:** Toyota HSR 3D meshes, from the ROS package `hsr_meshes`
(part of `hsr_description` v1.1.0).

**Creator and copyright:** © Toyota Motor Corporation. Package maintainers and
authors per `Assets/Models/hsr_description_v2/package.xml`: Koji Terada,
Akiyoshi Ochiai, Takeshita, Nishino, Murase, Mori.

**Licence:** Creative Commons Attribution-NonCommercial-NoDerivatives 4.0
International (CC BY-NC-ND 4.0).
Full text: <https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode>
Summary: <https://creativecommons.org/licenses/by-nc-nd/4.0/>
The licence text as supplied by the licensor is retained verbatim at
`Assets/Models/hsr_description_v2/hsr_meshes/LICENSE.txt`, together with the
licensor's own `README.md`.

**Disclaimer of warranties:** as stated in §5 of the licence text above. The
Licensed Material is provided as-is, without warranties of any kind.

**Indication of modification** — required by §3(a)(1)(B):

> The mesh geometry has **not** been modified. No vertices, topology, materials
> or textures have been altered, added or removed.
>
> The meshes have been **format-converted** for use in the Unity engine, using
> Unity's URDF Importer (Apache-2.0). This produced `.asset` files under
> `hsr_description_v2/robots/hsr_meshes/`, which are Unity's binary
> serialisation of the identical geometry supplied in the licensor's `.dae`,
> `.obj` and `.stl` files. Those originals are also redistributed here,
> unaltered, so the conversion can be verified against them.
>
> The accompanying `.mat` and `.prefab` files are **not** derived from the
> Licensed Material. They are separately authored engine metadata — shader and
> colour parameters, and transform hierarchies — that reference the meshes by
> identifier.
>
> This conversion is asserted to fall under §2(a)(4) of the licence, which
> authorises "technical modifications necessary" to exercise the Licensed
> Rights in a given medium and provides that such modifications "never produce
> Adapted Material".

**No endorsement:** nothing here implies that Toyota Motor Corporation endorses
this project or its use of the Licensed Material (§2(b)(3)).

Downstream users: the meshes reach you under CC BY-NC-ND, not under this
repository's MIT licence. You may redistribute them unmodified for
non-commercial purposes with the attribution above; you may not share modified
versions.

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
