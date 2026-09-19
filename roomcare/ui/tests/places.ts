/**
 * Every place a person can reach in Room Care, and how a test gets there — one list for every walk, so two
 * walks cannot disagree about which screens they visited (HH's shared-walk rule, `tests/walk.ts` in Jobs).
 *
 * Derived from the module's own section list and Setup's tab list, so a place added later is walked.
 */

import { activate } from "../application";
import { TABS } from "../screens/setup";
import { ATTENDANT, SUPERVISOR, click, host, mount, recorded, settle, type Call } from "./host";

/** A place: its name, who is looking, the section, a Setup tab, and a control pressed to get the rest of the way. */
export type Place = readonly [name: string, capabilities: readonly string[], section: string, tab: string | null, into: string | null];

export const PLACES: readonly Place[] = [
  ...["Board", "Prepare", "Room states", "Supervision", "Deep clean"].map((s) => [s, SUPERVISOR, s, null, null] as const),
  ...TABS.map((t) => [`Setup · ${t}`, SUPERVISOR, "Setup", t, null] as const),
  ["My rooms", ATTENDANT, "My rooms", null, null],
  // Reached by navigating rather than by a sheet, so a walk of the sections alone never presses their controls.
  ["A room's page", SUPERVISOR, "Board", null, ".body button.tile"],
  ["The door", ATTENDANT, "My rooms", null, ".body tr.pick button.opener"],
];

/** My rooms with a second page, so its pager has somewhere to go. */
function pagedMyRooms(): Record<string, unknown> {
  const mine = recorded<{ paging: { page: number; pageSize: number; total: number } }>("my-rooms");
  return { myRooms: { ...mine, paging: { ...mine.paging, total: mine.paging.pageSize + 1 } } };
}

/** Mount the module and go to a place. */
export async function reach(capabilities: readonly string[], section: string, tab: string | null, calls: Call[], into: string | null = null): Promise<HTMLElement> {
  const root = mount(activate, host(capabilities, pagedMyRooms(), calls));
  await settle();
  if (section !== "My rooms" && section !== "Board") click(root, ".head .tab", section);
  await settle();
  if (tab !== null) click(root, ".subnav .tab", tab);
  await settle();
  if (into !== null) {
    const control = root.querySelector<HTMLElement>(into);
    if (control === null) throw new Error(`no ${into} to press on the way in`);
    control.click();
    await settle();
  }
  return root;
}

/** The buttons a person could press there. */
export function pressable(root: HTMLElement): HTMLButtonElement[] {
  return [...root.querySelectorAll<HTMLButtonElement>(".body button")].filter((b) => !b.hasAttribute("disabled"));
}

/** Whether a button is what is already chosen — pressing it rightly changes nothing. A row's opener in the chosen
 * row (Services' `tr.sel`) is the current choice too. */
export function current(button: HTMLButtonElement): boolean {
  return button.getAttribute("aria-pressed") === "true" || button.matches(".pg.on") || button.matches("tr.sel button.opener");
}
