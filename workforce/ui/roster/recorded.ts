/**
 * The approved frames' own data, as the module would have received it.
 *
 * # Why a module ships facts at all
 *
 * The desktop has **no Workforce gRPC client**. The bridge answers a capability
 * by dispatching to a client the shell holds, and there is none for this
 * application yet — so `host.call` fails `unavailable` today and will succeed
 * unchanged when that client lands. Until then a screen has something true to
 * draw, and **is told which it got** so it can say so.
 *
 * # These are the frames' numbers, not invented ones
 *
 * Every name, code and count below is read off `01-workforce-gold.html` frame 2.
 * That matters for the audit: a capture beside the frame is only evidence if the
 * two are showing the same week. Inventing prettier data would make every
 * comparison pass and prove nothing.
 */

import type { Shift, Week } from "./model";

/** This property's catalogue — frame 9's five, as frame 2 uses them. */
const MORNING: Shift = { id: "s-m", code: "M", name: "Morning", tone: "brand", hours: "07:00–15:00" };
const AFTERNOON: Shift = { id: "s-a", code: "A", name: "Afternoon", tone: "ok", hours: "15:00–23:00" };
const NIGHT: Shift = { id: "s-n", code: "N", name: "Night", tone: "warn", hours: "23:00–07:00" };
const OFF: Shift = { id: "s-off", code: "OFF", name: "Week-off", tone: "neutral", hours: null };
const SPLIT: Shift = {
  id: "s-sb", code: "SB", name: "Split — Banquet", tone: "warn", hours: "10–14, 18–22",
};
const GENERAL: Shift = {
  id: "s-g", code: "G", name: "General", tone: "ok", hours: "09:00 – 18:00",
};

/** A day with a shift and nothing else true of it. */
function on(shift: Shift): Week["people"][number]["week"][number] {
  return { shift, override: null, leave: null, gap: false };
}

/** A day somebody is away, which the rota draws instead of a chip. */
function away(leave: string): Week["people"][number]["week"][number] {
  return { shift: null, override: null, leave, gap: false };
}

export const recordedWeek: Week = {
  department: "Front Office",
  label: "24 – 30 Aug",
  month: "Aug",
  days: ["Mon 24", "Tue 25", "Wed 26", "Thu 27", "Fri 28", "Sat 29", "Sun 30"],

  // The ribbon is a timeline, not seven day cells — a duty running 20:00→08:00
  // covers two dates and fits in neither. The uncovered Saturday stretch is
  // drawn rather than left blank: "nobody" and "not entered yet" are different
  // answers, and only one of them is safe to assume.
  duty: [
    { who: "Priya T.", department: null, hours: null, from: 0, span: 1 / 7, overnight: false },
    { who: "Rahul N.", department: "SEC", hours: null, from: 1 / 7, span: 1 / 7, overnight: false },
    { who: "Priya T.", department: null, hours: null, from: 2 / 7, span: 1 / 7, overnight: false },
    { who: "Anjali M.", department: null, hours: "20:00→08:00", from: 3 / 7, span: 1.5 / 7, overnight: true },
    { who: "Vishnu D.", department: null, hours: null, from: 4.5 / 7, span: 0.5 / 7, overnight: false },
    { who: null, department: null, hours: null, from: 5 / 7, span: 1 / 7, overnight: false },
    { who: "Priya T.", department: null, hours: null, from: 6 / 7, span: 1 / 7, overnight: false },
  ],

  people: [
    {
      id: "p-priya", name: "Priya Thomas", initials: "PT", role: "Supervisor",
      zone: "Zone 1", head: true,
      week: [on(MORNING), on(MORNING), on(MORNING), on(MORNING), on(MORNING), on(OFF), on(OFF)],
    },
    {
      id: "p-anjali", name: "Anjali Menon", initials: "AM", role: "Receptionist",
      zone: "Zone 3", head: false,
      week: [on(MORNING), on(MORNING), on(MORNING), on(AFTERNOON), on(MORNING), on(MORNING), on(OFF)],
    },
    {
      id: "p-vishnu", name: "Vishnu Das", initials: "VD", role: "Night auditor",
      zone: "Zone 1", head: false,
      week: [on(NIGHT), on(NIGHT), on(NIGHT), on(NIGHT), on(NIGHT), on(OFF), on(NIGHT)],
    },
    {
      id: "p-sneha", name: "Sneha Iyer", initials: "SI", role: "Receptionist",
      zone: "Zone 2", head: false,
      week: [on(AFTERNOON), on(AFTERNOON), away("Sick"), away("Sick"), on(AFTERNOON), on(AFTERNOON), on(OFF)],
    },
    {
      id: "p-joseph", name: "Joseph Kurian", initials: "JK", role: "Bell captain",
      zone: "Zone 1", head: false,
      week: [on(OFF), on(MORNING), on(SPLIT), on(AFTERNOON), on(AFTERNOON), on(MORNING), on(MORNING)],
    },
    {
      id: "p-rani", name: "Rani Rajan", initials: "RR", role: "Guest relations",
      zone: "Zone 2", head: false,
      week: [
        on(AFTERNOON),
        on(OFF),
        // The uncovered slot the header counts. It is a cell that knows it is
        // empty, not an absent one — the difference between "nobody is on" and
        // "nobody has decided yet".
        { shift: null, override: null, leave: null, gap: true },
        on(AFTERNOON), on(AFTERNOON), on(AFTERNOON),
        { shift: null, override: null, leave: null, gap: false },
      ],
    },
  ],

    // The property's six, as frames 8 and 9 list them. The printed week's legend
  // is rendered from this, so a catalogue missing an entry prints a sheet whose
  // legend cannot explain one of its own cells.
  catalogue: [MORNING, AFTERNOON, NIGHT, SPLIT, GENERAL, OFF],

  // Empty in the approved frame, and kept as a field rather than omitted: the
  // warning is a real state of this screen and the harness shows it in a pane
  // of its own, which is how the audit can see a design element that the
  // ordinary week does not contain.
  overtime: [],
};

/** The same week, with somebody planned past the threshold. */
export const recordedOvertime: Week = {
  ...recordedWeek,
  overtime: [{ who: "Vishnu Das", planned: "60.0", threshold: "48" }],
};
