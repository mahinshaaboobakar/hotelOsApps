import type { HostApi, PropertyEnvironment, ReadFailure } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { failureBody } from "../chrome/failure";
import { failureCard } from "../widgets/card";

/**
 * The failure surface draws the SDK's structured facts, not its finished strings.
 *
 * # What this pins, and why each needs a test
 *
 * **The instant is the property's, not ISO.** `Fact.value` still carries the
 * ISO form for the clipboard, so a surface that draws `value` compiles and
 * renders `2026-09-17T08:53:23.000Z` on a hotel's screen — the one mistake the
 * compiler cannot see, because both are `string`.
 *
 * **The permission and the fault's clause are set apart**, as 64b draws them.
 * They arrive as runs; a surface that draws the rendered `note` or `why` shows
 * the same words with no emphasis, and nothing else would notice.
 *
 * **The card takes `brief` and `onward`.** `why` fits the card too, just badly,
 * and a literal link label would compile — so both are asserted by their words.
 *
 * Every expected string is spelled out, never computed with the formatter or
 * the SDK the code under test calls: an assertion built from the same call as
 * the code is a tautology, not coverage.
 */

/** Qatar, so the instant moves three hours — a UTC rendering cannot pass. */
const QATAR: PropertyEnvironment = { timezone: "Asia/Qatar", locale: "en-GB" };

function failure(cause: ReadFailure["cause"]): ReadFailure {
  return {
    cause,
    capability: "roster.read",
    method: "week",
    said: null,
    at: new Date("2026-09-17T08:53:23.000Z"),
  };
}

function fact(body: HTMLElement, label: string): HTMLElement {
  const row = Array.from(body.querySelectorAll(".fail-fact"))
    .find((node) => node.querySelector(".fail-fk")?.textContent === label);

  if (row === undefined) throw new Error(`no fact labelled ${label}`);
  return row.querySelector(".fail-fv") as HTMLElement;
}

describe("the failure screen's facts", () => {
  it("draws the moment in the property's locale and zone, never as ISO", () => {
    const body = failureBody(failure("unanswered"), { the: "the rota" }, QATAR);

    expect(fact(body, "At").textContent).toBe("17 Sept, 11:53");
  });

  it("names what could not be read, in the screen's own words", () => {
    // **This asserted `roster.read · week`** — the permission's code name and
    // its method, on a live card — until the owner's `64g` §2 B ruling, and
    // the SDK stopped sending it as the fact's value on 2026-09-20
    // (`6751c7de`). The code name stays on `Fact.permission` and on `wire`,
    // which the SDK documents as *not for drawing*; this surface was reading
    // it anyway, alone among the four applications.
    //
    // Recorded rather than quietly replaced: a test asserting a contract that
    // has moved is ADR 0034, and the old assertion is what a reader needs in
    // order to know the change was deliberate.
    const body = failureBody(failure("forbidden"), { the: "the rota" }, QATAR);
    const asked = fact(body, "Asked for");

    expect(asked.textContent).toBe("the rota");
    expect(asked.textContent).not.toContain("roster.read");
  });

  it("sets the permission apart in the refusal's note", () => {
    const body = failureBody(failure("forbidden"), { the: "the rota" }, QATAR);
    const note = body.querySelector(".fail-note") as HTMLElement;

    expect(note.querySelector("b")?.textContent).toBe("roster.read");
    expect(note.textContent).toBe(
      "This screen needs roster.read, and no grant at this property names this user.",
    );
  });

  it("sets the fault's clause apart, as 64b draws it", () => {
    const body = failureBody(failure("faulted"), { the: "the rota" }, QATAR);
    const why = body.querySelector(".fail-why") as HTMLElement;

    expect(why.querySelector("b")?.textContent).toBe("This is not something the property has done");
  });
});

describe("contract v2's three states, as 64e draws them", () => {
  // The two local refusals have different fixes, so their notes must name
  // different things — the application, or the account. A surface that drew
  // one note for both would hide which of them needs attention (SHELL-Q59).
  it("names the application when the application is not admitted", () => {
    const body = failureBody(failure("unadmitted"), { the: "the rota" }, QATAR);
    const note = body.querySelector(".fail-note") as HTMLElement;

    expect(note.textContent).toBe("This application was not granted roster.read at this property.");
    expect(note.querySelector("b")?.textContent).toBe("roster.read");
    expect(body.querySelector(".fail-mark.fail-unadmitted")).not.toBeNull();
  });

  it("names the account when the account lacks the grant", () => {
    const body = failureBody(failure("ungranted"), { the: "the rota" }, QATAR);
    const note = body.querySelector(".fail-note") as HTMLElement;

    expect(note.textContent).toBe("This account has not been granted roster.read at this property.");
    expect(body.querySelector(".fail-mark.fail-ungranted")).not.toBeNull();
  });

  it("names the model and never the person when the model cannot decide", () => {
    const body = failureBody(failure("undecidable"), { the: "the rota" }, QATAR);
    const said = body.querySelector(".fail-said")?.textContent ?? "";

    expect(said).toBe("Access to the rota could not be checked");
    expect(said).not.toMatch(/\byou\b|account/iu);
    expect(body.querySelector(".fail-why b")?.textContent)
      .toBe("the model it has loaded cannot decide this permission");
    expect(body.querySelector(".fail-acts button")?.textContent).toBe("Copy these details");
  });
});

describe("the widget's failure card", () => {
  const reach = {
    host: {} as HostApi,
    opens: "rota",
    again: () => Promise.resolve(document.createElement("div")),
  };

  it("says the card's own sentence and offers to open the application", () => {
    const card = failureCard("Shift board", failure("forbidden"), { the: "the rota" }, reach);

    expect(card.querySelector(".wf-why")?.textContent)
      .toBe("The service refused. Nothing is broken; a permission is missing.");
    expect(card.querySelector(".wf-open")?.textContent).toBe("Open Workforce →");
  });

  it("drops the subject from a fault's headline, and keeps it for a refusal", () => {
    const fault = failureCard("Shift board", failure("faulted"), { the: "the rota" }, reach);
    const refusal = failureCard("Shift board", failure("forbidden"), { the: "the rota" }, reach);

    expect(fault.querySelector(".wf-said")?.textContent).toBe("Workforce could not build this");
    expect(refusal.querySelector(".wf-said")?.textContent).toBe("You do not have access to the rota");
  });

  it("offers to try again only where the read went unanswered", () => {
    const card = failureCard("Shift board", failure("unanswered"), { the: "the rota" }, reach);

    expect(card.querySelector(".wf-open")?.textContent).toBe("Try again →");
  });
});
