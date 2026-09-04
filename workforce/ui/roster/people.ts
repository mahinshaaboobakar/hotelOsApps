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
  who: string;

  /** When the posting began, and whether the person holds more than one. */
  since: string;

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
      who: "Priya Thomas", since: "Since 12 Mar 2023", departments: ["FO"],
      zone: "Zone 1", role: "Supervisor", reportsTo: "— department head",
      capability: "3 valid", tone: "ok",
    },
    {
      who: "Anjali Menon", since: "Since 4 Jan 2025", departments: ["FO"],
      zone: "Zone 3", role: "Receptionist", reportsTo: "Priya Thomas",
      capability: "1 expiring", tone: "warn",
    },
    {
      who: "Vishnu Das", since: "Since 19 Aug 2021", departments: ["FO"],
      zone: "Zone 1", role: "Night auditor", reportsTo: "Priya Thomas",
      capability: "2 valid", tone: "ok",
    },
    // Two postings, and an expired certification which is shown, named, and
    // blocks nothing — WF-Q16's judgment side.
    {
      who: "Rajan Pillai", since: "Since 2 Feb 2020 · 2 postings",
      departments: ["KIT", "BQT"], zone: "Zone 5", role: "Sous chef",
      reportsTo: "Mathew George", capability: "1 expired", tone: "bad",
    },
    {
      who: "Rahul Nair", since: "Since 8 Jun 2024", departments: ["SEC"],
      zone: "Zone 1", role: "Security officer", reportsTo: "Thomas Varghese",
      capability: "4 valid", tone: "ok",
    },
    {
      who: "Sneha Iyer", since: "Since 30 Nov 2025", departments: ["FO"],
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
      who: "Mathew George", since: "Since 3 Mar 2019", departments: ["KIT"],
      zone: "Zone 5", role: "Executive chef", reportsTo: "— department head",
      capability: "5 valid", tone: "ok",
    },
    {
      who: "Thomas Varghese", since: "Since 14 Jul 2018", departments: ["SEC"],
      zone: "Zone 1", role: "Security manager", reportsTo: "— department head",
      capability: "2 valid", tone: "ok",
    },
    {
      who: "Deepa Menon", since: "Since 21 Sep 2022", departments: ["HK"],
      zone: "Zone 4", role: "Housekeeping supervisor", reportsTo: "— department head",
      capability: "3 valid", tone: "ok",
    },
    {
      who: "Arun Kumar", since: "Since 5 May 2023", departments: ["HK"],
      zone: "Zone 4", role: "Room attendant", reportsTo: "Deepa Menon",
      capability: "1 expiring", tone: "warn",
    },
    {
      who: "Lakshmi Nair", since: "Since 12 Aug 2024", departments: ["HK"],
      zone: "Zone 6", role: "Room attendant", reportsTo: "Deepa Menon",
      capability: "2 valid", tone: "ok",
    },
    {
      who: "Jose Mathew", since: "Since 2 Jan 2021", departments: ["ENG"],
      zone: "Zone 1", role: "Maintenance technician", reportsTo: "Suresh Babu",
      capability: "4 valid", tone: "ok",
    },
    {
      who: "Suresh Babu", since: "Since 8 Nov 2017", departments: ["ENG"],
      zone: "Zone 1", role: "Chief engineer", reportsTo: "— department head",
      capability: "6 valid", tone: "ok",
    },
    {
      who: "Fathima Rasheed", since: "Since 17 Feb 2025", departments: ["FB"],
      zone: "Zone 3", role: "Server", reportsTo: "Nikhil Varma",
      capability: "none recorded", tone: "neu",
    },
    {
      who: "Nikhil Varma", since: "Since 29 Apr 2020", departments: ["FB"],
      zone: "Zone 3", role: "Restaurant manager", reportsTo: "— department head",
      capability: "3 valid", tone: "ok",
    },
    {
      who: "Meera Suresh", since: "Since 6 Jun 2023", departments: ["FB"],
      zone: "Zone 3", role: "Server", reportsTo: "Nikhil Varma",
      capability: "1 expiring", tone: "warn",
    },
    {
      who: "Aravind Pillai", since: "Since 23 Oct 2021", departments: ["KIT"],
      zone: "Zone 5", role: "Commis chef", reportsTo: "Mathew George",
      capability: "2 valid", tone: "ok",
    },
    {
      who: "Sara Thomas", since: "Since 11 Dec 2024", departments: ["SPA"],
      zone: "Zone 7", role: "Therapist", reportsTo: "Divya Krishnan",
      capability: "3 valid", tone: "ok",
    },
    {
      who: "Divya Krishnan", since: "Since 4 Apr 2019", departments: ["SPA"],
      zone: "Zone 7", role: "Spa manager", reportsTo: "— department head",
      capability: "4 valid", tone: "ok",
    },
    {
      who: "Manoj Kurup", since: "Since 19 Jan 2022", departments: ["SEC"],
      zone: "Zone 2", role: "Security officer", reportsTo: "Thomas Varghese",
      capability: "1 expired", tone: "bad",
    },
    {
      who: "Reshma Anil", since: "Since 27 Jul 2025", departments: ["FO"],
      zone: "Zone 1", role: "Guest relations", reportsTo: "Priya Thomas",
      capability: "none recorded", tone: "neu",
    },
    {
      who: "Gopal Menon", since: "Since 15 Mar 2020", departments: ["ENG"],
      zone: "Zone 6", role: "Electrician", reportsTo: "Suresh Babu",
      capability: "3 valid", tone: "ok",
    },
    {
      who: "Anu Jacob", since: "Since 9 Sep 2023", departments: ["HK"],
      zone: "Zone 4", role: "Linen attendant", reportsTo: "Deepa Menon",
      capability: "2 valid", tone: "ok",
    },
    {
      who: "Vinod Raj", since: "Since 1 Feb 2024", departments: ["FB"],
      zone: "Zone 3", role: "Bartender", reportsTo: "Nikhil Varma",
      capability: "1 valid", tone: "ok",
    },
    {
      who: "Kavya Nambiar", since: "Since 22 May 2025", departments: ["KIT"],
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
