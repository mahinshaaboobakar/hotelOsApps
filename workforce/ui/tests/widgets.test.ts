import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { attendanceToday } from "../widgets/panel/attendance-today";
import { comingUp } from "../widgets/panel/coming-up";
import { onLeave } from "../widgets/panel/on-leave";
import { pendingRequests } from "../widgets/panel/pending-requests";
import { shiftBoard } from "../widgets/panel/shift-board";
import { recordedShiftBoard } from "../roster/summaries";

/**
 * The five widgets' rules — the shape rules, held still.
 *
 * These assert what `SHELL-Q35` and `56-app-widgets.md` decided, not what the
 * cards look like: one frame for every widget, read-only, every element taps
 * through, and a number the domain cannot answer is absent rather than drawn.
 * Colour, spacing and whether the card is beautiful are the capture's job, and
 * neither substitutes for the other.
 *
 * A sixth widget added without a destination on its rows, or with a figure the
 * backend cannot compute, fails here rather than in a screenshot nobody took.
 */

/** A host that cannot answer — the shell today, with no Workforce client. */
function unavailable(): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: null },
    call: () =>
      Promise.reject(
        new HostCallError({ kind: "unavailable", message: "no Workforce client" }),
      ),
    on: () => () => {},
  };
}

/** A host that answers, so the live path is exercised too. */
function answering(answer: unknown): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: null },
    call: () => Promise.resolve(answer),
    on: () => () => {},
  };
}

/** Every widget, by the name its manifest entry carries. */
const WIDGETS = [
  { name: "Shift Board", panel: shiftBoard },
  { name: "Attendance Today", panel: attendanceToday },
  { name: "Pending Requests", panel: pendingRequests },
  { name: "Coming Up", panel: comingUp },
  { name: "On Leave", panel: onLeave },
] as const;

describe("every Workforce widget", () => {
  it("draws one card, with the application named on it", async () => {
    for (const widget of WIDGETS) {
      const card = await widget.panel(unavailable());

      expect(card.className).toBe("wcard");
      expect(card.querySelector(".wtitle")?.textContent).toBe(widget.name);
      // The header names the application, because a popover off a dock tile has
      // no other chrome that says whose it is.
      expect(card.querySelector(".wapp")?.textContent).toContain("Workforce");
    }
  });

  it("says so when the figures are not this property's", async () => {
    for (const widget of WIDGETS) {
      const fallen = await widget.panel(unavailable());

      // The one deliberate divergence from the artboards, and the reason is the
      // page's own: a widget that looks current when it is not is worse than
      // one showing nothing.
      expect(fallen.dataset["live"]).toBe("false");
      expect(fallen.querySelector(".wapp")?.textContent).toBe("Workforce · recorded");
    }
  });

  it("drops the marker when the platform answers", async () => {
    const live = await shiftBoard(answering(recordedShiftBoard));

    expect(live.dataset["live"]).toBe("true");
    expect(live.querySelector(".wapp")?.textContent).toBe("Workforce");
  });

  it("gives every row a screen to open, and makes every row operable", async () => {
    for (const widget of WIDGETS) {
      const card = await widget.panel(unavailable());
      const rows = Array.from(card.querySelectorAll(".wrow"));

      expect(rows.length).toBeGreaterThan(0);

      for (const row of rows) {
        // Tap-through is the answer's last property — 56: *not to the app's
        // home*. A row with nowhere to go is a row that wasted the tap.
        expect(row.getAttribute("data-opens")).toBeTruthy();

        // The artboards draw a div. A div is not focusable, not announced and
        // not operable from a keyboard.
        expect(row.tagName).toBe("BUTTON");
      }
    }
  });

  it("taps through with shell.open, naming no application", async () => {
    const asked: { capability: string; method: string; params: unknown }[] = [];
    const host: HostApi = {
      identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
      property: { timezone: "Asia/Kolkata", locale: null },
      call: (capability, method, params) => {
        asked.push({ capability, method, params });
        // The data call is answered so the card draws; only the tap is under
        // test here.
        return Promise.resolve(capability === "roster.read" ? recordedShiftBoard : null);
      },
      on: () => () => {},
    };

    const card = await shiftBoard(host);
    asked.length = 0;
    card.querySelector<HTMLElement>(".wrow")?.click();
    await new Promise((resolve) => setTimeout(resolve, 0));

    // The entry contract's one channel. A widget cannot navigate — no
    // `window.parent`, no network, no route to the shell's document.
    expect(asked).toEqual([
      {
        capability: "shell.open",
        method: "at",
        params: { destination: "rota?department=HK" },
      },
    ]);
  });

  it("says so when a tap is refused, rather than looking like it worked", async () => {
    const card = await shiftBoard(unavailable());
    card.querySelector<HTMLElement>(".wrow")?.click();
    await new Promise((resolve) => setTimeout(resolve, 0));

    // The one outcome worse than a tap that says why is a tap that silently
    // does nothing, on a design whose rule is that every element taps through.
    expect(card.querySelector(".wrefusal")).not.toBeNull();
  });

  it("takes no action, because v1 widgets are read-only", async () => {
    for (const widget of WIDGETS) {
      const card = await widget.panel(unavailable());

      // Every control on a widget is a row that opens a screen. An approve, a
      // check-in or an assign would be a write from a surface with no
      // confirmation and no undo.
      for (const control of Array.from(card.querySelectorAll("button"))) {
        expect(control.className).toContain("wrow");
      }
      expect(card.querySelectorAll("input, select, textarea")).toHaveLength(0);
    }
  });
});

