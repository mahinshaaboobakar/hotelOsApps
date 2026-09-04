/**
 * The form's structure against frame 3 — `CONN-Q12`, *"the UI follows the
 * frame"*.
 *
 * A capture is the rendering guard and this is the other half: it cannot see
 * that a card is the wrong colour, and it can see that a field is in the wrong
 * card. The two defects this suite exists for were both invisible to the build
 * — `esbuild` does not type and does not lay out — and one of them was a
 * comment describing a pairing the code did not do.
 *
 * Tests live here rather than beside the source: ADR 0025, and TypeScript is
 * explicitly not an exception to it.
 */

import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { SECRETS, SETTINGS } from "../configuration";
import type { HostApi } from "@hotelos/sdk";

const GRANTED = ["integration.read", "integration.configure"];

/** What the platform answers for a connector nobody has configured yet. */
const CONFIGURATION = {
  integrationId: "oracle-cloud",
  settings: {},
  configuredSecrets: [],
  propertyTimeZone: "Asia/Kolkata",
};

function host(granted: readonly string[] = GRANTED): HostApi {
  return {
    identity: { id: "pms-oracle", version: "0.1.0", capabilities: granted },

    // The host hands every module its property's zone and locale — the SDK
    // gained this with `JOBS-Q1(8)`, after this suite was written, so `tsc` was
    // already refusing it before this round touched the file. Stated rather
    // than left null: a form that renders an instant should be tested against a
    // property that has one.
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: () => Promise.resolve(CONFIGURATION),
    on: () => () => {},
  };
}

/** Mount the module and let its first call resolve. */
async function mount(granted?: readonly string[]): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.append(root);

  activate(host(granted)).mount(root);
  await new Promise((resolve) => setTimeout(resolve, 0));

  return root;
}

/** The `name` of every field drawn in one card, in document order. */
function fieldsIn(root: HTMLElement, section: string): string[] {
  const card = root.querySelector(`[data-section="${section}"]`);
  expect(card, `no card for ${section}`).not.toBeNull();

  return [...(card as HTMLElement).querySelectorAll("input")].map((input) => input.name);
}

describe("frame 3's cards", () => {
  it("draws the application key in Connection, not in Authentication", async () => {
    const root = await mount();

    // **The regrouping's whole point.** `application-key` is a tenancy
    // identifier that happens to be secret: it travels as `x-app-key` beside
    // `x-hotelid` and `x-externalsystem` on every request, and grouping it with
    // the password grant separated it from the two headers it never travels
    // without. `CONN-Q12`'s `masked = secret, plain = setting` decides which
    // fields are secrets — not which card holds them.
    expect(fieldsIn(root, "connection")).toContain("application-key");
    expect(fieldsIn(root, "authentication")).not.toContain("application-key");
  });

  it("puts each identity next to the secret that proves it", async () => {
    const root = await mount();

    // The grid is two across, so adjacency *is* the pairing. Emitting every
    // setting and then every secret puts the two identities in one row and
    // their two proofs in the next — four unrelated boxes where the frame
    // draws two questions and their answers.
    expect(fieldsIn(root, "authentication")).toEqual([
      "pmsUsername",
      "pms-password",
      "clientId",
      "client-secret",
    ]);
  });

  it("puts the key that proves nothing last, where the frame draws it", async () => {
    const root = await mount();

    // It has no partner because it addresses the tenancy rather than
    // authenticating to it, and having no partner is what puts it at the end —
    // the position follows from what the field is.
    expect(fieldsIn(root, "connection")).toEqual([
      "endpoint",
      "hotelCode",
      "externalSystemCode",
      "application-key",
    ]);
  });

  it("draws every declared field somewhere, and nothing twice", async () => {
    const root = await mount();

    const drawn = ["connection", "authentication", "polling"].flatMap((section) =>
      fieldsIn(root, section),
    );

    // **Derived from the declarations rather than listed here.** A field added
    // to `SETTINGS` or `SECRETS` with a section nobody draws would otherwise
    // vanish silently, which is the failure a form of four cards invites.
    const declared = [...SETTINGS, ...SECRETS]
      .filter((one) => one.section !== "scope")
      .map((one) => one.name);

    expect([...drawn].sort()).toEqual([...declared].sort());
    expect(new Set(drawn).size).toBe(drawn.length);
  });
});

describe("what the cards say", () => {
  it("warns about the Vault under whichever card holds a masked field", async () => {
    const root = await mount();

    // The sentence is about the Token Vault, not about authentication. The
    // application key is a secret and is not a credential, and it needs the
    // same warning — so the note follows the masked fields rather than sitting
    // under one card by habit.
    for (const section of ["connection", "authentication"]) {
      const card = root.querySelector(`[data-section="${section}"]`);
      expect(card?.querySelector(".note")?.textContent).toContain("never read back");
    }
  });

  it("says nothing about the Vault under a card with no secrets", async () => {
    const root = await mount();

    expect(root.querySelector('[data-section="polling"] .note')).toBeNull();
  });
});
