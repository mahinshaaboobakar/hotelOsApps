/**
 * The stay's other five tabs — frames 4, 5, 5b, 6 and 7.
 */

import type { Activity, Payment, Requests, Servicing } from "../model";

/**
 * Frame 4 — fourteen things that happened, from three kinds of source.
 *
 * **This is the screen that answers a complaint.** Why does it say 214? Who
 * moved the departure? Was the guest's ID taken? A list showing only GuestOps's
 * own facts would answer none of the questions a duty manager asks at 9 p.m.,
 * because half the story belongs to Opera, Room Care and Jobs.
 *
 * What makes it safe is that **nothing is copied**: our rows come from our own
 * event stream, and the other applications' rows are resolved live.
 */
export const recordedActivity: Activity = {
  filters: [
    { label: "Everything", on: true },
    { label: "Ours", on: false },
    { label: "Opera", on: false },
    { label: "Other apps", on: false },
  ],

  entries: [
    {
      date: "28 Aug", time: "09:14",
      who: { mark: "pms", text: "Opera" },
      what: "Booked — 4 nights, Deluxe King, ₹ 8 400.00 per night",
      detail: "reservation 84119377 · business day 28 Aug · arrived via the Integration Hub",
      disagrees: false,
    },
    {
      date: "29 Aug", time: "17:02",
      who: { mark: "pms", text: "Opera" },
      what: "Amended — departure moved from 2 Sep to 4 Sep",
      detail: "the fetched state decided this, not the source's event type",
      disagrees: false,
    },
    {
      date: "30 Aug", time: "21:40",
      who: { mark: "other", text: "Room Care" },
      what: "Room 214 inspected and released",
      detail: "read through the Context Service · GuestOps stores none of this",
      disagrees: false,
    },
    {
      date: "31 Aug", time: "14:10",
      who: { mark: "override", text: "Anitha M." },
      what: "Assigned room 214",
      detail: "override — Opera held no assignment at that moment",
      disagrees: false,
    },
    {
      date: "31 Aug", time: "14:10",
      who: { mark: "override", text: "Anitha M." },
      what: "Checked in",
      detail: "override · arrival time observed, not derived",
      disagrees: false,
    },
    {
      date: "31 Aug", time: "14:12",
      who: { mark: "override", text: "Anitha M." },
      what: "Registration captured — GRC 2026/08/1147",
      detail: "ID scanned · signature on file",
      disagrees: false,
    },
    {
      date: "31 Aug", time: "14:35",
      who: { mark: "override", text: "Anitha M." },
      what: 'Request logged — "AC not cooling"',
      detail: "raised as a job in Jobs at 14:40",
      disagrees: false,
    },
    {
      date: "31 Aug", time: "15:06",
      who: { mark: "other", text: "Jobs" },
      what: "JOB-8821 assigned to Engineering — in progress",
      detail: "read through the Context Service",
      disagrees: false,
    },
    {
      date: "31 Aug", time: "15:30",
      who: { mark: "disagrees", text: "Opera" },
      what: "Reported room 208 — differs from ours (214)",
      detail: "recorded as a disagreement · not applied · your entry stands",
      disagrees: true,
    },
    {
      date: "1 Sep", time: "10:20",
      who: { mark: "other", text: "Room Care" },
      what: "Stayover service completed by Suma T.",
      detail: "read through the Context Service",
      disagrees: false,
    },
  ],
};

/**
 * Frame 5 — the request is ours, the job is Jobs'.
 *
 * **Not every request is a job.** A late checkout is answered at the desk, and
 * it still lives here: a request is a fact about the guest's stay whether or
 * not any work follows from it.
 */
export const recordedRequests: Requests = {
  ours: [
    {
      key: "14:35", what: "AC not cooling",
      state: "raised as JOB-8821", stateTone: "warn", note: null,
    },
    {
      key: "16:02", what: "Late checkout on 4 Sep",
      state: "no job needed", stateTone: "neutral", note: null,
    },
  ],

  jobs: [
    {
      key: "JOB-8821", what: "AC not cooling · room 214",
      state: "In progress", stateTone: "warn", note: null,
    },
    {
      key: "Assigned", what: "Engineering · Ramesh K.",
      state: null, stateTone: "neutral", note: "since 15:06",
    },
    {
      key: "Raised by", what: "Anitha Menon, from this stay",
      state: null, stateTone: "neutral", note: null,
    },
  ],

  jobsInstalled: true,
};

/**
 * Frame 5b — the same tab with Jobs absent.
 *
 * **The tab is renamed, not emptied.** The request is still recorded; what
 * disappears is the raising, not the guest's complaint. That is the owner's
 * ruling of 2026-08-31 in one screen: *an application's own flow is never gated
 * on another application being installed — an absent dependency loses its
 * capability, never the flow.*
 */
export const recordedRequestsAlone: Requests = {
  ours: [
    { key: "14:35", what: "AC not cooling", state: "logged", stateTone: "neutral", note: null },
    {
      key: "16:02", what: "Late checkout on 4 Sep",
      state: "logged", stateTone: "neutral", note: null,
    },
  ],

  jobs: null,
  jobsInstalled: false,
};

