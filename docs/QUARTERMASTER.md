# Quartermaster catalogue

The CSV catalogue of the Society Quartermaster in the Magnimar Lodge: 395 lines, 11700 items, generated from `data/quartermaster.csv` against installed game data (2.1.7b). In game the shop also stocks a scroll of every Wizard spell of levels 0-6 (99 each) and, when it is enabled, the optional Tabletop Rules Expansion lines; see `docs/WR-PLAYTEST-01-STOCK.md` for the full in-game audit.

**Pricing:** native vendor rules with no price overrides. Buying costs each item's `Cost` (listed below). Selling pays `Cost x BlueprintRoot.Vendors.SellModifier`, so buying then re-selling always loses money. An item sold to the vendor joins its stock. Stock lives in the save (`Player.SharedVendorTables`); a catalogue change reaches a visited shop once, by the native difference (new lines arrive, a removed retail line leaves, sold goods stay). **Resupply:** when the shop opens, any line whose shelf count is below its catalogue count is topped up to it with new merchant stock; nothing above it changes.

**Deliberate omissions:** named or quest items, artifacts, kingdom rewards and developer copies; inherent-bonus tomes and manuals; weapons and armor above +3 (the one exception is the +4 composite longbow, because the game has no +2/+3 composite longbow blueprint); +6 stat items; greater metamagic rods and the normal Quicken rod; Gloves of Dexterity (owner policy, WR-PLAYTEST-01). Tabletop items absent from Kingmaker are not added. **Quantities:** at least 12 of every equipment line, 99 of every potion and scroll, 24 of every wand, 999 diamond dust.

## Melee weapons

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Longsword +1 | 12 | 2,400 | `03d706655c07d804cb9d5a5583f9aec5` |
| Longsword +2 | 12 | 8,300 | `193972650bc0e40409a6773e924ad612` |
| Longsword +3 | 12 | 18,500 | `68fbe8e38f215d2448ce7610e8d57a29` |
| Greatsword +1 | 12 | 2,400 | `e4925df9c7ab2714793cd5834e0a3039` |
| Greatsword +2 | 12 | 8,300 | `8b0ad785727b2154ab4029508738fbdd` |
| Greatsword +3 | 12 | 18,500 | `b734631d6b981a844bd817c2961d5f29` |
| Bastard Sword +1 | 12 | 2,400 | `adbc18abae2534740a7f162ae33a86fc` |
| Bastard Sword +2 | 12 | 8,300 | `8b5d119f1f2009b4aaab7024bf53e483` |
| Bastard Sword +3 | 12 | 18,500 | `1cf115afab1777245b035ffe8012c281` |
| Dueling Sword +1 | 12 | 2,400 | `66106d59fd4615842af854cc9b7cbea4` |
| Dueling Sword +2 | 12 | 8,300 | `695057131a48b4f4884c00f690363f5e` |
| Dueling Sword +3 | 12 | 18,500 | `ea664cd1cf41a4044bbf3e6bcff18ebc` |
| Rapier +1 | 12 | 2,400 | `9d23d26b2fdbe484b89867667aabeebe` |
| Rapier +2 | 12 | 8,300 | `d713662146582ac48a38692aa6f7ec07` |
| Rapier +3 | 12 | 18,500 | `1a5f81e26e4c41c44a2a3b034369ef0b` |
| Scimitar +1 | 12 | 2,400 | `7bef1327d4cb31249823d9fcb24af332` |
| Scimitar +2 | 12 | 8,300 | `a231bbf909edfb548928e56091f71b56` |
| Scimitar +3 | 12 | 18,500 | `536277f87a94d4a4ca7a6f777866059f` |
| Shortsword +1 | 12 | 2,400 | `9f455505128866146a9bd81895d4cecd` |
| Shortsword +2 | 12 | 8,300 | `0af6a4ac898a2ca4c9b0a7e76ec7522b` |
| Shortsword +3 | 12 | 18,500 | `9ed29d74f5f9fc64786cc340806d420f` |
| Falchion +1 | 12 | 2,400 | `f93a7882fb01963428b091f259b15d3a` |
| Falchion +2 | 12 | 8,300 | `4884c93b5dcfcd341bbea50751f4cd20` |
| Falchion +3 | 12 | 18,500 | `a00f39e6fed1e544588782a73a5e9d99` |
| Dagger +1 | 12 | 2,400 | `2a45458f776442e43bba57de65f9b738` |
| Dagger +2 | 12 | 8,300 | `4607f83de16245844acb4596fc797147` |
| Dagger +3 | 12 | 18,500 | `4c08dc9c56fd3ea4da4c6825105f0498` |
| Kukri +1 | 12 | 2,400 | `cece2e26b026b244fba86a2075fc9811` |
| Kukri +2 | 12 | 8,300 | `ce7ef76c973ecfc47ba7515a743c8fd4` |
| Kukri +3 | 12 | 18,500 | `04284ae47696e4e4fa5fbdfe9e1f04cf` |
| Greataxe +1 | 12 | 2,400 | `4f0704c1d4d53cf419bd200bdfc12a0a` |
| Greataxe +2 | 12 | 8,300 | `b45b18552598a2d4c9cdc8296c490825` |
| Greataxe +3 | 12 | 18,500 | `61c1565f7a9eeb74799d27f526718f40` |
| Battleaxe +1 | 12 | 2,400 | `0ce4b3f4a3d70fa45851313a7f3d233f` |
| Battleaxe +2 | 12 | 8,300 | `21ed826b9c60c7f43af2a3c041046670` |
| Battleaxe +3 | 12 | 18,500 | `3d9e3fc1d4f4d8640bc4fac27a50fd5c` |
| Handaxe +1 | 12 | 2,400 | `2618c8c4e4be9bc48acdbcc7d92d6dc1` |
| Handaxe +2 | 12 | 8,300 | `a8c3b480482b24f4bb4f49bc47804ece` |
| Warhammer +1 | 12 | 2,400 | `1ffd6bb86a1aedd4687b4a89ffb478e8` |
| Warhammer +2 | 12 | 8,300 | `11ea0d6335a0fee4a87bfdc9201076c3` |
| Warhammer +3 | 12 | 18,500 | `57898b7567742fb458a5e57d112c19ec` |
| Heavy Mace +1 | 12 | 2,400 | `86d5d758c2dd24747bad1c7b1f32e9df` |
| Heavy Mace +2 | 12 | 8,300 | `3e728d0847794234fbedd98fe62cdec8` |
| Heavy Mace +3 | 12 | 18,500 | `ed5c9e684981d774eab7aeec8e21a418` |
| Light mace +1 | 12 | 2,400 | `5fe90cd1d77d8f446ac955dec7c8759f` |
| Light mace +2 | 12 | 8,300 | `6d228172531659e449fe189a4c579ea3` |
| Heavy Flail +1 | 12 | 2,400 | `93db492fa4e99c34eabacb1ca5c159dd` |
| Heavy Flail +2 | 12 | 8,300 | `b3d3daa4153f12e4db798ce76bdf554b` |
| Heavy Flail +3 | 12 | 18,500 | `e3c21d29a55842146af65892cd2927cd` |
| Glaive +1 | 12 | 2,400 | `9b64866b008a35d4ab1ba6f1a23bb7dc` |
| Glaive +2 | 12 | 8,300 | `1ccf54ab622d5294f9a2ba65b96d09f7` |
| Glaive +3 | 12 | 18,500 | `7e4224e752358094a923c0ab5f636b3b` |
| Longspear +1 | 12 | 2,400 | `9479af1ecd44ceb47a1e54b7268175c3` |
| Longspear +2 | 12 | 8,300 | `d512ed2b42265fa41bd3066eb11f6a90` |
| Quarterstaff +1 | 12 | 2,400 | `2c4a3077efc2378499c60b03d2b0527a` |
| Quarterstaff +2 | 12 | 8,300 | `a2aad194f9a067f408752bce24965dd6` |
| Quarterstaff +3 | 12 | 18,500 | `1c6e64ffdb3c8af47b2c8b88f802c728` |
| Kama +1 | 12 | 2,400 | `833d02226c289ce4892666e9d5880ae5` |
| Kama +2 | 12 | 8,300 | `a58ef6b142297af4482a5154765e777f` |
| Kama +3 | 12 | 18,500 | `726719d53e2c663408fb739c25b96786` |
| Nunchaku +1 | 12 | 2,400 | `373894b330b2d7a4485284e1909d6b1a` |
| Nunchaku +2 | 12 | 8,300 | `c40d09652bcc37846b09f650b51963e9` |
| Nunchaku +3 | 12 | 18,500 | `9b24946e18686e244b935ed66bf29f14` |
| Earth Breaker +1 | 12 | 2,400 | `3329eae7f8a5e3e4d80e3e15746d82bb` |
| Earth Breaker +2 | 12 | 8,300 | `9cb48261dd41b7948bf630297ca6a92c` |
| Elven Curve Blade +1 | 12 | 2,400 | `158fbe1c26025f3459c1af29a8faf2d8` |
| Elven Curve Blade +2 | 12 | 8,300 | `a81078f8f9148b94ca9f438171792ce9` |
| Elven Curve Blade +3 | 12 | 18,500 | `62ba202662960dd488330f24999435b6` |

