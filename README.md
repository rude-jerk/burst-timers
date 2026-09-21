# Burst Timers

Standalone Dalamud API 15 plugin using Cactus Watcher's timeline rendering style. Cactus Watcher is not required.

The renderer and font loader are adapted from Cactus Watcher commit `dc2c33ca96799a7c183ad6d489b47940fd3b28f4`, matching the installed version. It uses native ImGui progress bars (including the current Dalamud theme's background, rounding and borders), Noto Sans CJK Regular at 23 pixels, and a stationary `02:00.00 - Dokumori` caption at a 10% inset. The bar is 98% of the 340-pixel window width and 30 pixels high, matching the installed settings. Bars start full and drain toward readiness and use Cactus Watcher's preview palette: periwinkle, then salmon for the final 20%. Live Cactus Watcher event colors can vary with the encounter's Cactbot timeline.

## Load

Install **Burst Timers** from the Dalamud Plugin Installer after adding this custom repository in `/xlsettings` under **Experimental**:

```text
https://raw.githubusercontent.com/rude-jerk/dalamud-plugins/refs/heads/main/repo.json
```

Use `/bursttimers` to position and lock the bars. If you previously loaded the development DLL, disable that copy before installing the release.

### Development loading

1. Open Dalamud settings with `/xlsettings` and select **Experimental**.
2. Add the absolute path to your built `bin/Release/BurstTimers.dll` under **Dev Plugin Locations** and save.
3. Open `/xlplugins`, find **Burst Timers** under development plugins, and enable it.
4. Use `/bursttimers` for settings. Drag the preview bars into place, then enable **Lock bars**. Locked bars are click-through and disappear when no timers are running.

## Behavior

- Seeing Dokumori (status 3849) on your current hard target starts one 120-second countdown, regardless of which Ninja applied it.
- Additional applications, multiple Ninjas, and target changes cannot restart or duplicate an active countdown. Losing the target or the debuff does not stop it.
- The countdown starts when the debuff is first detected, so selecting a target after application starts a full 120 seconds from that detection. This is an estimate of the next Dokumori, not a read of another player's cooldown.
- After expiration, a new detection starts the next countdown. An uninterrupted existing detection does not automatically restart it.
- Embolden, Starry Muse, Brotherhood, and Searing Light each start an independent 120-second countdown when detected on your character, including buffs from other players. Embolden recognizes both its caster status (1239) and party status (1297); the other status IDs are 3685, 1185, and 2703 respectively.
- Each party buff has only one timer, regardless of how many players apply it. Reapplications never restart an active timer. Buff expiration, death, or losing the target do not stop a countdown. These timers estimate cooldowns from first detection, rather than reading another player's recast data. A buff used outside your range that never reaches you cannot be detected.
- Potion reads your actual shared combat stat-potion recast (group 58), respecting HQ/NQ cooldowns and game cooldown resets. It also detects a cooldown already running when the plugin loads. Healing/mana potions are excluded.
- Dokumori persists across combat and territory changes and resets on logout or plugin reload. Use **Clear Dokumori timer** to clear an estimate after a wipe; a currently visible debuff will be detected again on the next update.
- Party buff timers also persist across combat and territory changes and reset on logout or plugin reload. **Clear party buff timers** clears all four estimates; buffs still on you will be detected again on the next update.
- Settings include preview, drag positioning, width, height, and text size. Colors and label layout follow the Cactus Watcher style.

## Build and verification

`dotnet build BurstTimers.csproj -c Release`

Requires the installed Dalamud development assemblies and .NET 10 SDK. Run regression checks with `dotnet run --project tests/BurstTimers.Checks.csproj`.

Live check: apply Dokumori to your selected target, confirm a single 2:00 bar, switch targets and apply a second Dokumori while it runs, and confirm the original countdown continues. Use a combat stat potion and compare the potion bar with the item hotbar cooldown. Lock bars to verify click-through behavior and automatic hiding at expiration.

Party buff live check: receive each of the four buffs and verify one labeled two-minute bar per buff. Have a second player apply the same buff during the countdown and verify it does not restart. On Red Mage, verify your own Embolden also starts the bar. Preview shows all six bars to check spacing.

Cooldown references: official job guides for [Red Mage](https://na.finalfantasyxiv.com/jobguide/redmage/), [Pictomancer](https://na.finalfantasyxiv.com/jobguide/pictomancer/), [Monk](https://na.finalfantasyxiv.com/jobguide/monk/), and [Summoner](https://na.finalfantasyxiv.com/jobguide/summoner/).

Recast API reference: https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/ActionManager.cs

Cactus Watcher source: https://codeberg.org/joshua-software-dev/CactusWatcher

Adapted Cactus Watcher code is copyright (c) 2024 joshua-software-dev, licensed under MIT; see `LICENSE.CactusWatcher`.

