# Third-party software

The release package of Chainsaw Warrior Access contains these unmodified third-party files.
They are not stored in this repository; `tools/fetch-deps.ps1` downloads them from their official sources.

| File | Project | License | Source |
|------|---------|---------|--------|
| `0Harmony.dll` | HarmonyX | MIT | BepInEx 5.4.23.2 x86 release, https://github.com/BepInEx/HarmonyX |
| `MonoMod.RuntimeDetour.dll`, `MonoMod.Utils.dll` | MonoMod | MIT | BepInEx 5.4.23.2 x86 release, https://github.com/MonoMod/MonoMod |
| `Mono.Cecil.dll` | Mono.Cecil | MIT | BepInEx 5.4.23.2 x86 release, https://github.com/jbevain/cecil |
| `nvdaControllerClient32.dll` | NVDA controller client (32-bit, renamed from `nvdaControllerClient.dll`) | LGPL 2.1, see `NVDA-controller-client-license.txt` | NV Access, https://download.nvaccess.org/releases/2024.4.2/ |

The setup program (`CWAccess-Setup.exe`) embeds Mono.Cecil 0.11.6 (MIT, https://github.com/jbevain/cecil).

Chainsaw Warrior is © Games Workshop, published by Auroch Digital. This project contains no game code or game assets.
