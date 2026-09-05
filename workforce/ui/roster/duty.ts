/**
 * What the Duty Register is given — `WF-Q8`, spans rather than day cells.
 *
 * The register shows **now and next**, because knowing who is on and who
 * follows is the whole of what a duty manager needs from this screen. Both are
 * the clock against the stored spans, never a flag somebody has to move.
 */

/** One duty on the week's timeline. */
export interface Duty {
  /** Who holds it, or null for a stretch nobody does. */
  who: string | null;

  /** Their department, shown because MOD is property-wide. */
  where: string | null;

  /**
   * The span, as **ISO instants** — null where nobody covers the band.
   *
   * Not rendered hours. A duty is stored as an instant, and a service that
   * wrote out "20–08" would render in whatever offset the row carried — UTC —
   * so a Kochi property would read 20:00 for a handover that happens at 01:30
   * its own time, with nothing on the screen saying which clock it meant. The
   * screen says it in the property's.
   */
  from: string | null;
  to: string | null;

  /** Which day column it starts in, 0–6. */
  day: number;

  /** Day duties sit in the upper band, night duties in the lower. */
  band: "day" | "night";
}

/** The register. */
/** Whoever holds the duty, and the span they hold it for. */
export interface Holder {
  who: string;

  /** ISO instants; the screen composes the sentence in the property's form. */
  from: string;
  to: string;
}

export interface Register {
  week: string;
  days: readonly string[];

  /** Who holds it at this instant, and what the screen says about them. */
  now: Holder | null;

  /** The next to begin. */
  next: Holder | null;

  duties: readonly Duty[];
}

/** Both bands of one day, as the frame draws them. */
/**
 * The recorded week's instants — the wire's shape, so both render alike.
 *
 * **The hour is the PROPERTY's, converted.** The frames draw a duty running
 * 08:00–20:00 at a hotel in Kochi, and the first version of this fixture wrote
 * `08:00Z` — which is 13:30 there. Writing a local hour into a UTC field is the
 * same mistake the service was making, one layer down, and it renders
 * convincingly: every band read "01:30 pm" and looked like a formatter bug
 * rather than a fixture that had asserted the wrong clock.
 *
 * `Asia/Kolkata` is +05:30, so 08:00 local is 02:30Z and 20:00 local is 14:30Z.
 */
function iso(day: number, localHour: number): string {
  const utc = Date.UTC(2026, 7, 24 + day, localHour, 0) - (5 * 60 + 30) * 60_000;
  return new Date(utc).toISOString();
}

function pair(day: number, dayName: string, nightName: string | null): readonly Duty[] {
  return [
    {
      who: dayName, where: null, day, band: "day",
      from: iso(day, 8), to: iso(day, 20),
    },
    {
      who: nightName, where: null, day, band: "night",
      from: nightName === null ? null : iso(day, 20),
      to: nightName === null ? null : iso(day + 1, 8),
    },
  ];
}

export const recordedRegister: Register = {
  week: "24 – 30 Aug",
  days: ["Mon 24", "Tue 25", "Wed 26", "Thu 27", "Fri 28", "Sat 29", "Sun 30"],

  now: { who: "Anjali Menon", from: iso(4, 20), to: iso(5, 8) },
  next: { who: "Vishnu Das", from: iso(5, 8), to: iso(5, 20) },

  duties: [
    ...pair(0, "Priya T.", "Rahul N."),
    ...pair(1, "Priya T.", "Rahul N."),
    ...pair(2, "Anjali M.", "Vishnu D."),
    ...pair(3, "Priya T.", "Rahul N."),
    ...pair(4, "Priya T.", "Anjali M."),
    // Saturday night nobody holds. Drawn dashed and stated, because a blank
    // would read as "not entered yet" when it means "nobody is on".
    ...pair(5, "Vishnu D.", null),
    ...pair(6, "Priya T.", null),
  ],
};
