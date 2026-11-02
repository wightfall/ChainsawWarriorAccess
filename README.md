# Chainsaw Warrior Access

A screen reader and keyboard accessibility mod for **Chainsaw Warrior** (Auroch Digital, Steam app 251710).
Every part of the game can be played without sight: menus, options, saving and loading, character creation,
the equipment shop, every house card, combat, dice rolls, tutorials, pop-up questions and the end screen.

The mod speaks through **NVDA** directly, through **JAWS** when it runs, and through **Windows SAPI** voices otherwise.
Braille displays get the same text through NVDA.

## Installing

1. Download `ChainsawWarriorAccess-vX.Y.Z.zip` from the [Releases](https://github.com/wightfall/ChainsawWarriorAccess/releases) page.
2. Extract it into the game folder, usually
   `C:\Program Files (x86)\Steam\steamapps\common\ChainsawWarrior`.
   You get a folder `CWAccess` next to `CWSteamPC.exe`.
3. Run `CWAccess\CWAccess-Setup.exe` once. It says "Installed" when done (press Enter to close it).
4. Start the game from Steam. First a small Windows window called **Chainsaw Warrior Configuration** appears
   (screen resolution and quality). Press **Enter** to press its **Play!** button.
5. The game says "Chainsaw Warrior Access loaded".

If Steam updates the game or you use "Verify integrity of game files", run `CWAccess-Setup.exe` again.

### Uninstalling

Run `CWAccess\CWAccess-Setup.exe --uninstall` from a command prompt, then delete the `CWAccess` folder.
(Or use Steam's "Verify integrity of game files" and delete the `CWAccess` folder.)

### How the setup works

Chainsaw Warrior is built with Unity 4, which the usual mod loaders (BepInEx, MelonLoader) do not support.
The setup therefore adds one line to the game's `Assembly-CSharp.dll` on your computer: at the start of
`CUIManager.Initialise` it calls `CWAccess.Boot.dll`, which loads the mod from the `CWAccess` folder.
Your original file is kept as `Assembly-CSharp.dll.cwaccess-original` and is put back by `--uninstall`.
Nothing else in the game is changed. If the mod fails to start, the reason is in `CWAccess\boot.log`
and the game runs normally without it.

## Keys

Press **F1** anywhere for the keys of the current screen.

### Everywhere

| Key | Action |
|-----|--------|
| Up / Down arrows, Tab / Shift+Tab | Move through the items of the screen |
| Home / End | First / last item |
| Enter | Press the button, choose the card |
| Space | Press the button; on card pickers only select the card |
| Left / Right arrows | Change a slider; switch the equipment tab |
| I | Details of the item (everything printed on a card) |
| Escape or Backspace | Back / cancel; on the game board: pause menu |
| A letter (menus) | Jump to the next item starting with that letter |
| F1 | Keys help |
| F2 | Mod settings |
| F5 | Say where you are again |
| F12 | Find the screen reader again (after starting or closing NVDA or JAWS) |
| Control | Stop speech |

### Game board

| Key | Action |
|-----|--------|
| 1 to 4 | Use choice 1 to 4 (Shoot, Hand-to-hand, Escape, Next...) |
| F | Turn over the house card |
| A | Say the available choices |
| R | Repeat the last message |
| C | Describe the current card (room, enemy, trap, other enemies present, combat round) |
| S | Full status |
| W | Wounds, endurance, venom and radiation |
| H | Hand-to-hand, marksmanship, reflexes and skill |
| T | Time left (and whether it is night) |
| N | Which deck and card you are on, cards left to escape the building |
| D | Last dice roll |
| P | Equipment points |
| E | List your equipment (Up/Down, I for the whole card, Escape to close) |
| Shift+E | Open the game's equipment card screen |
| L | Game log: everything that happened, newest last |

## What is spoken automatically

- Every message of the game's message box (encounters, rolls needed, results, tutorials).
- The room on each new card, and the whole card when it is turned over.
- Every dice roll, yours and the enemy's, with the total.
- The choices available whenever they change.
- Changes of wounds, endurance, venom, radiation, hand-to-hand, marksmanship, reflexes and equipment points.
- Equipment gained or lost, uses left and spare clips.
- The time left every 10 minutes of game time (changeable).
- The tutorial boxes, screen titles, questions and the end of game result.

Each of these can be switched off with **F2** (saved in `CWAccess\CWAccess.ini`).

### Cards and equipment

The game shows two cards at a time in a carousel. The mod lists **all** cards of the choice instead:
move with Up/Down, press **I** for the whole card. In the equipment shop, Enter buys a card or puts it back
(with the reason when the game refuses: not enough points, only one gun, only one heavy weapon, 13 cards at most),
and Left/Right switch between Clothing, Devices, Guns, Hand-to-hand, Heavy weapons and Special.
On Medium and Classic difficulty the shop cards are face down, and the mod says only what a sighted player can see.

Guns list the score needed with 2 dice against each kind of target, already adjusted for your marksmanship
and a CATS sight, as printed on the card.

## Translating

The mod's own texts can be replaced without rebuilding: create `CWAccess\strings.txt` with lines `key=text`
(`\n` for a line break). The keys and English texts are in [`src/CWAccess/Loc.cs`](src/CWAccess/Loc.cs).
The game's own texts are spoken as the game shows them.

## Building from source

Requirements: Windows, the .NET SDK (6 or newer), and Chainsaw Warrior installed (the mod compiles against the
game's DLLs, which are not part of this repository).

```powershell
.\tools\fetch-deps.ps1      # HarmonyX/MonoMod and the NVDA controller client into lib\
.\tools\package.ps1         # builds everything, makes dist\ChainsawWarriorAccess-vX.Y.Z.zip
```

If the game is not in the default Steam folder: `.\tools\package.ps1 -GameDir "D:\Games\ChainsawWarrior"`.
While developing, `dotnet build src\CWAccess\CWAccess.csproj` copies the mod straight into the game's `CWAccess` folder
(close the game first). For automated testing, `CommandFile = true` in `CWAccess.ini` makes the mod read key commands
from `CWAccess\commands.txt` (one per line, e.g. `key DownArrow`, `key E shift`, `dump`) and keep running in the background.

Projects:

- `src/CWAccess` - the mod (C#, .NET 3.5 for the game's Unity 4.6 Mono runtime, HarmonyX patches).
- `src/CWAccess.Boot` - the tiny loader the game calls; loads the mod and logs any failure.
- `src/CWAccess.Installer` - `CWAccess-Setup.exe`: adds / removes the loader call (Mono.Cecil).
- `src/CWAccess.SpeechHost` - speech through JAWS or SAPI for players without NVDA.

This repository contains no code or assets of the game.

## License

MIT, see [LICENSE](LICENSE). Third-party components: see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
Chainsaw Warrior is © Games Workshop; this is an unofficial fan-made accessibility mod.
