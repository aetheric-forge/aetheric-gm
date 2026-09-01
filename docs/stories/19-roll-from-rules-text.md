# Roll dice from rules text

As a game master consulting a rule, I want to activate a labelled dice expression within its prose so that I can resolve the rule immediately without leaving the material I am reading.

## Outcome

Rules authors can place a narrow declarative roll token in supported prose using `[roll:expression|label]`, such as `[roll:1d20+3|Strength check]` or `[roll:2d6|Fire damage]`. When rules text is presented for play, a valid token becomes an accessible roll link rather than executable package content.

Activating the link generates a result through the shared dice roller and opens a compact modal containing the reusable animated result panel, total breakdown, repeat action, and a route to the full dice tray. Modal rolls enter the same session history as manually constructed rolls and retain their originating rules context.

## Acceptance criteria

- Given supported rules prose containing `[roll:1d20+3|Strength check]`, then the token is presented as an interactive link identifying both `Strength check` and `1d20 + 3`.
- Given a valid inline expression, then it accepts only the supported dice types, dice counts, numeric modifiers, dotted value paths, and `kh1` or `kl1` advantage forms understood by the shared dice roller.
- Given a modifier such as `(ability.wisdom)`, then the stored roll template retains that declarative path and resolves it only when a character or NPC context supplies an integer value.
- Given a value path in a rules-authoring preview without character or NPC context, then the roll link is rendered but disabled with an explanation rather than silently substituting zero.
- Given malformed syntax, an unsupported die, an out-of-range value, or an unsupported expression, then no action is executed and the rules-authoring surface reports a location-specific diagnostic.
- Given ordinary rules text, links, or markup that does not match the explicit roll-token grammar, then it remains ordinary content and is never interpreted as executable code.
- Given a rendered roll link, when the game master activates it, then the result is generated once through the shared dice roller before its animation begins.
- Given an inline roll result, then a modal displays the rule-provided label, normalized expression, individual dice, kept and discarded dice, modifier, total, and natural-result emphasis using the same presentation as the full tray.
- Given an inline roll modal, then the game master can repeat the same contextual request or open the full dice tray.
- Given the modal is dismissed or its animation is interrupted, then the generated result remains unchanged and available in recent history.
- Given the full dice tray is opened from the modal, then the latest result is selected and the complete session history includes the inline roll.
- Given a roll originating in a published rules record, then its history entry retains the exact ruleset ID, version, record type, record key, and human-readable rule label when available.
- Given multiple rules views in one session, then they share the operator's scoped dice history without persisting it as campaign-wide history.
- Given keyboard or assistive-technology use, then the inline link, modal focus, result announcement, repeat action, close action, and full-tray navigation are operable and meaningfully labelled.
- Given a reduced-motion preference, then the modal uses the same immediate or minimal-motion result presentation as the full tray.

## Authoring syntax

```text
[roll:1d20|Wisdom check]
[roll:1d20+4|Goblin attack]
[roll:2d20kh1+3|Attack with advantage]
[roll:1d8+2|Longsword damage]
[roll:1d20+(ability.wisdom)|Wisdom check]
```

The stored token is declarative data. Rules packages cannot supply scripts, event handlers, arbitrary URLs, or presentation code through it.

## Not included

Arbitrary mathematical expressions, mixed dice pools, macros beyond dotted integer value paths, hidden rolls, automated rule resolution, package-supplied modal layouts, persistent campaign-wide roll logs, or multiplayer synchronization.
