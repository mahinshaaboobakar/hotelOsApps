/**
 * Attention — the honest list. Gold frame 12.
 *
 * **Not a defect log.** Every row is an ordinary condition of running a hotel
 * against a PMS feed: two records that may be one stay, a value the feed
 * disagrees about, a cancellation that arrived for a guest who is in the room,
 * a group whose other papers have not come. None of them is a fault, and none
 * of them resolves itself.
 *
 * Each card names the **class of problem** in its band, shows the two sides as
 * label–value rows, and says *why it is here* — the reason chips are the part
 * that makes the list honest rather than mysterious. `names look alike` is
 * amber on purpose: names **rank** the list and can never join two stays, and
 * the colour is what stops a person reading it as evidence.
 */

import type { HostApi } from "@hotelos/sdk";

import { APP, failureDrawing, load, type AttentionCard, type AttentionPage } from "../../book";
import { pager } from "../../chrome/pager";
import { el, fill } from "../../chrome/element";
import { mark, failed } from "../../chrome/marks";
import { actions, card, detail } from "../../chrome/panel";

/**
 * Render the attention list.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param page which page, 0-based
 * @param turn what to do when another page is chosen
 */
/** Cards per page. Fewer than a table's rows: each is several lines tall. */
const PAGE = 10;

export async function attention(
  host: HostApi,
  into: HTMLElement,
  page: number,
  turn: (page: number) => void,
): Promise<void> {
  const loaded = await load<AttentionPage>(host, "reservation.read", "attention", {
    page,
    pageSize: PAGE,
  });

  // **A read that did not answer renders the failure, not a stand-in** —
  // APPS-Q42. Nothing below this line runs on data nobody's platform produced.
  //
  // The retry is offered only where the SDK says one could succeed: this list
  // is a page, so re-asking for the same page is the whole of it.
  if (!loaded.ok) {
    into.replaceChildren(
      failed(
        failureDrawing(loaded.failure, { app: APP, the: "this property's attention list" }),
        () => turn(page),
      ));
    return;
  }

  // No page heading, and no `.head` — docs/working/64 §3. This built
  // `<div class="head">` holding `<div class="ht">Attention</div>`, which was
  // wrong twice after the rail became the top bar: it printed the section name
  // the bar already says, and it claimed the class that now MEANS the bar. The
  // sentence underneath is the screen's own and survives, as the drawing keeps
  // it — a hint, not a title.
  const body = el("div", "body");

  body.append(
    el(
      "div",
      "hint",
      `${count(loaded.value.total)} a person has to decide — nothing here decides itself`,
    ),
  );

  // **The cards are ONE list element, the pager's sibling** — CORE-Q28: only the
  // list scrolls, so there has to be one list to scroll. They were loose
  // children of the body, and a loose stack is a body that scrolls: the page-64
  // audit measured the document scrolling and the pager 184px below the window.
  const stack = el("div", "stack");

  if (loaded.value.total === 0) {
    const clear = card("Nothing waiting");
    clear.body.append(el("div", "hint", "Nothing needs a person."));
    stack.append(clear.root);
  }

  for (const item of loaded.value.cards) {
    stack.append(one(item));
  }

  body.append(stack);

  // A pager over cards, not rows — `64` §8 asks every list screen for one, and
  // a stack of cards a person scans is a list by the only test that matters.
  const turning = pager(
    loaded.value.total, page, PAGE, loaded.value.cards.length, turn, host.property);
  if (turning !== null) body.append(turning);

  into.replaceChildren(body);
}

/** One card: the band, the two sides, why it is here, and the ways out. */
function one(item: AttentionCard): HTMLElement {
  const { root, body } = card(item.kind, aside(item.status));

  for (const row of item.rows) {
    body.append(detail({ ...row, tags: row.tags }));
  }

  fill(
    body,
    item.note === null ? null : el("div", "note", item.note),
    item.hint === null ? null : el("div", "hint", item.hint),
    actions(item.actions, "Settling this from GuestOps is not available yet."),
  );

  return root;
}

/** The right of the band: a chip where the design gives one, else plain text. */
function aside(status: AttentionCard["status"]): Node | string | undefined {
  if (status === null) return undefined;
  return typeof status === "string" ? status : mark(status);
}

/**
 * The subtitle's count, written out.
 *
 * The design says "Four things"; the number is the list's length, so the screen
 * cannot claim four while showing three.
 */
function count(n: number): string {
  const words = ["Nothing", "One thing", "Two things", "Three things", "Four things"];
  return words[n] ?? `${n} things`;
}
