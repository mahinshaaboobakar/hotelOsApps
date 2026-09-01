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
}

export const recordedPeople: People = {
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
  ],
};
