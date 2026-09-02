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
import type { Secret, Setting, Toggle } from "./configuration";

/**
 * What kind of thing the status line is saying.
 *
 * Two tones and no default: `info` for progress and success, `failed` for a
 * refusal or an error. See [`panel`]'s `status`.
 */
export type Tone = "info" | "failed";

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

  /*
   * **A failure must not read as a success.** The status line said "Saved."
   * and "The configuration could not be saved." in identical muted ink, so an
   * administrator who mistyped an endpoint and glanced away could not tell
   * them apart. The color-bad token is published for exactly this — "a
   * failure, a refusal, a destructive action" — and nothing was using it.
   */
  .panel .status[data-tone="failed"] { color: var(--color-bad, currentColor); }

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

  /*
   * The color-ink-faint token is published for "hints, placeholders,
   * disabled text"
   * and nothing was claiming it — so every placeholder rendered in the user
   * agent's default grey. Close enough on this theme to pass the eye, which
   * is exactly why it survived a capture and five green tests; it is the
   * first theme change that would have found it.
   */
  .panel input::placeholder { color: var(--color-ink-faint, inherit); }

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

  /*
   * Frame 3 draws two actions side by side: a secondary Test connection and
   * the primary Save. The secondary is outlined rather than filled so the
   * primary stays the one obvious action - a second brand-filled button would
   * make the row ask which one the operator meant.
   */
  .panel .actions { display: flex; gap: 8px; align-items: center; }

  .panel .scope { display: flex; flex-direction: column; gap: 6px; }

  .panel .row {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 6px 0;
    border-top: 1px solid var(--color-line, currentColor);
  }

  .panel .row .name { flex: 1; }

  /*
   * A value this form shows and cannot change. Drawn as text rather than as a
   * disabled input: a greyed-out box invites somebody to look for the
   * permission that would unlock it, and there is none - the field belongs to
   * Core Administration.
   */
  .panel .locked { display: flex; flex-direction: column; gap: 4px; }
  .panel .locked .value { color: var(--color-ink-muted, inherit); font-size: 13px; }
  .panel .locked .source { color: var(--color-ink-faint, inherit); font-size: 11px; }

  /*
   * A capability that does not exist yet, drawn as absent rather than as a
   * control that lies. ADR 0128 s4 rules v1 inbound-only, so write-back has
   * nothing behind it - and a disabled checkbox invites somebody to look for
   * the permission that would enable it.
   */
  .panel .row.later .name { color: var(--color-ink-faint, inherit); }

  .panel .later-tag {
    font-size: 11px;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    color: var(--color-ink-faint, inherit);
    border: 1px solid var(--color-line, currentColor);
    border-radius: 99px;
    padding: 1px 8px;
  }

  .panel .test {
    padding: 8px 16px;
    background: transparent;
    color: var(--color-ink, inherit);
    border: 1px solid var(--color-line-strong, currentColor);
    border-radius: 6px;
    font: inherit;
    cursor: pointer;
  }

  .panel .test:disabled { opacity: 0.6; cursor: default; }
`;

interface Drawn {
  readonly title: string;
  readonly subtitle: string;
  readonly settings: readonly (Setting & { value: string })[];
  readonly secrets: readonly (Secret & { placeholder: string })[];
  readonly toggles: readonly (Toggle & { on: boolean })[];
  readonly deferred: readonly Toggle[];

  /** The property's zone, drawn locked. Absent when the platform has none. */
  readonly timeZone?: string;
  onSubmit(typed: {
    settings: Record<string, string>;
    secrets: Record<string, string>;
  }): void;

  /**
   * Ask the Hub to try what is stored.
   *
   * **Optional, and its absence is the grant.** Someone holding only
   * `integration.read` gets a form that renders and neither saves nor tests,
   * and the button is not drawn at all rather than drawn and refused — a
   * control whose only outcome is a refusal is one somebody presses twice.
   */
  onTest?: () => void;
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
  let test: HTMLButtonElement | undefined;

  root.replaceChildren(surface);

  return {
    /**
     * Say something, and say what kind of thing it is.
     *
     * **`tone` is required, and that is the fix.** With one status method and
     * no tone, "Saved." and "The configuration could not be saved." rendered
     * identically, and every caller got that outcome by default rather than by
     * choosing it. Now a caller cannot report an outcome without classifying
     * it — the rule lives in the signature instead of in a review comment.
     */
    status(text: string, tone: Tone): void {
      status.textContent = text;
      status.dataset["tone"] = tone;
      if (status.parentElement === null) surface.appendChild(status);
    },

    saving(text: string): void {
      // **Both, because either action is a round trip.** Leaving Test enabled
      // during a save would let an administrator ask the vendor about a
      // configuration that is still being written.
      if (save !== undefined) save.disabled = true;
      if (test !== undefined) test.disabled = true;
      this.status(text, "info");
    },

    saved(): void {
      if (save !== undefined) save.disabled = false;
      if (test !== undefined) test.disabled = false;
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

      if (drawn.timeZone !== undefined && drawn.timeZone !== "") {
        const locked = node("div", "locked");
        locked.dataset["field"] = "propertyTimeZone";

        locked.appendChild(node("span", "label", "Time zone"));
        locked.appendChild(node("span", "value", drawn.timeZone));
        locked.appendChild(
          node("span", "source", "From Core Administration · Property Registration"),
        );

        form.appendChild(locked);
      }

      const scope = node("div", "scope");

      for (const toggle of drawn.toggles) {
        const row = node("label", "row");
        row.dataset["field"] = toggle.name;

        const box = node("input");
        box.type = "checkbox";
        box.name = toggle.name;
        box.checked = toggle.on;

        row.appendChild(node("span", "name", toggle.label));
        row.appendChild(box);
        scope.appendChild(row);
      }

      for (const later of drawn.deferred) {
        const row = node("div", "row later");
        row.dataset["field"] = later.name;
        row.appendChild(node("span", "name", later.label));
        row.appendChild(node("span", "later-tag", "later"));
        scope.appendChild(row);
      }

      if (drawn.toggles.length > 0 || drawn.deferred.length > 0) {
        form.appendChild(scope);
      }

      const actions = node("div", "actions");

      if (drawn.onTest !== undefined) {
        test = node("button", "test", "Test connection");

        // `button`, not `submit`: inside a form the default type is submit, so
        // an unmarked button would save the configuration on its way to
        // testing it — and a test that silently writes is not a test.
        test.type = "button";
        test.dataset["field"] = "test";
        test.addEventListener("click", () => drawn.onTest?.());
        actions.appendChild(test);
      }

      save = node("button", "save", "Save & enable");
      save.type = "submit";
      actions.appendChild(save);
      form.appendChild(actions);

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

        // **A checkbox stores `on` or `off`, never an absent key.** An
        // unchecked box sends nothing in form data, so reading the elements
        // rather than the FormData is what makes "turned this off" different
        // from "this connector has no such setting".
        for (const toggle of drawn.toggles) {
          const box = form.querySelector<HTMLInputElement>(`input[name="${toggle.name}"]`);
          settings[toggle.name] = box?.checked === true ? "on" : "off";
        }

        drawn.onSubmit({ settings, secrets });
      });

      // **The status is cleared as the form arrives.** It last said "Loading
      // configuration…", and leaving that under a form that has finished
      // loading is a screen telling an operator the opposite of what it shows.
      // A caller with something to say says it after this returns.
      status.textContent = "";
      status.dataset["tone"] = "info";

      surface.replaceChildren(form, status);
    },
  };
}