## Exotic weapon families not covered above (WR-04-R4 audit of the installed game's exotic categories)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Fauchard +1 | 12 | 2,400 | `36cadeb70d35fc84a9b69db9fb7e64f5` |
| Fauchard +2 | 12 | 8,300 | `70a5422b73f42e441a1482c112e71afb` |
| Fauchard +3 | 12 | 18,500 | `a802f19be1140cf4683b85fab38928ac` |
| Sai +1 | 12 | 2,400 | `096350418ea1f614bb489191398ab64b` |
| Sai +2 | 12 | 8,300 | `97462573cb0d5c04283e0825d5182d6e` |
| Sai +3 | 12 | 18,500 | `ebb1ce40634dbd24392a9225147eaa7d` |
| Dwarven Waraxe +1 | 12 | 2,400 | `a06dd370e2d02cd449f82b209cbd61d6` |
| Dwarven Waraxe +2 | 12 | 8,300 | `8e6bada1ed926e94f851e9c50585d8b7` |
| Dwarven Waraxe +3 | 12 | 18,500 | `04798cc8a7a55494d8fb898fa1b52ab3` |
| Estoc +1 | 12 | 2,400 | `11c66ec41f62708498f751abd911db88` |
| Estoc +2 | 12 | 8,300 | `8c31edb13b069104a946599c31cf9079` |
| Estoc +3 | 12 | 18,500 | `3ae572d645784214794eb667dccb3a98` |
| Falcata +1 | 12 | 2,400 | `babd2990d6ff6734d8a755a36cf8ad40` |
| Falcata +2 | 12 | 8,300 | `9b90a5b27e1a8994bae5cbb7298e1c15` |
| Falcata +3 | 12 | 18,500 | `678a667e5223d54419ea5deeb86c82f0` |
| Tongi +1 | 12 | 2,400 | `9d3d4b12be558c548b65c9093ff6ec54` |
| Tongi +2 | 12 | 8,300 | `d75803253b68d5944b7cc40fdb6211f6` |
| Tongi +3 | 12 | 18,500 | `a65588031b28e124780c77af70577ffa` |
| Orc Double Axe +1 | 12 | 2,400 | `acf57e1471b5b1d40893c2f3186e49fa` |
| Orc Double Axe +2 | 12 | 8,300 | `aa5ec13aa89ab6346a272c9ab92afd64` |
| Orc Double Axe +3 | 12 | 18,500 | `aee6a20693a182a4fbd84b2746b13ef3` |
| Two-bladed Sword +1 | 12 | 2,400 | `f1db33a3f2b5dfd40a10fae7ea1fd76d` |
| Two-bladed Sword +2 | 12 | 8,300 | `c7f6f2e50a37d8c4baaa41035a918037` |
| Two-bladed Sword +3 | 12 | 18,500 | `e08a031aed7947b4ca43f1eef03d321f` |
| Dwarven Urgrosh +1 | 12 | 2,400 | `924b5edb3e09b2c4fbeb314b070b9eeb` |
| Dwarven Urgrosh +2 | 12 | 8,300 | `bad4fb2a5e668d44c80e541477f7457d` |
| Dwarven Urgrosh +3 | 12 | 18,500 | `2c5e6213acdcc3c489dc27dbbd301395` |
| Gnome Hooked Hammer +1 | 12 | 2,400 | `2ea8925adb856fe45990a4fd01ff0148` |
| Gnome Hooked Hammer +2 | 12 | 8,300 | `cbaa12b9fc981af40b8658b966114040` |
| Gnome Hooked Hammer +3 | 12 | 18,500 | `18449108bafff5d4f932cd3fb8621b9b` |
| Sling Staff +1 | 12 | 2,400 | `643b5b476b7fc1942aa24a50dedc8de4` |
| Sling Staff +2 | 12 | 8,300 | `1dc78587aa10b0849b863f57615ddce2` |
| Sling Staff +3 | 12 | 18,500 | `69245594968164347b63b2ddc3c24eef` |

