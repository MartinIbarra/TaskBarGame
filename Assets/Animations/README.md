# Unit animation system

All heroes and enemies use the shared `Unit.controller`. The reusable prefab
keeps animated artwork separate from the world-space HUD:

```text
Unit
├── Visual Root        (Animator)
│   └── Artwork        (SpriteRenderer)
└── HUD
    ├── Health Background
    │   └── Health Fill
    └── Name Label
```

This prevents attack, hit and death motion from moving health bars or labels.

## Animator contract

| Parameter | Type | Purpose |
| --- | --- | --- |
| `Speed` | Float | Selects Idle or Running |
| `Attack` | Trigger | Plays one attack and returns to locomotion |
| `Hit` | Trigger | Plays one damage reaction and returns to locomotion |
| `IsDead` | Bool | Enters Death and does not exit |

`UnitAnimationBridge` is the only runtime component that writes these
parameters. Combat and health code call its focused methods instead of using
`Animator.Play`.

## Clips

- `Unit_Idle`: looping breathing/bobbing motion.
- `Unit_Running`: looping run cadence.
- `Unit_Attack`: anticipation, lunge and recovery.
- `Unit_Hit`: short recoil and squash.
- `Unit_Death`: collapse with no return transition.

The current clips animate the visual transform so the generated static hero
artwork can be used immediately. They are intentionally shared and
replaceable. Future frame-by-frame sprite sheets can replace the motions in
`Unit.controller` without changing combat simulation or gameplay code.

Regenerate the editable controller, clips, prefab and scene with:

`Taskbar Tactics > Build Editable Vertical Slice`
