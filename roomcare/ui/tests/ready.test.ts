import { describe, expect, it } from "vitest";

import { actions, readyWhen, sheet } from "../chrome/overlay";

/**
 * Page 64 §2 (checklist C11): "A primary action with nothing to send is drawn
 * `off`, with the reason beside it — never live-and-refusing." The audit pressed
 * every overlay's primary the moment it opened: eleven were live with nothing to
 * send — sending an empty person, empty dates, an unchanged routine — or
 * refusing in place. `readyWhen()` holds the primary until the overlay has something to send.
 */
describe("an overlay's primary action", () => {
  it("is drawn off with its reason until the overlay has something to send, then goes live", () => {
    const frame = document.createElement("div");
    document.body.replaceChildren(frame);
    const overlay = sheet(frame, "Grant");
    const person = document.createElement("input");
    overlay.body.append(person);
    let sent = 0;
    actions(overlay, "Grant", () => { sent += 1; });
    readyWhen(overlay, () => (person.value.trim() === "" ? "name the person" : null));

    const foot = overlay.foot;
    expect(foot.querySelector(".btn.pri")).toBeNull();
    expect(foot.querySelector(".btn.off")?.textContent).toBe("Grant — name the person");

    person.value = "u-17";
    person.dispatchEvent(new Event("input", { bubbles: true }));
    const live = foot.querySelector<HTMLButtonElement>(".btn.pri")!;
    expect(live.textContent).toBe("Grant");
    live.click();
    expect(sent).toBe(1);

    person.value = " ";
    person.dispatchEvent(new Event("input", { bubbles: true }));
    expect(foot.querySelector(".btn.pri")).toBeNull();
    expect(foot.querySelector(".btn.off")?.textContent).toBe("Grant — name the person");
  });

  it("stays live, as before, when nothing is asked of it", () => {
    const frame = document.createElement("div");
    const overlay = sheet(frame, "End");
    actions(overlay, "Confirm", () => {});
    expect(overlay.foot.querySelector(".btn.pri")?.textContent).toBe("Confirm");
  });
});
