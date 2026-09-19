/**
 * The department picker, drawn off — every department shown, none choosable.
 *
 * Five screens draw one (Attendance, People, Reports, the Rota, Teams), and
 * each was a `div.sel` with a ▾ and nothing behind it — a menu that did not
 * open (tests/no-dead-controls). Every one of their reads takes a `department`
 * and none is ever sent one, so what each screen shows is **every
 * department**, and the label says exactly that.
 *
 * One place, because five copies of one sentence are five sentences the day
 * one of them is built: the screen that gains a real filter replaces its call
 * and the other four still agree with each other.
 */

import { unavailable } from "./element";

/** The picker, disabled, with what it shows and why it cannot change. */
export function allDepartments(): HTMLElement {
  return unavailable("btn", "All departments ▾",
    "Every department is shown. Choosing one is not built yet.");
}
