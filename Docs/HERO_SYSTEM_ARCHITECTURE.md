# Hero System Architecture

This document is the source of truth for the first hero-logic rewrite. It records the
design decisions agreed on August 15, 2026 and should be updated when hero stats,
equipment, status effects, or combat timing change.

## Design goals

- Every hero owns editable level-one stats and editable per-level growth.
- Runtime resources and progression are saved separately from authored data.
- Equipment, item bonuses, skills, buffs, and debuffs share one stat-modifier model.
- Basic attacks use a continuous independent timeline rather than global turns.
- Combat remains deterministic for seeded and offline simulation.
- The detailed item/loot redesign is intentionally deferred. This phase only defines
  compatibility and how equipped items affect hero stats.
- No equipment UI redesign is part of this phase.

## Hero identities

| Stable ID | Code enum | Visible name |
| --- | --- | --- |
| `warrior` | `Warrior` | Warrior |
| `cleric` | `Cleric` | Cleric |
| `mage` | `Mage` | Mage |
| `archer` | `Archer` | Archer |
| `rogue` | `Rogue` | Rogue |
| `magic_warrior` | `MagicWarrior` | Magic Warrior |

Legacy artwork keeps its original filenames and is mapped by the editor/content
builder. Legacy hero definition IDs are not valid save identifiers after save version 2.

## Authored data and saved state

`HeroDefinition` is the editable `ScriptableObject` for:

- class identity, names, artwork, tags, and skills;
- level-one `HeroStats`;
- `HeroStats` gained per level;
- allowed weapons, off-hands, and armor types.

`HeroState` contains only runtime/persistent data:

- stable definition ID;
- level and experience toward the next level;
- current HP and mana;
- formation and selected skills;
- equipped item instance per slot;
- status effects explicitly marked to persist between combats.

Effective stats are calculated when needed. They are not saved, which prevents stale
values after authored content or equipment changes.

## Stats

`HeroStats` contains:

- Max Health and Max Mana
- Attack Power and Spell Power
- Defense and Magic Resistance
- Attack Speed and Cast Speed
- Attack Range
- Critical Chance and Critical Damage
- Accuracy and Evasion
- Health Regeneration and Mana Regeneration
- Cooldown Reduction

Current HP, current mana, experience, and level belong to `HeroState`, not to base
stats. Dual-wield capability belongs to `HeroEquipmentProfile`.

### Modifier order

For every stat:

```text
levelValue = base + growthPerLevel * (level - 1)
flatValue = levelValue + sum(flat modifiers)
additiveValue = flatValue * (1 + sum(additive percent) / 100)
effectiveValue = additiveValue * every multiplicative-percent modifier
```

The final value is clamped after all operations. Equipment base modifiers, random item
bonuses, skills, buffs, and debuffs use `StatModifier` and therefore follow this order.

### Safety limits

| Stat | Limit |
| --- | ---: |
| Attack Speed | 0.1 to 5 attacks/second |
| Cast Speed | 0.1x to 4x |
| Cooldown Reduction | 0% to 75% |
| Evasion | 0% to 75% |
| Critical Chance | 0% to 100% |
| Critical Damage | 1x to 5x |
| Final hit chance | 5% to 100% |

Health, mana, powers, defenses, and regeneration cannot become negative. Max Health
has a minimum of 1 and Attack Range has a minimum of 1.

## Initial balance

### Level-one stats

| Hero | HP | Mana | Attack | Spell | Defense | Magic Res. | Attack Speed | Cast Speed | Range |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Warrior | 220 | 60 | 22 | 5 | 60 | 25 | 0.95 | 0.80 | 2 |
| Cleric | 160 | 120 | 15 | 22 | 45 | 45 | 0.80 | 1.05 | 3 |
| Mage | 110 | 160 | 8 | 32 | 15 | 55 | 0.70 | 1.25 | 5 |
| Archer | 125 | 80 | 26 | 8 | 25 | 20 | 1.05 | 0.95 | 5 |
| Rogue | 115 | 70 | 24 | 8 | 20 | 20 | 1.40 | 1.00 | 2 |
| Magic Warrior | 175 | 110 | 21 | 20 | 40 | 35 | 1.05 | 1.10 | 3 |

| Hero | Crit | Crit Damage | Accuracy | Evasion | HP Regen/s | Mana Regen/s | CDR |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Warrior | 8% | 1.75x | 95 | 5 | 1.20 | 0.60 | 0% |
| Cleric | 7% | 1.60x | 95 | 5 | 0.80 | 1.80 | 5% |
| Mage | 10% | 1.80x | 100 | 8 | 0.35 | 2.80 | 5% |
| Archer | 15% | 2.00x | 110 | 12 | 0.45 | 1.00 | 0% |
| Rogue | 20% | 2.00x | 105 | 20 | 0.40 | 1.00 | 0% |
| Magic Warrior | 10% | 1.75x | 100 | 8 | 0.70 | 1.50 | 3% |

### Per-level growth

| Hero | HP | Mana | Attack | Spell | Defense | Magic Res. | Attack Speed | Cast Speed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Warrior | 12 | 2 | 1.4 | 0.2 | 2.0 | 0.8 | 0.003 | 0.001 |
| Cleric | 8 | 6 | 0.6 | 1.3 | 1.2 | 1.5 | 0.002 | 0.004 |
| Mage | 5 | 8 | 0.2 | 1.7 | 0.5 | 1.8 | 0.001 | 0.006 |
| Archer | 6 | 3 | 1.5 | 0.3 | 0.7 | 0.6 | 0.004 | 0.002 |
| Rogue | 5 | 3 | 1.4 | 0.2 | 0.6 | 0.6 | 0.005 | 0.002 |
| Magic Warrior | 9 | 5 | 1.0 | 1.0 | 1.1 | 1.0 | 0.003 | 0.004 |

