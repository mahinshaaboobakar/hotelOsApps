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
  { name: "pollSeconds", label: "Poll interval (seconds)", hint: "30" },
];

export const SECRETS: readonly Secret[] = [
  { name: "client-id", label: "Client ID" },
  { name: "client-secret", label: "Client secret" },
];
