/**
 * The overlay pair — a sheet composes, a dialog confirms (page 64 §9).
 *
 * One head/body/foot; the scrim decides which. Positioned absolutely inside the
 * module's frame, a sibling of the body, never `fixed` (a module runs in an
 * iframe). The scrim dismisses; the surface does not. A refusal keeps the
 * overlay open and says why — closing on failure would leave a person
 * believing the thing happened.
 */

import { control, el } from "./element";

export interface Overlay {
  body: HTMLElement;
  foot: HTMLElement;
  /** Say, inside the overlay, why the act was refused. */
  refuse(because: string): void;
  close(): void;
}

function open(frame: HTMLElement, kind: "sheet" | "dlg", title: string, onClose?: () => void): Overlay {
  const scrim = el("div", kind === "sheet" ? "scrim" : "scrim mid");
  const surface = el("div", kind);
  surface.setAttribute("role", "dialog");
  surface.setAttribute("aria-label", title);
  const body = el("div", "db");
  const foot = el("div", "df");
  surface.append(el("div", "dh", title), body, foot);
  scrim.append(surface);
  const close = (): void => {
    scrim.remove();
    onClose?.();
  };
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });
  frame.append(scrim);
  return {
    body,
    foot,
    refuse(because) {
      body.querySelector(".said.bad")?.remove();
      body.append(el("p", "said bad", because));
    },
    close,
  };
}

/** A sheet from the right — where a person composes something. */
export function sheet(frame: HTMLElement, title: string, onClose?: () => void): Overlay {
  return open(frame, "sheet", title, onClose);
}

/** A dialog in the middle — where a person confirms something. */
export function dialog(frame: HTMLElement, title: string, onClose?: () => void): Overlay {
  return open(frame, "dlg", title, onClose);
}

/** The foot's two controls: the act, and Back. */
export function actions(overlay: Overlay, label: string, act: () => void, style = "btn pri"): HTMLElement {
  const go = control(style, label, act);
  overlay.foot.append(control("btn", "Back", () => overlay.close()), go);
  return go;
}

/**
 * Hold the overlay's primary — the last control `actions` put in its foot —
 * until it has something to send: drawn off, with its reason beside it, never
 * live and refusing (page 64 §2, checklist C11). `ready` answers null when there
 * is something to send, or the reason there is not; it is asked again whenever
 * anything inside the overlay's body is typed, changed or pressed.
 */
export function readyWhen(overlay: Overlay, ready: () => string | null): void {
  const go = overlay.foot.lastElementChild as HTMLElement;
  const label = go.textContent ?? "";
  const idle = el("span", "btn off");
  const check = (): void => {
    const reason = ready();
    if (reason === null) {
      if (idle.isConnected) idle.replaceWith(go);
      return;
    }
    idle.textContent = `${label} — ${reason}`;
    if (go.isConnected) go.replaceWith(idle);
  };
  for (const event of ["input", "change", "click"]) overlay.body.addEventListener(event, check);
  check();
}
