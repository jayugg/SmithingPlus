# TODO

- TODO [Minor] Fix edge case if players that try to remove tongs while setting the flip tool mode when they are required
- TODO Store overall transform for flip tool mode to properly reconstruct recipe outline rotation.
  Current method can have bugs and is harder to maintain.
- TODO [Minor] Auto transform for forgeable toolheads?
- TODO [Major] Config rework
    - TODO [Tweak] Config option to disable iron bloom modifications from helve hammers
- TODO [Major] Tool dismantling / tool head removal
- TODO [MAJOR] Rework tool detection system to not use wildcard or cache it / regen it
- TODO [Fix] Fix nugget recipes etc with better system
- TODO [Feature] Add better quenching and tempering tooltips

# v1.9.0-dev.2

- **Fix**: Fix handbook crash with null drop itemstack attribute in certain mold items
- **Tweak**: Remove tool buffs (sharpening, etc) on repair. Can be adjusted in config "ToolRepairForgettableAttributes".