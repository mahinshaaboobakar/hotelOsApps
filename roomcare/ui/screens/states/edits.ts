/**
 * The Room states tab's pending edits — one set shared by the three views, so a
 * change made in the sheet is still there in the tap grid, and one Save sends
 * them all (redline 5).
 */

import type { StateRow } from "../../model";

export interface Edit {
  condition?: string;
  occupancy?: string;
  stay?: string;
  soldAt?: string | null;
}

export class Edits {
  private readonly changes = new Map<string, Edit>();

  constructor(private readonly rows: ReadonlyMap<string, StateRow>) {}

  get size(): number {
    return this.changes.size;
  }

  /** Set one fact on a room; setting it back to what the room holds removes the edit. */
  set(roomId: string, fact: keyof Edit, value: string | null): void {
    const row = this.rows.get(roomId);
    if (row === undefined || row.blocked) return;
    const edit = { ...(this.changes.get(roomId) ?? {}) };
    const current = fact === "soldAt" ? row.soldAt : row[fact];
    if (value === current) delete edit[fact];
    else (edit as Record<string, string | null>)[fact] = value;
    if (Object.keys(edit).length === 0) this.changes.delete(roomId);
    else this.changes.set(roomId, edit);
  }

  /** Undo a room's edits — a second tap in the grid. */
  clear(roomId: string): void {
    this.changes.delete(roomId);
  }

  has(roomId: string): boolean {
    return this.changes.has(roomId);
  }

  /** The value shown for a fact: the edit if there is one, else the room's own. */
  value(row: StateRow, fact: "condition" | "occupancy" | "stay"): string {
    return this.changes.get(row.roomId)?.[fact] ?? row[fact];
  }

  /** The body of one Save — each room with the version its edit is based on. */
  payload(): { rooms: object[] } {
    return {
      rooms: [...this.changes.entries()].map(([roomId, edit]) => ({
        roomId,
        version: this.rows.get(roomId)?.version ?? 0,
        condition: edit.condition ?? null,
        occupancy: edit.occupancy ?? null,
        stay: edit.stay ?? null,
        soldAt: edit.soldAt === undefined || edit.soldAt === null ? null : edit.soldAt,
        clearSold: edit.soldAt === null,
      })),
    };
  }

  discard(): void {
    this.changes.clear();
  }
}
