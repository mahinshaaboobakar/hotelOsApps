/**
 * The settings screen, and the first run before any of it matters. Frames 16, 13.
 */

import type { FirstRun, Setup } from "../model";

/**
 * Install day on a PMS-connected property — frame 13.
 *
 * The Hub has been holding normalised reservation and guest facts since the
 * connector shipped, **deferred**, because this domain did not exist to own
 * them. Installing GuestOps turns that queue on, and the replay is idempotent
 * by construction: a fact already applied changes nothing and publishes
 * nothing.
 */
export const recordedFirstRun: FirstRun = {
  headline: "Bringing in what Opera already sent",
  what: "2 314 reservations and 1 806 guests",
  since: "have been held since the Opera connector was configured on 12 August. "
    + "They are replaying now, in the order they happened — arrivals, moves and "
    + "departures alike.",
  reassurance: "This runs once. You can start working as soon as today's "
    + "arrivals appear.",
};

/**
 * Frame 16 — the screen a general manager sets up once.
 *
 * Every deadline here is an **offset**, every list is the **property's**, and
 * the one thing the platform cannot do is stated on the screen rather than
 * drawn as a button.
 */
export const recordedSetup: Setup = {
  sections: [
    { label: "Registration", on: true },
    { label: "Guest reporting", on: false },
    { label: "Stop-sell", on: false },
    { label: "Stay defaults", on: false },
  ],

  lead: {
    title: "Stop-sell",
    aside: "the seller's control — not an inventory fact",

    rows: [
      {
        label: "Executive Suite",
        value: "3 Sep → 7 Sep · 4 rooms · ",
        quiet: "wedding party",
        note: "set by Rakesh Varma, 22 Aug",
        tags: [],
      },
      {
        label: "Deluxe Twin",
        value: "14 Oct → 20 Oct · all · ",
        quiet: "corridor renovation",
        tags: [],
      },
    ],

    hint: "This says “we choose not to sell”, never “this room cannot be "
      + "used.” A room that is genuinely unusable is out of order — "
      + "EngineeringOps's to declare and ours only to hear. Two sentences, two "
      + "owners, and availability subtracts both.",

    note: null,
    actions: ["＋ Close a room type for dates"],
  },

  pair: [
    {
      title: "Guest reporting",
      aside: { kind: "pill", tone: "ok", text: "on" },

      rows: [
        {
          label: "Applies to",
          value: "Guests from outside the home country",
          tags: [{ kind: "lock", tone: "neutral", text: "OR EVERY GUEST" }],
        },
        {
          label: "Authority",
          value: "Kerala Police — the property names its own",
          tags: [],
        },
        {
          label: "Due",
          value: "",
          strong: "24 hours",
          tail: " after arrival",
          tags: [{ kind: "lock", tone: "neutral", text: "AN OFFSET, NOT A DATE" }],
        },
        {
          label: "Who may file",
          value: "Front Office Manager · Duty Manager",
          tags: [{ kind: "lock", tone: "neutral", text: "reporting.file" }],
        },
        {
          label: "How it is sent",
          value: "",
          // The refusal is the point of the row, so it is the whole value.
          tags: [{ kind: "lock", tone: "bad", text: "BY A PERSON, ON THE AUTHORITY'S PORTAL" }],
        },
      ],

      hint: null,

      note: "HotelOS does not submit anything. This screen sets the policy, "
        + "raises the flag and records what was filed — the authority, the "
        + "reference, who filed it and when. Sending it automatically is an "
        + "integration, and every integration on this platform is a connector; "
        + "that one does not exist and is not pretended here.",

      actions: [],
    },
    {
      title: "Due to file",
      aside: "3 stays",

      rows: [
        {
          label: "Fatima Sheikh",
          value: "arrived 31 Aug · ",
          tags: [{ kind: "pill", tone: "warn", text: "due 1 Sep" }],
        },
        {
          label: "Daniel Fernandes",
          value: "arrived 31 Aug · ",
          tags: [{ kind: "pill", tone: "warn", text: "due 1 Sep" }],
        },
        {
          label: "Chen Wei",
          value: "arrived 29 Aug · ",
          tags: [{ kind: "pill", tone: "bad", text: "overdue" }],
        },
        {
          label: "Filed today",
          value: "2",
          note: "by Anitha Menon, ref KP/2026/8841",
          tags: [],
        },
      ],

      hint: "Overdue is shown, never enforced. Chen Wei is checked in, served "
        + "and will check out on time — the platform says what is owed and "
        + "stops nothing.",

      note: null,
      actions: ["Record a filing", "Open the list"],
    },
  ],

  card: {
    title: "Registration card",

    left: [
      {
        label: "Home country",
        value: "",
        strong: "India",
        tags: [{ kind: "lock", tone: "neutral", text: "DECIDES WHO IS “FROM OUTSIDE”" }],
      },
      {
        label: "GRC series",
        value: "2026/08/•••• · resets yearly · next 1153",
        tags: [],
      },
      {
        label: "Accepted IDs",
        value: "the property's own list",
        tags: [{ kind: "lock", tone: "neutral", text: "SEEDED FOR ITS COUNTRY" }],
      },
      { label: "Signature", value: "Required · pad or scan", tags: [] },
      {
        label: "Print at check-in",
        value: "Yes",
        tags: [{ kind: "lock", tone: "neutral", text: "PLATFORM PRINT SURFACE" }],
      },
    ],

    right: [
      {
        label: "Required · home country",
        value: "name · address · ID type & number · purpose",
        tags: [],
      },
      {
        label: "Required · from outside",
        value: "the above ",
        strong: "+",
        tail: " passport, visa, arrival in country, port",
        tags: [],
      },
    ],

    hint: "Two required sets, and the property decides both — which is what "
      + "lets one product serve a hotel in Kochi and a hotel in Dubai without a "
      + "country written into it. Everything not required stays on the card as "
      + "optional; it is never removed from the record.",
  },
};
