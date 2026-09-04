# OblivionMP Arena Template

A working free-for-all arena game mode for OblivionMP, the community multiplayer mod for The Elder Scrolls IV: Oblivion Remastered. It is built on the OblivionMP mod template and covers most of what a server-side game mode needs: server RPC, per-tick systems, ECS queries against player state, area events, HUD messages and key bindings on the client.

Use it as a starting point for your own mode. The Known Limitations section at the bottom says what does not work yet.

Refer to the [OblivionMP SDK documentation](https://docs.ready.mp) for the SDK itself.

## Requirements

* [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later
* If you are using Visual Studio, you need version 2026 or later
* To test it, an OblivionMP server you can drop mods into and at least two clients

## Repository structure

An OblivionMP mod is two halves that share a common core. Client mods are handed to players by the server when they join. Server mods run on the relay and never leave it.

- `ArenaMod.Common/`: shared by both halves. `RpcContracts.cs` declares every message the arena sends, `RArenaCommon.cs` holds the arena cell id, center, radii and timers.
- `ArenaMod.Client/`: loaded by the game. `Mod.cs` is the entry point and binds keys, `ExampleServerRpc.cs` receives server messages and drives the HUD, `DetectDeadPlayerSystem.cs` prints the rules when you die, `RArenaClient.cs` holds the HUD strings.
- `ArenaMod.Server/`: loaded by the relay. `Mod.cs` registers everything. `RArenaPlugin/RArenaSystem.cs` is the match loop, `RArenaConditions.cs` the loss and win rules, `RArenaAreaEventHandlers.cs` reacts to players entering and leaving the arena cell, `RArenaSaveFileDispatcher.cs` hands connecting players a preset save file.
- `Content/manifest.json`: mod id, name, version, dependencies.
- `Dependencies/`: the OblivionMP SDK assemblies the projects compile against. `Client/` is what the game provides, `Server/` what the relay provides. They are referenced with `Private=false` and never copied into your mod.

## How the arena plays

1. Walk onto the arena floor and you are registered as a contender. The HUD shows how many are registered and how many are ready.
2. Step into the inner ring to ready up. Step out and you are not ready. Walk off the floor and you are removed.
3. Nobody can die before the match starts. The server heals registered players every tick.
4. When more than half of the contenders are ready, a 30 second countdown starts. Anyone still not ready at zero is dropped. When everyone left is ready, the fight begins.
5. Dropping to 30 percent health eliminates you. Leaving the floor, or the cell, counts as giving up. Eliminated players and spectators are teleported to the stand.
6. Last contender standing wins. The match resets and the floor is open again.

Ctrl+Shift+P respawns you if you die. Debug builds also bind numpad keys to heal, heal everyone and teleport to the stand, for testing on your own server.

## Getting started

1. Clone this repository.
2. Open `ArenaMod.sln` in your C# IDE and build it. All dependencies resolve from `Dependencies/`.
3. Package it:

   ```powershell
   .\MakeModFolder.ps1 Release
   ```

   Use `Debug` instead to include `.pdb` symbol files. Pass `-NoExplorer` to skip opening the output folder.

4. `Output/mods/ArenaMod/` now holds `manifest.json`, `client/` and `server/`. Copy that folder into your server's `mods/` directory next to the `OblivionMp.Sdk` package, which the manifest depends on, and restart the server. Connecting players receive the client half automatically.
5. Optional: put one or more `.sav` files in `saves/arena/` next to the relay. Each connecting player gets a copy of a random one as a preset character. With no files there, players keep their own character.

If you downloaded this as a ZIP rather than cloning, Windows marks the scripts as remote and PowerShell refuses to load them. Clear the mark once:

```powershell
Get-ChildItem -Recurse | Unblock-File
```

## Making it yours

Rename the mod first, so it does not collide with this one on a server:

```powershell
.\RenameMod.ps1 MyStudio.MyArena
```

That renames the projects, folders, solution, namespaces and the manifest id. Then edit `Content/manifest.json` (name, author, version, description) and, if you rename projects by hand later, `ModFiles.ps1`.

Where to change what:

- **Location.** `RArenaCommon.Data` in Common: cell id, center, outer radius, inner ring, spectator stand. The cell id is also written out literally in `RArenaAreaEventHandlers.cs`, in two places. Change those too.
- **Elimination rule.** `RArenaSystem.Init` constructs `RArenaPlayerLossCondition` with a 30 percent threshold. Pass `EArenaLossCondition.PlayerDead` for a fight to zero, or change `targetHpPercentage`. The death message in `DetectDeadPlayerSystem.cs` hard-codes the percentage, so update it together.
- **Win rule.** Implement `IRArenaCondition` (a `Priority`, `OnUpdate`, `WasFullfilled`, `Reset`), add it to `rArena.ArenaWinningConditions` in `RArenaSystem.Init`, and add a `case` for it in `ConcludeArenaMatch` so a result gets announced. Higher priority is checked first.
- **Messages between client and server.** Add a method to `RpcContracts.cs`, rebuild, implement the generated `partial void On...` on the receiving side and call the generated `Send...` on the sending side. Server-side sends take the target `PlayerId` first.
- **HUD text.** All player-facing strings are in `RArenaClient.cs`.
- **Countdown.** `RArenaTimerCountdown.InitializeTimerStamps(30, ...)` in `RArenaSystem.Init`. The client mirrors the 30 seconds in its own `RArenaTimerCountdown` for display, so change both.
- **Replicated state.** The arena keeps match state on the server and pushes it out over RPC. If you want state every client can read directly, such as a score shown over each player, define a networked component in Common and attach it to an archetype on both sides. The commented-out `WalletComponent` lines in both `Mod.cs` files mark where that goes, and the SDK docs walk through it.

Client systems read `tick.deltaTime`, server systems read `tick.DeltaTime`. You will hit that the first time you copy a system between halves.

## Known limitations

- Only free-for-all last-one-standing works. Tournament, teams, kill count and best-of-N exist as enums and stubs.
- Server-side teleport is unreliable. Setting a player's position from the server does not always move them, which affects the spectator stand.
- Preset save files merge badly with the world save. We have seen the character come back with the loadout's appearance and the world save's attributes and inventory.
- Gear durability carries over between matches, so a long session on one loadout degrades.
- `PlayerEleminated` and `AnnouneWinnerTeam` are misspelled in the contract. The generator uses the names as written, so fixing them touches both sides.

## Links

- SDK docs: https://docs.ready.mp
- Discord: https://discord.gg/obmp
- Server owner early access: https://oblivionmp.firstlook.gg

---

OblivionMP is developed by ReadyCode Limited. Backed by Sony Innovation Fund, London Venture Partners, and Lifelike Capital.

Not an official product of Bethesda Softworks or Virtuos. Not affiliated with either studio. You need your own copy of The Elder Scrolls IV: Oblivion Remastered to play.