All growth fields remain editable in the Inspector. Unlisted secondary-stat growth is
initially zero rather than being absent from the data model.

## Progression and resources

- There is no gameplay level cap.
- Experience is stored as a 64-bit value.
- Experience required for the next level is `100 * current level`.
- A level-up raises current HP/mana by the increase in their maximum values; it does not
  fully heal the hero.
- Current HP and mana persist between expedition combats.
- Health/mana regeneration runs only while combat time advances.
- Returning to town, including after defeat or expedition completion, fully restores HP
  and mana and clears non-persistent status effects.

## Equipment slots

Every hero has these 16 slots:

1. Head
2. Shoulders
3. Neck
4. Chest
5. Bracers
6. Hands
7. Legs
8. Boots
9. Belt
10. Cloak
11. Main Weapon
12. Secondary Weapon
13. Ring 1
14. Ring 2
15. Earring 1
16. Earring 2

Armor type applies to Head, Shoulders, Chest, Bracers, Hands, Legs, and Boots. Belt,
Cloak, Neck, Rings, and Earrings are universal unless a future item adds a specific
class restriction.

Ring items can occupy either Ring slot, and Earring items can occupy either Earring
slot. Neck and Earrings remain distinct item families.

### Class compatibility

| Hero | Weapons | Secondary items | Armor | Can dual wield weapons |
| --- | --- | --- | --- | --- |
| Warrior | 1M/2M Sword, Mace, Axe | Shield or any allowed 1M weapon | Plate | Yes |
| Cleric | 1M Mace | Shield or Book | Mail, Plate | No |
| Mage | 2M Staff, 1M Wand | Book | Cloth | No |
| Archer | 2M Bow, Crossbow | None | Leather | No |
| Rogue | 1M Dagger | 1M Dagger | Leather | Yes |
| Magic Warrior | 1M/2M Sword | 1M Sword | Mail | Yes |

A 2M main weapon clears and blocks Secondary Weapon. Warrior may mix any compatible 1M
families, for example Sword/Axe or Mace/Sword.

An attacking weapon can only enter Secondary Weapon when an attacking 1M weapon is
already equipped in Main Weapon.

Shield and Book occupy Secondary Weapon but do not count as dual wield. `IsDualWielding`
is true only when Main Weapon and Secondary Weapon both contain compatible 1M attacking
weapons. The same item instance cannot be equipped twice or by two heroes.

## Item bonuses

The user-facing and code term for randomized item modifiers is **item bonus**.

- Common item: 0 random bonuses
- Rare item: 1 random bonus
- Epic item: 2 random bonuses

An item may also have permanent base stat modifiers. Both base modifiers and random
bonuses apply only while the item is equipped. Bonus generation restrictions and deeper
item behavior are intentionally left for the later item-system design.

## Buffs and debuffs

`StatusEffectDefinition` is an editable data asset. It supports:

- buff/debuff classification;
- duration in milliseconds;
- refresh, stack-and-refresh, or independent-instance behavior;
- maximum stacks;
- stat modifiers;
- periodic damage, healing, mana gain, or mana loss;
- prevention of basic attacks (`Stun`) and casting (`Stun`/`Silence`);
- dispel permission;
- optional persistence between expedition combats;
- tags for future skill and resistance rules.

Runtime effects store definition ID, source ID, stacks, time remaining, and time until
the next periodic event. Normal effects expire or are cleared after combat. Only effects
marked `PersistsBetweenCombats` are copied back to `HeroState`.

## Continuous combat

There are no global attack turns. Each combatant schedules its own next basic attack:

```text
attack interval milliseconds = 1000 / effective Attack Speed
```

The simulator uses integer milliseconds, a seeded random generator, and stable ID
ordering when actions share a timestamp. This preserves deterministic results and makes
offline simulation possible.

Dual-wield attacks alternate Main, Secondary, Main, Secondary on the hero's single
attack cadence. Shield and Book never enter this alternation. Weapon-specific attack
combos and procs are deliberately deferred.

Basic attacks currently deal physical damage:

```text
hit chance = clamp(Accuracy - target Evasion, 5, 100)
physical damage = raw damage * 100 / (100 + target Defense)
magical damage = raw damage * 100 / (100 + target Magic Resistance)
```

Cast Speed is a multiplier reserved for the later active-skill phase:

```text
actual cast time = skill base cast time / effective Cast Speed
```

Mana costs and cast execution are not activated in this phase.

## Save compatibility

`GameState.CurrentVersion` is 2. Version-1 saves are intentionally not migrated because
the hero IDs, equipment slots, resources, and bonus model all changed. Loading a v1 save
creates a fresh v2 state. The existing backup file behavior remains intact.

## Extension rules

- Add or tune hero values through `HeroDefinition`, not hard-coded presentation logic.
- Add new stat sources through `StatModifier` and the shared calculator.
- Do not save effective stats.
- Keep item-instance IDs unique across all hero loadouts.
- Keep buffs/debuffs data-driven; avoid skill-specific status classes unless behavior
  cannot be expressed by the shared definition.
- Preserve deterministic ordering and integer combat time for offline simulation.
- Update this document and the associated Edit Mode tests whenever a rule changes.
