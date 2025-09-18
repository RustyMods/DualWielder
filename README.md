# DualWielder

- Inspired by: [Smoothbrain's Dual Wield](https://thunderstore.io/c/valheim/p/Smoothbrain/DualWield/)
- Extracted from: [RustyMods' Almanac Class System](https://thunderstore.io/c/valheim/p/RustyMods/AlmanacClassSystem/)

**DualWielder** is a custom dual wield system that allows players to equip and use two different one-handed weapons simultaneously (e.g., Sword + Axe).

---

### Features

- Custom attach point for the **left-hand weapon** when holstered (only if dual wielding).
- Option to **merge left-hand damage into right-hand attacks** (`configurable`).
- Option to apply a **damage modifier** to the total damage after combining (`configurable`).
- New **Dual Wield skill** (`DualWielder`), which increases damage when dual wielding.

---

### Notes

- If **left-hand damage** is disabled (`Off`), then:
    - The **damage modifier** will not be applied.
    - The **Dual Wield skill** will not contribute to damage.
- These configs let you balance dual wielding by tuning how much extra damage is gained.

---

### Damage Calculation

1. Left-hand weapon damage is **added** to the right-hand weapon damage.
2. The total is then scaled by the **damage modifier**.
3. Finally, the **Dual Wield skill** is applied as a multiplier.

Formula:

- `Modifier` = damage scaling factor (0.0 – 1.0).
- `SkillFactor` = Dual Wield skill percentage (0.0 – 1.0).

---

### Example

- **Combine Weapon Damages**: `On`
- **Damage Modifier**: `0.5` (50%)
- **Dual Wield Skill**: 50 (≈ 50% = 0.5 SkillFactor)

Calculation:

1. Base damage = `Right + Left`
2. Apply modifier = `(Right + Left) * 0.5`
3. Apply skill = `(Right + Left) * 0.5 * (1 + 0.5 * (1 - 0.5))`
    - = `(Right + Left) * 0.5 * (1 + 0.25)`
    - = `(Right + Left) * 0.5 * 1.25`
    - = `(Right + Left) * 0.625`

**Result:** At skill 50, you deal **62.5% of the combined damage**.
