/**
 * The card the guest signs — frame 15.
 */

import type { RegistrationCard } from "../model";

/**
 * A guest from outside the property's home country.
 *
 * The block is showing because the UAE is not this property's home country —
 * **a fact about the guest and the property's setting, not about the country
 * the software runs in.** The same product in Dubai shows it for an Indian
 * guest and hides it for this one.
 */
export const recordedRegistration: RegistrationCard = {
  who: "Checking in · Fatima Sheikh",
  where: "Room 506 · 31 Aug → 4 Sep",
  series: "GRC 2026/08/1152 · the property's series, next number taken on save",

  rows: [
    { kind: "one", field: { label: "Name as on the ID", value: "Fatima Sheikh" } },
    {
      kind: "pair",
      fields: [
        { label: "Date of birth", value: "14 Mar 1986" },
        { label: "Nationality", value: "United Arab Emirates", aside: "▾" },
      ],
    },
    {
      kind: "one",
      field: {
        label: "Permanent address",
        value: "Villa 22, Al Barsha 2 · Dubai · United Arab Emirates",
        tall: true,
      },
    },
    {
      kind: "pair",
      fields: [
        { label: "Identity document", value: "Passport", aside: "▾" },

        // Masked at rest and masked here. The desk needs enough to match a
        // page against a record, not the whole number on a screen behind a
        // counter.
        { label: "Number", value: "P•••••4412" },
      ],
    },
    {
      kind: "pair",
      fields: [
        { label: "Arriving from", value: "Dubai" },
        { label: "Proceeding to", value: "Bengaluru" },
      ],
    },
    {
      kind: "pair",
      fields: [
        { label: "Purpose of visit", value: "Business", aside: "▾" },

        // A field this property does not require, drawn as its own prompt — it
        // is not deleted from the model, because a card must stay readable for
        // years and an unused field is simply not required.
        {
          label: "Vehicle",
          value: null,
          placeholder: "optional at this property",
        },
      ],
    },
  ],

  foreign: {
    title: "Guest from outside",
    because: "shown because UAE is not this property's home country",

    rows: [
      {
        kind: "pair",
        fields: [
          { label: "Passport issue · expiry", value: "02 Feb 2021 · 01 Feb 2031" },
          { label: "Place of issue", value: "Dubai" },
        ],
      },
      {
        kind: "pair",
        fields: [
          { label: "Visa type · number", value: "Business · V•••8890" },
          { label: "Visa expiry", value: "18 Dec 2026" },
        ],
      },
      {
        kind: "pair",
        fields: [
          { label: "Arrived in India", value: "29 Aug 2026" },
          { label: "Port of arrival", value: "Kochi (COK)" },
        ],
      },
    ],
  },

  closing: [
    {
      kind: "one",
      field: {
        label: "Documents",
        value: "Passport page · visa page",
        aside: "2 scanned",
      },
    },
    {
      kind: "one",
      field: {
        label: "Signature",
        value: null,
        placeholder: "Captured on the pad, or printed and scanned",
      },
    },
  ],

  note: "The block above appears because this guest's nationality is not the "
    + "property's own. The property sets its home country and both field lists "
    + "— so a hotel in Kochi treats an Emirati guest this way, and a hotel in "
    + "Dubai treats an Indian guest this way, from the same product with no "
    + "country written into it.",

  obligation: "Filing due 1 Sep (24 h after arrival).",
};
