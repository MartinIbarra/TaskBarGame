# Hero artwork

Conceptual character sprites generated for the six playable heroes.

| Stable ID | Character |
| --- | --- |
| `guardian` | Guardián / Guardian |
| `cleric` | Clériga / Cleric |
| `ranger` | Exploradora / Ranger |
| `rogue` | Pícara / Rogue |
| `pyromancer` | Piromante / Pyromancer |
| `spellblade` | Espada mágica / Spellblade |

The PNG files use RGBA transparency and are imported as uncompressed single
sprites with Point filtering, no mipmaps, a bottom-center pivot and 256 pixels
per unit. Each sprite is assigned to the `Artwork` field of its matching
`HeroDefinition`.

Run `Taskbar Tactics > Art > Import and Assign Hero Artwork` after replacing or
regenerating any PNG.

These are static concept/roster assets, not animation sheets. Production combat
animation should use dedicated lower-resolution sprite sheets derived from the
approved visual designs.
