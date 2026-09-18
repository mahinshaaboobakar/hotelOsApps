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

  it("sets the permission apart in what was asked", () => {
    const body = failureBody(failure("forbidden"), { the: "the rota" }, QATAR);
    const asked = fact(body, "Asked for");

    expect(asked.querySelector("b")?.textContent).toBe("roster.read");
    expect(asked.textContent).toBe("roster.read · week");
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
