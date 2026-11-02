# Chainsaw Warrior Access

A screen reader and keyboard accessibility mod for **Chainsaw Warrior** (Auroch Digital, Steam app 251710).
Every part of the game can be played without sight: menus, options, saving and loading, the intro comic,
character creation, the equipment shop, every house card, combat, dice rolls, tutorials, pop-up questions and the
end screen, plus a settings panel, sound cues and an English and Thai interface.

The mod speaks through **NVDA** directly, through **JAWS** when it runs, and through **Windows SAPI** voices otherwise.
Braille displays get the same text through NVDA.

## Installing

Download from the [Releases](https://github.com/wightfall/ChainsawWarriorAccess/releases) page, either:

- **`CWAccess-Setup-vX.Y.Z.exe`** (easiest): run it. It finds the game, shows what it will do in an **Events** list
  (one line per event, the newest selected), and installs with **Install or update**. Choose the setup's language in
  the **Setup language** box; tick **Use this language for the mod in the game** to make the mod speak it too, and
  **Start Chainsaw Warrior when setup finishes** to have the game started, with Play pressed in its settings window.
- or **`ChainsawWarriorAccess-vX.Y.Z.zip`**: extract it into the game folder, usually
  `C:\Program Files (x86)\Steam\steamapps\common\ChainsawWarrior`, then run `CWAccess\CWAccess-Setup.exe`.

When you start the game from Steam, a small window called **Chainsaw Warrior Configuration** appears first
(screen resolution). Press **Enter**: that is its **Play!** button. A few seconds later the game says
"Chainsaw Warrior Access loaded".

### Updates

The setup is copied to `CWAccess\CWAccess-Setup.exe` in the game folder. Run it any time: it checks GitHub, and when a
newer version exists it shows **What's new** (a list you can read line by line) before the **Yes** and **No** buttons.
If Steam updates the game or you use "Verify integrity of game files", run the setup again.

### Uninstalling

Run `CWAccess\CWAccess-Setup.exe` and press **Uninstall** (it asks whether to keep your settings).
From a command prompt: `CWAccess-Setup.exe --quiet --uninstall`.

### How the setup works

Chainsaw Warrior is built with Unity 4, which the usual mod loaders (BepInEx, MelonLoader) do not support.
The setup therefore adds one line to the game's `Assembly-CSharp.dll` on your computer: at the start of
`CUIManager.Initialise` it calls `CWAccess.Boot.dll`, which loads the mod from the `CWAccess` folder.
Your original file is kept as `Assembly-CSharp.dll.cwaccess-original` and is put back by Uninstall.
Nothing else in the game is changed. If the mod fails to start, the reason is in `CWAccess\boot.log`
and the game runs normally without it.

Setup command line: `CWAccess-Setup.exe [--quiet] [--install | --uninstall] [--game "folder"] [--mod-language th] [--start-game]`.

## Keys

Press **F1** on any screen to hear its keys (they are also listed: Up/Down to go through them, Escape to close).
Press **Shift+F1** for every key of the mod and the list of sound cues (Enter on a cue plays it).

### Everywhere

| Key | Action |
|-----|--------|
| Up / Down arrows, Tab / Shift+Tab | Move through the items of the screen |
| Home / End | First / last item |
| Enter | Press the button, choose the card |
| Space | Press the button; on card pickers only select the card |
| Left / Right arrows | Change a slider or setting; switch the equipment tab |
| I | Details of the item (everything printed on a card) |
| Escape or Backspace | Back / cancel; on the game board: pause menu |
| A letter (menus) | Jump to the next item starting with that letter |
| F1 / Shift+F1 | Keys of this screen / all keys and sound cues |
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
- The room on each new card, and the card when it is turned over.
- Every dice roll, yours and the enemy's, with the total.
- The choices available whenever they change.
- Changes of wounds, endurance, venom, radiation, hand-to-hand, marksmanship, reflexes and equipment points;
  each stat rolled during character creation.
- Equipment gained or lost, uses left and spare clips.
- The time left every 10 minutes of game time, and nightfall.
- The tutorial boxes, screen titles, questions and the end of game result.
- **The intro comic:** its five scenes are pictures with speech bubbles. The mod describes each scene, with all of
  its captions and dialogue, when it starts; the first item of the comic screen repeats the description.

### Cards and equipment

The game shows two cards at a time in a carousel. The mod lists **all** cards of the choice instead:
move with Up/Down, press **I** for the whole card. In the equipment shop, Enter buys a card or puts it back
(with the reason when the game refuses: not enough points, only one gun, only one heavy weapon, 13 cards at most),
and Left/Right switch between the tabs (Clothing, Devices, Guns, Hand-to-hand, Heavy weapons, Special).
On Medium and Classic difficulty the shop cards are face down, and the mod says only what a sighted player can see.
When choosing one card (a weapon to shoot with, an item to use, an enemy to fight), Enter chooses at once.

Guns list the score needed with 2 dice against each kind of target, already adjusted for your marksmanship
and a CATS sight, as printed on the card.

## Settings (F2)

Everything the mod does can be changed in the game with **F2** and is saved at once in `CWAccess\CWAccess.ini`:

- **Language** of the mod (automatic follows the system language when the mod has it; English; Thai).
- Saying the kind of control (button, slider) or only names.
- Each kind of announcement: choices, dice, stat changes, equipment, the room of each new card, the whole card or only
  its name when turned over, tutorial boxes, repeating the last message on the board, the comic descriptions, and
  how often the time left is said.
- **Sound cues** on or off, their overall volume, and the volume of each of the 25 cues (Left/Right change it and play
  the cue; I says what the cue means).
- Writing the speech to `CWAccess\CWAccess.log`, and **Reset all settings to their defaults** (press Enter twice).

## Sound cues

Short generated sounds (no sound files) for: moving, the edge of a list, pressing a button, unavailable or refused,
switched on and off, a new screen, lists opening and closing, new choices, a new face-down card, a card turned over,
an enemy, a trap, a safe room, dice, wounds lost and healed, venom or radiation, items gained and lost, cards selected
and unselected, the clock, and nightfall. Shift+F1 lists them all; Enter on one plays it.

## Languages

The mod and its setup come in **English** and **Thai** (ภาษาไทย). Language files are `CWAccess\Languages\<code>.txt`:
lines `key = text`, a `# Language: Name` header, and a `[setup]` section for the setup program. To add a language,
copy `th.txt`, translate the texts (keep `{0}`, `{1}` as they are) and choose it with F2. A personal
`CWAccess\strings.txt` in the same format replaces single texts.

The game's own texts (card texts, messages) are spoken as the game shows them, in English. They can be translated for
speech with `CWAccess\Languages\game\<code>.txt` (`[keys]` with the game's text keys, `[texts]` for texts shown on screen);
the game's English texts stay on screen because its fonts have no other alphabets.

## Building from source

Requirements: Windows, the .NET SDK (6 or newer), and Chainsaw Warrior installed (the mod compiles against the
game's DLLs, which are not part of this repository).

```powershell
.\tools\fetch-deps.ps1      # HarmonyX/MonoMod and the NVDA controller client into lib\
.\tools\package.ps1         # builds, runs the tests, makes dist\ChainsawWarriorAccess-vX.Y.Z.zip and CWAccess-Setup-vX.Y.Z.exe
dotnet test tests\CWAccess.Tests
```

If the game is not in the default Steam folder: `.\tools\package.ps1 -GameDir "D:\Games\ChainsawWarrior"`
(and `CWACCESS_GAME_DIR` for the tests). `package.ps1` also checks every program file of the release for signs of a
file-infecting virus before you publish.

While developing, `dotnet build src\CWAccess\CWAccess.csproj` copies the mod straight into the game's `CWAccess` folder
(close the game first). For automated testing, `CommandFile = true` in `CWAccess.ini` makes the mod read commands
from `CWAccess\commands.txt` (one per line, e.g. `key DownArrow`, `key E shift`, `dump`, `state`) and keep running
in the background.

Projects:

- `src/CWAccess` - the mod (C#, .NET 3.5 for the game's Unity 4.6 Mono runtime, HarmonyX patches).
- `src/CWAccess.Boot` - the tiny loader the game calls; loads the mod and logs any failure.
- `src/CWAccess.Installer` - `CWAccess-Setup.exe`: installer and updater (Windows Forms, Mono.Cecil).
- `src/CWAccess.SpeechHost` - speech through JAWS or SAPI for players without NVDA.
- `tests/CWAccess.Tests` - unit tests: language files and the Thai translation, settings, sound cues, the installer
  (a real install, update and uninstall on a copy of the game's DLL), and checks that every part of the game the mod
  reaches by name still exists.

This repository contains no code or assets of the game.

## License

MIT, see [LICENSE](LICENSE). Third-party components: see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
Chainsaw Warrior is © Games Workshop; this is an unofficial fan-made accessibility mod.
