/**
 * The card the guest signs — frame 15.
 *
 * **Reshaped when the card stopped being a drawing.** It carried rendered
 * values — `14 Mar 1986`, `Passport issue · expiry` as one box, `Arrived in
 * India` — and now carries what the service sends: ISO days, one box per stored
 * column, and a label that names no country. A fixture in a different shape
 * from the wire is a harness photographing itself, which is the fault this
 * application has already paid for three times.
 *
 * **The long note under the block is gone, and it was a mock's note.** It ended
 * *"from the same product with no country written into it"* — a sentence
 * addressed to whoever was reviewing the frame, not to the receptionist reading
 * the card. The `because` line tells the person at the desk why the block is
 * there; the rest was for us (owner ruling, 2026-09-19).
 */

import type { CardField, RegistrationCard } from "../model";

/** A box, with the defaults every one of them shares. */
function box(
  name: string,
  label: string,
  value: string | null,
  extra: Partial<CardField> = {},
): CardField {
  return { name, label, kind: "text", value, required: false, ...extra };
}

/**
 * A guest from outside the property's home country.
 *
 * The block is showing because AE is not this property's home country — **a
 * fact about the guest and the property's setting, not about the country the
 * software runs in.** The same product in Dubai shows it for an Indian guest
 * and hides it for this one.
 */
export const recordedRegistration: RegistrationCard = {
  stayId: "9f3b1c4e-0000-4000-8000-000000000015",
  who: "Fatima Sheikh",
  room: "506",
  version: 3,
  arriving: true,
  arrive: "2026-08-31",
  depart: "2026-09-04",
  homeCountry: "IN",

  series: { number: "GRC 2026/08/1152", taken: false },

  rows: [
    {
      kind: "one",
      field: box("name_as_on_id", "Name as on the ID", "Fatima Sheikh", { required: true }),
    },
    {
      kind: "pair",
      fields: [
        box("date_of_birth", "Date of birth", "1986-03-14", { kind: "date" }),
        box("nationality", "Nationality", "AE", {
          required: true,
          placeholder: "two-letter country code",
        }),
      ],
    },
    {
      kind: "one",
      field: box("address_line", "Permanent address", "Villa 22, Al Barsha 2", { tall: true }),
    },
    {
      kind: "pair",
      fields: [box("city", "City", "Dubai"), box("state", "State or province", null)],
    },
    {
      kind: "pair",
      fields: [
        box("country", "Country", "AE", { placeholder: "two-letter country code" }),
        box("postal_code", "Postal code", null),
      ],
    },
    {
      kind: "pair",
      fields: [
        // The one chooser on the card, over the property's own accepted list.
        box("id_type", "Identity document", "Passport", {
          kind: "choice",
          choices: ["Passport", "Aadhaar", "Driving licence"],
          required: true,
        }),

        // Masked at rest and whole underneath: the desk holding the passport
        // has to be able to retype it, and a mask that cannot be corrected is a
        // value the product can read and the property cannot fix.
        box("id_number", "Number", "P12344412", {
          masked: "P•••••4412",
          required: true,
        }),
      ],
    },
    {
      kind: "pair",
      fields: [
        box("id_issuer", "Issued by", null),
        box("id_expiry", "Expires", null, { kind: "date" }),
      ],
    },
    {
      kind: "pair",
      fields: [
        box("arriving_from", "Arriving from", "Dubai"),
        box("proceeding_to", "Proceeding to", "Bengaluru"),
      ],
    },
    {
      kind: "pair",
      fields: [
        box("purpose_of_visit", "Purpose of visit", "Business"),

        // A field this property does not require, drawn with its own prompt —
        // it is not deleted from the model, because a card must stay readable
        // for years and an unused field is simply not required.
        box("vehicle_number", "Vehicle", null, { placeholder: "optional at this property" }),
      ],
    },
  ],

  foreign: {
    title: "Guest from outside",
    because: "shown because AE is not IN, this property's home country",

    rows: [
      {
        kind: "pair",
        fields: [
          box("passport_number", "Passport number", "P12344412", {
            masked: "P•••••4412",
            required: true,
          }),
          box("passport_place", "Place of issue", "Dubai"),
        ],
      },
      {
        kind: "pair",
        fields: [
          box("passport_issue", "Passport issue", "2021-02-02", { kind: "date" }),
          box("passport_expiry", "Passport expiry", "2031-02-01", { kind: "date" }),
        ],
      },
      {
        kind: "pair",
        fields: [
          box("visa_type", "Visa type", "Business"),
          box("visa_number", "Visa number", "V1238890", { masked: "V•••8890" }),
        ],
      },
      {
        kind: "pair",
        fields: [
          box("visa_issue", "Visa issue", null, { kind: "date" }),
          box("visa_expiry", "Visa expiry", "2026-12-18", { kind: "date" }),
        ],
      },
      {
        kind: "pair",
        fields: [
          // The label names no country. Which country a guest arrived in is the
          // property's own, and writing one here is the defect this application
          // refuses everywhere else.
          box("arrived_in_country_on", "Arrived in the country", "2026-08-29", { kind: "date" }),
          box("port_of_arrival", "Port of arrival", "Kochi (COK)"),
        ],
      },
    ],
  },

  closing: [
    {
      kind: "one",
      field: box("documents", "Documents", "Passport page · visa page", {
        kind: "held",
        placeholder: "none scanned",
      }),
    },
    {
      kind: "one",
      field: box("signature", "Signature", null, {
        kind: "held",
        placeholder: "captured on the pad, or printed and scanned",
      }),
    },
  ],

  missing: [],

  obligation: { at: "2026-09-01T06:30:00Z", hours: 24 },

  signedAt: null,
};
