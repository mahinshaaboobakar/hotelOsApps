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

import { load, recordedAttention, type AttentionCard } from "../../book";
import { el, fill } from "../../chrome/element";
import { mark, standIn } from "../../chrome/marks";
import { actions, card, detail } from "../../chrome/panel";

/**
 * Render the attention list.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 */
export async function attention(host: HostApi, into: HTMLElement): Promise<void> {
  const loaded = await load(host, "reservation.read", "attention", recordedAttention);

  // No page heading, and no `.head` — docs/working/64 §3. This built
  // `<div class="head">` holding `<div class="ht">Attention</div>`, which was
  // wrong twice after the rail became the top bar: it printed the section name
  // the bar already says, and it claimed the class that now MEANS the bar. The
  // sentence underneath is the screen's own and survives, as the drawing keeps
  // it — a hint, not a title.
  const body = el("div", "body");
  if (!loaded.live) body.append(standIn(loaded.because));

  body.append(
    el(
      "div",
      "hint",
      `${count(loaded.value.length)} a person has to decide — nothing here decides itself`,
    ),
  );

  if (loaded.value.length === 0) {
    const clear = card("Nothing waiting");
    clear.body.append(el("div", "hint", "Nothing needs a person."));
    body.append(clear.root);
  }

  for (const item of loaded.value) {
    body.append(one(item));
  }

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
    actions(item.actions),
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
