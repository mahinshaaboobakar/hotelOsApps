/**
 * The Teams screens' own data, exactly as the locked frames draw them.
 *
 * Read off `03-workforce-teams.html` — the seven frames locked on 2026-09-04.
 * A capture beside a frame is evidence only when the two show the same
 * property, so nothing here is invented and nothing is prettier than the
 * drawing.
 *
 * The desktop has no Workforce client, so `host.call` fails `unavailable` and
 * these are what the screen draws until one lands — and it is told which it
 * got, like every other screen.
 */

import type { PostingEnding, Teams } from "./team";

/** The five teams frame 1 lists, one of them stood down. */
export const recordedTeams: Teams = {
  property: "Kochi Beach Resort",
  onDate: "2026-09-04",
  teams: [
    {
      id: "t-mc", name: "Morning Crew", department: "HK", departmentName: "Housekeeping",
      note: "Rooms 101–140",
      members: 6, formed: "2026-03-12T00:00:00Z", active: true,
    },
    {
      id: "t-tb", name: "Tower Block", department: "HK", departmentName: "Housekeeping",
      note: "Floors 8–12",
      members: 4, formed: "2026-03-12T00:00:00Z", active: true,
    },
    {
      id: "t-fd", name: "Front Desk — Early", department: "FO", departmentName: "Front Office",
      note: null,
      members: 3, formed: "2026-01-04T00:00:00Z", active: true,
    },
    {
      id: "t-bs", name: "Banquet Service", department: "KIT", departmentName: "Kitchen",
      note: null,
      members: 5, formed: "2026-08-19T00:00:00Z", active: true,
    },
    // Stood down, and drawn: the list hides it unless asked for, and frame 1
    // asks for it. Zero members is a fact about today rather than a team that
    // was never used.
    {
      id: "t-pb", name: "Pool Bar", department: "KIT", departmentName: "Kitchen",
      note: "Seasonal",
      members: 0, formed: "2025-11-01T00:00:00Z", active: false,
    },
  ],
  detail: {
    team: {
      id: "t-mc", name: "Morning Crew", department: "HK", departmentName: "Housekeeping",
      note: "Rooms 101–140",
      members: 6, formed: "2026-03-12T00:00:00Z", active: true,
    },
    onDate: "2026-09-04",
    members: [
      { staffId: "s-pd", name: "P. Das", initials: "PD", since: "2026-03-12" },
      { staffId: "s-rk", name: "R. Kurian", initials: "RK", since: "2026-03-12" },
      { staffId: "s-sk", name: "S. Kumar", initials: "SK", since: "2026-04-02" },
      { staffId: "s-np", name: "N. Pillai", initials: "NP", since: "2026-04-02" },
      { staffId: "s-dr", name: "D. Rao", initials: "DR", since: "2026-08-19" },
      { staffId: "s-vn", name: "V. Nambiar", initials: "VN", since: "2026-09-01" },
    ],
    candidates: [
      {
        staffId: "s-bs", name: "B. Shetty", role: "Room attendant",
        department: "Housekeeping", refused: null,
      },
      {
        staffId: "s-rn", name: "Rahul Nair", role: "Room attendant",
        department: "Housekeeping", refused: null,
      },
      // Frame 4's third row. Shown rather than filtered out: hiding him leaves a
      // supervisor wondering where Joseph went, and the reason is the whole
      // rule — a team exists to receive work in its own department.
      {
        staffId: "s-jk", name: "Joseph Kurian", role: "Bell captain",
        department: "Front Office", refused: "Not posted here",
      },
    ],
  },
  // The departments this property has, which is what the picker offers.
  // **Three, not two**: `KIT` has teams and `FO` has one, and a property
  // always has departments a team has not been formed in yet - which is
  // exactly the case a list derived from `teams` cannot represent.
  departments: [
    // In the order the read answers: by CODE, which is the same on every
    // machine. `ENG` first and `Maintenance` last is the whole point of the
    // fixture — the picker orders for the reader, and a fixture whose code
    // order and name order coincide could not tell the two apart.
    { code: "ENG", name: "Maintenance" },
    { code: "FO", name: "Front Office" },
    { code: "HK", name: "Housekeeping" },
    { code: "KIT", name: "Kitchen" },
  ],
};

/** Frame 7 — a property that has formed none. */
export const recordedNoTeams: Teams = {
  property: "Kochi Beach Resort",
  onDate: "2026-09-04",
  teams: [],
  detail: null,
  // A property with no teams still has departments, and this is the state that
  // proves the list is not derived from the teams above: derived, it would be
  // empty here, and Form a team would offer nothing on the one screen whose
  // whole purpose is forming the first one.
  // The departments this property has, which is what the picker offers.
  // **Three, not two**: `KIT` has teams and `FO` has one, and a property
  // always has departments a team has not been formed in yet - which is
  // exactly the case a list derived from `teams` cannot represent.
  departments: [
    // In the order the read answers: by CODE, which is the same on every
    // machine. `ENG` first and `Maintenance` last is the whole point of the
    // fixture — the picker orders for the reader, and a fixture whose code
    // order and name order coincide could not tell the two apart.
    { code: "ENG", name: "Maintenance" },
    { code: "FO", name: "Front Office" },
    { code: "HK", name: "Housekeeping" },
    { code: "KIT", name: "Kitchen" },
  ],
};

/**
 * Frame 6 — what ending a posting is about to do.
 *
 * # It names a different person than the drawing, and that is the finding
 *
 * The frame reads *"End S. Kumar's posting in Housekeeping?"* over two
 * Housekeeping teams, and **S. Kumar is on no roster this module records**: the
 * People screen's six postings, locked in the gold set, do not include him, and
 * the two teams he is drawn into are Morning Crew and Tower Block, whose recorded
 * roll does not include him either. A dialog that opens over a table not
 * containing the person it is about is a capture nobody can check.
 *
 * So the recording is retargeted onto somebody both screens already carry —
 * **Rajan Pillai**, the two-posting sous chef — ending the Kitchen half of it,
 * which closes his Banquet Service membership. One row rather than the drawn
 * two, because Kitchen has exactly one live team in the locked list.
 *
 * **The panel's shape is the drawing's**: the heading, the team rows, then the
 * sentence, all before the button. What changed is which true rows fill it. The
 * count is the round's one divergence from frame 6 and is the owner's to
 * adjudicate.
 */
export const recordedPostingEnding: PostingEnding = {
  who: "Rajan Pillai",
  department: "Kitchen",
  lastDay: "Thu 4 Sep 2026",
  alsoEnds: [
    { team: "Banquet Service", department: "KIT", since: "member since 19 Aug" },
  ],
};
