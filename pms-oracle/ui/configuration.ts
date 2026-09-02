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

/** The capability that submits, and that tests. */
export const CONFIGURE = "integration.configure";

/**
 * The method that asks the Hub to try this configuration.
 *
 * **On `CONFIGURE`, not `READ`.** A test authenticates somewhere on the
 * property's behalf and appears in the vendor's logs, so it is an action rather
 * than an observation — a read-only administrator can see what is configured
 * and cannot press this.
 */
export const TEST = "test";

/**
 * What a connection test found — `CONN-Q12`.
 *
 * Six outcomes rather than a flag: a wrong credential and an unreachable host
 * are the same red light only if you collapse them, and they send an
 * administrator to different people.
 */
export interface ConnectionTest {
  readonly outcome: string;
  readonly detail: string;
  readonly missing?: readonly string[];
}

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
  // Frame 3's Connection card.
  { name: "endpoint", label: "OHIP host", hint: "https://ohip.example.com" },
  { name: "hotelCode", label: "Hotel id · property code", hint: "KOCHI01" },
  { name: "externalSystemCode", label: "External system code", hint: "HOTELOS" },

  // Frame 3's Authentication card, legible half. `clientId` and `pmsUsername`
  // identify; they do not prove, and the frame draws them in plain text.
  { name: "clientId", label: "Client id", hint: "hotelos_client" },
  { name: "pmsUsername", label: "PMS username", hint: "hotelos_kochi" },

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
  // Frame 3's masked fields, and the drawing IS the vault split — `CONN-Q12`:
  // masked is a Token Vault secret, legible is a setting. That is why
  // `clientId` sits above and `client-secret` sits here.
  //
  // **Three credentials, proving three different things.** The application key
  // identifies the tenancy, the client pair proves the integration, and the
  // PMS password authenticates the OPERA user whose permissions a poll runs
  // under. A set carrying fewer would still look like OAuth and would be
  // refused by the tenancy.
  { name: "application-key", label: "Application key" },
  { name: "pms-password", label: "PMS password" },
  { name: "client-secret", label: "Client secret" },
];
