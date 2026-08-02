# Structured recommendation explanations

## Purpose

Recommendation explanations connect a selected feasible skill to the facts
that caused its selection. The output is structured Domain data suitable for
an API or UI; it is not generated prose.

No model service, prompt, network call, or probabilistic text generation is
used. Presentation layers may translate the structured fields later without
changing their meaning.

## Per-skill links

Every selected skill has at least one `RecommendationReason` and exposes:

- matched target threats, including severity, activation timing, and evidence;
- verified counter strength and activation timing, when a mapping exists;
- current direction, required direction, any manual direction change, and the
  expected raw effect identity;
- base, mastery-adjusted, legendary-book-adjusted, and effective slot cost;
- the resulting category budget; and
- every activation requirement with criticality, evaluation status, reason,
  and evidence reference.

A compatibility-only skill is still explained. Its counter field explicitly
states that no verified counter mapping is attached rather than implying a
combat effect.

## Assumptions

Threat evidence with any of these confidence levels is copied into a typed
assumption:

- current-screen observation;
- player observation; or
- hypothesis.

Verified rules and snapshot facts are not labelled assumptions.

## Unavailable data

Unavailable facts are preserved as typed caveats with stable codes. Current
codes cover:

- missing damage evidence;
- missing structured threat details;
- unavailable skill name or direction;
- unavailable base, mastery, legendary-book, or effective cost; and
- requirements whose status cannot be evaluated.

An unavailable value is never replaced by zero, an average, a guessed skill
name, or an inferred game mechanic.

## Read-only boundary

The explanation builder consumes immutable snapshots, threats, and a manual
plan. It can only create values in memory. It cannot write a save, call
GameData, equip a skill, select an active role, change a direction, or control
the game.
