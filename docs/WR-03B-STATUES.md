# WR-03B statue manifest

The authoritative table is `src/KingmakerWakingRune/Sanctum/StatueManifest.cs`; this is its readable copy. Each figure is an installed unit model cloned under an inactive parent, posed by aiming its limbs, baked to a plain mesh, and given a stone material painted from the model's own texture. No collider, animator, unit or perpetual update survives.

Models with human-rig names take every limb of a pose. Other rigs take the limbs they have, over their first idle frame.

## A1: petrified humans bearing stone
| Slot | Placement | Model | Pose | Finish |
|---|---|---|---|---|
| Support-N1 | North face, x 10.7 | ManInLeathers | BearingHigh | Limestone |
| Support-N2 | North face, x 11.5 | Citizen | BearingBowed | Sandstone |
| Support-N3 | North face, x 14.7 | Merchant | BearingWide | Greywacke |
| Support-N4 | North face, x 15.5 | SickOldMan | BearingStrained | Limestone |
| Support-T1 | Terrace, x 11.9 | Noblewoman | BearingOneArm | Sandstone |
| Support-T2 | Terrace, x 14.1 | Cultist | BearingHunched | Greywacke |
| Support-W1 | West face, y 17.9 | Labourer | BearingWide | Sandstone |
| Support-E1 | East face, y 17.9 | Townswoman | BearingHigh | Greywacke |
| Support-W2 | West face, y 21.2 | Priest | BearingHunched | Limestone |
| Support-E2 | East face, y 21.2 | GrievingWoman | BearingStrained | Sandstone |

## A3: drained extraplanar beings
L and R stand on the floor either side of the plate; T crouches above it.

| Plate | Rune | L | R | T |
|---|---|---|---|---|
| A3a | Life | Bralani, AngerFists, Limestone | HornedMan, AgonyClutchingHead, Sandstone | Mephit, DespairCrouching, Greywacke |
| A3b | Alacrity | Erinyes, AngerAccusing, Greywacke | Axiomite, DespairHidingFace, Limestone | Mephit, AgonyArched, Basalt |
| A3c | Defense | Thanadaemon, AngerRoaring, Basalt | Ghaele, AgonyArched, Sandstone | Mephit, DespairSeated, Limestone |
| A3d | Life | MovanicDeva, DespairKneeling, Limestone | HornedWoman, AgonyClutchingChest, Greywacke | Mephit, AngerFists, Sandstone |
| A3e | Armor | ApostateDevil, AngerRoaring, Greywacke | Astradaemon, AgonyClutchingChest, Basalt | Ghaele, DespairCrouching, Limestone |
| A3f | Control | HornedElder, AngerAccusing, Sandstone | EarthElemental, AgonyArched, Greywacke | Axiomite, AgonyClutchingHead, Basalt |
| A3g | Life | Thanadaemon, DespairHidingFace, Sandstone | Erinyes, AgonyClutchingHead, Limestone | Bralani, DespairCrouching, Greywacke |

The installed game has no monadic deva, so A3d's bound deva uses its Movanic Deva model. The metal flail before it is inert scenery, not an item.
