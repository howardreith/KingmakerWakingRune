# The Waking Rune: tester checklist and bug template

For playtesters of the WR-FINAL draft. Play as a player would: no console, no cheats, no mods beyond this one. Note the
mod version from the Unity Mod Manager window (Ctrl+F10) on every report.

## Before you start
- [ ] Pathfinder: Kingmaker 2.1.7 with Unity Mod Manager; only this mod enabled.
- [ ] Back up your saves folder (`%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`).
- [ ] Note your difficulty preset and whether you play real-time or turn-based.

## The journey (tick each when it worked; note the save name you made there)
| # | Step | Save here | Expected |
|---|---|---|---|
| 1 | New Game → The Waking Rune → difficulty → character creation | | the scenario's lodge loads, not the stock prologue |
| 2 | Lodge: briefing, recruitment, Quartermaster, rest | ✓ | companions recruitable; stock and supplies offered once |
| 3 | Take the refuge token | ✓ | travel to Kurshu's chamber (A1) |
| 4 | A1: Kurshu | ✓ after | the fight resolves; he stays down |
| 5 | A2–A3: the door, wards, statues, runes | ✓ | the door and wards behave as described in game texts |
| 6 | A4: Vandiana and her defenders | ✓ before and after | |
| 7 | A5: Krune awakens (parley, bark or unobserved) | ✓ before the exchange | no "development checkpoint" ever appears |
| 8 | The battle with Krune | ✓ during (if allowed) | he casts, summons, relocates, uses his gear |
| 9 | His death | ✓ after | sarcophagus, empty gem and foci crumble; light fades; mist goes; one loot bag |
| 10 | Loot: spear and rod | | both in your inventory; neither can be dropped |
| 11 | A4 pedestal: look, then step through | (auto) | warnings if anything is missing; "Before the portal home" save |
| 12 | Lodge debrief | ✓ after | thanks, handover of both artifacts, epilogue; masked figure leaves |
| 13 | Reload the lodge save | | the debrief does not repeat |

Do the battle once in real time and once in turn-based if you can.

## Other paths worth one try each
- [ ] **Withdraw:** step through the portal before Krune is slain (warned: one way). Expect the withdrawn debrief.
- [ ] **Leave an artifact behind:** after his death, go home without the spear or the rod. Expect a warning, then the
      partial debrief.
- [ ] **Defeat:** if the main character falls, expect the game's own game-over screen; Load Last Save restores.
- [ ] **Take the spear in hand:** a party member who equips it rolls Will DC 22 against its Ego each time; from the
      second try it strikes first (1d8+9); on a success it then tries to wrench itself free every round.
- [ ] **Use the rod:** with the rod carried, a caster's abilities panel has four rod toggles; a toggled spell shows the
      metamagic and the log names the charges spent.
- [ ] **Save and load during the Krune fight** (if the game allows a save), including while his spear dances.
- [ ] **Second new game** in the same session, and a **stock campaign** after: neither shows scenario content.

## Bug report template
```
Title: <one line: what went wrong, where>
Mod version: <from UMM>      Game: 2.1.7      Mode: real-time / turn-based      Difficulty: <preset>
Step: <number from the journey table, or "other path: ...">
What I did: <the clicks/orders, in order>
What I expected:
What happened instead: <exact text of any message>
Reproducible: always / sometimes / once     From save: <save name, attached if you can>
Attachments: UMM log (Kingmaker_Data\Managed\UnityModManager\Log.txt, copied BEFORE restarting the game), screenshot
```
