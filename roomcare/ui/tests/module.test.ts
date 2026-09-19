import { HostCallError } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate, sectionsFor } from "../application";
import { ATTENDANT, SUPERVISOR, click, host, mount, recorded, settle, type Call } from "./host";

describe("the Room Care module", () => {
  it("draws the six-tab bar a supervisor holds, and one section for an attendant", () => {
    expect(sectionsFor(host(SUPERVISOR))).toEqual(["Board", "Prepare", "Room states", "Supervision", "Deep clean", "Setup"]);
    expect(sectionsFor(host(ATTENDANT))).toEqual(["My rooms"]);
    expect(sectionsFor(host(["roomcare.read", "roomcare.assign"]))).toEqual(["Board", "Prepare"]);
  });

  it("names who is signed in as name · department · property", async () => {
    const root = mount(activate, host(SUPERVISOR));
    await settle();
    expect(root.querySelector(".who")?.textContent).toBe("Meera Krishnan · Housekeeping · Coral Cove Resort");
  });

  it("opens the board as the property's default view, every zone grouped, no pager", async () => {
    const root = mount(activate, host(SUPERVISOR));
    await settle();
    const board = recorded<{ zones: { name: string; rooms: unknown[] }[]; defaultView: string }>("board");
    expect(board.defaultView).toBe("MAP");
    expect(root.querySelectorAll(".tile").length).toBe(board.zones.reduce((n, z) => n + z.rooms.length, 0));
    expect([...root.querySelectorAll(".grp b")].map((b) => b.textContent)).toEqual(board.zones.map((z) => z.name));
    expect(root.querySelector(".pager")).toBeNull();
    expect(root.querySelector(".strip")?.textContent).toContain("PMS ok · last fact 09:11");
  });

  it("switches to the wall, where every room is one line with its outcome so far", async () => {
    const root = mount(activate, host(SUPERVISOR));
    await settle();
    click(root, "button.chip", "Wall");
    await settle();
    const rows = [...root.querySelectorAll("table.wall tbody tr:not(.g)")].map((r) => r.textContent ?? "");
    expect(rows.find((r) => r.startsWith("G01"))).toContain("in progress · 09:04");
    expect(rows.find((r) => r.startsWith("G05"))).toContain("⏸ DND 08:40 · re-check 09:40");
    expect(rows.find((r) => r.startsWith("L09"))).toContain("! PMS says dirty 09:11");
    expect(rows.find((r) => r.startsWith("G09"))).toContain("pending policy");
    expect(root.textContent).toContain("17 of 17 — no pages");
  });

  it("draws what it could not read and why when the board does not answer — never a stand-in board — and Try again reads it", async () => {
    const overrides: Record<string, unknown> = { board: new HostCallError({ kind: "unavailable", message: "down" }) };
    const root = mount(activate, host(SUPERVISOR, overrides));
    await settle();
    expect(root.querySelector(".fail-said")?.textContent).toBe("Room Care did not answer in time");
    expect(root.querySelectorAll(".tile").length).toBe(0);

    delete overrides.board;
    click(root, ".fail button", "Try again");
    await settle();
    expect(root.querySelector(".fail")).toBeNull();
    expect(root.querySelectorAll(".tile").length).toBeGreaterThan(0);
  });

  it("offers no Try again when the service refused the question — only the details to pass on, with the service's own words", async () => {
    const refused = new HostCallError({ kind: "rejected", message: "this room is not at this property" });
    const root = mount(activate, host(SUPERVISOR, { board: refused }));
    await settle();
    const state = root.querySelector(".fail");
    expect(state?.querySelector(".fail-said")?.textContent).toBe("Room Care could not build the board");
    expect(state?.querySelector(".fail-facts")?.textContent).toContain("this room is not at this property");
    expect([...(state?.querySelectorAll("button") ?? [])].map((b) => b.textContent)).toEqual(["Copy these details"]);
  });

  it("opens a room with its disagreement, and keeps ours with the version it was drawn at", async () => {
    const calls: Call[] = [];
    const root = mount(activate, host(SUPERVISOR, {}, calls));
    await settle();
    click(root, "button.tile", "L09");
    await settle();
    expect(root.querySelector("h2")?.textContent).toContain("Room L09");
    click(root, "button", "Keep ours");
    await settle();
    const clear = calls.find((c) => c.method === "clearDisagreement");
    const room = recorded<{ line: { id: string; version: number } }>("room-l09");
    expect(clear).toEqual({ capability: "roomcare.amend", method: "clearDisagreement", params: { roomId: room.line.id, version: room.line.version, kept: "OURS" } });
  });

  it("offers Add the new rooms with the count of changes since the last press", async () => {
    const root = mount(activate, host(SUPERVISOR));
    await settle();
    click(root, "button.tab", "Prepare");
    await settle();
    const prepare = recorded<{ changesSince: number; changesPaging: { total: number } }>("prepare");
    expect(root.textContent).toContain(`Add the new rooms (${prepare.changesSince})`);
    expect(root.querySelector(".pager")?.textContent).toContain(`showing 1–${prepare.changesPaging.total} of ${prepare.changesPaging.total}`);
  });

  it("saves many Room states edits in one call, each with its version", async () => {
    const calls: Call[] = [];
    const root = mount(activate, host(SUPERVISOR, { saveStates: { saved: 2, conflicts: [] } }, calls));
    await settle();
    click(root, "button.tab", "Room states");
    await settle();
    click(root, "button.chip", "Tap grid");
    await settle();
    // Painting a room the colour it already is changes nothing, so paint clean on two dirty rooms.
    click(root, ".dock .segs button", "clean");
    await settle();
    click(root, "button.tile", "G02");
    await settle();
    click(root, "button.tile", "G04");
    await settle();
    click(root, "button.pri", "Save 2 changes");
    await settle();
    const save = calls.find((c) => c.method === "saveStates");
    expect(save?.capability).toBe("roomcare.amend");
    const states = recorded<{ zones: { rooms: { roomId: string; number: string; version: number }[] }[] }>("states");
    const byNumber = new Map(states.zones.flatMap((z) => z.rooms).map((r) => [r.number, r]));
    expect((save?.params as { rooms: { roomId: string; version: number; condition: string }[] }).rooms).toEqual([
      expect.objectContaining({ roomId: byNumber.get("G02")?.roomId, version: byNumber.get("G02")?.version, condition: "CLEAN" }),
      expect.objectContaining({ roomId: byNumber.get("G04")?.roomId, version: byNumber.get("G04")?.version, condition: "CLEAN" }),
    ]);
  });

  it("shows an attendant their rooms, and the door's four endings", async () => {
    const root = mount(activate, host(ATTENDANT));
    await settle();
    const mine = recorded<{ rows: { room: string }[] }>("my-rooms");
    expect(root.querySelectorAll(".list table tr.pick").length).toBe(mine.rows.length);
    click(root, "tr.pick", "G01");
    await settle();
    click(root, "button", "End…");
    await settle();
    expect([...root.querySelectorAll(".sheet .btn.chip")].map((b) => b.textContent)).toEqual(["Done", "Partial", "Declined by guest", "DND board"]);
  });

  it("keeps a refused decision's sheet open with the service's sentence", async () => {
    const refusal = new HostCallError({ kind: "rejected", message: "this room's decision was already made, and a supervisor's decision is final" });
    const root = mount(activate, host(SUPERVISOR, { decide: refusal }));
    await settle();
    click(root, "button.tab", "Supervision");
    await settle();
    click(root, "button", "DND approved — no cleaning");
    await settle();
    click(root, ".dlg button.pri", "Decide");
    await settle();
    expect(root.querySelector(".dlg .said.bad")?.textContent).toContain("a supervisor's decision is final");
  });
});
