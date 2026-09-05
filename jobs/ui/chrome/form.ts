/**
 * The parts a person types into, and the line that tells them what the service
 * said — the difference between a drawn form and one that acts.
 */

import { control, el, fill } from "./element";

/** A labelled text box whose value a caller reads back. */
export function text(label: string, name: string, placeholder = "", value = ""): HTMLElement {
  const wrap = el("div");
  const box = document.createElement("input");
  box.type = "text";
  box.className = "field";
  box.name = name;
  box.placeholder = placeholder;
  box.value = value;
  wrap.append(el("label", "lbl", label), box);
  return wrap;
}

/**
 * A multi-line box, for the details and the note.
 *
 * The label is optional because some frames draw the field with its words
 * inside it and no label above — the note box is one — and adding a label the
 * drawing does not have is the same divergence as dropping one it does.
 */
export function lines(label: string | null, name: string, placeholder = ""): HTMLElement {
  const wrap = el("div");
  const box = document.createElement("textarea");
  box.className = "field";
  box.name = name;
  box.rows = 3;
  box.placeholder = placeholder;
  if (label !== null) wrap.append(el("label", "lbl", label));
  wrap.append(box);
  return wrap;
}

/** A labelled choice — the catalogue's items, a department, a priority. */
export function choose(
  label: string,
  name: string,
  options: readonly { value: string; label: string }[],
  hint?: string,
): HTMLElement {
  const wrap = el("div");
  const box = document.createElement("select");
  box.className = "field";
  box.name = name;
  for (const option of options) {
    const item = document.createElement("option");
    item.value = option.value;
    item.textContent = option.label;
    box.append(item);
  }

  wrap.append(el("label", "lbl", label), box);
  if (hint !== undefined) wrap.append(el("div", "hint mono", hint));
  return wrap;
}

/** A date, for scheduling a job for a day rather than now. */
export function day(label: string, name: string, hint?: string): HTMLElement {
  const wrap = el("div");
  const box = document.createElement("input");
  box.type = "date";
  box.className = "field";
  box.name = name;
  wrap.append(el("label", "lbl", label), box);
  if (hint !== undefined) wrap.append(el("div", "hint mono", hint));
  return wrap;
}

/** A switch a person can actually turn — restricted, follows shifts. */
export function toggle(label: string, name: string, on = false): HTMLElement {
  const box = document.createElement("input");
  box.type = "checkbox";
  box.className = "tog";
  box.name = name;
  box.checked = on;
  return fill(el("label", "row"), box, el("span", "mono", label));
}

/**
 * What a form holds, read at the moment the button is pressed.
 *
 * Read from the DOM rather than kept in a variable per field: the value a
 * person can see is the one that gets sent, and a mirror of it is one more
 * thing that can be stale.
 */
export function values(form: HTMLElement): Record<string, string | boolean> {
  const held: Record<string, string | boolean> = {};
  for (const field of Array.from(form.querySelectorAll("input, select, textarea"))) {
    const named = field as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
    if (named.name.length === 0) continue;
    held[named.name] = named instanceof HTMLInputElement && named.type === "checkbox"
      ? named.checked
      : named.value.trim();
  }

  return held;
}

/**
 * The line under a form that carries the service's answer.
 *
 * A refusal is shown where the person is looking, in the service's own words —
 * "job MRN-ENG-142 is RAISED and cannot be held" tells them what to do next,
 * where a toast that says "error" tells them to find somebody.
 */
export function saying(): { line: HTMLElement; say: (message: string, bad?: boolean) => void } {
  const line = el("div", "said mono");
  line.hidden = true;
  return {
    line,
    say(message: string, bad = true): void {
      line.textContent = message;
      line.className = bad ? "said mono bad" : "said mono ok";
      line.hidden = false;
    },
  };
}

/**
 * A small panel that asks for one thing before doing it — a hold's reason, a
 * cancellation's.
 *
 * Drawn in place rather than as a browser dialog: a module runs in a realm
 * where a native prompt is somebody else's chrome, and the frames draw a panel.
 */
export function asking(
  question: string,
  placeholder: string,
  onDone: (answer: string) => void,
  onCancel: () => void,
): HTMLElement {
  const panel = el("div", "ask");
  const box = document.createElement("input");
  box.type = "text";
  box.className = "field";
  box.name = "answer";
  box.placeholder = placeholder;
  panel.append(
    el("div", "sect", question),
    box,
    fill(
      el("div", "row"),
      control("btn pri", "Do it", () => onDone(box.value.trim())),
      control("btn", "Cancel", onCancel),
    ),
  );
  return panel;
}
