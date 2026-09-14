import { HostCallError } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { arrivalsWaiting, attendantsNow, attention, pendingPolicy, roomsReady } from "../widgets/panel/panels";
import { host, recorded } from "./host";

const ALL = ["roomcare.read"];

describe("the five widgets", () => {
  it("each answers one question under its own heading", async () => {
    const h = host(ALL);
    const headings = await Promise.all([roomsReady(h), arrivalsWaiting(h), attention(h), attendantsNow(h), pendingPolicy(h)]);
    expect(headings.map((w) => w.querySelector(".whead")?.firstChild?.textContent)).toEqual(["Rooms Ready", "Arrivals Waiting", "Attention", "Attendants Now", "Pending"]);
  });

  it("counts today's departures by the board's own rule", async () => {
    const counts = recorded<{ departures: number; ready: number; inProgress: number; dirty: number }>("widget-rooms-ready");
    const card = await roomsReady(host(ALL));
    expect([...card.querySelectorAll(".wfig b")].map((b) => Number(b.textContent))).toEqual([counts.ready, counts.inProgress, counts.dirty]);
    expect(card.textContent).toContain(`${counts.departures} departures`);
  });

  it("says Workforce has not announced who is on shift, rather than inventing a count", async () => {
    const card = await attendantsNow(host(ALL, { widgetAttendants: { onShift: null, inARoom: 0, rows: [], at: "2026-09-05T03:42:00Z" } }));
    expect(card.textContent).toContain("on shift — not announced by Workforce yet");
  });

  it("draws a failure, never a figure, when its read failed", async () => {
    const card = await roomsReady(host(ALL, { widgetRoomsReady: new HostCallError({ kind: "unavailable", message: "down" }) }));
    expect(card.querySelector(".wfig")).toBeNull();
    expect(card.textContent).toContain("could not be read");
  });
});
