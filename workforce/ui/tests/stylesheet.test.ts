import { existsSync, readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

import { stylesheet } from "../chrome/styles";

/**
 * The stylesheet's own rules, derived from the source rather than listed.
 *
 * # Three defects, one shape
 *
 * Every screen's rules compose into **one** `<style>`, and that is what makes
 * these three possible at all:
 *
 * ```text
 * a backtick inside a CSS template literal   ends the string, mid-rule
 * two screens defining one class             the later one wins, silently
 * a class no rule defines                    the element draws unstyled
 * ```
 *
 * All three have happened here. The first twice in one evening; the second
 * caught by hand while writing `.tsw`, which would have turned every colour
 * swatch in the shift dialog into a toggle; the third shipped `.standin` and
 * `.dact` into a dialog nobody had rules for. **None of them fails a build**,
 * and the first is the only one `tsc` sees at all.
 *
 * # It enumerates, so a new screen is covered the day it is written
 *
 * The list of stylesheets is `readdir`, never a literal. A guard naming the
 * files it checks stops checking the one somebody adds next week, which is
 * exactly the file most likely to carry a new mistake.
 */

const UI = join(__dirname, "..");
const SCREENS = join(UI, "screens");

/** Every stylesheet in the module — the chrome's, and one per screen. */
function stylesheets(): readonly { name: string; source: string; css: string }[] {
  // A screen with no rules of its own has no file — Shifts draws entirely on
  // the chrome's. Absent is legitimate; what is not legitimate is a guard that
  // stops looking at the directory somebody adds next.
  const screens = readdirSync(SCREENS, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => join(SCREENS, entry.name, "styles.ts"))
    .filter((path) => existsSync(path));

  return [join(UI, "chrome", "styles.ts"), ...screens].map((path) => {
    const source = readFileSync(path, "utf8");

    return {
      name: path.slice(UI.length + 1).replace(/\\/gu, "/"),
      source,

      // **The CSS is what lies inside the assignment's own backticks**, and
      // both halves of that sentence are corrections.
      //
      // *Inside*: scanning from the opener left it glued to the first selector,
      // so the leading-compound read saw a backtick where the class was and no
      // file's FIRST rule was ever owned by anybody. Printed's `.sheet` is a
      // page of white paper; it sat unseen while the chrome grew a `.dlg.sheet`,
      // and the form sheet rendered white on white in the capture that found it.
      //
      // *The assignment's*: the first backtick in the file is often in the
      // file's own documentation — a ```text fence — so `indexOf` swallowed
      // TypeScript and read `document.createElement` as a class.
      css: source.slice(
        source.indexOf("`", source.search(/=\s*`/u)) + 1, source.lastIndexOf("`")),
    };
  });
}

/** Every class a rule in `css` mentions, anywhere in a selector. */
function defined(css: string): Set<string> {
  return classes(css, false);
}

/**
 * The classes rules in `css` are **scoped by** — the leftmost of each selector.
 *
 * This is what ownership means: `.tvoid .big` is Teams styling its own empty
 * state, not Teams claiming `.big`, and `.first .note` is People arranging the
 * chrome's note rather than redefining it. Counting every class in a selector
 * reported thirty-two collisions and every one of them was a rule reaching
 * inside its own container.
 */
function scopes(css: string): Set<string> {
  return classes(css, true);
}

function classes(source: string, leadingOnly: boolean): Set<string> {
  const found = new Set<string>();

  // **Comments first.** A rule's own explanation names the classes it is about
  // — "on, not pick: the Rota owns .pick" — and a scan that reads those as
  // selectors reports the collision the comment exists to record as fixed.
  const css = source.replace(/\/\*[\s\S]*?\*\//gu, " ");

  // Selectors only — the part before `{`. A `content:".x"` or a token
  // fallback inside a declaration is not a definition of anything.
  for (const block of css.split("}")) {
    const selector = block.slice(0, block.indexOf("{"));
    if (selector.includes("@") || selector.trim().length === 0) continue;

    for (const one of selector.split(",")) {
      const compound = leadingOnly ? one.trim().split(/[\s>+~]+/u)[0] ?? "" : one;
      for (const match of compound.matchAll(/\.([a-z][a-z0-9-]*)/giu)) {
        found.add(match[1]!);
        if (leadingOnly) break;
      }
    }
  }

  return found;
}

describe("the module's one stylesheet", () => {
  it("closes every CSS template literal", () => {
    // A backtick inside the literal ends it, and what follows is parsed as
    // code — which `tsc` reports as a syntax error somewhere else entirely.
    // Two backticks means opened once and closed once.
    for (const sheet of stylesheets()) {
      const body = sheet.source.slice(sheet.source.search(/=\s*`/u));
      expect(`${sheet.name}: ${body.split("`").length - 1} backticks`)
        .toBe(`${sheet.name}: 2 backticks`);
    }
  });

  it("keeps a class a screen styles on its own out of every other screen", () => {
    const leads = new Map<string, string>();
    const mentions = new Map<string, Set<string>>();

    for (const sheet of stylesheets()) {
      for (const name of scopes(sheet.css)) leads.set(name, sheet.name);
      for (const name of defined(sheet.css)) {
        mentions.set(name, (mentions.get(name) ?? new Set()).add(sheet.name));
      }
    }

    // **The dangerous shape is a class one file styles on its own and another
    // file borrows as a qualifier.** The Rota styles `.pick` — a 420px column
    // panel — and Teams wrote `.tmem.pick` for a chosen row: one stylesheet, so
    // the row inherited `flex-direction: column` and drew its avatar centred
    // above the name. Every check passed; a capture found it.
    //
    // A tone word is the safe shape and stays legal: nothing styles `.ok`
    // alone, so `.pill.ok` and `.code.ok` cannot reach each other. So does a
    // screen qualifying something the chrome styles — that is what the chrome
    // is for.
    const chrome = "chrome/styles.ts";
    const shared: string[] = [];

    for (const [name, owner] of leads) {
      if (owner === chrome) continue;

      const others = [...mentions.get(name) ?? []].filter((one) => one !== owner);
      if (others.length > 0) shared.push(`.${name}: ${owner} styles it, ${others.join(" and ")} uses it`);
    }

    expect(shared.sort()).toEqual([]);
  });

  it("reads a selector's owner as its leftmost class", () => {
    // The guard above is only as good as this: proved on selectors rather than
    // on the repository, because a scan of a clean tree passes whatever it
    // does. Every shape here is one the module actually writes.
    expect([...scopes(".tvoid .big{a:b}")]).toEqual(["tvoid"]);
    expect([...scopes(".first .note{a:b}")]).toEqual(["first"]);
    expect([...scopes(".btn.go{a:b}")]).toEqual(["btn"]);
    expect([...scopes("button.tgrid:hover{a:b}")]).toEqual(["tgrid"]);
    expect([...scopes(".a{x:y} .b .a{x:y}")].sort()).toEqual(["a", "b"]);

    // And it sees the collision it exists for: two files scoping `.sw`.
    const one = scopes(".sw{a:b}");
    const two = scopes(".sw.on{a:b}");
    expect([...one].filter((name) => two.has(name))).toEqual(["sw"]);
  });

  it("sees the first rule in every file", () => {
    // The hole this closes: the scan used to start at the opening backtick, so
    // the first selector read as ``\n.sheet`` and no file's first rule was ever
    // owned by anybody. Printed's is `.sheet`; assert it, from the file rather
    // than from a literal, so the check follows the file if it is reordered.
    for (const sheet of stylesheets()) {
      const first = /\.([a-z][a-z0-9-]*)/iu.exec(sheet.css)?.[1];
      if (first === undefined) continue;

      expect(`${sheet.name}: ${scopes(sheet.css).has(first)}`)
        .toBe(`${sheet.name}: true`);
    }
  });

  it("has a rule for every class the screens draw", () => {
    const css = stylesheet(stylesheets().map((sheet) => sheet.css)).textContent ?? "";

    const rules = defined(css);
    const orphans = new Set<string>();

    for (const [file, source] of sources()) {
      // `el("div", "note twarn")` and `el("button", `tmem${…}`)` — the second
      // argument is the class list, and only the literal parts of it can be
      // checked. An interpolated one is checked by the capture, which is the
      // honest division of labour.
      for (const match of source.matchAll(/\bel\(\s*"[a-z]+"\s*,\s*"([a-z0-9 -]+)"/giu)) {
        for (const name of match[1]!.split(" ").filter((one) => one.length > 0)) {
          if (!rules.has(name)) orphans.add(`.${name} — ${file}`);
        }
      }
    }

    expect([...orphans].sort()).toEqual([]);
  });
});

/** Every view file in the module, with its path. */
function sources(): readonly (readonly [string, string])[] {
  const found: (readonly [string, string])[] = [];

  const walk = (directory: string): void => {
    for (const entry of readdirSync(directory, { withFileTypes: true })) {
      const path = join(directory, entry.name);

      if (entry.isDirectory()) {
        walk(path);
      } else if (entry.name.endsWith(".ts") && entry.name !== "styles.ts") {
        found.push([path.slice(UI.length + 1).replace(/\\/gu, "/"),
          readFileSync(path, "utf8")]);
      }
    }
  };

  walk(SCREENS);
  walk(join(UI, "chrome"));

  return found;
}