describe("Coming Up", () => {
  it("draws overlapping leave and says what it cannot measure", async () => {
    const card = await comingUp(unavailable());
    const text = card.textContent ?? "";

    expect(text).toContain("Two or more away, same department");
    expect(text).toContain("Certifications expiring");

    // The ruled gap, stated on the widget's own face rather than left to a
    // release note: no staffing demand model exists, so *unfilled* and *thin*
    // have nothing to be measured against.
    expect(card.querySelector(".wnote")?.textContent).toBe(
      "Unfilled posts and thin shifts are not drawn — Workforce has no staffing demand model.",
    );
  });

  it("does not draw the two rows it cannot compute", async () => {
    const card = await comingUp(unavailable());

    // **The figures are the assertion**, not a text search. A first draft
    // searched the card's text for "unfilled" and passed for a reason that had
    // nothing to do with the rule: `textContent` concatenates without
    // separators, so the note's own "…Abraham6dUnfilled…" has no word boundary
    // in front of it and `\bunfilled\b` never matched. It would have passed
    // just as green with the row present.
    const labels = Array.from(card.querySelectorAll(".wlabel")).map((l) => l.textContent);
    expect(labels).toEqual(["overlapping leave", "certs expiring"]);

    // And nothing outside the note mentions them. The note is where the gap is
    // explained; anywhere else would be the gap drawn.
    const note = card.querySelector(".wnote");
    note?.remove();
    expect(card.textContent ?? "").not.toMatch(/unfilled|thin/i);
  });
});

describe("Shift Board", () => {
  it("counts the property and lists what fits", async () => {
    const card = await shiftBoard(unavailable());
    const figures = Array.from(card.querySelectorAll(".wfigure")).map((f) => f.textContent);

    // Six departments, four rows: the size guarantee working, not truncation.
    // The shell does not resize to content, so the widget cuts.
    expect(figures[1]).toContain("6");
    expect(card.querySelectorAll(".wrow")).toHaveLength(4);
  });

  it("omits the changeover entirely when there is not one", async () => {
    const card = await shiftBoard(answering({ ...recordedShiftBoard, nextChange: null }));

    // Uncomputable is absent. A dash would read as a figure the widget failed
    // to fetch rather than as one that does not exist.
    expect(card.querySelector(".wchange")).toBeNull();
    expect(card.textContent).not.toContain("Next change");
  });
});
