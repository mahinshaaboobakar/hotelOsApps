#!/usr/bin/env python3
"""Every ADR and question number cited here resolves in the platform.

WHAT THIS IS, AND WHAT IT DELIBERATELY IS NOT
---------------------------------------------
**It is not a standards checker.**  ADR 0259 rules that there is ONE
authoritative standards check and that its scope explicitly names this
repository — *"do not solve this by merely adding a second script"* — and
since 2026-09-25 ``check_source_standards.py`` scans both repositories and
reports the root it scanned.  Ask it about the 300-line ceiling::

    python ../HosPilotOS/scripts/check_source_standards.py

**This file used to do that too.**  It was written on 2026-09-24, when nothing
could measure this repository at all, and it ran the platform's rules here by
importing them.  The planner's ruling made that the beginning of satisfying
0259 rather than the second script it forbids — and the end of satisfying it
is that the platform's checker does the scanning and this keeps only the half
that is genuinely a different question.

THE HALF THAT REMAINS
---------------------
**This repository cites the platform's decisions constantly and nothing
verified those citations resolve.**  102 distinct ADRs and 160 question
numbers, measured 2026-09-24 — in remarks, in commit-adjacent comments, in
chapters.  A citation outlives the memory of the ruling it points at, which is
exactly why it is written down; one pointing at nothing sends the next reader
looking for a document that was never there.

``check_documentation.py`` checks that ADR citations resolve, and
``check_rulings_recorded.py`` that every cited question has a register row.
Both resolve their roots from ``__file__``, so both walk the platform and
neither has ever read a line here — the same blindness ADR 0259 amended in the
third of the three, reported at the same time and not yet ruled on.

**Until it is, this asks the platform's own resolvers about this tree.**

NOTHING HERE REIMPLEMENTS A MATCHER, AND THE FIRST DRAFT PROVED WHY
-------------------------------------------------------------------
The question pattern, the range expansion and the parent-row rule are imported
from ``check_rulings_recorded``.  A hand-written matcher in the first draft of
this file reported four Workforce numbers as cited-but-unrecorded — ``WF-Q3``,
``WF-Q4``, ``WF-Q12``, ``WF-Q14``.  All four are covered by the register's
``WF-Q1–Q6`` and ``WF-Q11–Q15`` **range rows**, which ``recorded()`` expands
and a regular expression does not.

*Four false accusations against a colleague's shipped code, from a detector
written by somebody who had just read the real one.*  A detector is a
hypothesis about a fault's shape, and one that has never been shown failing is
a green with no provenance.

USAGE
-----
::

    python scripts/check_citations.py
    python scripts/check_citations.py --json

    exit 0   every citation resolves
    exit 1   a citation resolves to nothing
    exit 2   the platform repository was not found, so nothing was checked

**Exit 2 is UNVERIFIED and is not a pass.**  The authority for both questions
lives in the platform; without it no citation was checked, and an absent root
is not an empty one (ADR 0259).
"""

from __future__ import annotations

import importlib.util
import json
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

#: Where a citation can be written. Prose carries as many as code does.
SUFFIXES = (".cs", ".ts", ".tsx", ".mjs", ".js", ".py", ".rs", ".md", ".proto", ".yaml", ".yml")

#: `ADR 0259`, and nothing looser. A bare four-digit number is a room, a port,
#: a year and a line count before it is a decision.
ADR = re.compile(r"\bADR\s+(\d{4})\b")


def platform_root() -> Path | None:
    """The platform repository, or None — which is UNVERIFIED, not empty."""
    stated = os.environ.get("HOTELOS_PLATFORM")
    if stated:
        candidate = Path(stated).expanduser().resolve()
        return candidate if (candidate / "docs" / "decisions").is_dir() else None

    beside = ROOT.parent / "HosPilotOS"
    return beside if (beside / "docs" / "decisions").is_dir() else None


def load(platform: Path, name: str):
    """Import a platform script by path, without installing anything."""
    spec = importlib.util.spec_from_file_location(name, platform / "scripts" / f"{name}.py")
    if spec is None or spec.loader is None:
        raise ImportError(f"could not load {name} from {platform}")

    module = importlib.util.module_from_spec(spec)

    # Registered before it is executed: `@dataclass` resolves its class's
    # module through `sys.modules` while the class body is processed, and a
    # module absent from there fails three frames deep in `dataclasses` with
    # an `AttributeError` naming nothing that would send a reader here.
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


