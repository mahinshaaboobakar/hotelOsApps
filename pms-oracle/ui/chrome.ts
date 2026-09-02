/**
 * The drawing — every colour, radius and face from a token the shell publishes.
 *
 * **Bound 1 as it can be enforced across a realm.** An installed application
 * looks like HotelOS because the platform styles it, not because it renders the
 * platform's components: the host writes its tokens onto the realm's root, and
 * nothing here picks a value. A literal would be a second design system
 * arriving inside the first, and it would stay behind at the next theme change.
 *
 * `style-src 'unsafe-inline'` is the realm's one concession — nothing can be
 * fetched under `default-src 'none'`, so a module styles itself. What that
 * permits is layout, not a palette.
 *
 * The fallbacks are for a token the shell has stopped publishing. `tokens.ts`
 * omits such a token rather than writing it blank, precisely so a fallback can
 * do its job: an empty custom property would override it and render invisible
 * text.
 */
import type { Secret, Setting } from "./configuration";

const SHEET_ID = "oracle-styles";

const CSS = `
  .panel {
    display: flex;
    flex-direction: column;
    gap: 12px;
    max-width: 520px;
    padding: 16px;
    background: var(--color-surface-raised, transparent);
    color: var(--color-ink, inherit);
    border: 1px solid var(--color-line, currentColor);
    border-radius: var(--radius-panel, 8px);
    font-family: var(--font-sans, system-ui, sans-serif);
    font-size: 14px;
  }

  .panel .title { margin: 0; font-size: 16px; font-weight: 600; }

  .panel .subtitle,
  .panel .status { margin: 0; font-size: 12px; color: var(--color-ink-muted, inherit); }

  .panel .field { display: flex; flex-direction: column; gap: 4px; }
  .panel .label { font-size: 12px; color: var(--color-ink-muted, inherit); }

  .panel input {
    padding: 8px;
    background: var(--color-surface, transparent);
    color: var(--color-ink, inherit);
    border: 1px solid var(--color-line, currentColor);
    border-radius: 6px;
    font: inherit;
  }

  .panel input:focus-visible { outline: 2px solid var(--color-brand, currentColor); }

  .panel .save {
    align-self: flex-start;
    padding: 8px 16px;
    background: var(--color-brand, transparent);
    color: var(--color-ink-on-accent, inherit);
    border: 1px solid transparent;
    border-radius: 6px;
    font: inherit;
    cursor: pointer;
  }

  .panel .save:disabled { opacity: 0.6; cursor: default; }
`;

interface Drawn {
  readonly title: string;
  readonly subtitle: string;
  readonly settings: readonly (Setting & { value: string })[];
  readonly secrets: readonly (Secret & { placeholder: string })[];
  onSubmit(typed: {
    settings: Record<string, string>;
    secrets: Record<string, string>;
  }): void;
}

function node<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  className?: string,
  text?: string,
): HTMLElementTagNameMap[K] {
  const created = document.createElement(tag);
  if (className !== undefined) created.className = className;
  if (text !== undefined) created.textContent = text;
  return created;
}

/**
 * One labelled input.
 *
 * `data-field` on every one, because the audit reads the rendered form: a field
 * it cannot address is a field nobody can prove was drawn.
 */
function field(
  name: string,
  label: string,
  value: string,
  placeholder: string,
  secret: boolean,
): HTMLLabelElement {
  const wrapper = node("label", "field");
  wrapper.dataset["field"] = name;
  wrapper.appendChild(node("span", "label", label));

  const input = node("input");
  input.name = name;
  input.type = secret ? "password" : "text";
  input.value = value;
  input.placeholder = placeholder;

  wrapper.appendChild(input);
  return wrapper;
}

/** The surface this module draws on, and the three things it can say. */
export function panel(root: HTMLElement) {
  if (document.getElementById(SHEET_ID) === null) {
    const sheet = node("style");
    sheet.id = SHEET_ID;
    sheet.textContent = CSS;
    document.head.appendChild(sheet);
  }

  const surface = node("div", "panel");
  const status = node("p", "status");
  status.dataset["field"] = "status";

  let save: HTMLButtonElement | undefined;

  root.replaceChildren(surface);

  return {
    status(text: string): void {
      status.textContent = text;
      if (status.parentElement === null) surface.appendChild(status);
    },

    saving(text: string): void {
      if (save !== undefined) save.disabled = true;
      this.status(text);
    },

    saved(): void {
      if (save !== undefined) save.disabled = false;
    },

    form(drawn: Drawn): void {
      const form = node("form");
      form.dataset["state"] = "ready";

      form.appendChild(node("h1", "title", drawn.title));
      form.appendChild(node("p", "subtitle", drawn.subtitle));

      for (const setting of drawn.settings) {
        form.appendChild(field(setting.name, setting.label, setting.value, setting.hint, false));
      }

      for (const secret of drawn.secrets) {
        form.appendChild(field(secret.name, secret.label, "", secret.placeholder, true));
      }

      save = node("button", "save", "Save");
      save.type = "submit";
      form.appendChild(save);

      form.addEventListener("submit", (event) => {
        event.preventDefault();

        const values = new FormData(form);
        const settings: Record<string, string> = {};
        const secrets: Record<string, string> = {};

        for (const setting of drawn.settings) {
          settings[setting.name] = String(values.get(setting.name) ?? "");
        }

        for (const secret of drawn.secrets) {
          const typed = String(values.get(secret.name) ?? "");

          // **Blank means unchanged, so it is not sent.** Sending it would mean
          // "remove" — the API's only removal — and somebody editing an
          // endpoint would lose a credential they cannot retype.
          if (typed !== "") secrets[secret.name] = typed;
        }

        drawn.onSubmit({ settings, secrets });
      });

      // **The status is cleared as the form arrives.** It last said "Loading
      // configuration…", and leaving that under a form that has finished
      // loading is a screen telling an operator the opposite of what it shows.
      // A caller with something to say says it after this returns.
      status.textContent = "";

      surface.replaceChildren(form, status);
    },
  };
}
