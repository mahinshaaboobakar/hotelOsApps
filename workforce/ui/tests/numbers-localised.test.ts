import { formatNumber } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { ANSWERS, readable, SURFACES, surfaceHost } from "./surfaces";

/**
 * Every number a person reads is in the property's locale — U1, `NUM-Q1`,
 * ADR 0174: *"Every user-facing number goes through `@hotelos/sdk`'s
 * `formatNumber`, in the property's locale."*
 *
 * # Checked by running the screens, not by reading for three names
 *
 * The audit of 2026-09-19 passed U1 by searching for `toLocaleString`,
 * `Intl.NumberFormat` and `toFixed` — the approved names — and found none. HH's
 * Jobs audit showed that is the wrong population: a number reaches a screen as
 * `String(n)` or `${n}`, and neither mentions a locale. A search for the call
 * shape found ~46 here.
 *
 * So every surface is rendered under **`ar-EG`, whose digits are not ASCII**,
 * and the text must carry no ASCII digit. A number that went through
 * `formatNumber` (or a date through `formatDay`) comes out in the property's
 * digits; one that went through `String` comes out as `3`, and is caught.
 *
 * **Digits inside words the fixture itself carries are exempt**, and only those:
 * a balance sentence the service composed is data, not a number this screen
 * formatted. They are derived by walking the fixture, not listed — and a string
 * that is ONLY digits is not words, so it is not exempt.
 */

const host = surfaceHost("ar-EG");

/** Every string in the fixtures that carries an ASCII digit — data, exempt. */
function carried(value: unknown, into: Set<string> = new Set()): Set<string> {
  if (typeof value === "string") {
    // Words with a digit in them — `3 valid`, `4d`. A string that is only a
    // number is a number, and exempting it removed every matching digit from
    // the page: the first run passed twelve surfaces on bare "1"…"9".
    if (/[0-9]/.test(value) && /\p{L}/u.test(value)) into.add(value);
  } else if (Array.isArray(value)) {
    for (const one of value) carried(one, into);
  } else if (value !== null && typeof value === "object") {
    for (const one of Object.values(value)) carried(one, into);
  }
  return into;
}

const EXEMPT = [...carried(ANSWERS)].sort((a, b) => b.length - a.length);

/**
 * What the SERVICE formats, which no screen can fix — owed, named, and held.
 *
 * Each is a value the wire carries already written out, so this realm receives
 * a string and never a number. The repair is the wire's (a number, or an ISO
 * value, formatted here), and until it lands the element that renders it is
 * set aside **by identity** — the element, never a pattern over the text.
 *
 * Each entry also asserts its element still shows ASCII digits: **the day the
 * wire is fixed, the exemption fails and has to be removed**, rather than
 * standing on as a hole nobody remembers opening.
 *
 * A register id (`WF-Q18`) used to be stripped here as "not a quantity". It is
 * now its own check — tests/register-ids.test.ts — because it was never a
 * number to exempt: it was copy that should not have been on the screen.
 */
const FIGURE = { selector: ".wvalue", why: "Figure.value is a string the service formatted" };
const ROW_VALUE = { selector: ".wfig", why: "SummaryRow.value is a string the service formatted" };

const OWED: Record<string, readonly { selector: string; why: string }[]> = {
  schedule: [
    { selector: ".mpush", why: "Schedule.balance is a composed sentence (\"4 of 8 casual remaining\")" },
    { selector: ".cduty.tail", why: "ScheduleDay.tail is a composed clock (\"…08:00\")" },
  ],
  "attendance today": [FIGURE, ROW_VALUE,
    { selector: ".wmeta", why: "SummaryRow.meta carries a composed clock (\"07:00\")" }],
  "coming up": [FIGURE],
  "on leave": [FIGURE, ROW_VALUE],
  "pending requests": [FIGURE],
  // Its figures are counts the panel formats itself; only the rows are owed.
  "shift board": [ROW_VALUE],
};

/** The text a person reads, less what is owed and what the fixture wrote. */
function read(root: HTMLElement, owed: readonly { selector: string }[] = []): string {
  const copy = readable(root);
  for (const { selector } of owed) {
    for (const one of Array.from(copy.querySelectorAll(selector))) one.remove();
  }
  let text = copy.textContent ?? "";
  for (const data of EXEMPT) text = text.split(data).join(" ");
  return text;
}

/** Each ASCII digit run left, with a little of what surrounds it. */
function ascii(text: string): string[] {
  return Array.from(text.matchAll(/[0-9][0-9.,]*/g), (hit) =>
    text.slice(Math.max(0, hit.index - 18), hit.index + hit[0].length + 12)
      .replace(/\s+/g, " ").trim());
}

describe("numbers in the property's locale", () => {
  it("has a locale whose digits are not ASCII — or this proves nothing", () => {
    expect(formatNumber(3, host.property, "whole")).not.toBe("3");
  });

  for (const [name, draw] of SURFACES) {
    it(`${name} draws no number in ASCII digits`, async () => {
      const main = document.createElement("div");
      await draw(host, main);

      // A surface that drew its failure state has no numbers to check, and
      // would pass for that reason alone.
      expect(main.querySelector(".fail"), `${name} could not read its fixture`).toBeNull();

      const owed = OWED[name] ?? [];
      for (const { selector, why } of owed) {
        const shown = Array.from(main.querySelectorAll(selector), (one) => one.textContent ?? "");
        expect(shown.some((text) => /[0-9]/.test(text)),
          `${name}: ${selector} no longer shows ASCII digits — the wire was fixed (${why}); `
          + "remove this entry from OWED").toBe(true);
      }

      expect(ascii(read(main, owed))).toEqual([]);
    });
  }
});
