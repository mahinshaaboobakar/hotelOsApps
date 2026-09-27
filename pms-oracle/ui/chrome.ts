/**
 * The panel — the one surface this module draws, and the three things it says.
 *
 * **The drawing moved to `styles.ts`; this kept the shape that uses it.** The
 * module doc that sat here described the stylesheet alone — *"the drawing —
 * every colour, radius and face from a token the shell publishes"* — so one
 * file's summary named half its contents, which is ADR 0038's tell rather
 * than a length problem. It measured 328 code lines against the 300 ceiling,
 * red at HEAD since `a26a7ad` on 4 September, and the boundary the split
 * follows is the one that header had already drawn.
 */
import { SECTIONS } from "./configuration";
import type { Secret, Setting, Toggle } from "./configuration";
import { installStyles } from "./styles";

/**
 * What kind of thing the status line is saying.
 *
 * Two tones and no default: `neutral` for progress and success, `bad` for a
 * refusal or an error. See [`panel`]'s `status`.
 *
 * **The platform's names, for the platform's concept** — `42i` entry 4. The
 * desktop's `Tone` is `neutral | ok | warn | bad`, and this package had `info`
 * and `failed`: the same two roles under names that match neither. The SDK
 * publishes the colour tokens and not the vocabulary that uses them, so every
 * package picks its own — and picking the platform's is the cheapest way to
 * stop that becoming four vocabularies.
 *
 * **Two rather than four, and that is deliberate.** This form has no `ok` or
 * `warn` state to draw; adopting names it does not use would be inventing
 * surface to look conformant.
 */
export type Tone = "neutral" | "bad";


interface Drawn {
  readonly title: string;
  readonly subtitle: string;
  readonly settings: readonly (Setting & { value: string })[];
  readonly secrets: readonly (Secret & { placeholder: string })[];
  readonly toggles: readonly (Toggle & { on: boolean })[];
  readonly deferred: readonly Toggle[];

