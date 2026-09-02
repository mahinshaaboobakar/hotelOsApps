/**
 * The Oracle connector's configuration form — `CONN-Q9(b)`, ADR 0128 §7.
 *
 * A **package's** UI, hosted by Software Center inside Integration Management.
 * Built in the package, shipped in `ui/`, and run in its own iframe realm. Its
 * only connection to HotelOS is `@hotelos/sdk` and the port the host transfers
 * in — there is no ambient capability here, so no database, no tuple writer and
 * no route past the Hub, because **no name is bound to those things in this
 * realm**.
 *
 * # Why this is `application.ts` and not `module.ts`
 *
 * The artifact this package ships is `ui/module.js`, and a source file called
 * `module.ts` beside it makes `from "./module"` ambiguous: both vitest and
 * esbuild resolve the extensionless import to the built `.js`, so the bundle
 * gets built from itself. GuestOps hit exactly that. The artifact owns the
 * name; the source takes another.
 *
 * # What it may do, and who decides
 *
 * `manifest.yaml` declares the two permissions this form requests. That
 * declaration is the **bound**, shown at install; the **grant** is per user and
 * the Kernel's — `SHELL-Q34`: a capability id *is* a permission id, so this form
 * can do exactly what the person using it could do through the platform's own
 * surface, and the bridge refuses an undeclared name structurally.
 *
 *     integration.read       configuration    render what is stored
 *     integration.configure  save             submit settings and credentials
 *
 * Someone holding only `integration.read` gets a form that renders and cannot
 * save, and is told which of those two is true — the approve-one, decline-one
 * split made visible rather than theoretical.
 *
 * # Credentials go one way, and the form is shaped by that
 *
 * The platform never sends a secret back: `IntegrationConfiguration` has no
 * field for one (bound 4). So this shows **whether** a credential is set, from
 * `configuredSecrets`, and offers to replace it. A blank box means *unchanged*,
 * never *empty* — the API's only removal is a name submitted with an empty
 * value, so sending blanks would make editing an endpoint silently clear a
 * credential nobody can retype.
 *
 * # It is styled, never themed — bound 1
 *
 * Every colour, radius and face is a `var()` on a token the shell publishes.
 * An installed application looks like HotelOS because the platform styles it,
 * not because it renders the platform's components — and a literal here would
 * be a second design system arriving inside the first, drifting at the next
 * theme change.
 */
import type { Activate, CallFailure, HostApi, HostedModule } from "@hotelos/sdk";

import { panel } from "./chrome";
import {
  CONFIGURE,
  READ,
  SECRETS,
  SETTINGS,
  TEST,
  type Configuration,
  type ConnectionTest,
} from "./configuration";

/** Whether a failure carries a sentence a person may read — ADR 0041. */
function isForPeople(kind: CallFailure["kind"]): boolean {
  return kind === "rejected" || kind === "invalid";
}

function sentence(failure: unknown, fallback: string): string {
  const classified = failure as CallFailure | undefined;

  return classified !== undefined && isForPeople(classified.kind)
    ? classified.message
    : fallback;
}

export const activate: Activate = (host: HostApi): HostedModule => ({
  mount(root: HTMLElement): void {
    const surface = panel(root);

    // Something is on screen before the first call resolves. A form that
    // renders nothing until the platform answers renders nothing at all when
    // the platform is slow, and an operator cannot tell that from a break.
    surface.status("Loading configuration…", "info");

    void host
      .call(READ, "configuration")
      .then((answer) => draw(host, surface, answer as Configuration))
      .catch((failure: unknown) => {
        // Somebody holding neither permission is told why, once, rather than
        // shown an empty form that silently cannot do anything.
        surface.status(
          sentence(failure, "This connector's configuration could not be read."),
          "failed",
        );
      });
  },
});

function draw(
  host: HostApi,
  surface: ReturnType<typeof panel>,
  configuration: Configuration,
): void {
  const configured = new Set(configuration.configuredSecrets ?? []);

  surface.form({
    title: "Oracle OPERA",
    subtitle: `Configuration for ${configuration.integrationId}`,
    settings: SETTINGS.map((setting) => ({
      ...setting,
      value: configuration.settings?.[setting.name] ?? "",
    })),
    secrets: SECRETS.map((secret) => ({
      ...secret,

      // The placeholder is the whole affordance: it says "configured" without
      // the value, because the value was never sent and cannot be.
      placeholder: configured.has(secret.name) ? "•••••••• configured" : "Not set",
    })),
    onSubmit: (typed) => save(host, surface, typed),

    // **Always drawn, exactly as Save is** — and that is a consequence of
    // `SHELL-Q34` rather than a choice here. A module can see its *bound*
    // (`identity.capabilities`, what the manifest declares it may ever ask
    // for) and never its *grant*, which is per user and the Kernel's. So the
    // form cannot know in advance whether this person may test, any more than
    // it knows whether they may save.
    //
    // What makes that acceptable is the refusal being readable: ADR 0041 lets
    // a `rejected` sentence cross the bridge, so somebody holding only
    // `integration.read` presses it once and is told which of the two they
    // hold — rather than getting the fixed "that could not be completed" this
    // round had to go and fix.
    onTest: () => test(host, surface),
  });
}

/**
 * How a test result is drawn.
 *
 * **Only a reached source is good news.** Everything else is either work for
 * the administrator or a fact about the vendor, and drawing them all in the
 * same muted ink is the defect `color-bad` was claimed to fix — so the outcome
 * decides the tone rather than the sentence's wording.
 */
function toneFor(outcome: string): "info" | "failed" {
  return outcome === "reached" || outcome === "notSupported" ? "info" : "failed";
}

function test(host: HostApi, surface: ReturnType<typeof panel>): void {
  surface.saving("Testing…");

  void host
    .call(CONFIGURE, TEST)
    .then((answer) => {
      const found = answer as ConnectionTest;

      surface.saved();
      surface.status(found.detail, toneFor(found.outcome));
    })
    .catch((failure: unknown) => {
      surface.saved();
      surface.status(sentence(failure, "The connection could not be tested."), "failed");
    });
}

function save(
  host: HostApi,
  surface: ReturnType<typeof panel>,
  typed: { settings: Record<string, string>; secrets: Record<string, string> },
): void {
  surface.saving("Saving…");

  void host
    .call(CONFIGURE, "save", typed)
    .then((answer) => {
      // Redrawn from what the platform returned, never from what was typed.
      // The credential boxes come back empty with their placeholders updated,
      // which is the only honest thing a form can show about a value it cannot
      // read — and it is what keeps a typed secret out of the DOM afterwards.
      draw(host, surface, answer as Configuration);
      surface.status("Saved.", "info");
    })
    .catch((failure: unknown) => {
      surface.saved();
      surface.status(sentence(failure, "The configuration could not be saved."), "failed");
    });
}
