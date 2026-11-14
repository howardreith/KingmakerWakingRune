# WR-PLAYTEST-01: Quartermaster stock, Wizard scroll and skill coverage

Generated in game by the harness's READ command `stockaudit` (0.10.0, run `wrplay-p1`), from the live blueprints after every mod had published its content. Two profiles were audited:

- **No provider:** Lines: 446; items: 20292. Wizard audit: Wizard spells 0-6: 178; with scrolls: 178; wrappers: 0 (a labelling defect of that build: the shop pass counted the wrappers as native scrolls; fixed in `2cae765`, the load log shows 37 wrappers built); recorded exceptions: 0; unexplained omissions: 0 (PASS).
- **With Mr. Bard's Tabletop Rules Expansion 0.0.188:** Lines: 540; items: 30843. Optional provider: 0.0.188 enabled; 51 lines. Wizard audit: Wizard spells 0-6: 179; with scrolls: 179; wrappers: 38; recorded exceptions: 0; unexplained omissions: 0 (PASS).

(The no-provider audit above was taken before the catalogue additions of commit `2cae765`; the tables below are the provider profile on the final catalogue. The CSV part of the catalogue is listed in `docs/QUARTERMASTER.md`.) A *Waking Rune wrapper* is a scroll built on the game's own working spell where the game ships no scroll of it: the Wizard level, the minimum caster level and the native price (caster level × spell level × 25 gp; 12 gp for a cantrip).

## Ability and skill coverage (from enchantment effects)

| Stat | Stocked items (bonus, slot, price) |
|---|---|
| Strength | Belt of Giant Strength +2 (+2, belt, 4,000); Belt of Physical Flow +2 (+2, belt, 10,000); Belt of Physical Might +2 (+2, belt, 10,000); Belt of Giant Strength +4 (+4, belt, 16,000); Belt of Physical Perfection +2 (+2, belt, 16,000) |
| Dexterity | Belt of Incredible Dexterity +2 (+2, belt, 4,000); Belt of Physical Flow +2 (+2, belt, 10,000); Belt of Physical Form +2 (+2, belt, 10,000); Belt of Incredible Dexterity +4 (+4, belt, 16,000); Belt of Physical Perfection +2 (+2, belt, 16,000) |
| Constitution | Explorer's Belt (+2, belt, 2,500); Belt of Mighty Constitution +2 (+2, belt, 4,000); Belt of Physical Might +2 (+2, belt, 10,000); Belt of Physical Form +2 (+2, belt, 10,000); Belt of Mighty Constitution +4 (+4, belt, 16,000); Belt of Physical Perfection +2 (+2, belt, 16,000) |
| Intelligence | Headband of Vast Intelligence +2 (+2, head, 4,000); Headband of Mental Perfection +2 (+2, head, 10,000); Headband of Vast Intelligence +4 (+4, head, 16,000) |
| Wisdom | Headband of Inspired Wisdom +2 (+2, head, 4,000); Headband of Mental Perfection +2 (+2, head, 10,000); Headband of Inspired Wisdom +4 (+4, head, 16,000) |
| Charisma | Headband of Alluring Charisma +2 (+2, head, 4,000); Thaumaturgist's Cloak (+2, shoulders, 7,800); Headband of Mental Perfection +2 (+2, head, 10,000); Headband of Alluring Charisma +4 (+4, head, 16,000) |
| SkillAthletics | Riversong Amulet (+5, neck, 22,000); Athlete's Gloves (+3, gloves, 25,000) |
| SkillMobility | Boots of Elvenkind (+5, feet, 2,500); Athlete's Gloves (+3, gloves, 25,000) |
| SkillThievery | Trapspringer's Gloves (+5, gloves, 4,000); Rogue's Companion (+5, shoulders, 10,100) |
| SkillStealth | Shadow Leather +1 (+5, armor: Light, 1,160); Cloak of Shadows (+10, shoulders, 6,000) |
| SkillKnowledgeArcana | Thaumaturgist's Cloak (+5, shoulders, 7,800); Robe of the Wise (+3, armor: None, 12,000) |
| SkillKnowledgeWorld | Thaumaturgist's Cloak (+5, shoulders, 7,800); Robe of the Wise (+3, armor: None, 12,000) |
| SkillLoreNature | Robe of the Wise (+3, armor: None, 12,000) |
| SkillLoreReligion | Robe of the Wise (+3, armor: None, 12,000) |
| SkillPerception | Eyes of the Eagle (+5, head, 2,500) |
| SkillPersuasion | Ring of the Natural Born Commander (+3, ring, 9,000); Riversong Amulet (+5, neck, 22,000); Golden Tongue (+5, neck, 29,500) |
| SkillUseMagicDevice | Ring of Magic Devices (+5, ring, 16,000); Master Hand (+5, gloves, 25,000) |
| CheckBluff |  |
| CheckDiplomacy |  |
| CheckIntimidate |  |

## Wizard scrolls, levels 0-6

