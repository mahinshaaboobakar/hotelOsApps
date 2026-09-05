/**
 * The tab bar draws what it is given — including the parts a spread can hide.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import { tabs } from "../chrome/panel";

describe("the tab bar", () => {
  it("marks a tab whose application is not installed", () => {
    const bar = tabs(
      [{ label: "Servicing", gone: true }, { label: "Payment" }],
      "Payment",
      () => undefined,
    );

    const [servicing, payment] = [...bar.querySelectorAll(".tab")];

    expect(servicing?.className).toContain("gone");
    expect(payment?.className).not.toContain("gone");
  });

  /**
   * The dimming is a *class*, not a removal.
   *
   * A tab that vanished when its application was absent would take the
   * information with it — which tabs a stay has is what tells a property that
   * servicing is something HotelOS can show them.
   */
  it("keeps a dimmed tab in the bar", () => {
    const bar = tabs([{ label: "Servicing", gone: true }], "Overview", () => undefined);

    expect(bar.querySelectorAll(".tab")).toHaveLength(1);
    expect(bar.textContent).toContain("Servicing");
  });

  it("keeps a dimmed tab reachable, so its own empty state can be read", () => {
    const chosen: string[] = [];
    const bar = tabs([{ label: "Servicing", gone: true }], "Overview", (to) => chosen.push(to));

    bar.querySelector<HTMLElement>(".tab")?.click();

    expect(chosen).toEqual(["Servicing"]);
  });
});
