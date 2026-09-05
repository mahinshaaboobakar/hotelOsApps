/**
 * The scrim, and the two surfaces that sit on it — a sheet and a dialog.
 */

import { control, el, fill } from "./element";

/** A button in an overlay's foot: its words, and what it does. */
export interface Action {
  label: string;

  /** Drawn as the primary. Exactly one action is, or none is. */
  primary?: boolean;

  /**
   * Drawn as the destructive one, **filled** — docs/working/64 §2.
   *
   * The confirm step of a destructive flow is filled rather than outlined: an
   * outline danger button sitting where a person has already decided to delete
   * something is quieter than the Cancel beside it, which inverts the weight
   * of the choice. The outline stays for the affordance that *starts* the flow.
   */
  danger?: boolean;

  /**
   * What it does.
   *
   * `undefined` falls back to dismissing the overlay — which is right for a
   * Cancel button and wrong for a primary one, so a primary action that cannot
   * yet do its work sets `off` rather than leaving this out.
   */
  onClick?: (() => void) | undefined;

  /**
   * Drawn as unavailable, with the reason beside it.
   *
   * A control that cannot do its work is drawn as one — dashed and quiet —
   * rather than as an ordinary button that refuses when pressed. The frames
   * draw no such state, and this exists because two of group 1's actions
   * genuinely have nothing to send: showing them live would be the screen
   * claiming a capability it does not have.
   */
  off?: boolean | undefined;
}

/** What an overlay is made of — the same three parts either way. */
export interface Overlay {
  title: string;
  subtitle: string;

  /** The rows of the body, in the order the frame stacks them. */
  body: readonly (Node | null)[];

  /** The quiet sentence at the left of the foot, where the frame has one. */
  foot: string | null;

  actions: readonly Action[];

  /** What dismissing it does — the scrim, and any non-primary action. */
  onDismiss: () => void;
}

/**
 * A sheet: entered from the right, and where something is composed.
 *
 * @param overlay what it holds
 * @returns the scrim, with the sheet on it
 */
export function sheet(overlay: Overlay): HTMLElement {
  return scrim("scrim", "sheet", overlay);
}

/**
 * A dialog: centred, and where something is confirmed.
 *
 * @param overlay what it holds
 * @returns the scrim, with the dialog on it
 */
export function dialog(overlay: Overlay): HTMLElement {
  return scrim("scrim mid", "dlg", overlay);
}

/**
 * The two, which differ only in where the scrim puts them.
 *
 * One function rather than two near-copies: a sheet and a dialog share a head,
 * a body and a foot, and the day one of them grows a close button the other
 * has to grow it too or the application has two overlay idioms.
 */
function scrim(scrimClass: string, surfaceClass: string, overlay: Overlay): HTMLElement {
  const root = el("div", scrimClass);
  const surface = el("div", surfaceClass);

  const head = el("div", "dh");
  head.append(el("b", undefined, overlay.title), el("span", undefined, overlay.subtitle));

  const body = el("div", "db");
  fill(body, ...overlay.body);

  surface.append(head, body, foot(overlay));

  // Dismissed by the scrim and not by the surface: a click inside the sheet is
  // a person working in it, and closing on that would throw away what they had
  // typed the first time they clicked past a field.
  root.addEventListener("click", (event) => {
    if (event.target === root) overlay.onDismiss();
  });

  root.append(surface);
  return root;
}

function foot(overlay: Overlay): HTMLElement {
  const element = el("div", "df");

  if (overlay.foot !== null) {
    element.append(el("div", "hint", overlay.foot));
  }

  element.append(el("div", "grow"));

  for (const action of overlay.actions) {
    element.append(
      control(
        `btn sm${action.off === true ? " off" : ""}`
          + `${action.primary === true && action.off !== true ? " pri" : ""}`
          + `${action.danger === true && action.off !== true ? " danger confirm" : ""}`,
        action.label,
        action.onClick ?? overlay.onDismiss,
      ),
    );
  }

  return element;
}