## Cold iron weapons (WR-FINAL supplies audit: cheaper cold iron and magic for Kurshu's DR 10/cold iron and magic; +3 weapons, which the game treats as cold iron, were already stocked)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Cold Iron Longsword +1 | 12 | 4,500 | `203d3fbf74304ec4aba2ed1021742e59` |
| Cold Iron Earth Breaker +1 | 12 | 6,000 | `23855f41005569b43bcdca1c299471b4` |
| Cold Iron Dagger +1 | 12 | 3,000 | `ebea3139c4649e7459079d424c89e561` |
| Cold Iron Handaxe +1 | 12 | 5,000 | `76b3cfacefc66234ebe3f14669e9d208` |
| Cold Iron Dwarven Waraxe +3 | 12 | 20,500 | `4c30b1cd4041d214dbe028e293c7a5ff` |

## Material components (WR-FINAL supplies audit: stoneskin and communal stoneskin need diamond dust)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Diamond Dust | 999 | 50 | `92752bbbf04dfa1439af186f48aee0e9` |

## Ranged weapons

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Longbow +1 | 12 | 2,400 | `fd732e6688007e449964d8c5f2fc659d` |
| Longbow +2 | 12 | 8,300 | `335efd45aba89a245adf68a8da1fd3ca` |
| Longbow +3 | 12 | 18,500 | `426c900c0787c844c85bb4ac8cba6d27` |
| Shortbow +1 | 12 | 2,400 | `3e53a85140b50b24498e03e07a2df7e2` |
| Shortbow +2 | 12 | 8,300 | `2cbe261ab90d6cc4194cc1c8c85c3cbc` |
| Shortbow +3 | 12 | 18,500 | `3df5b2e2c59e5624eb564dbe9d754886` |
| Composite Longbow +1 | 12 | 2,500 | `2753a0159681be94da3a5eeabc8c8d1a` |
| Composite Longbow +4 | 12 | 32,000 | `862cf5b6dc694054bad497636834faa8` |
| Composite Shortbow +1 | 12 | 2,500 | `eec59da36fadf594aac6bd9add90652e` |
| Composite Shortbow +2 | 12 | 8,300 | `1e396f7871b73234fb41d72cd8c441f9` |
| Composite Shortbow +3 | 12 | 18,500 | `2f1673e87242d2941a1e49b850dbc9f2` |
| Light Crossbow +1 | 12 | 2,400 | `f5528acb89757ab4fb9350f45cc4f3b4` |
| Light Crossbow +2 | 12 | 8,300 | `1fb0ea666a65a004dac6327d0e6682bc` |
| Light Crossbow +3 | 12 | 18,500 | `8ee3176eb0ed29c418120b04dbb76aa9` |
| Heavy Crossbow +1 | 12 | 2,400 | `e8aa3f5ca26027b4b846de538b01ee50` |
| Heavy Crossbow +2 | 12 | 8,300 | `c98788ea762ecad44b73610c1e065bf8` |
| Heavy Crossbow +3 | 12 | 18,500 | `6967f62da3cfacb4eacb4f6227c4afd6` |
| Javelin +1 | 12 | 2,400 | `80756136484c44845ab19b2105c5702c` |
| Javelin +2 | 12 | 8,300 | `947df50f2c208c04388c7e165b8c8788` |
| Sling +1 | 12 | 2,400 | `32786a0a5807d5a448581a8b17864319` |
| Sling +2 | 12 | 8,300 | `c293e5abe38167b4e8b8cdaa1e95a054` |

## Armor

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Leather Armor +1 | 12 | 1,160 | `18c627302593ab142bb8219525e1aed1` |
| Leather Armor +2 | 12 | 4,160 | `f251cb7bbf176e340893b9e40f9a7c29` |
| Leather Armor +3 | 12 | 9,160 | `fb4e768611f820b4186a004cbd70ead6` |
| Hide Armor +1 | 12 | 1,165 | `45e86ae29df5f4b48a66da15fde62217` |
| Hide Armor +2 | 12 | 4,165 | `01117df73f22d5b4294136d2ae8efad4` |
| Chainshirt +1 | 12 | 1,250 | `91bf657f26eb80f4ba05b0b8440b1e8c` |
| Chainshirt +2 | 12 | 4,250 | `d80665c8497437a418b8f39e25daf4e2` |
| Chainshirt +3 | 12 | 9,250 | `19144cbc80e7cb84aa53692a7ddf5ba6` |
| Breastplate +1 | 12 | 1,350 | `5041415db3e6c394a8b2173c39fd4ec4` |
| Breastplate +2 | 12 | 4,350 | `c77060ede0a983f4a8c7c47032ecf1ea` |
| Breastplate +3 | 12 | 9,350 | `a7a2500acce8faf46b6ecb79ec05691e` |
| Chainmail +1 | 12 | 1,300 | `dd3834fe3f48182438b59fd99675fd6c` |
| Chainmail +2 | 12 | 4,300 | `be26685bdebb2a94693d2378e0235ef3` |
| Chainmail +3 | 12 | 9,300 | `22c16b38998ebf342acdccba4f55a3b6` |
| Half-Plate +1 | 12 | 1,750 | `65de3fcad4c01ac40bc8567f67901b5b` |
| Half-Plate +2 | 12 | 4,750 | `ce0076c7408f10741950cec394767b75` |
| Half-Plate +3 | 12 | 9,750 | `76e68c81dd261e64789d5f2bdc77302b` |
| Banded Mail +1 | 12 | 1,400 | `b277c8713472c3a4fb0f1e0ea6d6ed47` |
| Banded Mail +2 | 12 | 4,400 | `0c83f2926e8ac15439830be3106a6643` |
| Full Plate +1 | 12 | 2,650 | `ba780a9c1e5b0304892bd2bc0c22fe4d` |
| Full Plate +2 | 12 | 5,650 | `61787c56c01996f4c82866aca8fb0187` |
| Full Plate +3 | 12 | 10,650 | `1e0289b5e2a002d4592c0ac079d828d9` |
| Mithral Chainshirt +1 | 12 | 5,250 | `e832847483b219841b24e0a77acd7795` |
| Mithral Chainshirt +2 | 12 | 8,250 | `26b1c3857d4d46d4aa1b6afaa33a6f84` |
| Mithral Chainshirt +3 | 12 | 10,250 | `fb2664f7d8534dc40aeb23392dc58c0c` |
| Mithral Breastplate +1 | 12 | 6,350 | `9ad5e5e5ef3ea164482217696306c6fd` |
| Mithral Breastplate +2 | 12 | 9,350 | `72b5a62a1387ca84d9f0f1fb95dbe4fe` |
| Mithral Full Plate +1 | 12 | 12,650 | `888cd7959836acd4eae33e815e99102c` |
| Mithral Full Plate +2 | 12 | 15,650 | `529f8973ea1571244a48711135180e12` |

