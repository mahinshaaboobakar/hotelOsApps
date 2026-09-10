/**
 * What the People screen is given — postings, and what is operational about them.
 *
 * # Identity is Master Data's and read-only here
 *
 * Name, employee number, contact and photograph belong to the person and are
 * edited in Core Administration. This screen owns what is **operational** — the
 * posting, the job role, the reporting line, the zone and the department head.
 */

/** One posting, as the list shows it. */
import type { Paging } from "@hotelos/sdk";

export interface Posting {
  /**
   * The posting this row is about.
   *
   * **A row was keyed by the person's NAME.** End posting had nothing to send
   * even once its button was wired, and two people of one name would have been
   * one row to every screen that used it.
   */
  id: string;

  /** The row a write quotes back - optimistic concurrency. */
  version: number;

  who: string;

  /** When the posting began, and whether the person holds more than one. */
  /**
   * When the primary posting began, as the wire carries it - ADR 0152.
   *
   * The service used to send the whole sentence, `"Since 4 Jan 2025 - 2
   * postings"`, so a row's vocabulary lived in the payload and the month name
   * came from the account the service runs under.
   */
  since: string;

  /** How many postings this person holds - the clause that was inside `since`. */
  postings: number;

  /**
   * The department codes, plural.
   *
   * `WF-Q3`: a person may hold two postings, which is why this is a list and
   * not a field — one that could hold only one would make the second posting
   * unrepresentable rather than merely unusual.
   */
  departments: readonly string[];

  /** The zone the posting carries, when it carries one — `WF-Q7`. Optional. */
  zone: string | null;

  role: string;

  /** Who they report to, or the words that stand where a name would. */
  reportsTo: string;

  /** What the capability register says, and how it reads. */
  capability: string;
  tone: "ok" | "warn" | "bad" | "neu";
}

/** The screen. */
export interface People {
  postings: readonly Posting[];

  /**
   * The server's own page numbers — `PagedResponse`, echoed.
   *
   * **This is the one Workforce list that pages.** Every other read is bounded
   * by a natural key: one person's month, one department's week or day, the
   * property's own catalogue. This one is bounded by the property's headcount,
   * which at a resort is hundreds — so `CORE-Q13`'s paged pattern applies, the
   * count is a fact rather than a moving target, and an ordinal is honest.
   *
   * Never the numbers the screen asked for: a pager numbered from a requested
   * size the server clamped is wrong on every button while the list underneath
   * looks perfect.
   */
  paging: Paging;
}

