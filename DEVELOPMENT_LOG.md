# Development Log

## 2026-04-28 - RimWorld 1.6 load failure fixed

### Symptoms

- RimWorld entered a black screen with only the mouse cursor visible.
- The mod configuration list failed to load and was reset to empty.
- Forcing the mod list caused the game to black screen during startup.

### Root Cause

- `Player.log` showed RimWorld resetting the mod config after a `TypeLoadException`:
  `expected class 'Harmony.HarmonyPatch' in assembly '0Harmony, Version=1.2.0.1'`.
- The mod assembly was compiled against Harmony 1 (`Harmony.HarmonyPatch`, `HarmonyInstance`), while the RimWorld 1.6 setup loaded the official Harmony 2 mod (`brrainz.harmony`).
- Several language XML files also contained invalid XML, which could create additional load failures after the Harmony issue was fixed.

### Fixes

- Migrated Harmony startup code to Harmony 2 API:
  - `using HarmonyLib`
  - `new Harmony("akreedz.rimworld.antiairweapon").PatchAll(...)`
- Added `brrainz.harmony` as a mod dependency and load-after entry.
- Updated the project/build configuration so Harmony and HugsLib are referenced from separate assembly directories.
- Rebuilt `AntiAirWeapon.dll` and updated the local test package under `2802147499/1.6/Assemblies`.
- Repaired invalid Simplified Chinese, Traditional Chinese, and Japanese language XML files.

### Verification

- RimWorld successfully loaded the mod after the fix.
- Release build completed with only unused-variable warnings.
- All XML files parse successfully as UTF-8 XML.
- The rebuilt assembly references Harmony 2 instead of Harmony 1.

### Next Development Topic

- Current mod settings require manually typing `defName` values for allow/deny/intercept lists.
- Next step: design a friendlier settings UI for choosing target defs from discovered Skyfaller/projectile definitions.

## 2026-04-28 - Target selector settings UI

### Goal

- Replace manual `defName` entry as the primary workflow for interception rules.
- Keep the saved format as `defName` strings for backwards compatibility.
- Make the UI stable for large mod lists and safe for RimWorld 1.6.

### Implementation

- Added a HugsLib `CustomDrawer` summary row for each rule list:
  - Never intercept
  - Always intercept
  - Intercept if hostile
- Added a selector window with search, filters, fixed-height rows, and a 200-result display cap.
- Added candidate scanning from loaded `ThingDef`s for projectiles, overhead projectiles, skyfallers, and drop pod/transporter-like targets.
- Added automatic rule moving so a `defName` only appears in one rule list at a time.
- Added missing-item preservation for saved `defName`s that are not present in the current mod list.
- Added recently observed air target tracking in the world component, capped at 100 entries.
- Added an advanced toggle to expose the raw `defName` text editor only when needed.

### Compatibility Notes

- HugsLib references now prefer the RimWorld 1.6 assembly folder.
- The implementation intentionally avoids `CustomDrawerFullWidth` and `ForceSaveChanges`.
- Additional Unity UI module references are compile-time only and are not copied into the mod package.

### Verification

- Release build passes against RimWorld 1.6 managed assemblies.
- No source references to `CustomDrawerFullWidth`, `ForceSaveChanges`, old Harmony namespace, or `HarmonyInstance`.
- All XML files parse successfully as UTF-8 XML.
- The rebuilt assembly references RimWorld 1.6, Harmony 2, HugsLib, and the Unity UI modules required by the selector UI.
