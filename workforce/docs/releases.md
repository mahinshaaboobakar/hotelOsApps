# Workforce — packages cut

One row per signed `.hopkg`: what it carries, how it was built, and where it
went. Newest first. **A package's build provenance is part of the record** — a
binary a property runs is a statement about the tree it came from, so a build
from anything but a clean checkout of HEAD says so here.

| Version | Carries | Built from | Size · digest · key | Where it went |
|---|---|---|---|---|
| **0.3.3** | 0.3.2 + the widget's fault headline (`20fe389`); module contract **v1** | **shared working trees, not clean HEAD exports** — HotelOsApps at `f18ce11` (clean, checked); HosPilotOS at `e2e36563` for `@hotelos/sdk` (clean, checked) and **`HotelOS.Platform`, compiled from the working tree, not checked at the time**. Checked afterwards: the one later change that could have leaked in (`b1dfd1c0`, `AuthorizationOutcome` in `kernel.proto`) is **absent** from the shipped `HotelOS.Platform.dll`, with a string that must be there (`AuthorizeResponse`) found and a later DLL showing the search finds the name. **What that cannot prove: an uncommitted edit that was later discarded leaves nothing to find** | 17,179,574 bytes · `sha256:2b677522b7869df2c46b43f6bbd4a784b28f9d6458e3e839812a360fededa6d0` · `dev-local` | the property's registry (`%LOCALAPPDATA%\HotelOS\packages\registry`), 2026-09-19. Kept, not re-cut: architect, 2026-09-19 |
| 0.3.2 | structured failure facts, the SDK's wording and link line | shared working trees | 17,179,238 bytes · `dev-local` | `.build` only — superseded by 0.3.3 before install |
| 0.3.1 | the failure surface drawn to `64b`; the widget stylesheet | shared working trees | 17,176,998 bytes · `dev-local` | `.build` only |

**From the next version on** — the rule, 2026-09-19: built from **clean HEAD
exports of both repositories**, laid out side by side so `$(PlatformRoot)`
resolves to the export, `hopkg` from a clean worktree; both HEADs named here.
