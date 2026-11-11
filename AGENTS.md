# AGENTS.md — Sovereignty mod handover

Short context for AI / human contributors. Player-facing notes live in `README.md`.

## What this is

Community patch/mod for **Sovereignty: Crown of Kings**.

| Path | Role |
|------|------|
| `Patch/` | Modified game library source (`SovereigntyGameLib`) |
| `DecompiledSource/` | Original decompiled library (reference; don't treat as the live patch) |
| `GameData/` | Overlay for the game `Data/` folder (XML, UI scripts, campaign scripts, images) |

**Deploy:** copy `GameData/` contents into the game install `Data/` (overwrite). Build/replace `SovereigntyGameLib.dll` from `Patch/` (VS 2017+, .NET Framework 4.5, **Release/x86**).

This GitHub repo (`SzymonBylica/sovereigntyMod`) is the maintained line of work. Short `feat/...` branches off `main`; don't block on upstream PR merge.

## Hard rules

1. **Map / province data is fragile.** Don't add or renumber provinces (`provinceid`) on a feature branch that also changes gameplay unless pathing, outlines/hitmap, and scripts are updated together. ID gaps (e.g. sea zones vs land) can crash load via unsafe dictionary lookups.
2. **Saves break** after GameData province/map changes. Prefer new campaigns after such updates.
3. **Campaign scripts** that we override live under `GameData/Data/Scripts/Campaigns/` (currently `Riesental/`, `Dunmar/`). Null-check `GetProvince` / `LandNode` before use.
4. Keep PRs small: one feature or bugfix per branch.

## Known sharp edges

- **`CampaignMap.UpdateRealmNameText`** — resolves contiguous provinces by name/`ContainsKey` (not raw `AllProvinces[xmlId]`). Rebuild+deploy `SovereigntyGameLib.dll` after changing it.
- **Live install leftovers** — map experiments can leave `hitmap.png`, `Maps/Default/nodes.xml`, `Maps/Default/meshdata.dat`, and Rivenbank rows in install `Data/` even after `GameData/` is clean. Restore from `_backup_rivenbank` / `hitmap.png.bak_rivenbank` (or vanilla) and redeploy `GameData/Data/sov_*.xml`. Stale `nodes.xml` with province 270 + cleaned XML crashes `RegionOutlineRenderer.Update`.
- **Dunmar diplomatic choice** — must listen to both `AutoBattleStart` and `FullBattleStarted` (tactical), or the Cor Vilaad / Dragonhold popup never appears.
- **Riesental** — province names in scripts must match `sov_data.xml` exactly (ASCII apostrophes, etc.).

## Good next work

Small UI polish, crash null-guards in campaign scripts, battle/UX tweaks. Map experiments → separate long-lived branch only.

## Key files

- `Patch/SovereigntyGameLib/UI/Map/CampaignMap.cs` — realm names, map UI hooks
- `GameData/Data/Scripts/Campaigns/Dunmar/Dunmar1.cs`
- `GameData/Data/Scripts/Campaigns/Riesental/Riesental1.cs`
- `GameData/Data/sov_data.xml` / `sov_text.xml` — units, provinces, text
- `README.md` — changelog for players