| Level | Spell | Scroll | SL | CL | Price | Count | Source |
|---:|---|---|---:|---:|---:|---:|---|
| 0 | Acid Splash | Scroll of Acid Splash `b7567615bc77660b75e96ede61b818de` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Daze | Scroll of Daze `ac3a1d708c83d301c7e921b07cc1400e` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Disrupt Undead | Scroll of Disrupt Undead `88e16c1a9a4993e574b0300477a7cf80` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Flare | Scroll of Flare `fe0bf7f7f0381a5ef53226a541eafd76` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Jolt | Scroll of Jolt `b2ca055a8538f95ace16ef4864f729a5` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Light | Scroll of Light `2b51be19976a3f1669ea663c9eff0592` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Ray of Frost | Scroll of Ray of Frost `217900376a2a831c012aec4c18a5b96b` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Resistance | Scroll of Resistance `f66fb91178a1b8a5a74f8d4a9313f416` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 0 | Touch of Fatigue | Scroll of Touch of Fatigue `8fc63144b1b4b9fb27db4b3f56bb1103` | 0 | 1 | 12 | 99 | Waking Rune wrapper |
| 1 | Burning Hands | Scroll of Burning Hands `3c56e535129756e449af6c0e67fd937f` | 1 | 1 | 25 | 99 | native |
| 1 | Cause Fear | Scroll of Cause Fear `fe244c39bdd5cb64eae65af23c6759de` | 1 | 1 | 25 | 99 | native |
| 1 | Color Spray | Scroll of Color Spray `334842bcad874674b92f115412fd5d08` | 1 | 1 | 25 | 99 | native |
| 1 | Corrosive Touch | Scroll of Corrosive Touch `68d5aa212b7323e4e95e0fe731ea50cf` | 1 | 1 | 25 | 99 | native |
| 1 | Ear-Piercing Scream | Scroll of Ear-Piercing Scream `17afee356af23734393bb157daa8b29c` | 1 | 1 | 25 | 99 | native |
| 1 | Enlarge Person | Scroll of Enlarge Person `affbadb670f599b4084029e5ad784cb7` | 1 | 1 | 25 | 99 | native |
| 1 | Expeditious Retreat | Scroll of Expeditious Retreat `fbf7f9d21c12146479a5c261743afbde` | 1 | 1 | 25 | 99 | native |
| 1 | Flare Burst | Scroll of Flare Burst `2af933b93ac31ba449001e7e3b911b5b` | 1 | 1 | 25 | 99 | native |
| 1 | Grease | Scroll of Grease `a4fbba95ffa58144ca7189bc350ed622` | 1 | 1 | 25 | 99 | native |
| 1 | Hurricane Bow | Scroll of Hurricane Bow `ce41e625eae914d4bad729f090e9001f` | 1 | 1 | 25 | 99 | native |
| 1 | Hypnotism | Scroll of Hypnotism `78da1d5a10d8e384285e54df202dfb01` | 1 | 1 | 25 | 99 | native |
| 1 | Mage Armor | Scroll of Mage Armor `e8308a74821762e49bc3211358e81016` | 1 | 1 | 25 | 99 | native |
| 1 | Magic Missile | Scroll of Magic Missile `63caf94a780472b448f50d0bc183c38f` | 1 | 1 | 25 | 99 | native |
| 1 | Protection from Alignment | Scroll of Protection from Alignment `e12ba1c92eea8b626c3c2aed166b9ee8` | 1 | 1 | 25 | 99 | Waking Rune wrapper |
| 1 | Ray of Enfeeblement | Scroll of Ray of Enfeeblement `792862674c565ad4fbb1ab0c97c42acd` | 1 | 1 | 25 | 99 | native |
| 1 | Ray of Sickening | Scroll of Ray of Sickening `37b332a8c7d904e47b46b5df49e62057` | 1 | 1 | 25 | 99 | native |
| 1 | Reduce Person | Scroll of Reduce Person `a341ba25915761441905bc6c82677244` | 1 | 1 | 25 | 99 | native |
| 1 | Shield | Scroll of Shield `807763fd874989e4d96eb2d8e234139e` | 1 | 1 | 25 | 99 | native |
| 1 | Shocking Grasp | Scroll of Shocking Grasp `e10f63bd61591554791d133270f8d80f` | 1 | 1 | 25 | 99 | native |
| 1 | Sleep | Scroll of Sleep `f79c3fd5012a3534c8ab36dc18e85fb1` | 1 | 1 | 25 | 99 | native |
| 1 | Snowball | Scroll of Snowball `66fc961f9c39ae94fb87a79adc87212e` | 1 | 1 | 25 | 99 | native |
| 1 | Stone Fist | Scroll of Stone Fist `42d9445b9cdfac94385eaa2a3499b204` | 1 | 1 | 25 | 99 | native |
| 1 | Stunning Barrier | Scroll of Stunning Barrier `e029ec259c9a37249b113060df32a01d` | 1 | 1 | 25 | 99 | native |
| 1 | Summon Monster I | Scroll of Summon Monster I `26acf0a0265086a4cbd89f298241bc03` | 1 | 1 | 25 | 99 | native |
| 1 | Touch of Gracelessness | Scroll of Touch of Gracelessness `318abebf99d5a2e4593538bf7dbc5e04` | 1 | 1 | 25 | 99 | native |
| 1 | True Strike | Scroll of True Strike `5b6d4c0bbd882074eb6b6996ea77b77c` | 1 | 1 | 25 | 99 | native |
| 1 | Vanish | Scroll of Vanish `cd384c463dada4a4b97a521eab2539c0` | 1 | 1 | 25 | 99 | native |
| 2 | Acid Arrow | Scroll of Acid Arrow `b5f4c614c7b10f141b70bf3f64f9d82b` | 2 | 3 | 150 | 99 | native |
| 2 | Bear's Endurance | Scroll of Bear's Endurance `d6a7f98f92abfd5458b606f17de4db40` | 2 | 3 | 150 | 99 | native |
| 2 | Blindness | Scroll of Blindness `395bfedde7392aa42b52faea16da6b07` | 2 | 3 | 150 | 99 | native |
| 2 | Blur | Scroll of Blur `4e78560ea01fe2541b663c96e87b6057` | 2 | 3 | 150 | 99 | native |
| 2 | Boneshaker | Scroll of Boneshaker `56bb28cb527c4e54c9a5def8de6a3c31` | 2 | 3 | 150 | 99 | native |
| 2 | Bull's Strength | Scroll of Bull's Strength `102d903c65636f341ae7cb4533905ffa` | 2 | 3 | 150 | 99 | native |
| 2 | Burning Arc | Scroll of Burning Arc `b5239446c08850347b803eb57c271dab` | 2 | 3 | 150 | 99 | native |
| 2 | Cat's Grace | Scroll of Cat's Grace `5316739331090164f832cf31edb60696` | 2 | 3 | 150 | 99 | native |
| 2 | Create Pit | Scroll of Create Pit `bc948837c7acb664eb8d89ac7749fa18` | 2 | 3 | 150 | 99 | native |
| 2 | Eagle's Splendor | Scroll of Eagle's Splendor `7541eb357d7fc64489f140f437aee6af` | 2 | 3 | 150 | 99 | native |
| 2 | False Life | Scroll of False Life `04954e21ad7b5a54a9a9c29b4ed79052` | 2 | 3 | 150 | 99 | native |
| 2 | Fox's Cunning | Scroll of Fox's Cunning `a53b780b854b0864f8b6e0797e23e278` | 2 | 3 | 150 | 99 | native |
| 2 | Frigid Touch | Scroll of Frigid Touch `46ca6a02fa6cae04485f558aeaf9891c` | 2 | 3 | 150 | 99 | native |
| 2 | Glitterdust | Scroll of Glitterdust `82ce8bfaf96e215458b5da1953e73ff5` | 2 | 3 | 150 | 99 | native |
| 2 | Hideous Laughter | Scroll of Hideous Laughter `aed1e536ad8851947a3d37644fa87c03` | 2 | 3 | 150 | 99 | native |
| 2 | Invisibility | Scroll of Invisibility `c0af0b5277e91e347ade3aa8994b0d17` | 2 | 3 | 150 | 99 | native |
| 2 | Mirror Image | Scroll of Mirror Image `bae1ca27b27c3eb4f9e38c03abcdb64a` | 2 | 3 | 150 | 99 | native |
| 2 | Molten Orb | Scroll of Molten Orb `ecb145cb6e1b206408c8fd618337aded` | 2 | 3 | 150 | 99 | native |
| 2 | Owl's Wisdom | Scroll of Owl's Wisdom `8c89e97d333561741bedfc98a364ac67` | 2 | 3 | 150 | 99 | native |
| 2 | Pernicious Poison | Scroll of Pernicious Poison `ac73301ffe7e0ac4ebf7f1ed0ce014df` | 2 | 3 | 150 | 99 | native |
| 2 | Protection from Alignment, Communal | Scroll of Protection from Alignment, Communal `2dcc386fa4fef54ede9feaa8554a81fb` | 2 | 3 | 150 | 99 | Waking Rune wrapper |
| 2 | Protection from Arrows | Scroll of Protection from Arrows `179d91b899fa6304b8c076e002890317` | 2 | 3 | 150 | 99 | native |
| 2 | Resist Energy | Scroll of Resist Energy `707ea07c028315c7aea0a8536c38fe34` | 2 | 3 | 150 | 99 | Waking Rune wrapper |
| 2 | Scare | Scroll of Scare `a3f5fbdd5f43f274baa3ee8c96d8c1a8` | 2 | 3 | 150 | 99 | native |
| 2 | Scorching Ray | Scroll of Scorching Ray `95bf8273fd7930c4da3eaaf636a6cd29` | 2 | 3 | 150 | 99 | native |
| 2 | See Invisibility | Scroll of See Invisibility `12f4ee72c02537244b5b2bacfa236bc7` | 2 | 3 | 150 | 99 | native |
| 2 | Sense Vitals | Scroll of Sense Vitals `d4dc796367b4fb342ad7e8221fb8d813` | 2 | 3 | 150 | 99 | native |
| 2 | Stone Call | Scroll of Stone Call `87a562c6974bffb4aa9db89db0bccec8` | 2 | 3 | 150 | 99 | native |
| 2 | Summon Monster II | Scroll of Summon Monster II `c15397df7f44df5093f9ce829735d2b2` | 2 | 3 | 150 | 99 | Waking Rune wrapper |
| 2 | Summon Small Elemental | Scroll of Summon Small Elemental `fa3f0087bc9653e4b42640b38a2b3ebf` | 2 | 3 | 150 | 99 | Waking Rune wrapper |
| 2 | Web | Scroll of Web `efc97cdb15c92194bbbe4a2a3c1ecd78` | 2 | 3 | 150 | 99 | native |
| 3 | Battering Blast | Scroll of Battering Blast `1940573d3b0a47949832488a27831b19` | 3 | 5 | 375 | 99 | native |
| 3 | Beast Shape I | Scroll of Beast Shape I `3598a1d21491993419c7484028257eac` | 3 | 5 | 375 | 99 | native |
| 3 | Deep Slumber | Scroll of Deep Slumber `63bc71021dbba204c96cdb88971d9083` | 3 | 5 | 375 | 99 | native |
| 3 | Dispel Magic | Scroll of Dispel Magic `c7384b3c45e8dad28211bcf9a1705174` | 3 | 5 | 375 | 99 | Waking Rune wrapper |
| 3 | Displacement | Scroll of Displacement `4d9f5336b51cae34cb491cbde6aac4b6` | 3 | 5 | 375 | 99 | native |
| 3 | Fireball | Scroll of Fireball `5b172c2c3e356eb43ba5a8f8008a8a5a` | 3 | 5 | 375 | 99 | native |
| 3 | Haste | Scroll of Haste `6b3f04dd5eb9675499f815d5086fe018` | 3 | 5 | 375 | 99 | native |
| 3 | Heroism | Scroll of Heroism `50f9e398017b34c43aa08a2c2c44e8af` | 3 | 5 | 375 | 99 | native |
| 3 | Hold Person | Scroll of Hold Person `11987b82dbc808143963b9acaa81bf07` | 3 | 5 | 375 | 99 | native |
| 3 | Lightning Bolt | Scroll of Lightning Bolt `9d6d963e1a2d2474db1a23250abeb170` | 3 | 5 | 375 | 99 | native |
| 3 | Protection from Arrows, Communal | Scroll of Protection from Arrows, Communal `45e656affe6f0614ab8e99a5e09d56a2` | 3 | 5 | 375 | 99 | native |
| 3 | Protection From Energy | Scroll of Protection From Energy `10ff32501dc78a1a4f58c78a66bd935e` | 3 | 5 | 375 | 99 | Waking Rune wrapper |
| 3 | Rage | Scroll of Rage `81f35b521b23ad947acaa8151fa81139` | 3 | 5 | 375 | 99 | native |
| 3 | Resist Energy, Communal | Scroll of Resist Energy, Communal `355bf287b78abf3c555df45f134726f1` | 3 | 5 | 375 | 99 | Waking Rune wrapper |
| 3 | See Invisibility, Communal | Scroll of See Invisibility, Communal `36a77c24f4060044685daa8c9facf51f` | 3 | 5 | 375 | 99 | native |
| 3 | Slow | Scroll of Slow `fc4f7d5bfd3502a4d94e9de731a86c8a` | 3 | 5 | 375 | 99 | native |
| 3 | Spiked Pit | Scroll of Spiked Pit `8aca97895c1c6ae458bfd3cfd7fa4680` | 3 | 5 | 375 | 99 | native |
| 3 | Stinking Cloud | Scroll of Stinking Cloud `c92308c160d6d424fb64f1fd708aa6cd` | 3 | 5 | 375 | 99 | native |
| 3 | Summon Monster III | Scroll of Summon Monster III `580fd4b5d873e3711fe0cc5b6645d2b3` | 3 | 5 | 375 | 99 | Waking Rune wrapper |
| 3 | Vampiric Touch | Scroll of Vampiric Touch `b693e8cc94f4477498d396a40817ff77` | 3 | 5 | 375 | 99 | native |
| 4 | Acid Pit | Scroll of Acid Pit `bb030e6b3b1ae8c46ad2f013e42e597e` | 4 | 7 | 700 | 99 | native |
| 4 | Animate Dead | Scroll of Animate Dead `bc0180b8b29abf9468dea1a24332d159` | 4 | 7 | 700 | 99 | native |
| 4 | Beast Shape II | Scroll of Beast Shape II `d68c2728020fbe543826a9f6beaad379` | 4 | 7 | 700 | 99 | native |
| 4 | Bestow Curse | Scroll of Bestow Curse `a7be773786f85c85acbbdd8eec12d040` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Boneshatter | Scroll of Boneshatter `6f36a274115b20e4c86fa6771be8b7dd` | 4 | 7 | 700 | 99 | native |
| 4 | Confusion | Scroll of Confusion `1db86aaa479be6944abe90eaddb4afa2` | 4 | 7 | 700 | 99 | native |
| 4 | Contagion | Scroll of Contagion `b97aee68e62b74accbc872cb81dd9e9d` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Controlled Fireball | Scroll of Controlled Fireball `4b0ff254dca06894cba7eace7eef6bfe` | 4 | 7 | 700 | 99 | native |
| 4 | Crushing Despair | Scroll of Crushing Despair `5da62245cf5b9e54fa12aeaaf398b298` | 4 | 7 | 700 | 99 | native |
| 4 | Dimension Door | Scroll of Dimension Door `7aa654dc6c15e612c4bf5f57e1c481f3` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Dragon's Breath | Scroll of Dragon's Breath `83a7fc87fe99f7a0a5b1abc0ee75f8e4` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Elemental Body I | Scroll of Elemental Body I `3f9c266e88bf2cb65445ded41fc1c519` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Enervation | Scroll of Enervation `0589a68074a003b49961ec334a0e9138` | 4 | 7 | 700 | 99 | native |
| 4 | Enlarge Person, Mass | Scroll of Enlarge Person, Mass `6e77ed864d7112d44a2e9bc73a265060` | 4 | 7 | 700 | 99 | native |
| 4 | False Life, Greater | Scroll of False Life, Greater `7bb01ba2ab53cd64693ad324564af5cb` | 4 | 7 | 700 | 99 | native |
| 4 | Fear | Scroll of Fear `163001e34e0669a4f8cab43dc4e29dd9` | 4 | 7 | 700 | 99 | native |
| 4 | Greater Invisibility | Scroll of Greater Invisibility `d4bb60352b8a62845ba3a7b34ef29a0b` | 4 | 7 | 700 | 99 | native |
| 4 | Ice Storm | Scroll of Ice Storm `a5c98d83dd1e2b2448acf51552c9b9d3` | 4 | 7 | 700 | 99 | native |
| 4 | Obsidian Flow | Scroll of Obsidian Flow `695cc4b16a367144ca1717b37bce626e` | 4 | 7 | 700 | 99 | native |
| 4 | Overwhelming Grief | Scroll of Overwhelming Grief `8a2189c96e35b9f4da4a2f53bcdfa466` | 4 | 7 | 700 | 99 | native |
| 4 | Phantasmal Killer | Scroll of Phantasmal Killer `2b73dd1aecdbf1e4a8d7ca18139b1cca` | 4 | 7 | 700 | 99 | native |
| 4 | Protection from Energy, Communal | Scroll of Protection from Energy, Communal `6246693fa7ae14da7cb06c8e723a2e10` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Rainbow Pattern | Scroll of Rainbow Pattern `84cd707a7ae9f934389ed6bbf51b023a` | 4 | 7 | 700 | 99 | native |
| 4 | Reduce Person, Mass | Scroll of Reduce Person, Mass `fbde449fffbf2d64983ad2e11981a23a` | 4 | 7 | 700 | 99 | native |
| 4 | Remove Curse | Scroll of Remove Curse `f2b8655382429c14c8c888f1cf109020` | 4 | 7 | 700 | 99 | native |
| 4 | Shout | Scroll of Shout `4c73f11f91ca3fb4a8af325686b660d8` | 4 | 7 | 700 | 99 | native |
| 4 | Stoneskin | Scroll of Stoneskin `a8422c98704e6a0429ebc5a56e132d95` | 4 | 7 | 700 | 99 | native |
| 4 | Summon Medium Elemental | Scroll of Summon Medium Elemental `a718b9cbf588fbf7097f7adbacbf489d` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Summon Monster IV | Scroll of Summon Monster IV `3e8766e5b6c9bf59aba9ad84baaef52d` | 4 | 7 | 700 | 99 | Waking Rune wrapper |
| 4 | Touch of Slime | Scroll of Touch of Slime `d1a586649cea48f48a0c535aacecfba3` | 4 | 7 | 700 | 99 | native |
| 4 | Volcanic Storm | Scroll of Volcanic Storm `349a071cd51f6224e9647dc2cf06c243` | 4 | 7 | 700 | 99 | native |
| 5 | Acidic Spray | Scroll of Acidic Spray `ba78d5a438e8d154dbd6e4f991fc320f` | 5 | 9 | 1,125 | 99 | native |
| 5 | Angelic Aspect | Scroll of Angelic Aspect `4c79f5ad0bc683d43ac0bda57f7843b3` | 5 | 9 | 1,125 | 99 | native |
| 5 | Animal Growth | Scroll of Animal Growth `3d11bb02852d02649850a73629ea283d` | 5 | 9 | 1,125 | 99 | native |
| 5 | Baleful Polymorph | Scroll of Baleful Polymorph `06da2d0278495514bab116a4ebbb3f98` | 5 | 9 | 1,125 | 99 | native |
| 5 | Beast Shape III | Scroll of Beast Shape III `f7ff7c9f1fe595d4ba7d002d17d678c9` | 5 | 9 | 1,125 | 99 | native |
| 5 | Break Enchantment | Scroll of Break Enchantment `358ee9cb540a9af4e9bc76cc1af62e86` | 5 | 9 | 1,125 | 99 | native |
| 5 | Cloudkill | Scroll of Cloudkill `ecd7efc465dbbac44a07c7e9bbd06be2` | 5 | 9 | 1,125 | 99 | native |
| 5 | Cone of Cold | Scroll of Cone of Cold `5344f2240620b27478d12f00643fc292` | 5 | 9 | 1,125 | 99 | native |
| 5 | Constricting Coils | Scroll of Constricting Coils `ebaeaebc1a30536468ba680aff1d6471` | 5 | 9 | 1,125 | 99 | native |
| 5 | Dismissal | Scroll of Dismissal `aef1531dc1d3799428eaa5e9b70b5e2c` | 5 | 9 | 1,125 | 99 | native |
| 5 | Dominate Person | Scroll of Dominate Person `f199f6e5026488c499042900b572eb7f` | 5 | 9 | 1,125 | 99 | native |
| 5 | Echolocation | Scroll of Echolocation `33770ff24b320e343bb767815f800fc4` | 5 | 9 | 1,125 | 99 | native |
| 5 | Elemental Body II | Scroll of Elemental Body II `e73d2c10e29ffa040e3549b745164ee8` | 5 | 9 | 1,125 | 99 | Waking Rune wrapper |
| 5 | Feeblemind | Scroll of Feeblemind `17c17e5c409d6814786259d99bf9a43d` | 5 | 9 | 1,125 | 99 | native |
| 5 | Fire Snake | Scroll of Fire Snake `73d88f8434d24c848a1c77a9c05f68c0` | 5 | 9 | 1,125 | 99 | native |
| 5 | Geniekind | Scroll of Geniekind `012c230365f929a7a93497dbeefe8ae9` | 5 | 9 | 1,125 | 99 | Waking Rune wrapper |
| 5 | Hold Monster | Scroll of Hold Monster `d8c52df98e5815d4aa9d2bd2b73b88c9` | 5 | 9 | 1,125 | 99 | native |
| 5 | Hungry Pit | Scroll of Hungry Pit `bc25efed54c66a147b3615fb05f302e0` | 5 | 13 | 1,625 | 99 | native |
| 5 | Icy Prison | Scroll of Icy Prison `de05d46f44c8439488a8bbcc0059c09f` | 5 | 9 | 1,125 | 99 | native |
| 5 | Mind Fog | Scroll of Mind Fog `61bacc43652d76c42b60d965b65cd741` | 5 | 9 | 1,125 | 99 | native |
| 5 | Phantasmal Web | Scroll of Phantasmal Web `4b0dc7926c0ad8d4bae8d7a5f3eed6f8` | 5 | 9 | 1,125 | 99 | native |
| 5 | Polymorph | Scroll of Polymorph `5e4dcce7e622a8b55ad607c63a7bf290` | 5 | 9 | 1,125 | 99 | Waking Rune wrapper |
| 5 | Shadow Evocation | Scroll of Shadow Evocation `bd7614e3dcbf1f33a3646a9597305e14` | 5 | 9 | 1,125 | 99 | Waking Rune wrapper |
| 5 | Stoneskin, Communal | Scroll of Stoneskin, Communal `0dd764b2234a4d84dadeb45a2deaf9f9` | 5 | 9 | 1,125 | 99 | native |
| 5 | Summon Large Elemental | Scroll of Summon Large Elemental `bffbb8f8ca0d150f775c970dcaee644a` | 5 | 9 | 1,125 | 99 | Waking Rune wrapper |
| 5 | Summon Monster V | Scroll of Summon Monster V `0e5ad4837518462afcbdb5ef1cda777c` | 5 | 9 | 1,125 | 99 | Waking Rune wrapper |
| 5 | Teleport | Scroll of Teleport `4551541688de0b48a47e4890cfe60c9a` | 5 | 9 | 1,125 | 99 | Waking Rune wrapper |
| 5 | Vampiric Shadow Shield | Scroll of Vampiric Shadow Shield `1bcc05d40824dfa46b7f6cb6318b4926` | 5 | 9 | 1,125 | 99 | native |
| 5 | Waves of Fatigue | Scroll of Waves of Fatigue `3baaaa7cb91704342b0314b6f06cdc91` | 5 | 9 | 1,125 | 99 | native |
| 6 | Acid Fog | Scroll of Acid Fog `a931181d47d35ae4da06a243906eece2` | 6 | 11 | 1,650 | 99 | native |
| 6 | Banshee Blast | Scroll of Banshee Blast `f6073b26d3e1d61418f62928f34b601f` | 6 | 11 | 1,650 | 99 | native |
| 6 | Bear's Endurance, Mass | Scroll of Bear's Endurance, Mass `78631cf6b42590f40b44c79389c6af74` | 6 | 11 | 1,650 | 99 | native |
| 6 | Beast Shape IV | Scroll of Beast Shape IV `a09737f1304acb932c48fb2b3aab91b1` | 6 | 11 | 1,650 | 99 | Waking Rune wrapper |
| 6 | Bull's Strength, Mass | Scroll of Bull's Strength, Mass `7e6ca8816f50a7f4e9dccc90f958e4f4` | 6 | 11 | 1,650 | 99 | native |
| 6 | Cat's Grace, Mass | Scroll of Cat's Grace, Mass `e1e455570ca262244a572a3efed337ce` | 6 | 11 | 1,650 | 99 | native |
| 6 | Chain Lightning | Scroll of Chain Lightning `141ffc89f1630ff45812f50ed3922088` | 6 | 11 | 1,650 | 99 | native |
| 6 | Chains of Light | Scroll of Chains of Light `4360179eb458c3c4b896f68f66c33607` | 6 | 11 | 1,650 | 99 | native |
| 6 | Circle of Death | Scroll of Circle of Death `beab337b352b5ac479698e2bbc08f4ce` | 6 | 11 | 1,650 | 99 | native |
| 6 | Cloak of Dreams | Scroll of Cloak of Dreams `fc43f48bf3f0ad74a9eb9b81fc531f25` | 6 | 11 | 1,650 | 99 | native |
| 6 | Cold Ice Strike | Scroll of Cold Ice Strike `686c2a9cffbf0cc4d9038d66a852cc3b` | 6 | 11 | 1,650 | 99 | native |
| 6 | Disintegrate | Scroll of Disintegrate `bb2f172e429b40840a7dc25bc83732cb` | 6 | 11 | 1,650 | 99 | native |
| 6 | Dispel Magic, Greater | Scroll of Dispel Magic, Greater `56975b102c539b544a77e602c7d8d490` | 6 | 11 | 1,650 | 99 | native |
| 6 | Dragonkind I | Scroll of Dragonkind I `dbb1c74a280bf6915abac5e63189d6b0` | 6 | 11 | 1,650 | 99 | Waking Rune wrapper |
| 6 | Eagle's Splendor, Mass | Scroll of Eagle's Splendor, Mass `fe94b92146d7bb44c9983ed6d3080d8a` | 6 | 11 | 1,650 | 99 | native |
| 6 | Elemental Assessor | Scroll of Elemental Assessor `7dce00472dcde1644b908d4e0eefc078` | 6 | 11 | 1,650 | 99 | native |
| 6 | Elemental Body III | Scroll of Elemental Body III `577f66ea0da95598df91a1308dc606db` | 6 | 11 | 1,650 | 99 | Waking Rune wrapper |
| 6 | Eyebite | Scroll of Eyebite `f80549ec4e61ae64d8051bb3b24cf10e` | 6 | 11 | 1,650 | 99 | native |
| 6 | Fox's Cunning, Mass | Scroll of Fox's Cunning, Mass `bdfbfdcecfdc3b346a1e6fc37bca8bb7` | 6 | 11 | 1,650 | 99 | native |
| 6 | Hellfire Ray | Scroll of Hellfire Ray `eca3b7aa601fc1b46a940897cb2142d3` | 6 | 11 | 1,650 | 99 | native |
| 6 | Heroism, Greater | Scroll of Heroism, Greater `efb55589cd5b7dd4ca48d78d0d1d9248` | 6 | 11 | 1,650 | 99 | native |
| 6 | Owl's Wisdom, Mass | Scroll of Owl's Wisdom, Mass `2f247d1cb09ca6341bdd01cdcc3c46a9` | 6 | 11 | 1,650 | 99 | native |
| 6 | Phantasmal Putrefaction | Scroll of Phantasmal Putrefaction `cd9e8fa2df821d04ab46c8affaff9470` | 6 | 11 | 1,650 | 99 | native |
| 6 | Serenity | Scroll of Serenity `7c2fe7abcecee534d95d5ddd4d814585` | 6 | 11 | 1,650 | 99 | native |
| 6 | Sirocco | Scroll of Sirocco `c17e4bd5028d6534a8c8d317cd8244ca` | 6 | 11 | 1,650 | 99 | native |
| 6 | Stone to Flesh | Scroll of Stone to Flesh `394ba24aed75f0d459ed84d1b1b98f60` | 6 | 11 | 1,650 | 99 | native |
| 6 | Summon Huge Elemental | Scroll of Summon Huge Elemental `b571be8ddcfe584dd688aaf1959e7eca` | 6 | 11 | 1,650 | 99 | Waking Rune wrapper |
| 6 | Summon Monster VI | Scroll of Summon Monster VI `39c17f317c071040f53214fe38c89409` | 6 | 11 | 1,650 | 99 | Waking Rune wrapper |
| 6 | Tar Pool | Scroll of Tar Pool `d81fa529db47380468c9e8310f8d18ea` | 6 | 11 | 1,650 | 99 | native |
| 6 | Transformation | Scroll of Transformation `d7536624e1ab8d8499f6e8d9689940b6` | 6 | 11 | 1,650 | 99 | native |
| 6 | True Seeing | Scroll of True Seeing `7a05e6eea60a1864d935abd6c281a531` | 6 | 11 | 1,650 | 99 | native |
| 6 | Undeath to Death | Scroll of Undeath to Death `733c0b265de8d7a46afe43f404a1fa36` | 6 | 11 | 1,650 | 99 | native |