def source_files() -> list[Path]:
    """Every file here that could carry a citation, git's ignores respected.

    **Derived, never listed.** What this repository ignores is what it says it
    ignores; a hand-kept skip set is a hypothesis about what should be excluded
    and two of them disagreed by eight files before this was written.

    ``--directory`` is what makes it finish: without it git lists every ignored
    FILE, which on a tree carrying ``node_modules`` is tens of thousands.
    """
    listing = subprocess.run(
        ["git", "ls-files", "--others", "--ignored", "--exclude-standard", "--directory"],
        cwd=ROOT, capture_output=True, text=True, check=False)

    entries = [line.strip() for line in listing.stdout.splitlines() if line.strip()]
    ignored_files = {e for e in entries if not e.endswith("/")}
    ignored_dirs = tuple(e for e in entries if e.endswith("/"))

    found = []
    for path in sorted(ROOT.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in SUFFIXES:
            continue

        relative = path.relative_to(ROOT).as_posix()
        if relative.startswith(".git/"):
            continue
        if relative in ignored_files or relative.startswith(ignored_dirs):
            continue

        found.append(path)

    return found


def unresolved_questions(platform: Path, files: list[Path]) -> dict[str, list[str]]:
    """Question numbers cited here that the register never answers for."""
    rulings = load(platform, "check_rulings_recorded")
    known = rulings.recorded(
        (platform / rulings.REGISTER).read_text(encoding="utf-8", errors="replace"))

    cited: dict[str, set[str]] = {}
    for path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        for prefix, number, suffix in set(rulings.QUESTION.findall(text)):
            cited.setdefault(f"{prefix}-Q{number}{suffix}", set()).add(
                path.relative_to(ROOT).as_posix())

    return {
        question: sorted(where)
        for question, where in cited.items()
        if question not in known and not rulings.covering_parent(question, known)
    }


def unresolved_adrs(platform: Path, files: list[Path]) -> dict[str, list[str]]:
    """ADR numbers cited here with no such decision in the platform."""
    on_disk = {
        match.group(1)
        for decision in (platform / "docs" / "decisions").glob("*.md")
        if (match := re.match(r"^(\d{4})", decision.name))
    }

    cited: dict[str, set[str]] = {}
    for path in files:
        text = path.read_text(encoding="utf-8", errors="replace")
        for match in ADR.finditer(text):
            cited.setdefault(match.group(1), set()).add(path.relative_to(ROOT).as_posix())

    return {
        number: sorted(where)
        for number, where in cited.items()
        if number not in on_disk
    }


def main(argv: list[str]) -> int:
    platform = platform_root()

    if platform is None:
        print("UNVERIFIED — the platform repository was not found, so NO citation")
        print("was checked.")
        print()
        print("  Expected a sibling HosPilotOS, or HOTELOS_PLATFORM naming one.")
        print()
        print("This is not a pass and it is not a finding about any file here.")
        print("The ADRs and the question register are the authority for both")
        print("checks; without them nothing was resolved. An absent root is not")
        print("an empty one — ADR 0259.")
        return 2

    files = source_files()
    questions = unresolved_questions(platform, files)
    adrs = unresolved_adrs(platform, files)

    if "--json" in argv[1:]:
        print(json.dumps({
            "schema": 1,
            "platform": str(platform),
            "files_read": len(files),
            "adr_citations_unresolved": adrs,
            "questions_not_recorded": questions,
        }, indent=2))
        return 1 if (questions or adrs) else 0

    # **The denominator first.** A clean result is read for what it MEASURED
    # before it is read for what it FOUND.
    print(f"read {len(files)} file(s) in {ROOT.name}, resolving against {platform.name}")
    print()

    if adrs:
        print(f"ADR CITED, NO SUCH DECISION — {len(adrs)}")
        for number, where in sorted(adrs.items()):
            print(f"  ADR {number}")
            for path in where[:4]:
                print(f"      {path}")
        print()

    if questions:
        print(f"QUESTION CITED, NOT IN THE REGISTER — {len(questions)}")
        print("  A citation outlives the memory of the ruling. One the register")
        print("  cannot answer for is a decision nobody wrote down.")
        for question, where in sorted(questions.items()):
            print(f"  {question}")
            for path in where[:4]:
                print(f"      {path}")
        print()

    if not adrs and not questions:
        print(f"PASS — every ADR and question number cited in these {len(files)}")
        print("files resolves in the platform. This says nothing about the")
        print("300-line ceiling; check_source_standards.py measures that, and")
        print("since ADR 0259 its scope names this repository.")
        return 0

    print(f"FAILED — {len(adrs) + len(questions)} citation(s) resolving to nothing.")
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
