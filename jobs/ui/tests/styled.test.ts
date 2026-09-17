import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative } from "node:path";

import { describe, expect, it } from "vitest";

import { MARKS_CSS } from "../chrome/marks";
import { stylesheet as moduleSheet } from "../chrome/styles";
import { stylesheet as widgetSheet } from "../widgets/card";

/**
 * Every class this module puts on a screen resolves to a rule — **in the realm
 * that draws it**.
 *
 * **FF's finding, 2026-09-17, and it is the reason this file exists.** GuestOps
 * emitted `.fail`, `.fh`, `.fb` and `.fw`, and no style file defined any of
 * them: *"every failure this application has ever shown a property was unstyled
 * text."* The owner's *"the ui is not good"* was literal — there was nothing to
 * object to, because nothing was drawn.
 *
 * **Jobs had the same defect wearing a better disguise.** The rules existed,
 * in `chrome/styles.ts`, which builds the *module* realm's sheet — and the six
 * widgets are a separate realm with a separate sheet that carried none of them,
 * while every one of them draws that surface on a read that does not arrive.
 * From outside, a rule in the wrong realm and no rule at all are the same
 * screen.
 *
 * **So this checks per realm, and a union check is the wrong instrument.**
 * Summing both sheets and asking whether each class appears somewhere is
 * precisely the question that returns green on the bug this file was written
 * for. The realms are:
 *
 * ```text
 * module   application.ts, chrome/, screens/, board/   CHROME + FAILURE_CSS + MARKS
 * widget   widgets/                                    WIDGET_CSS + FAILURE_CSS
 * ```
 *
 * **No sweep could have caught either.** A fidelity sweep compares a drawing
 * against a rendering, and a class that resolves to nothing is absent from
 * both: the drawing and the build agreed about a style that did not exist. The
 * defect lives in the gap between two files that never refer to each other, so
 * the guard has to be the thing that refers to both.
 */
describe("every emitted class is defined in the realm that emits it", () => {
  const ui = join(import.meta.dirname, "..");

  /** Every `.ts` under these paths. `preview/` and `tests/` ship to nobody. */
  function files(from: string): string[] {
    const at = join(ui, from);
    if (!statSync(at).isDirectory()) return at.endsWith(".ts") ? [at] : [];

    return readdirSync(at, { withFileTypes: true }).flatMap((entry) => {
      const here = join(from, entry.name);
      return entry.isDirectory() ? files(here) : here.endsWith(".ts") ? [join(ui, here)] : [];
    });
  }

  /**
   * The class names a source file hands to an element.
   *
   * Read from the calls that set one — `el(tag, "…")`, `control("…")`,
   * `setAttribute("class", "…")` — rather than from every string in the file,
   * because a sentence a person reads is not a selector and counting it as one
   * is how a guard starts reporting noise. A composed name (`gap gap-${cause}`)
   * is covered by its literal half and by the named check at the end.
   */
  function emitted(source: string): string[] {
    const calls = [
      /\bel\(\s*"[a-z]+"\s*,\s*"([^"${}]+)"/g,
      /\bcontrol\(\s*"([^"${}]+)"/g,
      /setAttribute\(\s*"class"\s*,\s*"([^"${}]+)"/g,
      /\bclassName\s*=\s*"([^"${}]+)"/g,
    ];

    return calls
      .flatMap((call) => [...source.matchAll(call)].flatMap((match) => (match[1] ?? "").split(/\s+/)))
      .filter((name) => name !== "");
  }

  /** Every class any selector in a sheet mentions. */
  function defined(...sheets: readonly string[]): Set<string> {
    return new Set(
      sheets.flatMap((sheet) =>
        [...sheet.matchAll(/\.([A-Za-z][\w-]*)/g)].flatMap((match) => match[1] ?? []),
      ),
    );
  }

  /**
   * Each realm's stylesheet, **built by the realm's own builder**.
   *
   * The first draft of this listed what each sheet ought to contain — the
   * chrome, the marks, the failure rules — and that is a test asserting its
   * author's belief. A widget sheet that stopped composing `FAILURE_CSS` would
   * have passed it, which is the exact regression this file exists to stop.
   * Calling `stylesheet()` measures what a realm actually assembles, so the
   * only way to make it green is for the rules to really be there.
   *
   * `MARKS_CSS` is passed to the module's builder because `application.ts`
   * passes it; the builder takes the screens' sheets as parts, and that is the
   * one thing a test has to mirror rather than read.
   */
  const REALMS = [
    {
      realm: "module",
      from: ["application.ts", "main.ts", "board", "chrome", "screens"],
      css: () => [moduleSheet([MARKS_CSS]).textContent ?? ""],
    },
    {
      realm: "widget",
      from: ["widgets"],
      css: () => [widgetSheet().textContent ?? ""],
    },
  ];

  it("walks something, so a silent zero cannot pass", () => {
    // The same shape that made five widget captures look taken when none were:
    // a guard whose population is empty passes every assertion in it.
    for (const { realm, from, css } of REALMS) {
      expect(from.flatMap(files).length, `${realm} realm has sources`).toBeGreaterThan(3);
      expect(defined(...css()).size, `${realm} realm has rules`).toBeGreaterThan(10);
    }
  });

  for (const { realm, from, css } of REALMS) {
    it(`the ${realm} realm defines every class it emits`, () => {
      const rules = defined(...css());
      const orphans: string[] = [];

      for (const file of from.flatMap(files)) {
        for (const name of emitted(readFileSync(file, "utf8"))) {
          if (!rules.has(name)) orphans.push(`${relative(ui, file)} → .${name}`);
        }
      }

      expect(orphans, `classes this realm draws that resolve to no rule`).toEqual([]);
    });
  }

  /**
   * Proved to fail before this green was reported: removing `FAILURE_CSS` from
   * `widgets/card.ts`'s builder fails the check below with *".gap is missing
   * from the widget realm"*, and the two realm walks stay green — because the
   * surface lives in `chrome/` and the widget realm's walk reads `widgets/`.
   * That is the walk's limit and the reason this named check is not redundant
   * with it: a walk can only see the classes in the files it lists.
   */
  it("both realms carry the failure surface's rules", () => {
    // Named rather than left to the walks above, because this is the surface the
    // finding was about, it is drawn only when something has already gone wrong,
    // and no ordinary run of the application reaches it. A widget realm that
    // stopped importing FAILURE_CSS would pass every other test in this package.
    for (const { realm, css } of REALMS) {
      const rules = defined(...css());
      for (const part of [
        "gap", "gap-head", "gap-mark", "gap-label", "gap-said", "gap-why",
        "gap-facts", "gap-fact-label", "gap-fact-value", "gap-note",
        "gap-unanswered", "gap-forbidden", "gap-faulted",
      ]) {
        expect(rules, `.${part} is missing from the ${realm} realm`).toContain(part);
      }
    }
  });
});
