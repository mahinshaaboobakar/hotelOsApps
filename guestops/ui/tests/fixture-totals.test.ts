import { describe, expect, it } from "vitest";

import {
  recordedAttention,
  recordedBooking,
  recordedGroup,
} from "../book/recorded";

/**
 * A fixture's `total` is the length of what it holds, and nothing else.
 *
 * # Why this exists
 *
 * The pager draws `showing 1–n of total`, so `total` is a claim about how many
 * rows the list has. In a fixture there is exactly one honest value for it —
 * the number of rows the fixture carries — and any other number is the defect
 * this round was called for: a count nobody produced, rendered with confidence.
 *
 * It was written because the author made that mistake while fixing it. Adding
 * `total` to two fixtures by hand, one was right and one said `2` over a single
 * stay, and nothing would have caught it: the screen would have drawn
 * `showing 1–1 of 2` and a person would have looked for a stay that was not
 * there.
 *
 * **Derived, so a fixture that grows a row cannot pass.** The lengths are read
 * off the same objects the screens render, never restated here.
 */
describe("a fixture claims only the rows it has", () => {
  it("attention", () => {
    expect(recordedAttention.total).toBe(recordedAttention.cards.length);
  });

  it("a booking", () => {
    expect(recordedBooking.total).toBe(recordedBooking.stays.length);
  });

  it("a group booking", () => {
    expect(recordedGroup.total).toBe(recordedGroup.stays.length);
  });
});
