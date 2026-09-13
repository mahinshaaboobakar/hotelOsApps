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
import { ordinal, reason } from "../../chrome/words";
import { card, figures, openRow, unread } from "../card";

interface RoomRow {
  roomId: string;
  room: string;
  what: string;
  at: string | null;
  tone: string;
  detail: string | null;
}

export async function roomsReady(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ departures: number; ready: number; inProgress: number; dirty: number; at: string }>(host, "widgetRoomsReady");
  if (!got.ok) return unread("Rooms Ready", "today's departures", got.because);
  const v = got.value;
  const percent = v.departures === 0 ? 0 : Math.round((v.ready / v.departures) * 100);
  const total = el("div", "wrow");
  total.append(el("span", undefined, `${v.departures} departures`), el("span", "num", `${percent}% ready · ${clock(host, v.at)}`));
  return card("Rooms Ready", "today's departures", [
    figures([{ value: String(v.ready), label: "ready", tone: "ok" }, { value: String(v.inProgress), label: "in progress", tone: "warn" }, { value: String(v.dirty), label: "dirty", tone: "bad" }]),
    total,
  ]);
}

export async function arrivalsWaiting(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ total: number; rows: RoomRow[]; at: string }>(host, "widgetArrivals");
  if (!got.ok) return unread("Arrivals Waiting", "sold, not ready", got.because);
  const v = got.value;
  const words: Record<string, string> = { IN_PROGRESS: "in progress", NOBODY_AVAILABLE: "nobody available", NOT_STARTED: "not started" };
  const rows: (Node | null)[] = v.rows.map((r) => openRow(host, r.room, `${clock(host, r.at)} · ${words[r.what] ?? r.what.toLowerCase()}`, r.what === "NOBODY_AVAILABLE" ? "bad" : "", r.roomId));
  if (v.total === 0) rows.push(el("div", "wquiet", "Every room sold tonight is ready."));
  if (v.total > v.rows.length) rows.push(foot(`${v.total - v.rows.length} more`, "soonest first"));
  return card("Arrivals Waiting", "sold, not ready", rows);
}

export async function attention(host: HostApi): Promise<HTMLElement> {
  const got = await load<{ total: number; rows: RoomRow[]; at: string }>(host, "widgetAttention");
  if (!got.ok) return unread("Attention", "needs a person", got.because);
  const v = got.value;
  const rows: (Node | null)[] = v.rows.map((r) => openRow(host, r.room, attentionWords(r), r.tone, r.roomId));
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
    line.append(el("span", undefined, r.name), el("span", undefined, `${r.room} · since ${clock(host, r.since)}`));
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
  const rows: (Node | null)[] = v.rows.map((r) => openRow(host, r.room, r.what === "UNSOLD_DEPARTURE" ? "unsold departure · may wait" : `${r.detail ?? "no rule matched"} · see room`, "", r.roomId));
  if (v.total === 0) rows.push(el("div", "wquiet", "No room is waiting on a click."));
  return card("Pending", "waiting on a click", rows);
}

/** "3rd day without service", "disagreement · PMS says dirty" — the lane's reason with its one fact. */
function attentionWords(r: RoomRow): string {
  if (r.what === "DAYS_WITHOUT_SERVICE" && r.detail !== null) return `${ordinal(Number(r.detail))} day without service`;
  if (r.what === "DISAGREEMENT" && r.detail !== null) return `disagreement · PMS says ${r.detail.toLowerCase()}`;
  return reason(r.what).toLowerCase();
}

function foot(left: string, right: string): HTMLElement {
  const line = el("div", "wrow wfoot");
  line.append(el("span", undefined, left), el("span", undefined, right));
  return line;
}
