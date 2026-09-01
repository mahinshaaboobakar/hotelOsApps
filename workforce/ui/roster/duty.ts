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

  /** The hours, as the printed sheet spells them. */
  hours: string;

  /** Which day column it starts in, 0–6. */
  day: number;

  /** Day duties sit in the upper band, night duties in the lower. */
  band: "day" | "night";
}

/** The register. */
export interface Register {
  week: string;
  days: readonly string[];

  /** Who holds it at this instant, and what the screen says about them. */
  now: { who: string; detail: string } | null;

  /** The next to begin. */
  next: { who: string; detail: string } | null;

  duties: readonly Duty[];
}

/** Both bands of one day, as the frame draws them. */
function pair(day: number, dayName: string, nightName: string | null): readonly Duty[] {
  return [
    { who: dayName, where: null, hours: "08–20", day, band: "day" },
    { who: nightName, where: null, hours: "20–08", day, band: "night" },
  ];
}

export const recordedRegister: Register = {
  week: "24 – 30 Aug",
  days: ["Mon 24", "Tue 25", "Wed 26", "Thu 27", "Fri 28", "Sat 29", "Sun 30"],

  now: {
    who: "Anjali Menon",
    detail: "Front Office · since 20:00 · ends 08:00 tomorrow",
  },
  next: { who: "Vishnu Das", detail: "Front Office · Sat 29, 08:00 → 20:00" },

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
