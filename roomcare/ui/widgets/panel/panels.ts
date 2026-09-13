/**
 * The five widgets — one question each (page 56; frame 8).
 *
 * Rooms Ready answers *are we ready for tonight*; Arrivals Waiting *who is at
 * risk*; Attention *who needs a person*; Attendants Now *where is everyone*;
 * Pending *what is waiting on the manager*. Every number is the board's own,
 * counted by one rule in the backend; an uncomputable number is absent, never
 * approximate.
 */

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import { load } from "../../chrome/load";
import { reason } from "../../chrome/words";
import { card, figures, openRow, unread } from "../card";

interface RoomRow {
  roomId: string;
  room: string;
  what: string;
  at: string | null;
  tone: string;
}

export async function roomsReady(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ departures: number; ready: number; inProgress: number; dirty: number; at: string }>(host, "widgetRoomsReady");
  if (!got.ok) return unread("Rooms Ready", "today's departures", got.because);
  const v = got.value;
  const percent = v.departures === 0 ? 0 : Math.round((v.ready / v.departures) * 100);
  const bar = el("div", "wbar");
  const fill = el("i");
  fill.style.width = `${percent}%`;
  bar.append(fill);
  const foot = el("div", "wfoot");
  foot.append(el("span", undefined, `${v.departures} departures`), el("span", undefined, `${percent}% ready · ${clock(host, v.at)}`));
  return card("Rooms Ready", "today's departures", [
    figures([{ value: String(v.ready), label: "ready", tone: "ok" }, { value: String(v.inProgress), label: "in progress", tone: "run" }, { value: String(v.dirty), label: "dirty", tone: "bad" }]),
    bar, foot,
  ]);
}

export async function arrivalsWaiting(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ total: number; rows: RoomRow[]; at: string }>(host, "widgetArrivals");
  if (!got.ok) return unread("Arrivals Waiting", "sold, not ready", got.because);
  const v = got.value;
  const words: Record<string, string> = { IN_PROGRESS: "in progress", NOBODY_AVAILABLE: "nobody available", NOT_STARTED: "not started" };
  const rows: (Node | null)[] = v.rows.map((r) => openRow(host, r.room, `${clock(host, r.at)} · ${words[r.what] ?? r.what.toLowerCase()}`, r.what === "NOBODY_AVAILABLE" ? "bad" : r.what === "IN_PROGRESS" ? "run" : "warn", r.roomId));
  if (v.total === 0) rows.push(el("div", "wquiet", "Every room sold tonight is ready."));
  if (v.total > v.rows.length) rows.push(foot(`${v.total - v.rows.length} more`, "soonest first"));
  return card("Arrivals Waiting", "sold, not ready", rows);
}

export async function attention(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ total: number; rows: RoomRow[]; at: string }>(host, "widgetAttention");
  if (!got.ok) return unread("Attention", "needs a person", got.because);
  const v = got.value;
  const rows: (Node | null)[] = v.rows.map((r) => openRow(host, r.room, reason(r.what).toLowerCase(), r.tone, r.roomId));
  if (v.total === 0) rows.push(el("div", "wquiet", "Nothing needs a person right now."));
  if (v.total > v.rows.length) rows.push(foot(`${v.total - v.rows.length} more`, "in the supervision lane"));
  return card("Attention", "needs a person", rows);
}

export async function attendantsNow(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ onShift: number | null; inARoom: number; rows: { userId: string; name: string; room: string; since: string }[]; at: string }>(host, "widgetAttendants");
  if (!got.ok) return unread("Attendants Now", "who is where", got.because);
  const v = got.value;
  const rows: (Node | null)[] = v.rows.map((r) => {
    const line = el("div", "wrow");
    line.append(el("span", undefined, r.name), el("span", "num", `${r.room} · since ${clock(host, r.since)}`));
    return line;
  });
  if (v.rows.length === 0) rows.push(el("div", "wrefusal", "Nobody is in a room right now."));
  rows.push(foot(v.onShift === null ? "on shift — not announced by Workforce yet" : `${v.onShift} on shift`, `${v.inARoom} in a room`));
  return card("Attendants Now", "who is where", rows);
}

export async function pendingPolicy(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ total: number; rows: RoomRow[]; at: string }>(host, "widgetPending");
  if (!got.ok) return unread("Pending", "waiting on a click", got.because);
  const v = got.value;
  const rows: (Node | null)[] = v.rows.map((r) => openRow(host, r.room, r.what, "warn", r.roomId));
  if (v.total === 0) rows.push(el("div", "wquiet", "No room is waiting on a click."));
  return card("Pending", "waiting on a click", rows);
}

function foot(left: string, right: string): HTMLElement {
  const line = el("div", "wfoot");
  line.append(el("span", undefined, left), el("span", undefined, right));
  return line;
}