  /** The property's zone, drawn locked. Absent when the platform has none. */
  // `| undefined` written out, because the package sets
  // `exactOptionalPropertyTypes`: the caller reads an optional field off the
  // configuration and passes what it finds, so the property is genuinely
  // present-and-undefined rather than absent. This typecheck was already
  // failing before this round touched the file — esbuild does not type, so the
  // build never said so.
  readonly timeZone?: string | undefined;
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
  installStyles();

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
      this.status(text, "neutral");
    },

    saved(): void {
      if (save !== undefined) save.disabled = false;
      if (test !== undefined) test.disabled = false;
    },

    form(drawn: Drawn): void {
      const form = node("form");
      form.dataset["state"] = "ready";

      // The header row: what this is, and the two things that can be done to
      // it. Frame 3 puts the actions here rather than at the foot — a form
      // long enough to scroll would otherwise hide its own Save.
      const head = node("div", "head");
      const heading = node("div");
      heading.appendChild(node("h1", "title", drawn.title));
      heading.appendChild(node("p", "subtitle", drawn.subtitle));
      head.appendChild(heading);
      head.appendChild(node("div", "grow"));
      form.appendChild(head);

      const settingsIn = (section: string) =>
        drawn.settings.filter((setting) => setting.section === section);

      // Connection · Authentication — a card each, two fields across.
      for (const section of ["connection", "authentication"] as const) {
        const meta = SECTIONS.find((one) => one.id === section);
        const fields = settingsIn(section);
        const secrets = drawn.secrets.filter((secret) => secret.section === section);

        if (fields.length === 0 && secrets.length === 0) continue;

        const card = node("div", "card");
        card.dataset["section"] = section;

        const header = node("div", "cardh");
        header.appendChild(node("b", undefined, meta?.title ?? section));
        if (meta !== undefined) header.appendChild(node("small", undefined, meta.note));
        card.appendChild(header);

        const body = node("div", "cardbody");
        const grid = node("div", "grid2");

        // **A setting, then the secret that proves it.** The grid is two
        // across, so adjacency is the pairing: emitting every setting and then
        // every secret would put the two identities in one row and their two
        // proofs in the next, and frame 3 draws `PMS username` beside
        // `PMS password`. `Secret.proves` carries which, so the layout follows
        // from what the field is rather than from the order of two loops.
        for (const setting of fields) {
          grid.appendChild(
            field(setting.name, setting.label, setting.value, setting.hint, false),
          );

          for (const secret of secrets.filter((one) => one.proves === setting.name)) {
            grid.appendChild(field(secret.name, secret.label, "", secret.placeholder, true));
          }
        }

        // A secret that proves nothing has no partner to sit beside, and goes
        // last — which is where the frame draws the application key, because
        // it addresses the tenancy rather than authenticating to it.
        for (const secret of secrets.filter((one) => one.proves === undefined)) {
          grid.appendChild(field(secret.name, secret.label, "", secret.placeholder, true));
        }

        body.appendChild(grid);

        // **Written once, never read back** — under whichever card holds a
        // masked field, because the sentence is about the Vault rather than
        // about authentication. The application key is a secret and is not a
        // credential, and it needs the same warning.
        if (secrets.length > 0) {
          const note = node(
            "p",
            "note",
            "Written once, never read back. These go straight to the Token Vault: this " +
              "screen can replace them and can test them, and neither it nor the package " +
              "can display them again.",
          );
          note.style.marginTop = "14px";
          body.appendChild(note);
        }

        card.appendChild(body);
        form.appendChild(card);
      }

      // Polling and Sync scope, side by side.
      const pair = node("div", "pair");

      const pollingMeta = SECTIONS.find((one) => one.id === "polling");
      const polling = node("div", "card");
      polling.dataset["section"] = "polling";

      const pollingHead = node("div", "cardh");
      pollingHead.appendChild(node("b", undefined, pollingMeta?.title ?? "Polling"));
      if (pollingMeta !== undefined) {
        pollingHead.appendChild(node("small", undefined, pollingMeta.note));
      }
      polling.appendChild(pollingHead);

      const pollingBody = node("div", "cardbody");
      const pollingFields = node("div", "grid2");

      for (const setting of settingsIn("polling")) {
        pollingFields.appendChild(
          field(setting.name, setting.label, setting.value, setting.hint, false),
        );
      }

      pollingBody.appendChild(pollingFields);

      if (drawn.timeZone !== undefined && drawn.timeZone !== "") {
        const locked = node("div", "locked");
        locked.dataset["field"] = "propertyTimeZone";
        locked.style.marginTop = "14px";

        locked.appendChild(node("span", "label", "Time zone"));
        locked.appendChild(node("span", "value", drawn.timeZone));
        locked.appendChild(
          node("span", "source", "From Core Administration · Property Registration"),
        );

        pollingBody.appendChild(locked);
      }

      polling.appendChild(pollingBody);
      pair.appendChild(polling);

      const scopeMeta = SECTIONS.find((one) => one.id === "scope");
      const scopeCard = node("div", "card");
      scopeCard.dataset["section"] = "scope";

      const scopeHead = node("div", "cardh");
      scopeHead.appendChild(node("b", undefined, scopeMeta?.title ?? "Sync scope"));
      if (scopeMeta !== undefined) {
        scopeHead.appendChild(node("small", undefined, scopeMeta.note));
      }
      scopeCard.appendChild(scopeHead);

      const scopeBody = node("div", "cardbody");
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

      scopeBody.appendChild(scope);
      scopeCard.appendChild(scopeBody);
      pair.appendChild(scopeCard);
      form.appendChild(pair);

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

      // Into the header, beside the title — frame 3's placement.
      head.appendChild(actions);

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
      status.dataset["tone"] = "neutral";

      surface.replaceChildren(form, status);
    },
  };
}
