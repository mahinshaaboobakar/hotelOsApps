/**
 * What this connector is configured with, and what the platform calls it.
 *
 * # The vocabulary is the connector's, deliberately
 *
 * The Hub's `settings` map is opaque to it — ADR 0128 §7's split: the connector
 * owns its configuration vocabulary, the platform owns security, secrets,
 * authorization, transport and lifecycle. A Hub that knew these names would
 * need a schema per connector, which is the "second programming language"
 * `CONN-Q9` refused when it chose a signed module over a declarative form
 * engine.
 *
 * So this file is the single place the vocabulary lives on the UI side, and the
 * backend reads the same names at runtime. That duplication across the two
 * halves of one package is the one this design accepts; the alternative is
 * coupling the platform to it.
 */

/** The capability that renders — a permission id, `SHELL-Q34`. */
export const READ = "integration.read";

/** The capability that submits. */
export const CONFIGURE = "integration.configure";

/** One non-secret setting, and how to draw it. */
export interface Setting {
  readonly name: string;
  readonly label: string;
  readonly hint: string;
}

/** One credential, by the name the Token Vault stores it under. */
export interface Secret {
  readonly name: string;
  readonly label: string;
}

/**
 * The configuration as the platform returns it.
 *
 * **No secret field, inherited rather than decided.** The wire message has
 * nowhere to put a value (bound 4), so nothing downstream can carry one — this
 * interface could not add a field it has nothing to fill from.
 */
export interface Configuration {
  readonly integrationId: string;
  readonly propertyId: string;
  readonly settings?: Readonly<Record<string, string>>;
  readonly configuredSecrets?: readonly string[];
  readonly updatedAt?: string;
}

export const SETTINGS: readonly Setting[] = [
  { name: "endpoint", label: "OHIP endpoint", hint: "https://ohip.example.com" },
  { name: "hotelCode", label: "Hotel code", hint: "This property, as OPERA names it" },
  { name: "clientId", label: "Client ID", hint: "hotelos_client" },
  {
    name: "pollSeconds",
    label: "Poll interval (seconds)",

    // **Three hours, not thirty seconds** — `CONN-Q12`. The frame draws
    // "Every 3 hours" and this hint said 30, which is the same figure 360
    // times over against a vendor API that rate-limits. A default nobody
    // edits is the value most properties run, so it is the one that has to
    // be right.
    hint: "10800",
  },
];

export const SECRETS: readonly Secret[] = [
  // `client-id` is NOT here — `CONN-Q12`. Frame 3 draws it in plain text
  // (`hotelos_client`) and only the secret half masked, and that drawing is
  // the vault split: masked is a Token Vault secret, plain is a setting.
  // Held write-only, an administrator could see that *a* client id was
  // configured and never which one, which is the thing they need when two
  // properties are pointed at the wrong tenants.
  { name: "client-secret", label: "Client secret" },
];