## Shields

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Buckler +1 | 12 | 1,155 | `5615afebd80c5de43b6def5fc4646e1c` |
| Buckler +2 | 12 | 4,155 | `cff02b5c7e391f24e88239536f88416d` |
| Buckler +3 | 12 | 9,155 | `9552d3843ff7b4140a8cf04b753848eb` |
| Light Shield +1 | 12 | 1,160 | `cac3ca0cae325b44b9d7833bbe0e6c69` |
| Light Shield +2 | 12 | 4,160 | `2a298726445c2184bbeab9f34ece55eb` |
| Light Shield +3 | 12 | 9,160 | `3b3f25ba61ba5f346b4d546e702732cb` |
| Heavy Shield +1 | 12 | 1,170 | `5c7b898a1bfb6cb4f8c14a0ebc143abe` |
| Heavy Shield +2 | 12 | 4,170 | `d41ea649a1054b647aafc5b2cb4f8543` |
| Heavy Shield +3 | 12 | 9,170 | `7e23918977b18524591204c93fae0a37` |
| Tower Shield +1 | 12 | 1,180 | `e7b9e76870927424eb9de56611206867` |
| Tower Shield +2 | 12 | 4,180 | `294e15227b2c1d245ab1ff99922f339f` |

## Caster and unarmored protection

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Bracers of Armor +1 | 12 | 1,000 | `9482c62934be44044918c3aac3730232` |
| Bracers of Armor +2 | 12 | 4,000 | `bf063663cdb3df34c8ffa05ff1d1c877` |
| Bracers of Armor +3 | 12 | 9,000 | `cc5f45518020a324b8c51d0a231984a3` |
| Bracers of Armor +4 | 12 | 16,000 | `0a8cda1aaee1178419b32722c89dae64` |
| Cloak of Resistance +1 | 12 | 1,000 | `4aea6773c1da01c42bd382c1b7c384bc` |
| Cloak of Resistance +2 | 12 | 4,000 | `04dff7841c5f499478c91487d9bbdcef` |
| Cloak of Resistance +3 | 12 | 9,000 | `9f3c56d5247154e47b5ca9500f4d86ce` |
| Cloak of Resistance +4 | 12 | 16,000 | `096125bfb3b364849b02ab65297f71de` |
| Ring of Protection +1 | 12 | 2,000 | `f333bf86cd122974792162cbcd27c9ed` |
| Ring of Protection +2 | 12 | 8,000 | `254755ca73eb52a4d8ab1a663106659b` |
| Ring of Protection +3 | 12 | 18,000 | `31315100c28e6a2418396fb152466fcd` |
| Amulet of Natural Armor +1 | 12 | 2,000 | `209960f8d60e23a4c8e75d14e2261263` |
| Amulet of Natural Armor +2 | 12 | 8,000 | `becc4c1d40a77814f98c0b175e92d17c` |
| Amulet of Natural Armor +3 | 12 | 18,000 | `081a2ffe763320a469de20f1e9b1cd71` |
| Amulet of Mighty Fists +1 | 12 | 4,000 | `a82d4f94b327d6743ad9c2b9c440061c` |
| Amulet of Mighty Fists +2 | 12 | 16,000 | `c640025d7096aba49bd19e7ffcac9400` |
| Bracers of Armor +5 | 12 | 25,000 | `33179b698a3264e45bc3495a2181a0ea` |
| Bracers of Armor +6 | 12 | 36,000 | `4811d69bde8fb0243b61c2b41300ebe7` |
| Cloak of Resistance +5 | 12 | 25,000 | `a34cd0f80d04ec647af741d924a3e2a3` |
| Ring of Protection +4 | 12 | 32,000 | `1c8d2d0358c664447bc9bff52220f027` |
| Ring of Protection +5 | 12 | 50,000 | `2d576daea5f62ae489028bce40469285` |
| Amulet of Natural Armor +4 | 12 | 32,000 | `4378ef58796eb9744b96538c2baf8c07` |
| Amulet of Natural Armor +5 | 12 | 50,000 | `11f435140501db84e8e787bf8792fac2` |
| Amulet of Mighty Fists +3 | 12 | 36,000 | `975aad0cf1a32534fb4c1359b7d952b2` |
| Bracers of Deflection | 12 | 13,000 | `1e08c0e0300bff442ab9f8152f231f1f` |