## Every line

| Item | Kind | Count | Price | Source | Blueprint |
|---|---|---:|---:|---|---|
| Banded Mail +1 | armor: Heavy | 12 | 1,400 | catalogue | `b277c8713472c3a4fb0f1e0ea6d6ed47` |
| Half-Plate +1 | armor: Heavy | 12 | 1,750 | catalogue | `65de3fcad4c01ac40bc8567f67901b5b` |
| Full Plate +1 | armor: Heavy | 12 | 2,650 | catalogue | `ba780a9c1e5b0304892bd2bc0c22fe4d` |
| Banded Mail +2 | armor: Heavy | 12 | 4,400 | catalogue | `0c83f2926e8ac15439830be3106a6643` |
| Half-Plate +2 | armor: Heavy | 12 | 4,750 | catalogue | `ce0076c7408f10741950cec394767b75` |
| Full Plate +2 | armor: Heavy | 12 | 5,650 | catalogue | `61787c56c01996f4c82866aca8fb0187` |
| Half-Plate +3 | armor: Heavy | 12 | 9,750 | catalogue | `76e68c81dd261e64789d5f2bdc77302b` |
| Full Plate +3 | armor: Heavy | 12 | 10,650 | catalogue | `1e0289b5e2a002d4592c0ac079d828d9` |
| Mithral Full Plate +1 | armor: Heavy | 12 | 12,650 | catalogue | `888cd7959836acd4eae33e815e99102c` |
| Mithral Full Plate +2 | armor: Heavy | 12 | 15,650 | catalogue | `529f8973ea1571244a48711135180e12` |
| Leather Armor +1 | armor: Light | 12 | 1,160 | catalogue | `18c627302593ab142bb8219525e1aed1` |
| Shadow Leather +1 | armor: Light | 12 | 1,160 | catalogue | `ee429a40453dcac43b4d272bdf35a0fe` |
| Chainshirt +1 | armor: Light | 12 | 1,250 | catalogue | `91bf657f26eb80f4ba05b0b8440b1e8c` |
| Leather Armor +2 | armor: Light | 12 | 4,160 | catalogue | `f251cb7bbf176e340893b9e40f9a7c29` |
| Chainshirt +2 | armor: Light | 12 | 4,250 | catalogue | `d80665c8497437a418b8f39e25daf4e2` |
| Mithral Chainshirt +1 | armor: Light | 12 | 5,250 | catalogue | `e832847483b219841b24e0a77acd7795` |
| Mithral Chainshirt +2 | armor: Light | 12 | 8,250 | catalogue | `26b1c3857d4d46d4aa1b6afaa33a6f84` |
| Leather Armor +3 | armor: Light | 12 | 9,160 | catalogue | `fb4e768611f820b4186a004cbd70ead6` |
| Chainshirt +3 | armor: Light | 12 | 9,250 | catalogue | `19144cbc80e7cb84aa53692a7ddf5ba6` |
| Mithral Chainshirt +3 | armor: Light | 12 | 10,250 | catalogue | `fb2664f7d8534dc40aeb23392dc58c0c` |
| Hide Armor +1 | armor: Medium | 12 | 1,165 | catalogue | `45e86ae29df5f4b48a66da15fde62217` |
| Chainmail +1 | armor: Medium | 12 | 1,300 | catalogue | `dd3834fe3f48182438b59fd99675fd6c` |
| Breastplate +1 | armor: Medium | 12 | 1,350 | catalogue | `5041415db3e6c394a8b2173c39fd4ec4` |
| Hide Armor +2 | armor: Medium | 12 | 4,165 | catalogue | `01117df73f22d5b4294136d2ae8efad4` |
| Chainmail +2 | armor: Medium | 12 | 4,300 | catalogue | `be26685bdebb2a94693d2378e0235ef3` |
| Breastplate +2 | armor: Medium | 12 | 4,350 | catalogue | `c77060ede0a983f4a8c7c47032ecf1ea` |
| Mithral Breastplate +1 | armor: Medium | 12 | 6,350 | catalogue | `9ad5e5e5ef3ea164482217696306c6fd` |
| Chainmail +3 | armor: Medium | 12 | 9,300 | catalogue | `22c16b38998ebf342acdccba4f55a3b6` |
| Breastplate +3 | armor: Medium | 12 | 9,350 | catalogue | `a7a2500acce8faf46b6ecb79ec05691e` |
| Mithral Breastplate +2 | armor: Medium | 12 | 9,350 | catalogue | `72b5a62a1387ca84d9f0f1fb95dbe4fe` |
| Robe of the Wise | armor: None | 12 | 12,000 | catalogue | `21918303993ba97408b858167f96f378` |
| Explorer's Belt | belt | 12 | 2,500 | catalogue | `ec34985b81b9f744488456e7009044ee` |
| Belt of Giant Strength +2 | belt | 12 | 4,000 | catalogue | `c354de364a714ee4dbe2cfee628df267` |
| Belt of Incredible Dexterity +2 | belt | 12 | 4,000 | catalogue | `b6af4c1834999e74497b41588d1071cd` |
| Belt of Mighty Constitution +2 | belt | 12 | 4,000 | catalogue | `b9cc5d85d4a5032458ef4491f5fed251` |
| Belt of Physical Flow +2 | belt | 12 | 10,000 | catalogue | `6577d56f2da72874a8abea1a3cbd2fa0` |
| Belt of Physical Might +2 | belt | 12 | 10,000 | catalogue | `c95c552c19a5c5a4492462802104b071` |
| Belt of Physical Form +2 | belt | 12 | 10,000 | catalogue | `939ad0109fec02b46adcac4b85e1249a` |
| Belt of Giant Strength +4 | belt | 12 | 16,000 | catalogue | `ede812de6b31b194686595fab33fc3ed` |
| Belt of Incredible Dexterity +4 | belt | 12 | 16,000 | catalogue | `20539ffabe4365e419ca7413129ab78a` |
| Belt of Mighty Constitution +4 | belt | 12 | 16,000 | catalogue | `dbeb5ba83e9fd2046a31590636b8b36d` |
| Belt of Physical Perfection +2 | belt | 12 | 16,000 | catalogue | `7abdce2cab3e3dd43b9e6c41c1b44525` |
| Boots of Elvenkind | feet | 12 | 2,500 | catalogue | `1223ceb45ed647b44a04b44a9312328b` |
| Swiftfoot Boots | feet | 12 | 3,500 | catalogue | `8644bca1c3bf6f64ab4d89be643a8850` |
| Boots of the Light Step | feet | 12 | 7,800 | catalogue | `815cc85ce13ab64428253aea3b6708a8` |
| Trapspringer's Gloves | gloves | 12 | 4,000 | catalogue | `0dc9944c49e3ae343b8802a2da33973c` |
| Athlete's Gloves | gloves | 12 | 25,000 | catalogue | `d5480772bdd29e545b627415186de20e` |
| Master Hand | gloves | 12 | 25,000 | catalogue | `b1b4dbff499ceb541a148d70ef17046e` |
| Expert's Hat | head | 12 | 1,500 | catalogue | `405abbf543c9cc148a6f091bb5e58d14` |
| Eyes of the Eagle | head | 12 | 2,500 | catalogue | `303e3a1df232b0747b7fde723828fa45` |
| Headband of Vast Intelligence +2 | head | 12 | 4,000 | catalogue | `22184af355826444087506d7f2f8f0b1` |
| Headband of Inspired Wisdom +2 | head | 12 | 4,000 | catalogue | `3d33605336c62274483f382f58908f5b` |
| Headband of Alluring Charisma +2 | head | 12 | 4,000 | catalogue | `a3e8e907908ca7a40ac6da78e70bf33d` |
| Lesser Phylactery of Positive Channeling | head | 12 | 4,000 | catalogue | `6b50bf738b4e6964b916539efc582a9d` |
| Lesser Phylactery of Negative Channeling | head | 12 | 4,000 | catalogue | `9751f7b073f740b458ab170cc81f5f1b` |
| Headband of Mental Perfection +2 | head | 12 | 10,000 | catalogue | `b9b49933be6ac7f4aa3c71c602227332` |
| Phylactery of Positive Channeling | head | 12 | 12,000 | catalogue | `01bc7670bd0225745a66957f21f1ba23` |
| Phylactery of Negative Channeling | head | 12 | 12,000 | catalogue | `7ccc1833353d9a64cb671c7bc747bade` |
| Headband of Vast Intelligence +4 | head | 12 | 16,000 | catalogue | `ee15329357f8f974bacf02521d8d37fe` |
| Headband of Inspired Wisdom +4 | head | 12 | 16,000 | catalogue | `716bdfe5c57be9b43a958bb612c1ecd4` |
| Headband of Alluring Charisma +4 | head | 12 | 16,000 | catalogue | `71b4c3f8976784d4abf46976b15eba4a` |
| Amulet of Natural Armor +1 | neck | 12 | 2,000 | catalogue | `209960f8d60e23a4c8e75d14e2261263` |
| Amulet of Mighty Fists +1 | neck | 12 | 4,000 | catalogue | `a82d4f94b327d6743ad9c2b9c440061c` |
| Amulet of Natural Armor +2 | neck | 12 | 8,000 | catalogue | `becc4c1d40a77814f98c0b175e92d17c` |
| Amulet of Mighty Fists +2 | neck | 12 | 16,000 | catalogue | `c640025d7096aba49bd19e7ffcac9400` |
| Amulet of Natural Armor +3 | neck | 12 | 18,000 | catalogue | `081a2ffe763320a469de20f1e9b1cd71` |
| Riversong Amulet | neck | 12 | 22,000 | catalogue | `c4b62855e9f9bfa40b710445cbd91141` |
| Golden Tongue | neck | 12 | 29,500 | catalogue | `a35a6b6cb58453f47b10a81cddb6b9c3` |
| Amulet of Natural Armor +4 | neck | 12 | 32,000 | catalogue | `4378ef58796eb9744b96538c2baf8c07` |
| Amulet of Mighty Fists +3 | neck | 12 | 36,000 | catalogue | `975aad0cf1a32534fb4c1359b7d952b2` |
| Amulet of Natural Armor +5 | neck | 12 | 50,000 | catalogue | `11f435140501db84e8e787bf8792fac2` |
| Acid Flask | other | 99 | 10 | catalogue | `4639724c4a9cc9544a2f622b66931658` |
| Alchemist's Fire | other | 99 | 20 | catalogue | `fd56596e273d1ff49a8c29cc9802ae6e` |
| Oil of Enhancement II | other | 99 | 750 | catalogue | `218241b6d97158149be37b3128597b22` |
| Lesser Extend Metamagic Rod | other | 12 | 3,000 | catalogue | `1cf04842d5dbd0f49946b1af1022cd1a` |
| Lesser Reach Metamagic Rod | other | 12 | 3,000 | catalogue | `8b0261621069c9a41a70f1aaefa21c75` |
| Lesser Empower Metamagic Rod | other | 12 | 9,000 | catalogue | `1e7a5a4d257cf434a87e687c9ee7a872` |
| Extend Metamagic Rod | other | 12 | 11,000 | catalogue | `1b2a09528da9e9948aa9026037bada90` |
| Reach Metamagic Rod | other | 12 | 11,000 | catalogue | `648f56fbbaa71624c8ba968ade382ac6` |
| Lesser Maximize Metamagic Rod | other | 12 | 14,000 | catalogue | `651b0460f600d5f42b0467e7186aab80` |
| Empower Metamagic Rod | other | 12 | 32,500 | catalogue | `a02f06b63af839a448147dadff3724f2` |
| Lesser Quicken Metamagic Rod | other | 12 | 35,000 | catalogue | `55a059b32df920c4abe65b8ee8b56056` |
| Maximize Metamagic Rod | other | 12 | 54,000 | catalogue | `9a511d3b04f08944eb3db4462f88c2c0` |
| Potion of Cure Light Wounds | potion | 99 | 50 | catalogue | `d52566ae8cbe8dc4dae977ef51c27d91` |
| Potion of Enlarge Person | potion | 99 | 50 | catalogue | `fe7e87dc45fbcb845b7755f0733b5465` |
| Potion of Mage Armor | potion | 99 | 50 | catalogue | `ab4961408ba080449afd3e65aa0acbc1` |
| Potion of Shield of Faith | potion | 99 | 50 | catalogue | `bc93a78d71bef084fa155e529660ed0d` |
| Potion of Protection from Evil | potion | 99 | 50 | catalogue | `de000ebb9b86c8f48b77576965303183` |
| Potion of Feather Step | potion | 99 | 50 | catalogue | `a05bfdfe2df01204ab8e367c2c0b72ce` |
| Potion of Cure Moderate Wounds | potion | 99 | 300 | catalogue | `f991f3051c3b9e64fabc87891077b613` |
| Potion of Restoration, Lesser | potion | 99 | 300 | catalogue | `1365aee7d18c85d438f5b027122ca8cf` |
| Potion of Blur | potion | 99 | 300 | catalogue | `5bb722fed5172ed48baa2139246bc355` |
| Potion of Invisibility | potion | 99 | 300 | catalogue | `5f9fe6030695f48489b59887536748e8` |
| Potion of Barkskin | potion | 99 | 300 | catalogue | `99ba9a93eb1c3cf42bfa641a59580f5b` |
| Potion of Bull's Strength | potion | 99 | 300 | catalogue | `2983c97b71ba60443b1c749854ef53a1` |
| Potion of Cat's Grace | potion | 99 | 300 | catalogue | `94b24d55f514cdd45a79d34904fea944` |
| Potion of Bear's Endurance | potion | 99 | 300 | catalogue | `a3879b4e94c50854a873a0334fbbe36c` |
| Potion of Fox's Cunning | potion | 99 | 300 | catalogue | `17f132fa2c8c213479640212ae444005` |
| Potion of Owl's Wisdom | potion | 99 | 300 | catalogue | `db548855a06daba4cb57f34f4780fd02` |
| Potion of Eagle's Splendor | potion | 99 | 300 | catalogue | `91d681b0bf6c39047a46f50ec98bf72d` |
| Potion of Resist Fire | potion | 99 | 300 | catalogue | `6e37f3e99bfa1fb4a9ad8db1775aa7aa` |
| Potion of Resist Cold | potion | 99 | 300 | catalogue | `755b60529a25f1e4f8e44436d927fc6f` |
| Potion of Resist Acid | potion | 99 | 300 | catalogue | `2b269b69a86843546bdf17a9c3c431c3` |
| Potion of Resist Electricity | potion | 99 | 300 | catalogue | `5d4a59a63e6192c40a73d4b2bd722328` |
| Potion of Aid | potion | 99 | 300 | catalogue | `3bcbf79b01e4db044a6f06d86cec9117` |
| Potion of Cure Serious Wounds | potion | 99 | 750 | catalogue | `e76d14096063ee041bdb1d13d8172599` |
| Potion of Remove Blindness | potion | 99 | 750 | catalogue | `bafd5734adc579641904284aa380dd48` |
| Potion of Remove Curse | potion | 99 | 750 | catalogue | `152944f618996bb4992030141bc96659` |
| Potion of Heroism | potion | 99 | 750 | catalogue | `42f01c3ad12d4f84c93f89e165384780` |
| Potion of Displacement | potion | 99 | 750 | catalogue | `e21a5ab9509a943409bbd791e9685b03` |
| Potion of Protection from Fire | potion | 99 | 750 | catalogue | `4cd7b3330e46f424e88602da4e358836` |
| Potion of Protection from Cold | potion | 99 | 750 | catalogue | `3a66c695016483a47b4f77ed0ff2e7fb` |
| Potion of Protection from Acid | potion | 99 | 750 | catalogue | `30d3dfb67a23cbf4a98044a8f1141b67` |
| Potion of Protection from Electricity | potion | 99 | 750 | catalogue | `4d85e4763c365454d8f03335197ba6c4` |
| Ring of Protection +1 | ring | 12 | 2,000 | catalogue | `f333bf86cd122974792162cbcd27c9ed` |
| Ring of Protection +2 | ring | 12 | 8,000 | catalogue | `254755ca73eb52a4d8ab1a663106659b` |
| Ring of the Natural Born Commander | ring | 12 | 9,000 | catalogue | `9772ab64fdf645245ac8ee27b1e5ba69` |
| Ring of Magic Devices | ring | 12 | 16,000 | catalogue | `66ab84747fa8c5f43851b4bd02ed3f85` |
| Ring of Protection +3 | ring | 12 | 18,000 | catalogue | `31315100c28e6a2418396fb152466fcd` |
| Ring of Evasion | ring | 12 | 25,000 | catalogue | `ae3638b2f8016f84d87122ae60d26937` |
| Ring of Protection +4 | ring | 12 | 32,000 | catalogue | `1c8d2d0358c664447bc9bff52220f027` |
| Ring of Protection +5 | ring | 12 | 50,000 | catalogue | `2d576daea5f62ae489028bce40469285` |
| Scroll of Remove Fear | scroll | 99 | 25 | catalogue | `c87c32984fe3909419ee22cd7c93ce37` |
| Scroll of Mage Armor | scroll | 99 | 25 | catalogue | `e8308a74821762e49bc3211358e81016` |
| Scroll of Remove Paralysis | scroll | 99 | 150 | catalogue | `64b5a0f0d955c254d8dda9d19bb472ca` |
| Scroll of Barkskin | scroll | 99 | 150 | catalogue | `95b6d995d4d806a4da7d4ee5989c7188` |
| Scroll of Blur | scroll | 99 | 150 | catalogue | `4e78560ea01fe2541b663c96e87b6057` |
| Scroll of Invisibility | scroll | 99 | 150 | catalogue | `c0af0b5277e91e347ade3aa8994b0d17` |
| Scroll of Remove Disease | scroll | 99 | 375 | catalogue | `48ffa76066648a8408d4183e055346ce` |
| Scroll of Remove Blindness | scroll | 99 | 375 | catalogue | `de172db6e10f6d54896cb6a48b9fe8f7` |
| Scroll of Remove Curse | scroll | 99 | 375 | catalogue | `7475d23822bb7f748b54c1f3deea5080` |
| Scroll of Neutralize Poison | scroll | 99 | 375 | catalogue | `62e633bef8ae9b241a5c084525075711` |
| Scroll of Dispel Magic (Point) | scroll | 99 | 375 | catalogue | `4b2d0e65fb9775341b6c4f7c178f0fe5` |
| Scroll of Dispel Magic (Target) | scroll | 99 | 375 | catalogue | `92a1bfd42ef1ec04794355579c699da0` |
| Scroll of Protection from Fire | scroll | 99 | 375 | catalogue | `c2cadc1c19cc4b54eb73b987f540ddca` |
| Scroll of Protection from Cold | scroll | 99 | 375 | catalogue | `2f55c17cbac95664cb1a39568df63d8b` |
| Scroll of Protection from Acid | scroll | 99 | 375 | catalogue | `c7185bb3eb517804da246aa323d98fd8` |
| Scroll of Protection from Electricity | scroll | 99 | 375 | catalogue | `3b064dd1174e9f0449fb7f3cc585caf1` |
| Scroll of Protection from Sonic | scroll | 99 | 375 | catalogue | `15fd666cb2d6678438d96cbcc7f574d2` |
| Scroll of Haste | scroll | 99 | 375 | catalogue | `6b3f04dd5eb9675499f815d5086fe018` |
| Scroll of Heroism | scroll | 99 | 375 | catalogue | `50f9e398017b34c43aa08a2c2c44e8af` |
| Scroll of Displacement | scroll | 99 | 375 | catalogue | `4d9f5336b51cae34cb491cbde6aac4b6` |
| Scroll of Fireball | scroll | 99 | 375 | catalogue | `5b172c2c3e356eb43ba5a8f8008a8a5a` |
| Scroll of Lightning Bolt | scroll | 99 | 375 | catalogue | `9d6d963e1a2d2474db1a23250abeb170` |
| Scroll of Restoration | scroll | 99 | 700 | catalogue | `27f9b643fe41dd34081509f0c65a3408` |
| Scroll of Death Ward | scroll | 99 | 700 | catalogue | `89e10c3f21fa50c4b8719e004c7628d3` |
| Scroll of Freedom of Movement | scroll | 99 | 700 | catalogue | `f49fc4e47cef56e42a49d561289dd500` |
| Scroll of Greater Invisibility | scroll | 99 | 700 | catalogue | `d4bb60352b8a62845ba3a7b34ef29a0b` |
| Scroll of Breath of Life | scroll | 99 | 1,125 | catalogue | `5318313176485d64ca13c1f046591326` |
| Scroll of Cone of Cold | scroll | 99 | 1,125 | catalogue | `5344f2240620b27478d12f00643fc292` |
| Scroll of Heal | scroll | 99 | 1,650 | catalogue | `fb822a8d451d91b438514541a9a986de` |
| Scroll of Stone to Flesh | scroll | 99 | 1,650 | catalogue | `394ba24aed75f0d459ed84d1b1b98f60` |
| Scroll of Heroism, Greater | scroll | 99 | 1,650 | catalogue | `efb55589cd5b7dd4ca48d78d0d1d9248` |
| Scroll of Disintegrate | scroll | 99 | 1,650 | catalogue | `bb2f172e429b40840a7dc25bc83732cb` |
| Scroll of Raise Dead | scroll | 99 | 6,000 | catalogue | `a43d2960b3f9bed49a02ae44efef4cfc` |
| Buckler +1 | shield | 12 | 1,155 | catalogue | `5615afebd80c5de43b6def5fc4646e1c` |
| Light Shield +1 | shield | 12 | 1,160 | catalogue | `cac3ca0cae325b44b9d7833bbe0e6c69` |
| Heavy Shield +1 | shield | 12 | 1,170 | catalogue | `5c7b898a1bfb6cb4f8c14a0ebc143abe` |
| Tower Shield +1 | shield | 12 | 1,180 | catalogue | `e7b9e76870927424eb9de56611206867` |
| Buckler +2 | shield | 12 | 4,155 | catalogue | `cff02b5c7e391f24e88239536f88416d` |
| Light Shield +2 | shield | 12 | 4,160 | catalogue | `2a298726445c2184bbeab9f34ece55eb` |
| Heavy Shield +2 | shield | 12 | 4,170 | catalogue | `d41ea649a1054b647aafc5b2cb4f8543` |
| Tower Shield +2 | shield | 12 | 4,180 | catalogue | `294e15227b2c1d245ab1ff99922f339f` |
| Buckler +3 | shield | 12 | 9,155 | catalogue | `9552d3843ff7b4140a8cf04b753848eb` |
| Light Shield +3 | shield | 12 | 9,160 | catalogue | `3b3f25ba61ba5f346b4d546e702732cb` |
| Heavy Shield +3 | shield | 12 | 9,170 | catalogue | `7e23918977b18524591204c93fae0a37` |
| Cloak of Resistance +1 | shoulders | 12 | 1,000 | catalogue | `4aea6773c1da01c42bd382c1b7c384bc` |
| Cloak of Resistance +2 | shoulders | 12 | 4,000 | catalogue | `04dff7841c5f499478c91487d9bbdcef` |
| Cloak of Shadows | shoulders | 12 | 6,000 | catalogue | `42aa20c20917c41448fd6cb27de98d60` |
| Thaumaturgist's Cloak | shoulders | 12 | 7,800 | catalogue | `2595ae738523d9c4ea96f0d7e8b35470` |
| Cloak of Resistance +3 | shoulders | 12 | 9,000 | catalogue | `9f3c56d5247154e47b5ca9500f4d86ce` |
| Rogue's Companion | shoulders | 12 | 10,100 | catalogue | `d24d5bcf3b99c6948a85c83d8c711ed2` |
| Cloak of Resistance +4 | shoulders | 12 | 16,000 | catalogue | `096125bfb3b364849b02ab65297f71de` |
| Cloak of Resistance +5 | shoulders | 12 | 25,000 | catalogue | `a34cd0f80d04ec647af741d924a3e2a3` |
| Diamond Dust | supplies | 999 | 50 | catalogue | `92752bbbf04dfa1439af186f48aee0e9` |
| Wand of Bless | wand | 24 | 135 | catalogue | `8bd02291b14bd7b4b914d6327b01787f` |
| Wand of Cure Light Wounds | wand | 24 | 465 | catalogue | `201dcf730b7185344ba701f91dd405db` |
| Wand of Mage Armor | wand | 24 | 750 | catalogue | `021b4a12739c59541922e3857f3fb3a4` |
| Wand of Restoration, Lesser | wand | 24 | 1,980 | catalogue | `002b777e1442b7a41a4ac8cc73aeff64` |
| Wand of Remove Disease | wand | 24 | 2,025 | catalogue | `a8c05388dec2bd64d9c413f3045fbaa7` |
| Wand of Remove Blindness | wand | 24 | 2,250 | catalogue | `93c55c75856218e47b74348e3d1791cd` |
| Wand of Neutralize Poison | wand | 24 | 3,825 | catalogue | `afb6a606b5d7f3a42a1a809ae8e269bb` |
| Wand of Haste | wand | 24 | 3,825 | catalogue | `fb97ef0889c25344f91b0d326967743a` |
| Wand of Cure Moderate Wounds | wand | 24 | 4,500 | catalogue | `e456cd580ff88e2449f9a8e4c204c4dc` |
| Wand of Blur | wand | 24 | 4,500 | catalogue | `e3c33b3af83b02c458b7ccaf822eb3b6` |
| Wand of Stoneskin | wand | 24 | 7,140 | catalogue | `22715f7c51efac54fa98b0b64686909b` |
| Wand of Cure Serious Wounds | wand | 24 | 11,250 | catalogue | `35fa690e41fb6b143b8c49cce316215d` |
| Bastard Sword +1 | weapon: BastardSword | 12 | 2,400 | catalogue | `adbc18abae2534740a7f162ae33a86fc` |
| Bastard Sword +2 | weapon: BastardSword | 12 | 8,300 | catalogue | `8b5d119f1f2009b4aaab7024bf53e483` |
| Bastard Sword +3 | weapon: BastardSword | 12 | 18,500 | catalogue | `1cf115afab1777245b035ffe8012c281` |
| Battleaxe +1 | weapon: Battleaxe | 12 | 2,400 | catalogue | `0ce4b3f4a3d70fa45851313a7f3d233f` |
| Battleaxe +2 | weapon: Battleaxe | 12 | 8,300 | catalogue | `21ed826b9c60c7f43af2a3c041046670` |
| Battleaxe +3 | weapon: Battleaxe | 12 | 18,500 | catalogue | `3d9e3fc1d4f4d8640bc4fac27a50fd5c` |
| Dagger +1 | weapon: Dagger | 12 | 2,400 | catalogue | `2a45458f776442e43bba57de65f9b738` |
| Cold Iron Dagger +1 | weapon: Dagger | 12 | 3,000 | catalogue | `ebea3139c4649e7459079d424c89e561` |
| Dagger +2 | weapon: Dagger | 12 | 8,300 | catalogue | `4607f83de16245844acb4596fc797147` |
| Dagger +3 | weapon: Dagger | 12 | 18,500 | catalogue | `4c08dc9c56fd3ea4da4c6825105f0498` |
| Orc Double Axe +1 | weapon: DoubleAxe | 12 | 2,400 | catalogue | `acf57e1471b5b1d40893c2f3186e49fa` |
| Orc Double Axe +2 | weapon: DoubleAxe | 12 | 8,300 | catalogue | `aa5ec13aa89ab6346a272c9ab92afd64` |
| Orc Double Axe +3 | weapon: DoubleAxe | 12 | 18,500 | catalogue | `aee6a20693a182a4fbd84b2746b13ef3` |
| Two-bladed Sword +1 | weapon: DoubleSword | 12 | 2,400 | catalogue | `f1db33a3f2b5dfd40a10fae7ea1fd76d` |
| Two-bladed Sword +2 | weapon: DoubleSword | 12 | 8,300 | catalogue | `c7f6f2e50a37d8c4baaa41035a918037` |
| Two-bladed Sword +3 | weapon: DoubleSword | 12 | 18,500 | catalogue | `e08a031aed7947b4ca43f1eef03d321f` |
| Dueling Sword +1 | weapon: DuelingSword | 12 | 2,400 | catalogue | `66106d59fd4615842af854cc9b7cbea4` |
| Dueling Sword +2 | weapon: DuelingSword | 12 | 8,300 | catalogue | `695057131a48b4f4884c00f690363f5e` |
| Dueling Sword +3 | weapon: DuelingSword | 12 | 18,500 | catalogue | `ea664cd1cf41a4044bbf3e6bcff18ebc` |
| Dwarven Waraxe +1 | weapon: DwarvenWaraxe | 12 | 2,400 | catalogue | `a06dd370e2d02cd449f82b209cbd61d6` |
| Dwarven Waraxe +2 | weapon: DwarvenWaraxe | 12 | 8,300 | catalogue | `8e6bada1ed926e94f851e9c50585d8b7` |
| Dwarven Waraxe +3 | weapon: DwarvenWaraxe | 12 | 18,500 | catalogue | `04798cc8a7a55494d8fb898fa1b52ab3` |
| Cold Iron Dwarven Waraxe +3 | weapon: DwarvenWaraxe | 12 | 20,500 | catalogue | `4c30b1cd4041d214dbe028e293c7a5ff` |
| Earth Breaker +1 | weapon: EarthBreaker | 12 | 2,400 | catalogue | `3329eae7f8a5e3e4d80e3e15746d82bb` |
| Cold Iron Earth Breaker +1 | weapon: EarthBreaker | 12 | 6,000 | catalogue | `23855f41005569b43bcdca1c299471b4` |
| Earth Breaker +2 | weapon: EarthBreaker | 12 | 8,300 | catalogue | `9cb48261dd41b7948bf630297ca6a92c` |
| Elven Curve Blade +1 | weapon: ElvenCurvedBlade | 12 | 2,400 | catalogue | `158fbe1c26025f3459c1af29a8faf2d8` |
| Elven Curve Blade +2 | weapon: ElvenCurvedBlade | 12 | 8,300 | catalogue | `a81078f8f9148b94ca9f438171792ce9` |
| Elven Curve Blade +3 | weapon: ElvenCurvedBlade | 12 | 18,500 | catalogue | `62ba202662960dd488330f24999435b6` |
| Estoc +1 | weapon: Estoc | 12 | 2,400 | catalogue | `11c66ec41f62708498f751abd911db88` |
| Estoc +2 | weapon: Estoc | 12 | 8,300 | catalogue | `8c31edb13b069104a946599c31cf9079` |
| Estoc +3 | weapon: Estoc | 12 | 18,500 | catalogue | `3ae572d645784214794eb667dccb3a98` |
| Falcata +1 | weapon: Falcata | 12 | 2,400 | catalogue | `babd2990d6ff6734d8a755a36cf8ad40` |
| Falcata +2 | weapon: Falcata | 12 | 8,300 | catalogue | `9b90a5b27e1a8994bae5cbb7298e1c15` |
| Falcata +3 | weapon: Falcata | 12 | 18,500 | catalogue | `678a667e5223d54419ea5deeb86c82f0` |
| Falchion +1 | weapon: Falchion | 12 | 2,400 | catalogue | `f93a7882fb01963428b091f259b15d3a` |
| Falchion +2 | weapon: Falchion | 12 | 8,300 | catalogue | `4884c93b5dcfcd341bbea50751f4cd20` |
| Falchion +3 | weapon: Falchion | 12 | 18,500 | catalogue | `a00f39e6fed1e544588782a73a5e9d99` |
| Fauchard +1 | weapon: Fauchard | 12 | 2,400 | catalogue | `36cadeb70d35fc84a9b69db9fb7e64f5` |
| Fauchard +2 | weapon: Fauchard | 12 | 8,300 | catalogue | `70a5422b73f42e441a1482c112e71afb` |
| Fauchard +3 | weapon: Fauchard | 12 | 18,500 | catalogue | `a802f19be1140cf4683b85fab38928ac` |
| Glaive +1 | weapon: Glaive | 12 | 2,400 | catalogue | `9b64866b008a35d4ab1ba6f1a23bb7dc` |
| Glaive +2 | weapon: Glaive | 12 | 8,300 | catalogue | `1ccf54ab622d5294f9a2ba65b96d09f7` |
| Glaive +3 | weapon: Glaive | 12 | 18,500 | catalogue | `7e4224e752358094a923c0ab5f636b3b` |
| Greataxe +1 | weapon: Greataxe | 12 | 2,400 | catalogue | `4f0704c1d4d53cf419bd200bdfc12a0a` |
| Greataxe +2 | weapon: Greataxe | 12 | 8,300 | catalogue | `b45b18552598a2d4c9cdc8296c490825` |
| Greataxe +3 | weapon: Greataxe | 12 | 18,500 | catalogue | `61c1565f7a9eeb74799d27f526718f40` |
| Greatsword +1 | weapon: Greatsword | 12 | 2,400 | catalogue | `e4925df9c7ab2714793cd5834e0a3039` |
| Greatsword +2 | weapon: Greatsword | 12 | 8,300 | catalogue | `8b0ad785727b2154ab4029508738fbdd` |
| Greatsword +3 | weapon: Greatsword | 12 | 18,500 | catalogue | `b734631d6b981a844bd817c2961d5f29` |
| Handaxe +1 | weapon: Handaxe | 12 | 2,400 | catalogue | `2618c8c4e4be9bc48acdbcc7d92d6dc1` |
| Cold Iron Handaxe +1 | weapon: Handaxe | 12 | 5,000 | catalogue | `76b3cfacefc66234ebe3f14669e9d208` |
| Handaxe +2 | weapon: Handaxe | 12 | 8,300 | catalogue | `a8c3b480482b24f4bb4f49bc47804ece` |
| Heavy Crossbow +1 | weapon: HeavyCrossbow | 12 | 2,400 | catalogue | `e8aa3f5ca26027b4b846de538b01ee50` |
| Heavy Crossbow +2 | weapon: HeavyCrossbow | 12 | 8,300 | catalogue | `c98788ea762ecad44b73610c1e065bf8` |
| Heavy Crossbow +3 | weapon: HeavyCrossbow | 12 | 18,500 | catalogue | `6967f62da3cfacb4eacb4f6227c4afd6` |
| Heavy Flail +1 | weapon: HeavyFlail | 12 | 2,400 | catalogue | `93db492fa4e99c34eabacb1ca5c159dd` |
| Heavy Flail +2 | weapon: HeavyFlail | 12 | 8,300 | catalogue | `b3d3daa4153f12e4db798ce76bdf554b` |
| Heavy Flail +3 | weapon: HeavyFlail | 12 | 18,500 | catalogue | `e3c21d29a55842146af65892cd2927cd` |
| Heavy Mace +1 | weapon: HeavyMace | 12 | 2,400 | catalogue | `86d5d758c2dd24747bad1c7b1f32e9df` |
| Heavy Mace +2 | weapon: HeavyMace | 12 | 8,300 | catalogue | `3e728d0847794234fbedd98fe62cdec8` |
| Heavy Mace +3 | weapon: HeavyMace | 12 | 18,500 | catalogue | `ed5c9e684981d774eab7aeec8e21a418` |
| Gnome Hooked Hammer +1 | weapon: HookedHammer | 12 | 2,400 | catalogue | `2ea8925adb856fe45990a4fd01ff0148` |
| Gnome Hooked Hammer +2 | weapon: HookedHammer | 12 | 8,300 | catalogue | `cbaa12b9fc981af40b8658b966114040` |
| Gnome Hooked Hammer +3 | weapon: HookedHammer | 12 | 18,500 | catalogue | `18449108bafff5d4f932cd3fb8621b9b` |
| Javelin +1 | weapon: Javelin | 12 | 2,400 | catalogue | `80756136484c44845ab19b2105c5702c` |
| Javelin +2 | weapon: Javelin | 12 | 8,300 | catalogue | `947df50f2c208c04388c7e165b8c8788` |
| Kama +1 | weapon: Kama | 12 | 2,400 | catalogue | `833d02226c289ce4892666e9d5880ae5` |
| Kama +2 | weapon: Kama | 12 | 8,300 | catalogue | `a58ef6b142297af4482a5154765e777f` |
| Kama +3 | weapon: Kama | 12 | 18,500 | catalogue | `726719d53e2c663408fb739c25b96786` |
| Kukri +1 | weapon: Kukri | 12 | 2,400 | catalogue | `cece2e26b026b244fba86a2075fc9811` |
| Kukri +2 | weapon: Kukri | 12 | 8,300 | catalogue | `ce7ef76c973ecfc47ba7515a743c8fd4` |
| Kukri +3 | weapon: Kukri | 12 | 18,500 | catalogue | `04284ae47696e4e4fa5fbdfe9e1f04cf` |
| Light Crossbow +1 | weapon: LightCrossbow | 12 | 2,400 | catalogue | `f5528acb89757ab4fb9350f45cc4f3b4` |
| Light Crossbow +2 | weapon: LightCrossbow | 12 | 8,300 | catalogue | `1fb0ea666a65a004dac6327d0e6682bc` |
| Light Crossbow +3 | weapon: LightCrossbow | 12 | 18,500 | catalogue | `8ee3176eb0ed29c418120b04dbb76aa9` |
| Light mace +1 | weapon: LightMace | 12 | 2,400 | catalogue | `5fe90cd1d77d8f446ac955dec7c8759f` |
| Light mace +2 | weapon: LightMace | 12 | 8,300 | catalogue | `6d228172531659e449fe189a4c579ea3` |
| Longbow +1 | weapon: Longbow | 12 | 2,400 | catalogue | `fd732e6688007e449964d8c5f2fc659d` |
| Composite Longbow +1 | weapon: Longbow | 12 | 2,500 | catalogue | `2753a0159681be94da3a5eeabc8c8d1a` |
| Longbow +2 | weapon: Longbow | 12 | 8,300 | catalogue | `335efd45aba89a245adf68a8da1fd3ca` |
| Longbow +3 | weapon: Longbow | 12 | 18,500 | catalogue | `426c900c0787c844c85bb4ac8cba6d27` |
| Composite Longbow +4 | weapon: Longbow | 12 | 32,000 | catalogue | `862cf5b6dc694054bad497636834faa8` |
| Longspear +1 | weapon: Longspear | 12 | 2,400 | catalogue | `9479af1ecd44ceb47a1e54b7268175c3` |
| Longspear +2 | weapon: Longspear | 12 | 8,300 | catalogue | `d512ed2b42265fa41bd3066eb11f6a90` |
| Longsword +1 | weapon: Longsword | 12 | 2,400 | catalogue | `03d706655c07d804cb9d5a5583f9aec5` |
| Cold Iron Longsword +1 | weapon: Longsword | 12 | 4,500 | catalogue | `203d3fbf74304ec4aba2ed1021742e59` |
| Longsword +2 | weapon: Longsword | 12 | 8,300 | catalogue | `193972650bc0e40409a6773e924ad612` |
| Longsword +3 | weapon: Longsword | 12 | 18,500 | catalogue | `68fbe8e38f215d2448ce7610e8d57a29` |
| Nunchaku +1 | weapon: Nunchaku | 12 | 2,400 | catalogue | `373894b330b2d7a4485284e1909d6b1a` |
| Nunchaku +2 | weapon: Nunchaku | 12 | 8,300 | catalogue | `c40d09652bcc37846b09f650b51963e9` |
| Nunchaku +3 | weapon: Nunchaku | 12 | 18,500 | catalogue | `9b24946e18686e244b935ed66bf29f14` |
| Quarterstaff +1 | weapon: Quarterstaff | 12 | 2,400 | catalogue | `2c4a3077efc2378499c60b03d2b0527a` |
| Quarterstaff +2 | weapon: Quarterstaff | 12 | 8,300 | catalogue | `a2aad194f9a067f408752bce24965dd6` |
| Quarterstaff +3 | weapon: Quarterstaff | 12 | 18,500 | catalogue | `1c6e64ffdb3c8af47b2c8b88f802c728` |
| Rapier +1 | weapon: Rapier | 12 | 2,400 | catalogue | `9d23d26b2fdbe484b89867667aabeebe` |
| Rapier +2 | weapon: Rapier | 12 | 8,300 | catalogue | `d713662146582ac48a38692aa6f7ec07` |
| Rapier +3 | weapon: Rapier | 12 | 18,500 | catalogue | `1a5f81e26e4c41c44a2a3b034369ef0b` |
| Sai +1 | weapon: Sai | 12 | 2,400 | catalogue | `096350418ea1f614bb489191398ab64b` |
| Sai +2 | weapon: Sai | 12 | 8,300 | catalogue | `97462573cb0d5c04283e0825d5182d6e` |
| Sai +3 | weapon: Sai | 12 | 18,500 | catalogue | `ebb1ce40634dbd24392a9225147eaa7d` |
| Scimitar +1 | weapon: Scimitar | 12 | 2,400 | catalogue | `7bef1327d4cb31249823d9fcb24af332` |
| Scimitar +2 | weapon: Scimitar | 12 | 8,300 | catalogue | `a231bbf909edfb548928e56091f71b56` |
| Scimitar +3 | weapon: Scimitar | 12 | 18,500 | catalogue | `536277f87a94d4a4ca7a6f777866059f` |
| Shortbow +1 | weapon: Shortbow | 12 | 2,400 | catalogue | `3e53a85140b50b24498e03e07a2df7e2` |
| Composite Shortbow +1 | weapon: Shortbow | 12 | 2,500 | catalogue | `eec59da36fadf594aac6bd9add90652e` |
| Shortbow +2 | weapon: Shortbow | 12 | 8,300 | catalogue | `2cbe261ab90d6cc4194cc1c8c85c3cbc` |
| Composite Shortbow +2 | weapon: Shortbow | 12 | 8,300 | catalogue | `1e396f7871b73234fb41d72cd8c441f9` |
| Shortbow +3 | weapon: Shortbow | 12 | 18,500 | catalogue | `3df5b2e2c59e5624eb564dbe9d754886` |
| Composite Shortbow +3 | weapon: Shortbow | 12 | 18,500 | catalogue | `2f1673e87242d2941a1e49b850dbc9f2` |
| Shortsword +1 | weapon: Shortsword | 12 | 2,400 | catalogue | `9f455505128866146a9bd81895d4cecd` |
| Shortsword +2 | weapon: Shortsword | 12 | 8,300 | catalogue | `0af6a4ac898a2ca4c9b0a7e76ec7522b` |
| Shortsword +3 | weapon: Shortsword | 12 | 18,500 | catalogue | `9ed29d74f5f9fc64786cc340806d420f` |
| Sling +1 | weapon: Sling | 12 | 2,400 | catalogue | `32786a0a5807d5a448581a8b17864319` |
| Sling +2 | weapon: Sling | 12 | 8,300 | catalogue | `c293e5abe38167b4e8b8cdaa1e95a054` |
| Sling Staff +1 | weapon: SlingStaff | 12 | 2,400 | catalogue | `643b5b476b7fc1942aa24a50dedc8de4` |
| Sling Staff +2 | weapon: SlingStaff | 12 | 8,300 | catalogue | `1dc78587aa10b0849b863f57615ddce2` |
| Sling Staff +3 | weapon: SlingStaff | 12 | 18,500 | catalogue | `69245594968164347b63b2ddc3c24eef` |
| Tongi +1 | weapon: Tongi | 12 | 2,400 | catalogue | `9d3d4b12be558c548b65c9093ff6ec54` |
| Tongi +2 | weapon: Tongi | 12 | 8,300 | catalogue | `d75803253b68d5944b7cc40fdb6211f6` |
| Tongi +3 | weapon: Tongi | 12 | 18,500 | catalogue | `a65588031b28e124780c77af70577ffa` |
| Dwarven Urgrosh +1 | weapon: Urgrosh | 12 | 2,400 | catalogue | `924b5edb3e09b2c4fbeb314b070b9eeb` |
| Dwarven Urgrosh +2 | weapon: Urgrosh | 12 | 8,300 | catalogue | `bad4fb2a5e668d44c80e541477f7457d` |
| Dwarven Urgrosh +3 | weapon: Urgrosh | 12 | 18,500 | catalogue | `2c5e6213acdcc3c489dc27dbbd301395` |
| Warhammer +1 | weapon: Warhammer | 12 | 2,400 | catalogue | `1ffd6bb86a1aedd4687b4a89ffb478e8` |
| Warhammer +2 | weapon: Warhammer | 12 | 8,300 | catalogue | `11ea0d6335a0fee4a87bfdc9201076c3` |
| Warhammer +3 | weapon: Warhammer | 12 | 18,500 | catalogue | `57898b7567742fb458a5e57d112c19ec` |
| Bracers of Armor +1 | wrist | 12 | 1,000 | catalogue | `9482c62934be44044918c3aac3730232` |
| Bracers of Armor +2 | wrist | 12 | 4,000 | catalogue | `bf063663cdb3df34c8ffa05ff1d1c877` |
| Lesser Bracers of Archery | wrist | 12 | 4,000 | catalogue | `01d51ff5f3db2164b88aaa662a9b0f2e` |
| Bracers of Armor +3 | wrist | 12 | 9,000 | catalogue | `cc5f45518020a324b8c51d0a231984a3` |
| Bracers of Archery | wrist | 12 | 10,000 | catalogue | `0e7a0b96f67660c4ca74786c187a02d2` |
| Bracers of Deflection | wrist | 12 | 13,000 | catalogue | `1e08c0e0300bff442ab9f8152f231f1f` |
| Bracers of Armor +4 | wrist | 12 | 16,000 | catalogue | `0a8cda1aaee1178419b32722c89dae64` |
| Bracers of Armor +5 | wrist | 12 | 25,000 | catalogue | `33179b698a3264e45bc3495a2181a0ea` |
| Bracers of Armor +6 | wrist | 12 | 36,000 | catalogue | `4811d69bde8fb0243b61c2b41300ebe7` |
| Lead Ball | supplies | 999 | 1 | Tabletop Rules Expansion | `55c29771445947d685dba9e1ead46a42` |
| Black Powder Charge | supplies | 999 | 10 | Tabletop Rules Expansion | `ea966bf998a647cf97b0ed92f71c4b7d` |
| Paper Cartridge | supplies | 999 | 12 | Tabletop Rules Expansion | `fea7337cfd06417a853546af9d950f77` |
| Gunsmith's Kit | supplies | 12 | 100 | Tabletop Rules Expansion | `d52aacb753434691b1ed85a16cc87104` |
| Elven Branched Spear | weapon: 4934983 | 12 | 20 | Tabletop Rules Expansion | `6edc216d68810960f85417237748b042` |
| Cold Iron Elven Branched Spear | weapon: 4934983 | 12 | 40 | Tabletop Rules Expansion | `8c0de00a236fe0f532d31711dcaa00a2` |
| Masterwork Elven Branched Spear | weapon: 4934983 | 12 | 320 | Tabletop Rules Expansion | `9c9edabf91f2117fd1b642c4d39b9574` |
| Masterwork Cold Iron Elven Branched Spear | weapon: 4934983 | 12 | 340 | Tabletop Rules Expansion | `b16c34215cae9d60345042157149a4c0` |
| +1 Elven Branched Spear | weapon: 4934983 | 12 | 2,320 | Tabletop Rules Expansion | `66111becd22690a2a19444a5c6bd0c7b` |
| +1 Cold Iron Elven Branched Spear | weapon: 4934983 | 12 | 4,340 | Tabletop Rules Expansion | `25d8f6c6f4767b3168f4700a2890954f` |
| +2 Elven Branched Spear | weapon: 4934983 | 12 | 8,320 | Tabletop Rules Expansion | `59c1cc59146a42b0bcecd080bf418f2b` |
| +3 Elven Branched Spear | weapon: 4934983 | 12 | 18,320 | Tabletop Rules Expansion | `5ee1cb27471943cea19b3e76b3e06e46` |
| Wakizashi | weapon: 4934984 | 12 | 35 | Tabletop Rules Expansion | `b61ee7e62bc9288004eb0121c8f5d37e` |
| Cold Iron Wakizashi | weapon: 4934984 | 12 | 70 | Tabletop Rules Expansion | `1aa01a0528eb595b5cbf19ac7c71a64e` |
| Masterwork Wakizashi | weapon: 4934984 | 12 | 335 | Tabletop Rules Expansion | `58fd0f272f4523458016dc3656b778c3` |
| +1 Wakizashi | weapon: 4934984 | 12 | 2,335 | Tabletop Rules Expansion | `83a507873a518b54793d0da632def246` |
| +2 Wakizashi | weapon: 4934984 | 12 | 8,335 | Tabletop Rules Expansion | `744ffd16fc1b4bd98bd06eb419c0ec7d` |
| +3 Wakizashi | weapon: 4934984 | 12 | 18,335 | Tabletop Rules Expansion | `331b4668855841da933887e29adf69d9` |
| Katana | weapon: 4934985 | 12 | 50 | Tabletop Rules Expansion | `aba40a9e8302b31e4daa2acf6ab48a46` |
| Cold Iron Katana | weapon: 4934985 | 12 | 100 | Tabletop Rules Expansion | `599f8f45f325911ffcbdbd6544ba114f` |
| Masterwork Katana | weapon: 4934985 | 12 | 350 | Tabletop Rules Expansion | `37e8c76b2fc196e9f82e1196e918263c` |
| +1 Katana | weapon: 4934985 | 12 | 2,350 | Tabletop Rules Expansion | `87b3d851726a4a9abd0baec6beca957c` |
| +2 Katana | weapon: 4934985 | 12 | 8,350 | Tabletop Rules Expansion | `8a6b48a90a4743c9a74cd65b6d063094` |
| +3 Katana | weapon: 4934985 | 12 | 18,350 | Tabletop Rules Expansion | `cc429a9210234467acfeeb16d6859193` |
| Nodachi | weapon: 4934986 | 12 | 60 | Tabletop Rules Expansion | `35b7082d98ff45ba51dce536a1bc68a1` |
| Cold Iron Nodachi | weapon: 4934986 | 12 | 120 | Tabletop Rules Expansion | `0db026048052031be931b9701b3859ef` |
| Masterwork Nodachi | weapon: 4934986 | 12 | 360 | Tabletop Rules Expansion | `df5a3b333eab59c04028d88084d7ada9` |
| +1 Nodachi | weapon: 4934986 | 12 | 2,360 | Tabletop Rules Expansion | `38e31ba5cdbdc668f8dcd8985070c0b7` |
| +2 Nodachi | weapon: 4934986 | 12 | 8,360 | Tabletop Rules Expansion | `be8ad97d3b5f479788ddb73988b4fda8` |
| +3 Nodachi | weapon: 4934986 | 12 | 18,360 | Tabletop Rules Expansion | `f25d6a89ba364cb0874bb22e559e06bf` |
| Musket | weapon: HeavyCrossbow | 12 | 1,500 | Tabletop Rules Expansion | `6c9cdfa2d47e4894847fa85d5319fbd2` |
| Blunderbuss | weapon: HeavyCrossbow | 12 | 2,000 | Tabletop Rules Expansion | `236f3e167f5542bcac22bca72046fb1f` |
| Musket +1 | weapon: HeavyCrossbow | 12 | 3,800 | Tabletop Rules Expansion | `3402fe01de1648b187c192500e370f01` |
| Blunderbuss +1 | weapon: HeavyCrossbow | 12 | 4,300 | Tabletop Rules Expansion | `1dc7efe0792040f187a18adfdc54c6e0` |
| Musket +2 | weapon: HeavyCrossbow | 12 | 9,800 | Tabletop Rules Expansion | `839dd0d49e7c4fbc92764475e31ee175` |
| Reliable Musket +1 | weapon: HeavyCrossbow | 12 | 9,800 | Tabletop Rules Expansion | `ffd5c4578cce403ebb42efb161a30f43` |
| Blunderbuss +2 | weapon: HeavyCrossbow | 12 | 10,300 | Tabletop Rules Expansion | `ec12481bf56a4fa4addeefafdcfb0be7` |
| Reliable Blunderbuss +1 | weapon: HeavyCrossbow | 12 | 10,300 | Tabletop Rules Expansion | `8ca37dd6f1c741bdbe7f18570037699c` |
| Musket +3 | weapon: HeavyCrossbow | 12 | 19,800 | Tabletop Rules Expansion | `6980d1bfc1d94cd19ea033ad31335c79` |
| Reliable Musket +2 | weapon: HeavyCrossbow | 12 | 19,800 | Tabletop Rules Expansion | `ae4b32641ad847bfb4dc4e257dbd6415` |
| Blunderbuss +3 | weapon: HeavyCrossbow | 12 | 20,300 | Tabletop Rules Expansion | `1324e5cb0c374dd6b998546e83509d20` |
| Reliable Blunderbuss +2 | weapon: HeavyCrossbow | 12 | 20,300 | Tabletop Rules Expansion | `3d45b093ac204b4ea859c76be35ecf0d` |
| Reliable Musket +3 | weapon: HeavyCrossbow | 12 | 33,800 | Tabletop Rules Expansion | `e9fe300679ec44dfacf6be958ebf7b19` |
| Reliable Blunderbuss +3 | weapon: HeavyCrossbow | 12 | 34,300 | Tabletop Rules Expansion | `237cdb2b9292418387674526e5aea35d` |
| Pistol | weapon: LightCrossbow | 12 | 1,000 | Tabletop Rules Expansion | `a303d71d244640959827e9464df5a867` |
| Pistol +1 | weapon: LightCrossbow | 12 | 3,300 | Tabletop Rules Expansion | `d0145d0410a34df08d68a67367c1dfc9` |
| Pistol +2 | weapon: LightCrossbow | 12 | 9,300 | Tabletop Rules Expansion | `b767c7389ba64eb19008fa9d66712985` |
| Reliable Pistol +1 | weapon: LightCrossbow | 12 | 9,300 | Tabletop Rules Expansion | `03e9ba98b2954416b93f73b7045daed9` |
| Pistol +3 | weapon: LightCrossbow | 12 | 19,300 | Tabletop Rules Expansion | `e52bc130e64a4000875eaa0f903b5ffe` |
| Reliable Pistol +2 | weapon: LightCrossbow | 12 | 19,300 | Tabletop Rules Expansion | `04b5b1bb6e3f47bd8b7b7427ba913a7b` |
| Reliable Pistol +3 | weapon: LightCrossbow | 12 | 33,300 | Tabletop Rules Expansion | `aff91d56deba4542b28ac1a57741a1af` |
| Scroll of Acid Splash | scroll | 99 | 12 | Wizard scrolls | `b7567615bc77660b75e96ede61b818de` |
| Scroll of Daze | scroll | 99 | 12 | Wizard scrolls | `ac3a1d708c83d301c7e921b07cc1400e` |
| Scroll of Disrupt Undead | scroll | 99 | 12 | Wizard scrolls | `88e16c1a9a4993e574b0300477a7cf80` |
| Scroll of Flare | scroll | 99 | 12 | Wizard scrolls | `fe0bf7f7f0381a5ef53226a541eafd76` |
| Scroll of Jolt | scroll | 99 | 12 | Wizard scrolls | `b2ca055a8538f95ace16ef4864f729a5` |
| Scroll of Light | scroll | 99 | 12 | Wizard scrolls | `2b51be19976a3f1669ea663c9eff0592` |
| Scroll of Ray of Frost | scroll | 99 | 12 | Wizard scrolls | `217900376a2a831c012aec4c18a5b96b` |
| Scroll of Resistance | scroll | 99 | 12 | Wizard scrolls | `f66fb91178a1b8a5a74f8d4a9313f416` |
| Scroll of Touch of Fatigue | scroll | 99 | 12 | Wizard scrolls | `8fc63144b1b4b9fb27db4b3f56bb1103` |
| Scroll of Burning Hands | scroll | 99 | 25 | Wizard scrolls | `3c56e535129756e449af6c0e67fd937f` |
| Scroll of Cause Fear | scroll | 99 | 25 | Wizard scrolls | `fe244c39bdd5cb64eae65af23c6759de` |
| Scroll of Color Spray | scroll | 99 | 25 | Wizard scrolls | `334842bcad874674b92f115412fd5d08` |
| Scroll of Corrosive Touch | scroll | 99 | 25 | Wizard scrolls | `68d5aa212b7323e4e95e0fe731ea50cf` |
| Scroll of Ear-Piercing Scream | scroll | 99 | 25 | Wizard scrolls | `17afee356af23734393bb157daa8b29c` |
| Scroll of Enlarge Person | scroll | 99 | 25 | Wizard scrolls | `affbadb670f599b4084029e5ad784cb7` |
| Scroll of Expeditious Retreat | scroll | 99 | 25 | Wizard scrolls | `fbf7f9d21c12146479a5c261743afbde` |
| Scroll of Flare Burst | scroll | 99 | 25 | Wizard scrolls | `2af933b93ac31ba449001e7e3b911b5b` |
| Scroll of Grease | scroll | 99 | 25 | Wizard scrolls | `a4fbba95ffa58144ca7189bc350ed622` |
| Scroll of Hurricane Bow | scroll | 99 | 25 | Wizard scrolls | `ce41e625eae914d4bad729f090e9001f` |
| Scroll of Shield | scroll | 99 | 25 | Wizard scrolls | `807763fd874989e4d96eb2d8e234139e` |
| Scroll of Magic Missile | scroll | 99 | 25 | Wizard scrolls | `63caf94a780472b448f50d0bc183c38f` |
| Scroll of Protection from Alignment | scroll | 99 | 25 | Wizard scrolls | `e12ba1c92eea8b626c3c2aed166b9ee8` |
| Scroll of Ray of Enfeeblement | scroll | 99 | 25 | Wizard scrolls | `792862674c565ad4fbb1ab0c97c42acd` |
| Scroll of Ray of Sickening | scroll | 99 | 25 | Wizard scrolls | `37b332a8c7d904e47b46b5df49e62057` |
| Scroll of Reduce Person | scroll | 99 | 25 | Wizard scrolls | `a341ba25915761441905bc6c82677244` |
| Scroll of Shocking Grasp | scroll | 99 | 25 | Wizard scrolls | `e10f63bd61591554791d133270f8d80f` |
| Scroll of Sleep | scroll | 99 | 25 | Wizard scrolls | `f79c3fd5012a3534c8ab36dc18e85fb1` |
| Scroll of Snowball | scroll | 99 | 25 | Wizard scrolls | `66fc961f9c39ae94fb87a79adc87212e` |
| Scroll of Stone Fist | scroll | 99 | 25 | Wizard scrolls | `42d9445b9cdfac94385eaa2a3499b204` |
| Scroll of Stunning Barrier | scroll | 99 | 25 | Wizard scrolls | `e029ec259c9a37249b113060df32a01d` |
| Scroll of Summon Monster I | scroll | 99 | 25 | Wizard scrolls | `26acf0a0265086a4cbd89f298241bc03` |
| Scroll of Touch of Gracelessness | scroll | 99 | 25 | Wizard scrolls | `318abebf99d5a2e4593538bf7dbc5e04` |
| Scroll of True Strike | scroll | 99 | 25 | Wizard scrolls | `5b6d4c0bbd882074eb6b6996ea77b77c` |
| Scroll of Vanish | scroll | 99 | 25 | Wizard scrolls | `cd384c463dada4a4b97a521eab2539c0` |
| Scroll of Hypnotism | scroll | 99 | 25 | Wizard scrolls | `78da1d5a10d8e384285e54df202dfb01` |
| Scroll of Acid Arrow | scroll | 99 | 150 | Wizard scrolls | `b5f4c614c7b10f141b70bf3f64f9d82b` |
| Scroll of Bear's Endurance | scroll | 99 | 150 | Wizard scrolls | `d6a7f98f92abfd5458b606f17de4db40` |
| Scroll of Blindness | scroll | 99 | 150 | Wizard scrolls | `395bfedde7392aa42b52faea16da6b07` |
| Scroll of Boneshaker | scroll | 99 | 150 | Wizard scrolls | `56bb28cb527c4e54c9a5def8de6a3c31` |
| Scroll of Bull's Strength | scroll | 99 | 150 | Wizard scrolls | `102d903c65636f341ae7cb4533905ffa` |
| Scroll of Burning Arc | scroll | 99 | 150 | Wizard scrolls | `b5239446c08850347b803eb57c271dab` |
| Scroll of Cat's Grace | scroll | 99 | 150 | Wizard scrolls | `5316739331090164f832cf31edb60696` |
| Scroll of Create Pit | scroll | 99 | 150 | Wizard scrolls | `bc948837c7acb664eb8d89ac7749fa18` |
| Scroll of Eagle's Splendor | scroll | 99 | 150 | Wizard scrolls | `7541eb357d7fc64489f140f437aee6af` |
| Scroll of False Life | scroll | 99 | 150 | Wizard scrolls | `04954e21ad7b5a54a9a9c29b4ed79052` |
| Scroll of Fox's Cunning | scroll | 99 | 150 | Wizard scrolls | `a53b780b854b0864f8b6e0797e23e278` |
| Scroll of Frigid Touch | scroll | 99 | 150 | Wizard scrolls | `46ca6a02fa6cae04485f558aeaf9891c` |
| Scroll of Glitterdust | scroll | 99 | 150 | Wizard scrolls | `82ce8bfaf96e215458b5da1953e73ff5` |
| Scroll of Hideous Laughter | scroll | 99 | 150 | Wizard scrolls | `aed1e536ad8851947a3d37644fa87c03` |
| Scroll of Mirror Image | scroll | 99 | 150 | Wizard scrolls | `bae1ca27b27c3eb4f9e38c03abcdb64a` |
| Scroll of Molten Orb | scroll | 99 | 150 | Wizard scrolls | `ecb145cb6e1b206408c8fd618337aded` |
| Scroll of Owl's Wisdom | scroll | 99 | 150 | Wizard scrolls | `8c89e97d333561741bedfc98a364ac67` |
| Scroll of Pernicious Poison | scroll | 99 | 150 | Wizard scrolls | `ac73301ffe7e0ac4ebf7f1ed0ce014df` |
| Scroll of Protection from Alignment, Communal | scroll | 99 | 150 | Wizard scrolls | `2dcc386fa4fef54ede9feaa8554a81fb` |
| Scroll of Protection from Arrows | scroll | 99 | 150 | Wizard scrolls | `179d91b899fa6304b8c076e002890317` |
| Scroll of Resist Energy | scroll | 99 | 150 | Wizard scrolls | `707ea07c028315c7aea0a8536c38fe34` |
| Scroll of Scare | scroll | 99 | 150 | Wizard scrolls | `a3f5fbdd5f43f274baa3ee8c96d8c1a8` |
| Scroll of Scorching Ray | scroll | 99 | 150 | Wizard scrolls | `95bf8273fd7930c4da3eaaf636a6cd29` |
| Scroll of See Invisibility | scroll | 99 | 150 | Wizard scrolls | `12f4ee72c02537244b5b2bacfa236bc7` |
| Scroll of Sense Vitals | scroll | 99 | 150 | Wizard scrolls | `d4dc796367b4fb342ad7e8221fb8d813` |
| Scroll of Stone Call | scroll | 99 | 150 | Wizard scrolls | `87a562c6974bffb4aa9db89db0bccec8` |
| Scroll of Summon Small Elemental | scroll | 99 | 150 | Wizard scrolls | `fa3f0087bc9653e4b42640b38a2b3ebf` |
| Scroll of Summon Monster II | scroll | 99 | 150 | Wizard scrolls | `c15397df7f44df5093f9ce829735d2b2` |
| Scroll of Web | scroll | 99 | 150 | Wizard scrolls | `efc97cdb15c92194bbbe4a2a3c1ecd78` |
| Scroll of Battering Blast | scroll | 99 | 375 | Wizard scrolls | `1940573d3b0a47949832488a27831b19` |
| Scroll of Beast Shape I | scroll | 99 | 375 | Wizard scrolls | `3598a1d21491993419c7484028257eac` |
| Scroll of Deep Slumber | scroll | 99 | 375 | Wizard scrolls | `63bc71021dbba204c96cdb88971d9083` |
| Scroll of Dispel Magic | scroll | 99 | 375 | Wizard scrolls | `c7384b3c45e8dad28211bcf9a1705174` |
| Scroll of Hold Person | scroll | 99 | 375 | Wizard scrolls | `11987b82dbc808143963b9acaa81bf07` |
| Scroll of Protection from Arrows, Communal | scroll | 99 | 375 | Wizard scrolls | `45e656affe6f0614ab8e99a5e09d56a2` |
| Scroll of Protection From Energy | scroll | 99 | 375 | Wizard scrolls | `10ff32501dc78a1a4f58c78a66bd935e` |
| Scroll of Rage | scroll | 99 | 375 | Wizard scrolls | `81f35b521b23ad947acaa8151fa81139` |
| Scroll of Resist Energy, Communal | scroll | 99 | 375 | Wizard scrolls | `355bf287b78abf3c555df45f134726f1` |
| Scroll of See Invisibility, Communal | scroll | 99 | 375 | Wizard scrolls | `36a77c24f4060044685daa8c9facf51f` |
| Scroll of Slow | scroll | 99 | 375 | Wizard scrolls | `fc4f7d5bfd3502a4d94e9de731a86c8a` |
| Scroll of Spiked Pit | scroll | 99 | 375 | Wizard scrolls | `8aca97895c1c6ae458bfd3cfd7fa4680` |
| Scroll of Stinking Cloud | scroll | 99 | 375 | Wizard scrolls | `c92308c160d6d424fb64f1fd708aa6cd` |
| Scroll of Summon Monster III | scroll | 99 | 375 | Wizard scrolls | `580fd4b5d873e3711fe0cc5b6645d2b3` |
| Scroll of Vampiric Touch | scroll | 99 | 375 | Wizard scrolls | `b693e8cc94f4477498d396a40817ff77` |
| Scroll of Bestow Curse | scroll | 99 | 700 | Wizard scrolls | `a7be773786f85c85acbbdd8eec12d040` |
| Scroll of Contagion | scroll | 99 | 700 | Wizard scrolls | `b97aee68e62b74accbc872cb81dd9e9d` |
| Scroll of Crushing Despair | scroll | 99 | 700 | Wizard scrolls | `5da62245cf5b9e54fa12aeaaf398b298` |
| Scroll of Overwhelming Grief | scroll | 99 | 700 | Wizard scrolls | `8a2189c96e35b9f4da4a2f53bcdfa466` |
| Scroll of Remove Curse | scroll | 99 | 700 | Wizard scrolls | `f2b8655382429c14c8c888f1cf109020` |
| Scroll of Acid Pit | scroll | 99 | 700 | Wizard scrolls | `bb030e6b3b1ae8c46ad2f013e42e597e` |
| Scroll of Animate Dead | scroll | 99 | 700 | Wizard scrolls | `bc0180b8b29abf9468dea1a24332d159` |
| Scroll of Beast Shape II | scroll | 99 | 700 | Wizard scrolls | `d68c2728020fbe543826a9f6beaad379` |
| Scroll of Boneshatter | scroll | 99 | 700 | Wizard scrolls | `6f36a274115b20e4c86fa6771be8b7dd` |
| Scroll of Confusion | scroll | 99 | 700 | Wizard scrolls | `1db86aaa479be6944abe90eaddb4afa2` |
| Scroll of Controlled Fireball | scroll | 99 | 700 | Wizard scrolls | `4b0ff254dca06894cba7eace7eef6bfe` |
| Scroll of Dimension Door | scroll | 99 | 700 | Wizard scrolls | `7aa654dc6c15e612c4bf5f57e1c481f3` |
| Scroll of Dragon's Breath | scroll | 99 | 700 | Wizard scrolls | `83a7fc87fe99f7a0a5b1abc0ee75f8e4` |
| Scroll of Elemental Body I | scroll | 99 | 700 | Wizard scrolls | `3f9c266e88bf2cb65445ded41fc1c519` |
| Scroll of Enervation | scroll | 99 | 700 | Wizard scrolls | `0589a68074a003b49961ec334a0e9138` |
| Scroll of Enlarge Person, Mass | scroll | 99 | 700 | Wizard scrolls | `6e77ed864d7112d44a2e9bc73a265060` |
| Scroll of False Life, Greater | scroll | 99 | 700 | Wizard scrolls | `7bb01ba2ab53cd64693ad324564af5cb` |
| Scroll of Fear | scroll | 99 | 700 | Wizard scrolls | `163001e34e0669a4f8cab43dc4e29dd9` |
| Scroll of Ice Storm | scroll | 99 | 700 | Wizard scrolls | `a5c98d83dd1e2b2448acf51552c9b9d3` |
| Scroll of Obsidian Flow | scroll | 99 | 700 | Wizard scrolls | `695cc4b16a367144ca1717b37bce626e` |
| Scroll of Phantasmal Killer | scroll | 99 | 700 | Wizard scrolls | `2b73dd1aecdbf1e4a8d7ca18139b1cca` |
| Scroll of Protection from Energy, Communal | scroll | 99 | 700 | Wizard scrolls | `6246693fa7ae14da7cb06c8e723a2e10` |
| Scroll of Rainbow Pattern | scroll | 99 | 700 | Wizard scrolls | `84cd707a7ae9f934389ed6bbf51b023a` |
| Scroll of Reduce Person, Mass | scroll | 99 | 700 | Wizard scrolls | `fbde449fffbf2d64983ad2e11981a23a` |
| Scroll of Shout | scroll | 99 | 700 | Wizard scrolls | `4c73f11f91ca3fb4a8af325686b660d8` |
| Scroll of Stoneskin | scroll | 99 | 700 | Wizard scrolls | `a8422c98704e6a0429ebc5a56e132d95` |
| Scroll of Summon Medium Elemental | scroll | 99 | 700 | Wizard scrolls | `a718b9cbf588fbf7097f7adbacbf489d` |
| Scroll of Summon Monster IV | scroll | 99 | 700 | Wizard scrolls | `3e8766e5b6c9bf59aba9ad84baaef52d` |
| Scroll of Touch of Slime | scroll | 99 | 700 | Wizard scrolls | `d1a586649cea48f48a0c535aacecfba3` |
| Scroll of Volcanic Storm | scroll | 99 | 700 | Wizard scrolls | `349a071cd51f6224e9647dc2cf06c243` |
| Scroll of Angelic Aspect | scroll | 99 | 1,125 | Wizard scrolls | `4c79f5ad0bc683d43ac0bda57f7843b3` |
| Scroll of Echolocation | scroll | 99 | 1,125 | Wizard scrolls | `33770ff24b320e343bb767815f800fc4` |
| Scroll of Acidic Spray | scroll | 99 | 1,125 | Wizard scrolls | `ba78d5a438e8d154dbd6e4f991fc320f` |
| Scroll of Animal Growth | scroll | 99 | 1,125 | Wizard scrolls | `3d11bb02852d02649850a73629ea283d` |
| Scroll of Baleful Polymorph | scroll | 99 | 1,125 | Wizard scrolls | `06da2d0278495514bab116a4ebbb3f98` |
| Scroll of Beast Shape III | scroll | 99 | 1,125 | Wizard scrolls | `f7ff7c9f1fe595d4ba7d002d17d678c9` |
| Scroll of Break Enchantment | scroll | 99 | 1,125 | Wizard scrolls | `358ee9cb540a9af4e9bc76cc1af62e86` |
| Scroll of Cloudkill | scroll | 99 | 1,125 | Wizard scrolls | `ecd7efc465dbbac44a07c7e9bbd06be2` |
| Scroll of Constricting Coils | scroll | 99 | 1,125 | Wizard scrolls | `ebaeaebc1a30536468ba680aff1d6471` |
| Scroll of Dismissal | scroll | 99 | 1,125 | Wizard scrolls | `aef1531dc1d3799428eaa5e9b70b5e2c` |
| Scroll of Dominate Person | scroll | 99 | 1,125 | Wizard scrolls | `f199f6e5026488c499042900b572eb7f` |
| Scroll of Elemental Body II | scroll | 99 | 1,125 | Wizard scrolls | `e73d2c10e29ffa040e3549b745164ee8` |
| Scroll of Feeblemind | scroll | 99 | 1,125 | Wizard scrolls | `17c17e5c409d6814786259d99bf9a43d` |
| Scroll of Fire Snake | scroll | 99 | 1,125 | Wizard scrolls | `73d88f8434d24c848a1c77a9c05f68c0` |
| Scroll of Geniekind | scroll | 99 | 1,125 | Wizard scrolls | `012c230365f929a7a93497dbeefe8ae9` |
| Scroll of Hold Monster | scroll | 99 | 1,125 | Wizard scrolls | `d8c52df98e5815d4aa9d2bd2b73b88c9` |
| Scroll of Icy Prison | scroll | 99 | 1,125 | Wizard scrolls | `de05d46f44c8439488a8bbcc0059c09f` |
| Scroll of Mind Fog | scroll | 99 | 1,125 | Wizard scrolls | `61bacc43652d76c42b60d965b65cd741` |
| Scroll of Phantasmal Web | scroll | 99 | 1,125 | Wizard scrolls | `4b0dc7926c0ad8d4bae8d7a5f3eed6f8` |
| Scroll of Polymorph | scroll | 99 | 1,125 | Wizard scrolls | `5e4dcce7e622a8b55ad607c63a7bf290` |
| Scroll of Shadow Evocation | scroll | 99 | 1,125 | Wizard scrolls | `bd7614e3dcbf1f33a3646a9597305e14` |
| Scroll of Stoneskin, Communal | scroll | 99 | 1,125 | Wizard scrolls | `0dd764b2234a4d84dadeb45a2deaf9f9` |
| Scroll of Summon Large Elemental | scroll | 99 | 1,125 | Wizard scrolls | `bffbb8f8ca0d150f775c970dcaee644a` |
| Scroll of Summon Monster V | scroll | 99 | 1,125 | Wizard scrolls | `0e5ad4837518462afcbdb5ef1cda777c` |
| Scroll of Vampiric Shadow Shield | scroll | 99 | 1,125 | Wizard scrolls | `1bcc05d40824dfa46b7f6cb6318b4926` |
| Scroll of Waves of Fatigue | scroll | 99 | 1,125 | Wizard scrolls | `3baaaa7cb91704342b0314b6f06cdc91` |
| Scroll of Teleport | scroll | 99 | 1,125 | Wizard scrolls | `4551541688de0b48a47e4890cfe60c9a` |
| Scroll of Hungry Pit | scroll | 99 | 1,625 | Wizard scrolls | `bc25efed54c66a147b3615fb05f302e0` |
| Scroll of Chains of Light | scroll | 99 | 1,650 | Wizard scrolls | `4360179eb458c3c4b896f68f66c33607` |
| Scroll of Serenity | scroll | 99 | 1,650 | Wizard scrolls | `7c2fe7abcecee534d95d5ddd4d814585` |
| Scroll of Acid Fog | scroll | 99 | 1,650 | Wizard scrolls | `a931181d47d35ae4da06a243906eece2` |
| Scroll of Banshee Blast | scroll | 99 | 1,650 | Wizard scrolls | `f6073b26d3e1d61418f62928f34b601f` |
| Scroll of Bear's Endurance, Mass | scroll | 99 | 1,650 | Wizard scrolls | `78631cf6b42590f40b44c79389c6af74` |
| Scroll of Beast Shape IV | scroll | 99 | 1,650 | Wizard scrolls | `a09737f1304acb932c48fb2b3aab91b1` |
| Scroll of Bull's Strength, Mass | scroll | 99 | 1,650 | Wizard scrolls | `7e6ca8816f50a7f4e9dccc90f958e4f4` |
| Scroll of Cat's Grace, Mass | scroll | 99 | 1,650 | Wizard scrolls | `e1e455570ca262244a572a3efed337ce` |
| Scroll of Chain Lightning | scroll | 99 | 1,650 | Wizard scrolls | `141ffc89f1630ff45812f50ed3922088` |
| Scroll of Circle of Death | scroll | 99 | 1,650 | Wizard scrolls | `beab337b352b5ac479698e2bbc08f4ce` |
| Scroll of Cloak of Dreams | scroll | 99 | 1,650 | Wizard scrolls | `fc43f48bf3f0ad74a9eb9b81fc531f25` |
| Scroll of Cold Ice Strike | scroll | 99 | 1,650 | Wizard scrolls | `686c2a9cffbf0cc4d9038d66a852cc3b` |
| Scroll of Dispel Magic, Greater | scroll | 99 | 1,650 | Wizard scrolls | `56975b102c539b544a77e602c7d8d490` |
| Scroll of Eagle's Splendor, Mass | scroll | 99 | 1,650 | Wizard scrolls | `fe94b92146d7bb44c9983ed6d3080d8a` |
| Scroll of Elemental Assessor | scroll | 99 | 1,650 | Wizard scrolls | `7dce00472dcde1644b908d4e0eefc078` |
| Scroll of Elemental Body III | scroll | 99 | 1,650 | Wizard scrolls | `577f66ea0da95598df91a1308dc606db` |
| Scroll of Eyebite | scroll | 99 | 1,650 | Wizard scrolls | `f80549ec4e61ae64d8051bb3b24cf10e` |
| Scroll of Dragonkind I | scroll | 99 | 1,650 | Wizard scrolls | `dbb1c74a280bf6915abac5e63189d6b0` |
| Scroll of Fox's Cunning, Mass | scroll | 99 | 1,650 | Wizard scrolls | `bdfbfdcecfdc3b346a1e6fc37bca8bb7` |
| Scroll of Hellfire Ray | scroll | 99 | 1,650 | Wizard scrolls | `eca3b7aa601fc1b46a940897cb2142d3` |
| Scroll of Owl's Wisdom, Mass | scroll | 99 | 1,650 | Wizard scrolls | `2f247d1cb09ca6341bdd01cdcc3c46a9` |
| Scroll of Phantasmal Putrefaction | scroll | 99 | 1,650 | Wizard scrolls | `cd9e8fa2df821d04ab46c8affaff9470` |
| Scroll of Sirocco | scroll | 99 | 1,650 | Wizard scrolls | `c17e4bd5028d6534a8c8d317cd8244ca` |
| Scroll of Summon Huge Elemental | scroll | 99 | 1,650 | Wizard scrolls | `b571be8ddcfe584dd688aaf1959e7eca` |
| Scroll of Summon Monster VI | scroll | 99 | 1,650 | Wizard scrolls | `39c17f317c071040f53214fe38c89409` |
| Scroll of Tar Pool | scroll | 99 | 1,650 | Wizard scrolls | `d81fa529db47380468c9e8310f8d18ea` |
| Scroll of Transformation | scroll | 99 | 1,650 | Wizard scrolls | `d7536624e1ab8d8499f6e8d9689940b6` |
| Scroll of True Seeing | scroll | 99 | 1,650 | Wizard scrolls | `7a05e6eea60a1864d935abd6c281a531` |
| Scroll of Undeath to Death | scroll | 99 | 1,650 | Wizard scrolls | `733c0b265de8d7a46afe43f404a1fa36` |
