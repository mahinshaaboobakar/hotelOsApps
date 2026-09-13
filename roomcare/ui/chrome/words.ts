/**
 * Room Care's vocabulary as a person reads it — the one place a wire word
 * becomes screen words, so "DEPARTURE_CLEAN" is "Departure clean" everywhere.
 */

const SERVICE: Record<string, string> = {
  DEPARTURE_CLEAN: "Departure clean",
  DAILY_SERVICE: "Daily service",
  TURNDOWN: "Turndown",
  REFRESH: "Refresh",
  AREA_CLEAN: "Area routine",
};

const SOURCE: Record<string, string> = {
  ATTENDANT: "attendant",
  INSPECTION: "inspection",
  SUPERVISOR: "supervisor",
  PMS: "PMS",
  GUESTOPS: "desk",
  MANUAL: "manual",
  SYSTEM: "system",
  ENGINEERING: "engineering",
};

const REASON: Record<string, string> = {
  DAYS_WITHOUT_SERVICE: "days without service",
  DISAGREEMENT: "Disagreement",
  ARRIVAL_BEFORE_WINDOW: "Arrival before window",
  NOBODY_AVAILABLE: "Nobody available",
};

const PHASE: Record<string, string> = { STRIP: "strip", CLEAN: "clean", MAKE_UP: "make up", DONE: "done", INSPECT: "inspect" };

export function service(value: string | null | undefined): string {
  return value === null || value === undefined ? "—" : SERVICE[value] ?? lower(value);
}

export function source(value: string | null | undefined): string {
  return value === null || value === undefined ? "—" : SOURCE[value] ?? lower(value);
}

export function reason(value: string): string {
  return REASON[value] ?? lower(value);
}

/** A phase's name; turndown and refresh walk the clean phase under their own word (frame 7b). */
export function phase(value: string, serviceName?: string): string {
  if (value === "CLEAN" && serviceName === "TURNDOWN") return "turn down";
  if (value === "CLEAN" && serviceName === "REFRESH") return "refresh";
  return PHASE[value] ?? lower(value);
}

/** `CHECKED_OUT` → `checked out`. */
export function lower(value: string): string {
  return value.toLowerCase().replaceAll("_", " ");
}

/** An ordinal day — "3rd day without service". */
export function ordinal(n: number): string {
  const tail = n % 100 >= 11 && n % 100 <= 13 ? "th" : ({ 1: "st", 2: "nd", 3: "rd" } as Record<number, string>)[n % 10] ?? "th";
  return `${n}${tail}`;
}

/** The condition's class — the fill of a tile and a glyph. */
export function conditionClass(condition: string): string {
  return condition === "CLEAN" ? "clean" : condition === "INSPECTED" ? "insp" : "dirty";
}