## Common enhancement equipment

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Belt of Giant Strength +2 | 12 | 4,000 | `c354de364a714ee4dbe2cfee628df267` |
| Belt of Giant Strength +4 | 12 | 16,000 | `ede812de6b31b194686595fab33fc3ed` |
| Belt of Incredible Dexterity +2 | 12 | 4,000 | `b6af4c1834999e74497b41588d1071cd` |
| Belt of Incredible Dexterity +4 | 12 | 16,000 | `20539ffabe4365e419ca7413129ab78a` |
| Belt of Mighty Constitution +2 | 12 | 4,000 | `b9cc5d85d4a5032458ef4491f5fed251` |
| Belt of Mighty Constitution +4 | 12 | 16,000 | `dbeb5ba83e9fd2046a31590636b8b36d` |
| Headband of Vast Intelligence +2 | 12 | 4,000 | `22184af355826444087506d7f2f8f0b1` |
| Headband of Vast Intelligence +4 | 12 | 16,000 | `ee15329357f8f974bacf02521d8d37fe` |
| Headband of Inspired Wisdom +2 | 12 | 4,000 | `3d33605336c62274483f382f58908f5b` |
| Headband of Inspired Wisdom +4 | 12 | 16,000 | `716bdfe5c57be9b43a958bb612c1ecd4` |
| Headband of Alluring Charisma +2 | 12 | 4,000 | `a3e8e907908ca7a40ac6da78e70bf33d` |
| Headband of Alluring Charisma +4 | 12 | 16,000 | `71b4c3f8976784d4abf46976b15eba4a` |

## Mixed physical belts below 20,000 gp (WR-PLAYTEST-01)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Belt of Physical Flow +2 | 12 | 10,000 | `6577d56f2da72874a8abea1a3cbd2fa0` |
| Belt of Physical Might +2 | 12 | 10,000 | `c95c552c19a5c5a4492462802104b071` |
| Belt of Physical Form +2 | 12 | 10,000 | `939ad0109fec02b46adcac4b85e1249a` |
| Belt of Physical Perfection +2 | 12 | 16,000 | `7abdce2cab3e3dd43b9e6c41c1b44525` |
| Headband of Mental Perfection +2 | 12 | 10,000 | `b9b49933be6ac7f4aa3c71c602227332` |

## Utility wondrous items

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Ring of Evasion | 12 | 25,000 | `ae3638b2f8016f84d87122ae60d26937` |
| Boots of Elvenkind | 12 | 2,500 | `1223ceb45ed647b44a04b44a9312328b` |
| Swiftfoot Boots | 12 | 3,500 | `8644bca1c3bf6f64ab4d89be643a8850` |
| Explorer's Belt | 12 | 2,500 | `ec34985b81b9f744488456e7009044ee` |
| Expert's Hat | 12 | 1,500 | `405abbf543c9cc148a6f091bb5e58d14` |
| Lesser Extend Metamagic Rod | 12 | 3,000 | `1cf04842d5dbd0f49946b1af1022cd1a` |
| Lesser Empower Metamagic Rod | 12 | 9,000 | `1e7a5a4d257cf434a87e687c9ee7a872` |
| Lesser Reach Metamagic Rod | 12 | 3,000 | `8b0261621069c9a41a70f1aaefa21c75` |

## Healing and condition removal

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Potion of Cure Light Wounds | 99 | 50 | `d52566ae8cbe8dc4dae977ef51c27d91` |
| Potion of Cure Moderate Wounds | 99 | 300 | `f991f3051c3b9e64fabc87891077b613` |
| Potion of Cure Serious Wounds | 99 | 750 | `e76d14096063ee041bdb1d13d8172599` |
| Potion of Restoration, Lesser | 99 | 300 | `1365aee7d18c85d438f5b027122ca8cf` |
| Potion of Remove Blindness | 99 | 750 | `bafd5734adc579641904284aa380dd48` |
| Potion of Remove Curse | 99 | 750 | `152944f618996bb4992030141bc96659` |

## Combat and defensive potions

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Potion of Heroism | 99 | 750 | `42f01c3ad12d4f84c93f89e165384780` |
| Potion of Displacement | 99 | 750 | `e21a5ab9509a943409bbd791e9685b03` |
| Potion of Blur | 99 | 300 | `5bb722fed5172ed48baa2139246bc355` |
| Potion of Invisibility | 99 | 300 | `5f9fe6030695f48489b59887536748e8` |
| Potion of Barkskin | 99 | 300 | `99ba9a93eb1c3cf42bfa641a59580f5b` |
| Potion of Bull's Strength | 99 | 300 | `2983c97b71ba60443b1c749854ef53a1` |
| Potion of Cat's Grace | 99 | 300 | `94b24d55f514cdd45a79d34904fea944` |
| Potion of Bear's Endurance | 99 | 300 | `a3879b4e94c50854a873a0334fbbe36c` |
| Potion of Fox's Cunning | 99 | 300 | `17f132fa2c8c213479640212ae444005` |
| Potion of Owl's Wisdom | 99 | 300 | `db548855a06daba4cb57f34f4780fd02` |
| Potion of Eagle's Splendor | 99 | 300 | `91d681b0bf6c39047a46f50ec98bf72d` |
| Potion of Enlarge Person | 99 | 50 | `fe7e87dc45fbcb845b7755f0733b5465` |
| Potion of Mage Armor | 99 | 50 | `ab4961408ba080449afd3e65aa0acbc1` |
| Potion of Shield of Faith | 99 | 50 | `bc93a78d71bef084fa155e529660ed0d` |
| Potion of Resist Fire | 99 | 300 | `6e37f3e99bfa1fb4a9ad8db1775aa7aa` |
| Potion of Resist Cold | 99 | 300 | `755b60529a25f1e4f8e44436d927fc6f` |
| Potion of Resist Acid | 99 | 300 | `2b269b69a86843546bdf17a9c3c431c3` |
| Potion of Resist Electricity | 99 | 300 | `5d4a59a63e6192c40a73d4b2bd722328` |
| Potion of Protection from Fire | 99 | 750 | `4cd7b3330e46f424e88602da4e358836` |
| Potion of Protection from Cold | 99 | 750 | `3a66c695016483a47b4f77ed0ff2e7fb` |
| Potion of Protection from Acid | 99 | 750 | `30d3dfb67a23cbf4a98044a8f1141b67` |
| Potion of Protection from Electricity | 99 | 750 | `4d85e4763c365454d8f03335197ba6c4` |

