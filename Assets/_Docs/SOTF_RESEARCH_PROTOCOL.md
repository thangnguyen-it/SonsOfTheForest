# Sons of the Forest Research Protocol

Targeted research is required before implementing each gameplay system. Search only the behavior in scope and preserve enough source context to reproduce the conclusion.

## Evidence order

Prefer current official evidence, then controlled current-build observation, dated gameplay evidence, historical structural evidence, and finally community inference. Record the URL or pinned revision, publication/build date when available, supported behavior, limitations, and implementation impact.

## Classification

- `VERIFIED`: directly established by suitable current evidence.
- `HISTORICAL`: true only of an identified earlier source/build and not assumed current.
- `INFERRED`: a reasoned conclusion from evidence that does not directly prove it.
- `UNKNOWN`: evidence is absent, conflicting, or insufficient.
- `PROVISIONAL`: a replaceable engineering baseline selected for an unknown so clean implementation can continue.

When sources conflict, document the conflict and follow the evidence order. Never silently promote a guess, field name, empty IL2CPP stub, trainer value, or historical dump into current behavior or a numeric default.

## From evidence to code

Convert supported observations into explicit behavior statements and black-box tests. Keep tuning in configuration and external conditions behind contracts. Test evidence-backed invariants, provisional seams, invalid data, and edge cases separately. Missing tuning does not block a clean architecture when the unknown remains configurable; a missing behavior-defining contract does block implementation and must be reported.

Each feature handoff must state what was recreated, what remains provisional, what is deferred, and what still differs from the reference experience.
