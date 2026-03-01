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

# v1.8.4

Last version pre-1.22 update, from here only critical bug fixes for 1.21

- **Fix**: Hopefully fix chisel crash once and for all (PR by `TheFifthRider`)
- **Localisation**: Add Belarusian Localisation (thanks to `k1llo`)
- **Compatibility**: Toolmold units are now server-authoritative, should fix XSkills incompatibility
- **Fix**: Fix occasional crash when sending hammer tool mode (cannot find channel for some reason)