export const recordedPeople: People = {
  // One full page of a real roster: 25 of 42, so the pager says two pages and
  // the list scrolls inside its own viewport rather than the page being sized
  // by how many rows a fixture happened to hold.
  paging: { page: 0, pageSize: 25, total: 42 },
  postings: [
    {
      id: "p-pt", version: 1, who: "Priya Thomas", since: "2023-03-12", postings: 1, departments: ["FO"],
      zone: "Zone 1", role: "Supervisor", reportsTo: "— department head",
      capability: "3 valid", tone: "ok",
    },
    {
      id: "p-am", version: 1, who: "Anjali Menon", since: "2025-01-04", postings: 1, departments: ["FO"],
      zone: "Zone 3", role: "Receptionist", reportsTo: "Priya Thomas",
      capability: "1 expiring", tone: "warn",
    },
    {
      id: "p-vd", version: 1, who: "Vishnu Das", since: "2021-08-19", postings: 1, departments: ["FO"],
      zone: "Zone 1", role: "Night auditor", reportsTo: "Priya Thomas",
      capability: "2 valid", tone: "ok",
    },
    // Two postings, and an expired certification which is shown, named, and
    // blocks nothing — WF-Q16's judgment side.
    {
      id: "p-rp", version: 1, who: "Rajan Pillai", since: "2020-02-02", postings: 2,
      departments: ["KIT", "BQT"], zone: "Zone 5", role: "Sous chef",
      reportsTo: "Mathew George", capability: "1 expired", tone: "bad",
    },
    {
      id: "p-rn", version: 1, who: "Rahul Nair", since: "2024-06-08", postings: 1, departments: ["SEC"],
      zone: "Zone 1", role: "Security officer", reportsTo: "Thomas Varghese",
      capability: "4 valid", tone: "ok",
    },
    {
      id: "p-si", version: 1, who: "Sneha Iyer", since: "2025-11-30", postings: 1, departments: ["FO"],
      zone: "Zone 2", role: "Receptionist", reportsTo: "Priya Thomas",
      capability: "none recorded", tone: "neu",
    },
    // The rest of the page. **A recorded page is a full page**, or the pager
    // under it is a lie: the fixture held six and the pager read
    // *showing 1–6 of 42*, which made six the page size — a number nobody
    // chose, leaked out of how many rows somebody had typed. Twenty-five is the
    // platform's default and §6's worked example, and the six above stay first
    // because every other frame in the set draws them.
    {
      id: "p-mg", version: 1, who: "Mathew George", since: "2019-03-03", postings: 1, departments: ["KIT"],
      zone: "Zone 5", role: "Executive chef", reportsTo: "— department head",
      capability: "5 valid", tone: "ok",
    },
    {
      id: "p-tv", version: 1, who: "Thomas Varghese", since: "2018-07-14", postings: 1, departments: ["SEC"],
      zone: "Zone 1", role: "Security manager", reportsTo: "— department head",
      capability: "2 valid", tone: "ok",
    },
    {
      id: "p-dm", version: 1, who: "Deepa Menon", since: "2022-09-21", postings: 1, departments: ["HK"],
      zone: "Zone 4", role: "Housekeeping supervisor", reportsTo: "— department head",
      capability: "3 valid", tone: "ok",
    },
    {
      id: "p-ak", version: 1, who: "Arun Kumar", since: "2023-05-05", postings: 1, departments: ["HK"],
      zone: "Zone 4", role: "Room attendant", reportsTo: "Deepa Menon",
      capability: "1 expiring", tone: "warn",
    },
    {
      id: "p-ln", version: 1, who: "Lakshmi Nair", since: "2024-08-12", postings: 1, departments: ["HK"],
      zone: "Zone 6", role: "Room attendant", reportsTo: "Deepa Menon",
      capability: "2 valid", tone: "ok",
    },
    {
      id: "p-jm", version: 1, who: "Jose Mathew", since: "2021-01-02", postings: 1, departments: ["ENG"],
      zone: "Zone 1", role: "Maintenance technician", reportsTo: "Suresh Babu",
      capability: "4 valid", tone: "ok",
    },
    {
      id: "p-sb", version: 1, who: "Suresh Babu", since: "2017-11-08", postings: 1, departments: ["ENG"],
      zone: "Zone 1", role: "Chief engineer", reportsTo: "— department head",
      capability: "6 valid", tone: "ok",
    },
    {
      id: "p-fr", version: 1, who: "Fathima Rasheed", since: "2025-02-17", postings: 1, departments: ["FB"],
      zone: "Zone 3", role: "Server", reportsTo: "Nikhil Varma",
      capability: "none recorded", tone: "neu",
    },
    {
      id: "p-nv", version: 1, who: "Nikhil Varma", since: "2020-04-29", postings: 1, departments: ["FB"],
      zone: "Zone 3", role: "Restaurant manager", reportsTo: "— department head",
      capability: "3 valid", tone: "ok",
    },
    {
      id: "p-ms", version: 1, who: "Meera Suresh", since: "2023-06-06", postings: 1, departments: ["FB"],
      zone: "Zone 3", role: "Server", reportsTo: "Nikhil Varma",
      capability: "1 expiring", tone: "warn",
    },
    {
      id: "p-ap", version: 1, who: "Aravind Pillai", since: "2021-10-23", postings: 1, departments: ["KIT"],
      zone: "Zone 5", role: "Commis chef", reportsTo: "Mathew George",
      capability: "2 valid", tone: "ok",
    },
    {
      id: "p-st", version: 1, who: "Sara Thomas", since: "2024-12-11", postings: 1, departments: ["SPA"],
      zone: "Zone 7", role: "Therapist", reportsTo: "Divya Krishnan",
      capability: "3 valid", tone: "ok",
    },
    {
      id: "p-dk", version: 1, who: "Divya Krishnan", since: "2019-04-04", postings: 1, departments: ["SPA"],
      zone: "Zone 7", role: "Spa manager", reportsTo: "— department head",
      capability: "4 valid", tone: "ok",
    },
    {
      id: "p-mk", version: 1, who: "Manoj Kurup", since: "2022-01-19", postings: 1, departments: ["SEC"],
      zone: "Zone 2", role: "Security officer", reportsTo: "Thomas Varghese",
      capability: "1 expired", tone: "bad",
    },
    {
      id: "p-ra", version: 1, who: "Reshma Anil", since: "2025-07-27", postings: 1, departments: ["FO"],
      zone: "Zone 1", role: "Guest relations", reportsTo: "Priya Thomas",
      capability: "none recorded", tone: "neu",
    },
    {
      id: "p-gm", version: 1, who: "Gopal Menon", since: "2020-03-15", postings: 1, departments: ["ENG"],
      zone: "Zone 6", role: "Electrician", reportsTo: "Suresh Babu",
      capability: "3 valid", tone: "ok",
    },
    {
      id: "p-aj", version: 1, who: "Anu Jacob", since: "2023-09-09", postings: 1, departments: ["HK"],
      zone: "Zone 4", role: "Linen attendant", reportsTo: "Deepa Menon",
      capability: "2 valid", tone: "ok",
    },
    {
      id: "p-vr", version: 1, who: "Vinod Raj", since: "2024-02-01", postings: 1, departments: ["FB"],
      zone: "Zone 3", role: "Bartender", reportsTo: "Nikhil Varma",
      capability: "1 valid", tone: "ok",
    },
    {
      id: "p-kn", version: 1, who: "Kavya Nambiar", since: "2025-05-22", postings: 1, departments: ["KIT"],
      zone: "Zone 5", role: "Pastry chef", reportsTo: "Mathew George",
      capability: "2 valid", tone: "ok",
    },
  ],
};

/**
 * A property that has posted nobody — the first run.
 *
 * The empty state names **the consequence**, not the button: until a posting
 * exists, `department#posted` resolves to nobody and every department-scoped
 * document grant in My Hotel is dormant. Saying so is honest, and it is also
 * the strongest argument for doing it first.
 */
export const recordedFirstRun: People = {
  postings: [],

  // Nothing to page, and the pager draws nothing rather than a disabled row of
  // one under an empty state promising pages of nothing.
  paging: { page: 0, pageSize: 25, total: 0 },
};