## Scrolls for healing and restoration

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Scroll of Breath of Life | 99 | 1,125 | `5318313176485d64ca13c1f046591326` |
| Scroll of Heal | 99 | 1,650 | `fb822a8d451d91b438514541a9a986de` |
| Scroll of Restoration | 99 | 700 | `27f9b643fe41dd34081509f0c65a3408` |
| Scroll of Remove Disease | 99 | 375 | `48ffa76066648a8408d4183e055346ce` |
| Scroll of Remove Blindness | 99 | 375 | `de172db6e10f6d54896cb6a48b9fe8f7` |
| Scroll of Remove Curse | 99 | 375 | `7475d23822bb7f748b54c1f3deea5080` |
| Scroll of Neutralize Poison | 99 | 375 | `62e633bef8ae9b241a5c084525075711` |
| Scroll of Remove Paralysis | 99 | 150 | `64b5a0f0d955c254d8dda9d19bb472ca` |
| Scroll of Remove Fear | 99 | 25 | `c87c32984fe3909419ee22cd7c93ce37` |
| Scroll of Raise Dead | 99 | 6,000 | `a43d2960b3f9bed49a02ae44efef4cfc` |
| Scroll of Stone to Flesh | 99 | 1,650 | `394ba24aed75f0d459ed84d1b1b98f60` |

## Scrolls for protection

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Scroll of Death Ward | 99 | 700 | `89e10c3f21fa50c4b8719e004c7628d3` |
| Scroll of Freedom of Movement | 99 | 700 | `f49fc4e47cef56e42a49d561289dd500` |
| Scroll of Dispel Magic (Point) | 99 | 375 | `4b2d0e65fb9775341b6c4f7c178f0fe5` |
| Scroll of Dispel Magic (Target) | 99 | 375 | `92a1bfd42ef1ec04794355579c699da0` |
| Scroll of Protection from Fire | 99 | 375 | `c2cadc1c19cc4b54eb73b987f540ddca` |
| Scroll of Protection from Cold | 99 | 375 | `2f55c17cbac95664cb1a39568df63d8b` |
| Scroll of Protection from Acid | 99 | 375 | `c7185bb3eb517804da246aa323d98fd8` |
| Scroll of Protection from Electricity | 99 | 375 | `3b064dd1174e9f0449fb7f3cc585caf1` |
| Scroll of Protection from Sonic | 99 | 375 | `15fd666cb2d6678438d96cbcc7f574d2` |
| Scroll of Mage Armor | 99 | 25 | `e8308a74821762e49bc3211358e81016` |

## Scrolls for combat

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Scroll of Haste | 99 | 375 | `6b3f04dd5eb9675499f815d5086fe018` |
| Scroll of Heroism | 99 | 375 | `50f9e398017b34c43aa08a2c2c44e8af` |
| Scroll of Heroism, Greater | 99 | 1,650 | `efb55589cd5b7dd4ca48d78d0d1d9248` |
| Scroll of Displacement | 99 | 375 | `4d9f5336b51cae34cb491cbde6aac4b6` |
| Scroll of Barkskin | 99 | 150 | `95b6d995d4d806a4da7d4ee5989c7188` |
| Scroll of Blur | 99 | 150 | `4e78560ea01fe2541b663c96e87b6057` |
| Scroll of Invisibility | 99 | 150 | `c0af0b5277e91e347ade3aa8994b0d17` |
| Scroll of Greater Invisibility | 99 | 700 | `d4bb60352b8a62845ba3a7b34ef29a0b` |
| Scroll of Fireball | 99 | 375 | `5b172c2c3e356eb43ba5a8f8008a8a5a` |
| Scroll of Lightning Bolt | 99 | 375 | `9d6d963e1a2d2474db1a23250abeb170` |
| Scroll of Cone of Cold | 99 | 1,125 | `5344f2240620b27478d12f00643fc292` |
| Scroll of Disintegrate | 99 | 1,650 | `bb2f172e429b40840a7dc25bc83732cb` |

## Wands

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Wand of Cure Light Wounds | 24 | 465 | `201dcf730b7185344ba701f91dd405db` |
| Wand of Cure Moderate Wounds | 24 | 4,500 | `e456cd580ff88e2449f9a8e4c204c4dc` |
| Wand of Cure Serious Wounds | 24 | 11,250 | `35fa690e41fb6b143b8c49cce316215d` |
| Wand of Restoration, Lesser | 24 | 1,980 | `002b777e1442b7a41a4ac8cc73aeff64` |
| Wand of Remove Disease | 24 | 2,025 | `a8c05388dec2bd64d9c413f3045fbaa7` |
| Wand of Remove Blindness | 24 | 2,250 | `93c55c75856218e47b74348e3d1791cd` |
| Wand of Neutralize Poison | 24 | 3,825 | `afb6a606b5d7f3a42a1a809ae8e269bb` |
| Wand of Mage Armor | 24 | 750 | `021b4a12739c59541922e3857f3fb3a4` |
| Wand of Bless | 24 | 135 | `8bd02291b14bd7b4b914d6327b01787f` |
| Wand of Blur | 24 | 4,500 | `e3c33b3af83b02c458b7ccaf822eb3b6` |
| Wand of Haste | 24 | 3,825 | `fb97ef0889c25344f91b0d326967743a` |
| Wand of Stoneskin | 24 | 7,140 | `22715f7c51efac54fa98b0b64686909b` |

## Thrown items and oils

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Acid Flask | 99 | 10 | `4639724c4a9cc9544a2f622b66931658` |
| Alchemist's Fire | 99 | 20 | `fd56596e273d1ff49a8c29cc9802ae6e` |
| Oil of Enhancement II | 99 | 750 | `218241b6d97158149be37b3128597b22` |

