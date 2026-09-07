# Roll animated dice

As a game master resolving an action, I want to roll recognizable animated dice so that I can see a trustworthy result without losing the tactile pleasure of rolling at the table.

## Outcome

A rules-neutral dice tray rolls standard polyhedral dice, reveals each predetermined result through a brief two-dimensional animation, explains the final total, and retains recent rolls. The same roller accepts labelled requests from character sheets, NPCs, and other application features without making those features responsible for randomization or presentation.

## Acceptance criteria

- Given the dice tray, then the game master can select and roll one or more d4, d6, d8, d10, d12, d20, or d100 dice with a positive, negative, or zero modifier.
- Given a completed roll, then each die result, the dice subtotal, the modifier, and the final total are clearly displayed.
- Given any supported die, then it has a recognizable two-dimensional silhouette and a legible settled value; percentile results are represented unambiguously from 1 through 100.
- Given a roll begins, then its result is generated before a brief reveal animation and cannot be changed by interrupting, skipping, or disabling that animation.
- Given multiple dice, then their individual values remain available even when the tray must wrap, resize, or summarize their presentation.
- Given a d20 roll, then the game master can choose a normal roll, advantage, or disadvantage; both advantage dice remain visible and the selected higher or lower natural result is identified before applying modifiers.
- Given a natural 20 or natural 1, then restrained emphasis distinguishes it using more than colour and does not confuse the natural result with the modified total.
- Given a labelled roll request from another feature, then the tray uses the same generation, animation, result display, and history behavior while identifying the action and available character, NPC, or rules context.
- Given completed rolls, then recent history records their label, dice expression, individual results, modifier, total, advantage state, available source context, and time, and allows a roll to be repeated.
- Given rapid consecutive rolls, then their results and history entries remain distinct and the tray does not enter an invalid state.
- Given a reduced-motion preference, then the result appears immediately or with a minimal non-motion transition.
- Given keyboard or assistive-technology use, then dice selection, controls, state, discarded dice, and results are operable and meaningfully labelled without relying on animation or colour alone.

## Not included

Three-dimensional dice, simulated physics, elaborate animation, multiplayer synchronization, cryptographically verifiable rolls, user-created dice skins, permanent campaign-wide roll logs, advanced expression scripting, or automated encounter resolution.

