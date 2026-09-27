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

const SHEET_ID = "oracle-styles";

const CSS = `
  /*
   * **Full width, and that is frame 3's shape rather than a preference.**
   * This read a 520-pixel cap on a single flex column, so every capture of
   * a hosted form had a blank right half and Polling sat under Sync scope
   * instead of beside it. The frames draw four cards across the width of the
   * region the host gives us; the host gives us all of it.
   */
  .panel {
    display: flex;
    flex-direction: column;
    gap: 14px;
    padding: 16px;
    color: var(--color-ink, inherit);
    font-family: var(--font-sans, system-ui, sans-serif);
    font-size: 14px;
  }

  /* The two actions sit with the title, as the frame's header row has them. */
  .panel .head { display: flex; align-items: flex-end; gap: 14px; }
  .panel .head .grow { flex: 1; }

  .panel .card {
    background: var(--color-surface-raised, transparent);
    border: 1px solid var(--color-line, currentColor);
    border-radius: var(--radius-panel, 8px);
  }

  .panel .cardh {
    display: flex;
    align-items: baseline;
    gap: 10px;
    padding: 11px 16px;
    border-bottom: 1px solid var(--color-line, currentColor);
  }

  .panel .cardh b { font-weight: 600; }
  .panel .cardh small { font-size: 12px; color: var(--color-ink-muted, inherit); }
  .panel .cardbody { padding: 14px 16px; }

  /*
   * Two fields across, one on a narrow region. The frames pair them because
   * the questions pair — a host beside its property code, a client id beside
   * the secret that proves it.
   */
  .panel .grid2 { display: grid; grid-template-columns: repeat(2, 1fr); gap: 14px; }

  /* Polling and Sync scope side by side, which is how frame 3 draws them. */
  .panel .pair { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; align-items: start; }

  @media (max-width: 860px) {
    .panel .grid2, .panel .pair { grid-template-columns: 1fr; }
  }

  .panel .note {
    margin: 0;
    padding: 11px 14px;
    border-left: 3px solid var(--color-brand, currentColor);
    border-radius: 8px;
    background: var(--color-surface, transparent);
    font-size: 12px;
    color: var(--color-ink-muted, inherit);
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
  .panel .status[data-tone="bad"] { color: var(--color-bad, currentColor); }

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

  .panel .scope { display: flex; flex-direction: column; }

  .panel .scope .row:first-child { border-top: 0; }

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
/**
 * Install the stylesheet, once per realm.
 *
 * **Once, and the check is the id rather than a flag.** A module is drawn
 * again whenever the shell re-enters it, and a second `<style>` carrying the
 * same rules costs nothing visible — which is exactly why nothing would ever
 * report it. The id is the state.
 *
 * `document.createElement` rather than `chrome.ts`'s `node`: that helper adds
 * a class and text this element does not take, and importing it back would
 * make the drawing depend on the panel that draws with it.
 */
export function installStyles(): void {
  if (document.getElementById(SHEET_ID) !== null) return;

  const sheet = document.createElement("style");
  sheet.id = SHEET_ID;
  sheet.textContent = CSS;
  document.head.appendChild(sheet);
}