## Skill equipment (WR-PLAYTEST-01 audit by actual enchantment effects; Trickery is the game's Thievery skill)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Trapspringer's Gloves | 12 | 4,000 | `0dc9944c49e3ae343b8802a2da33973c` |
| Rogue's Companion | 12 | 10,100 | `d24d5bcf3b99c6948a85c83d8c711ed2` |
| Athlete's Gloves | 12 | 25,000 | `d5480772bdd29e545b627415186de20e` |
| Riversong Amulet | 12 | 22,000 | `c4b62855e9f9bfa40b710445cbd91141` |
| Cloak of Shadows | 12 | 6,000 | `42aa20c20917c41448fd6cb27de98d60` |
| Shadow Leather +1 | 12 | 1,160 | `ee429a40453dcac43b4d272bdf35a0fe` |
| Thaumaturgist's Cloak | 12 | 7,800 | `2595ae738523d9c4ea96f0d7e8b35470` |
| Robe of the Wise | 12 | 12,000 | `21918303993ba97408b858167f96f378` |
| Eyes of the Eagle | 12 | 2,500 | `303e3a1df232b0747b7fde723828fa45` |
| Ring of the Natural Born Commander | 12 | 9,000 | `9772ab64fdf645245ac8ee27b1e5ba69` |
| Golden Tongue | 12 | 29,500 | `a35a6b6cb58453f47b10a81cddb6b9c3` |
| Ring of Magic Devices | 12 | 16,000 | `66ab84747fa8c5f43851b4bd02ed3f85` |
| Master Hand | 12 | 25,000 | `b1b4dbff499ceb541a148d70ef17046e` |

## Channeling (positive and negative energy; effect verified in game, see docs/QUARTERMASTER.md)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Lesser Phylactery of Positive Channeling | 12 | 4,000 | `6b50bf738b4e6964b916539efc582a9d` |
| Phylactery of Positive Channeling | 12 | 12,000 | `01bc7670bd0225745a66957f21f1ba23` |
| Lesser Phylactery of Negative Channeling | 12 | 4,000 | `9751f7b073f740b458ab170cc81f5f1b` |
| Phylactery of Negative Channeling | 12 | 12,000 | `7ccc1833353d9a64cb671c7bc747bade` |

## Mobility and class support

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Boots of the Light Step | 12 | 7,800 | `815cc85ce13ab64428253aea3b6708a8` |
| Lesser Bracers of Archery | 12 | 4,000 | `01d51ff5f3db2164b88aaa662a9b0f2e` |
| Bracers of Archery | 12 | 10,000 | `0e7a0b96f67660c4ca74786c187a02d2` |

## Caster support: metamagic rods (lesser and normal; greater rods and the normal Quicken rod stay out)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Lesser Maximize Metamagic Rod | 12 | 14,000 | `651b0460f600d5f42b0467e7186aab80` |
| Lesser Quicken Metamagic Rod | 12 | 35,000 | `55a059b32df920c4abe65b8ee8b56056` |
| Extend Metamagic Rod | 12 | 11,000 | `1b2a09528da9e9948aa9026037bada90` |
| Empower Metamagic Rod | 12 | 32,500 | `a02f06b63af839a448147dadff3724f2` |
| Reach Metamagic Rod | 12 | 11,000 | `648f56fbbaa71624c8ba968ade382ac6` |
| Maximize Metamagic Rod | 12 | 54,000 | `9a511d3b04f08944eb3db4462f88c2c0` |

## More potions

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Potion of Aid | 99 | 300 | `3bcbf79b01e4db044a6f06d86cec9117` |
| Potion of Protection from Evil | 99 | 50 | `de000ebb9b86c8f48b77576965303183` |
| Potion of Feather Step | 99 | 50 | `a05bfdfe2df01204ab8e367c2c0b72ce` |

## Dueling gloves (WR-PLAYTEST-02: native items; Weapon Training rank +1 lesser, +2 normal)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Lesser Gloves of Dueling | 12 | 4,000 | `c33129949c59ee044900e741691a64d3` |
| Gloves of Dueling | 12 | 15,000 | `c8f949c03b92f714b83edd610f3e9348` |

## Adamantine armor (WR-PLAYTEST-02: every native armor carrying the adamantine material enchantment, DR 1/2/3 by weight class; Armag's duplicate breastplate excluded)