/**
 * Frame 6 — four nights, and none of it is ours.
 *
 * Three facts from the PMS study are why the strip is per night rather than one
 * status. A room that sat empty before arrival is **freshened, not turned
 * around**. A room whose guest is due out and into which nobody arrives tonight
 * is **stripped rather than made ready** — a decision that needs to know when
 * the room is next sold, which no room status can tell you (R3). And **a
 * declined day is neither clean nor dirty; it is declined** (R1).
 */
export const recordedServicing: Servicing = {
  roomCareInstalled: true,

  nights: [
    {
      weekday: "Sun", date: "31 Aug", qualifier: "arrival", now: false,
      mark: { mark: "other", text: "Prepared before arrival" },
      state: null, stateTone: "neutral",
      detail: "Inspected 21:40 the night before · the room sat vacant, so it was "
        + "freshened rather than fully cleaned",
      action: null,
    },
    {
      weekday: "Mon", date: "1 Sep", qualifier: null, now: false,
      mark: { mark: "other", text: "Serviced 10:20" },
      state: null, stateTone: "neutral",
      detail: "Suma T. · stayover service",
      action: null,
    },
    {
      weekday: "Tue", date: "2 Sep", qualifier: "today", now: false,
      mark: null,
      state: "Not serviced", stateTone: "warn",
      detail: "Do-not-disturb at 09:50 and 13:15 · guest declined",
      action: "Ask again this evening",
    },
    {
      weekday: "Wed", date: "3 Sep", qualifier: null, now: true,
      mark: null,
      state: "Planned", stateTone: "neutral",
      detail: "Linen change due — third night",
      action: null,
    },
    {
      weekday: "Thu", date: "4 Sep", qualifier: "departure", now: false,
      mark: null,
      state: "Planned after 11:00", stateTone: "neutral",
      detail: "Nobody arrives into 214 that night, so it is planned as a strip "
        + "rather than a turnaround",
      action: null,
    },
  ],
};

/**
 * Frame 7 — the terms, and the folio that is a reported finding.
 *
 * **Every amount carries three things** — value, currency, and whether tax is
 * included (R19) — because the reference system wrote one vendor's before-tax
 * figure and another's gross figure into one column, and nothing anywhere
 * recorded which.
 */
export const recordedPayment: Payment = {
  terms: [
    {
      label: "Rate", value: "", strong: "₹ 8 400.00", tail: " INR per night", big: true,
      tags: [
        { kind: "lock", tone: "neutral", text: "GROSS — TAX INCLUDED" },
        { kind: "lock", tone: "neutral", text: "FROM OPERA" },
      ],
    },
    {
      label: "Four nights", value: "", strong: "₹ 33 600.00", tail: " INR", big: true,
      tags: [{ kind: "lock", tone: "neutral", text: "GROSS" }],
    },
    {
      label: "Rate plan", value: "BAR-FLEX · Best Available, flexible", tags: [],
    },
    {
      label: "Guarantee", value: "Credit card ",
      tags: [
        { kind: "pill", tone: "neutral", text: "holds inventory" },
        { kind: "pill", tone: "neutral", text: "on hold" },
      ],
    },
    {
      label: "Deposit policy", value: "30% · due ", strong: "7 days after booking",
      tail: " → 4 Sep",
      tags: [{ kind: "lock", tone: "neutral", text: "COMPUTED FROM OFFSET" }],
    },
    {
      label: "Cancellation", value: "1 night if within ", strong: "48 h of arrival",
      tail: ", drop 18:00 → Sat 30 Aug 14:00",
      tags: [{ kind: "pill", tone: "warn", text: "deadline passed" }],
    },
    {
      label: "Penalty if cancelled", value: "", strong: "₹ 8 400.00", tail: " INR",
      tags: [
        { kind: "lock", tone: "neutral", text: "GROSS" },
        { kind: "lock", tone: "neutral", text: "1 NIGHT" },
      ],
    },
  ],

  note: "The deadlines are computed, never stored. The record holds “48 hours "
    + "before arrival”; move the arrival and the deadline moves with it. A stored "
    + "deadline silently stops matching its reservation, and that is a chargeable "
    + "error.",

  folio: [
    { label: "Deposit received", because: "NEEDS FINANCE OR A CONNECTOR CAPABILITY" },
    { label: "Room & tax posted", because: "NOT AVAILABLE" },
    { label: "Extras", because: "NOT AVAILABLE" },
    { label: "Balance due", because: "NOT AVAILABLE" },
    { label: "Settle · invoice", because: "FINANCE, A LATER ROUND" },
  ],

  folioNote: "Two different gaps, and they need two different answers. In a "
    + "PMS-connected property the folio lives in Opera and the desk settles there "
    + "— showing it here needs the connector to carry a balance, a capability v1's "
    + "inbound contract does not include. In a standalone property there is no "
    + "Opera, so settlement happens nowhere in v1 — a consequence accepted "
    + "knowingly with GUEST-Q6, and the reason the first deployments are "
    + "PMS-connected.",
};

/**
 * Frame 6's other state — Room Care absent.
 *
 * The tab is **dimmed and still reachable**, so a property can open it and read
 * why it is empty. A tab that vanished would take the information with it.
 */
export const recordedServicingAlone: Servicing = {
  nights: null,
  roomCareInstalled: false,
};
