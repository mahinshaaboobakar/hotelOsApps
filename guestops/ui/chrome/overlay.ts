/**
 * The scrim, and the two surfaces that sit on it — a sheet and a dialog.
 */

import { control, el, fill, unavailable } from "./element";

/** What every action in a foot carries, live or not. */
interface Labelled {
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
}

/**
 * A button in an overlay's foot: its words, and what it does — or why it cannot.
 *
 * # `off` used to be a class, and the caller had to remember the rest
 *
 * It added `off` to the class name and **nothing else**: the button stayed
 * pressable, and an action with no `onClick` falls back to `onDismiss`, so an
 * *off* primary silently CLOSED the overlay — throwing away whatever the desk
 * had typed into it. The one existing caller defended itself by hand
 * (`onClick: reason === null ? () => undefined : …`), which is the tell: a rule
 * of the form *the caller must also neutralise Y* is a rule that will be
 * forgotten, and this one had already been forgotten once by a dead primary
 * action on the registration card.
 *
 * **So the two states are two shapes.** An off action cannot be written without
 * its reason, and cannot carry an `onClick` at all — the mistake is not caught,
 * it is inexpressible. The reason reaches a person as the control's tooltip and
 * its accessible description, which is what `unavailable` is for.
 */
export type Action =
  | (Labelled & {
    off?: false;

    /**
     * What it does.
     *
     * `undefined` falls back to dismissing the overlay — right for a Cancel
     * button. A primary action that cannot do its work yet is the `off` shape
     * below rather than this one with the handler left out.
     */
    onClick?: (() => void) | undefined;
  })
  | (Labelled & {
    /**
     * Drawn as unavailable — dashed, quiet, and genuinely disabled.
     *
     * A control that cannot do its work is drawn as one rather than as an
     * ordinary button that refuses when pressed. The frames draw no such state;
     * it exists because some actions genuinely have nothing to send, and
     * showing them live would be the screen claiming a capability it has not
     * got.
     */
    off: true;

    /** Why, in words a person at the desk understands — never a code. */
    why: string;
  });

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
    // **Genuinely disabled, and carrying its reason.** `unavailable` sets
    // `disabled` and puts the reason where a pointer and a screen reader both
    // find it; the old form added a class and left the button live, so an off
    // primary fell through to `onDismiss` and closed the overlay.
    if (action.off === true) {
      element.append(unavailable("btn sm", action.label, action.why));
      continue;
    }

    element.append(
      control(
        `btn sm${action.primary === true ? " pri" : ""}`
          + `${action.danger === true ? " danger confirm" : ""}`,
        action.label,
        action.onClick ?? overlay.onDismiss,
      ),
    );
  }

  return element;
}