| Item | Stock | Price (gp) | Blueprint |
|---|---:|---:|---|
| Adamantine Chainshirt | 12 | 4,000 | `9dc76ea1dfc5d7d4b907288e2a6485c0` |
| Adamantine Chainshirt +1 | 12 | 6,250 | `31e5cfe1c81fba44291f0d78fc89b7a7` |
| Adamantine Chainshirt +2 | 12 | 9,250 | `33bd5baef9240cb40afdaba9d71a2daa` |
| Adamantine Chainshirt +3 | 12 | 14,000 | `2aa4f59d22182db4883e592596bf160f` |
| Adamantine Chainshirt +4 | 12 | 21,000 | `b4ecbf5393473e949a7b01ab52a17cdf` |
| Adamantine Chainshirt +5 | 12 | 30,000 | `3b5b1f3da5704f34eb33e84557e9319f` |
| Leather Armor | 12 | 7,000 | `cb390f2ae6df5724c9ec586842169ba4` |
| Leather Armor | 12 | 18,000 | `e1616329aa7c05845b538d0f28bcc7c8` |
| Leather Armor | 12 | 22,000 | `3885106d163f4ea4083887fdaa23aec6` |
| Leather Armor | 12 | 30,000 | `59fc699b1d7aff14f969919c7e6daca1` |
| Leather Armor | 12 | 50,000 | `99f57eda92fdc0d49be16ee2b4634d2d` |
| Leather Armor | 12 | 80,000 | `de03b0e4e279b304c95aee8a5439b7bf` |
| Padded Armor | 12 | 7,000 | `8e9417ea0c1aa884d9615a11dbd5c2b0` |
| Padded Armor | 12 | 18,000 | `dc75ee756a1d73740b5a1b0be4cc37f6` |
| Padded Armor | 12 | 22,000 | `2f64aed9afb8937499f520ced8f4987e` |
| Padded Armor | 12 | 30,000 | `7ca49db1cde980d489b6aa5112f8319f` |
| Padded Armor | 12 | 50,000 | `9c7327c0221f04a4482f3860a6302a48` |
| Padded Armor | 12 | 80,000 | `a057d77b4f780e6459229cabead9e7be` |
| Studded Leather | 12 | 7,000 | `ff4117b74c2f3d34f90a306f6fbcdf08` |
| Studded Leather | 12 | 18,000 | `3fab8986c64d16c4d811eefa972572cc` |
| Studded Leather | 12 | 22,000 | `d629b83aaba78c247a37423bb2da6fcf` |
| Studded Leather | 12 | 30,000 | `0aea4beb2c17f8a4caa6779074c5bb74` |
| Studded Leather | 12 | 50,000 | `d8bd56780dae4d946a2890ff6c86207a` |
| Studded Leather | 12 | 80,000 | `2eee42d5f094af649affbdabf0c653b7` |
| Hide Armor | 12 | 7,000 | `bea0adc2ebc043c4cbcd5f910b0516c2` |
| Hide Armor | 12 | 18,000 | `c7d722804ea31e14fad47f257ca1f271` |
| Hide Armor | 12 | 2,000 | `1e27aa1665f0ba34bbb8cfbc534bdf9d` |
| Hide Armor | 12 | 30,000 | `4a3ae924f673e6e42b4047306f718fdd` |
| Hide Armor | 12 | 50,000 | `bae8f2593ea154445a4cf5ab85ff65df` |
| Hide Armor | 12 | 80,000 | `5906be44cc0cee84b9a35f2df2e2868d` |
| Adamantine Scalemail | 12 | 5,000 | `406d54885c5bac643b480ec775bd5cd0` |
| Adamantine Scalemail +1 | 12 | 10,200 | `eca09c3357123ce4fa45e1b717df8b5d` |
| Adamantine Scalemail +2 | 12 | 13,200 | `d7107904c47d8994987c612fa462bfb1` |
| Adamantine Scalemail +3 | 12 | 18,000 | `c5646b825ace09744ab2f9927c614efc` |
| Adamantine Scalemail +4 | 12 | 25,000 | `fc1b904f361f742419f18634034c1945` |
| Adamantine Scalemail +5 | 12 | 80,000 | `749edb2a6e483be4d971d2698ddc5e78` |
| Adamantine Chainmail | 12 | 6,000 | `cdffad69c7081b24eae8e2bb8ee70394` |
| Adamantine Chainmail +1 | 12 | 11,300 | `12f0f9a858ab4c74c931fcd0cffaf264` |
| Adamantine Chainmail +2 | 12 | 14,300 | `5a61b70b4d413c64e882353d61edd31c` |
| Adamantine Chainmail +3 | 12 | 19,000 | `6495fe239fe8cdd4b9ff01c3c09e4756` |
| Adamantine Chainmail +4 | 12 | 26,000 | `00f40bc591e6a39498614f793a2e1503` |
| Adamantine Chainmail +5 | 12 | 35,000 | `c031c8a4c74020e41b6610be97d50797` |
| Adamantine Breastplate | 12 | 7,000 | `94386ea872981864b99a383ba133f0aa` |
| Adamantine Breastplate +1 | 12 | 12,350 | `d884f3cdce5ce064d81f11a729ea9890` |
| Adamantine Breastplate +2 | 12 | 15,350 | `5326c95949313484c80af1fae2673acc` |
| Adamantine Breastplate +3 | 12 | 22,000 | `8b4085ffcf2c1984fa7500f9da06214a` |
| Adamantine Breastplate +4 | 12 | 28,000 | `8865a1ccef15029439e041f020493d52` |
| Adamantine Breastplate +5 | 12 | 37,000 | `bafb44ecda0ac85468856cec6510810b` |
| Adamantine Banded Mail | 12 | 8,000 | `ed04d2a914a93e046aadee8cc3109f65` |
| Adamantine Banded Mail +1 | 12 | 15,400 | `de21042fc1a39864dbcec28763ad2f45` |
| Adamantine Banded Mail +2 | 12 | 18,000 | `1cee090860de51b40bc1170b4e87ad22` |
| Adamantine Banded Mail +3 | 12 | 23,000 | `07830c760d8028e47986d929e0e48cbc` |
| Adamantine Banded Mail +4 | 12 | 30,000 | `b3e6341e19b2e274695b54ebf0c0ed3a` |
| Adamantine Banded Mail +5 | 12 | 39,000 | `a28443ab2393b624186bd249df43bb62` |
| Adamantine Half-plate | 12 | 9,000 | `76eaf90831b52634582f488ef41c63e4` |
| Adamantine Half-plate +1 | 12 | 16,750 | `b94131f88a4f9934fae216f27ccc7aad` |
| Adamantine Half-plate +2 | 12 | 19,750 | `f77ba5fbf2f3f554ca459ddace21db31` |
| Adamantine Half-plate +3 | 12 | 25,000 | `0ba22b150266b604cb6b98443e09bfc0` |
| Adamantine Half-plate +4 | 12 | 30,000 | `c496ac4c996ab254baeee37ee3a7414c` |
| Adamantine Half-plate +5 | 12 | 40,000 | `cc64816a11d79e247a2f93cb4d975267` |
| Adamantine Full Plate | 12 | 12,000 | `4a72801171b0cca44b1989b4622b5820` |
| Adamantine Full Plate +1 | 12 | 18,650 | `6287c31b1ab7049469030c247868e7a2` |
| Adamantine Full Plate +2 | 12 | 21,650 | `b067c338ede1bfc45be97d2d9ab934e8` |
| Adamantine Full Plate +3 | 12 | 27,000 | `9404338472b5b8b4c9013cb8dcf74b7e` |
| Adamantine Full Plate +4 | 12 | 34,000 | `0dab0f3855529264aa53e748735cdd0a` |
| Adamantine Full Plate +5 | 12 | 43,000 | `000213dc917b821498a0d5f80138d3a9` |
| Ravena's Embrace | 12 | 50,000 | `615de12de63abcb42a606686354459cc` |
| Stainless Chainshirt | 12 | 11,000 | `dc9742ba2fdbeed46acd3682c9244f02` |
| Assassin's Chainshirt | 12 | 43,000 | `fd54ed3702d98a24283a400bd76c6b03` |
| Void Mirror | 12 | 23,100 | `21dedb7862f249248b7c959d8b4d615f` |

