import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { minutes } from "../chrome/instant";
import type { Nav } from "../chrome/nav";
import { supervision } from "../screens/supervision";
import { roomsReady } from "../widgets/panel/panels";
import { host, recorded } from "./host";

/**
 * Every count a person reads goes through the SDK's `formatNumber` in the
 * property's locale — page 64 §12, `NUM-Q1`, ADR 0174. Grouped where the
 * property's locale groups; **ungrouped** where no locale is established, never
 * a country's grouping substituted by the screen.
 */

const GROUPING = { locale: "de-DE", timezone: "Asia/Kolkata" };
const UNESTABLISHED = { locale: null, timezone: null };

function at(property: HostApi["property"], overrides: Record<string, unknown>): HostApi {
  return { ...host(["roomcare.read"], overrides), property };
}

function nav(): Nav {
  return { frame: document.createElement("div"), show: () => {}, openRoom: () => {}, back: () => {} };
}

async function lane(property: HostApi["property"]): Promise<HTMLElement> {
  const answer = recorded<Record<string, unknown>>("supervision");
  const body = document.createElement("div");
  await supervision(at(property, {
    supervision: { ...answer, needDecision: 1234, decidedToday: 5678, paging: { page: 1000, pageSize: 12, total: 20000 } },
  }), body, nav(), 1000, () => {});
  return body;
}

async function widget(property: HostApi["property"]): Promise<HTMLElement> {
  return roomsReady(at(property, { widgetRoomsReady: { departures: 5000, ready: 1234, inProgress: 2000, dirty: 1766, at: "2026-09-05T09:12:00+05:30" } }));
}

describe("numbers a person reads", () => {
  it("groups a screen strip's counts as the property's locale groups", async () => {
    const body = await lane(GROUPING);
    expect([...body.querySelectorAll(".strip b")].map((b) => b.textContent)).toEqual(["1.234", "5.678"]);
  });

  it("leaves a screen strip's counts ungrouped where no locale is established", async () => {
    const body = await lane(UNESTABLISHED);
    expect([...body.querySelectorAll(".strip b")].map((b) => b.textContent)).toEqual(["1234", "5678"]);
  });

  it("groups the pager's range, total, page size and page labels", async () => {
    const pager = (await lane(GROUPING)).querySelector(".pager")!;
    expect(pager.firstElementChild?.textContent).toBe("showing 12.001–12.002 of 20.000 · 12 per page");
    expect([...pager.querySelectorAll("button.pg.on")].map((b) => b.textContent)).toEqual(["1.001"]);
  });

  it("leaves the pager ungrouped where no locale is established", async () => {
    const pager = (await lane(UNESTABLISHED)).querySelector(".pager")!;
    expect(pager.firstElementChild?.textContent).toBe("showing 12001–12002 of 20000 · 12 per page");
    expect([...pager.querySelectorAll("button.pg.on")].map((b) => b.textContent)).toEqual(["1001"]);
  });

  it("groups a widget's figures and its sentence", async () => {
    const card = await widget(GROUPING);
    expect([...card.querySelectorAll(".wfig b")].map((b) => b.textContent)).toEqual(["1.234", "2.000", "1.766"]);
    expect(card.textContent).toContain("5.000 departures");
  });

  it("leaves a widget's figures ungrouped where no locale is established", async () => {
    const card = await widget(UNESTABLISHED);
    expect([...card.querySelectorAll(".wfig b")].map((b) => b.textContent)).toEqual(["1234", "2000", "1766"]);
    expect(card.textContent).toContain("5000 departures");
  });

  it("keeps a shift's words and shape and gives its digits the property's form", () => {
    const grouped = at(GROUPING, {});
    const neutral = at(UNESTABLISHED, {});
    expect([minutes(grouped, 45), minutes(grouped, 65), minutes(grouped, 1234 * 60 + 5)]).toEqual(["45 min", "1 h 05", "1.234 h 05"]);
    expect(minutes(neutral, 1234 * 60 + 5)).toBe("1234 h 05");
  });
});
