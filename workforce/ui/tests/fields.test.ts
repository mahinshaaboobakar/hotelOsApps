import { describe, expect, it } from "vitest";

import { stylesheet } from "../chrome/styles";

/**
 * The field box and its label — page 64 §10, as the app surface audit
 * (2026-09-19, F2 · D2) measured them against it.
 *
 * §10: *"`.fld label` 11px · uppercase · .07em · ink-faint"* and *"`.inp` 9px
 * 12px · radius 10 · line-strong border · ink at 2%"*. The build measured .04em,
 * `7px 11px`, radius 8, on `--color-surface`. The owner ruled the input box to
 * the written standard over the drawing (`APPS-Q27`).
 *
 * Read from the stylesheet the module actually injects, with comments removed,
 * so a value that survives only inside a comment does not pass.
 */

function rule(selector: string): Map<string, string> {
  const css = (stylesheet().textContent ?? "").replace(/\/\*[\s\S]*?\*\//g, "");
  const at = css.indexOf(`\n${selector}{`);
  if (at < 0) throw new Error(`no ${selector} rule in the stylesheet`);

  const body = css.slice(css.indexOf("{", at) + 1, css.indexOf("}", at));
  const declarations = new Map<string, string>();
  for (const one of body.split(";")) {
    const colon = one.indexOf(":");
    if (colon > 0) declarations.set(one.slice(0, colon).trim(), one.slice(colon + 1).trim());
  }
  return declarations;
}

describe("§10's field", () => {
  it("labels a field at 11px, uppercase, .07em, ink-faint", () => {
    const label = rule(".fld-label");
    expect(label.get("font-size")).toBe("11px");
    expect(label.get("text-transform")).toBe("uppercase");
    expect(label.get("letter-spacing")).toBe(".07em");
    expect(label.get("color")).toMatch(/^var\(--color-ink-faint[,)]/);
  });

  it("draws the box at 9px 12px, radius 10, a line-strong border, ink at 2%", () => {
    const box = rule(".inp");
    expect(box.get("padding")).toBe("9px 12px");
    expect(box.get("border-radius")).toBe("10px");
    expect(box.get("border")).toMatch(/^1px solid var\(--color-line-strong[,)]/);
    expect(box.get("background")).toMatch(
      /^color-mix\(in srgb, var\(--color-ink[,)][^%]*\b2%, transparent\)$/);
  });
});
