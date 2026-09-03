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

/** The method that turns the connector on — frame 3's second half. */
export const ENABLE = "enable";

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
/**
 * Which of frame 3's cards a field is drawn in.
 *
 * The frames group by **what a person is answering** — where the PMS lives,
 * how we prove who we are, how often we ask, and what we ask for — and the
 * grouping was in comments here while the form drew one flat column. A comment
 * cannot lay anything out, so it is a field now.
 */
export type Section = "connection" | "authentication" | "polling" | "scope";

export interface Setting {
  readonly name: string;
  readonly label: string;
  readonly hint: string;
  readonly section: Section;
}

/** One credential, by the name the Token Vault stores it under. */
export interface Secret {
  readonly name: string;
  readonly label: string;

  /**
   * Which card it is drawn in — and a secret does not always mean
   * *Authentication*.
   *
   * `application-key` is a **tenancy identifier that happens to be secret**: it
   * travels as `x-app-key` on every request beside `x-hotelid` and
   * `x-externalsystem`, and the backend's own note says a call missing one of
   * the three *"is refused by the tenancy with a message about the header
   * rather than about the credential, which is a long way from the cause."*
   * Grouping it with the password grant separates it from the two headers it
   * never travels without.
   *
   * Frame 3 draws it in Connection. `CONN-Q12` ruled *"the UI follows the
   * frame"*, and its `masked = secret, plain = setting` clause decides which
   * fields are secrets — not which card holds them. This form had read it as
   * both.
   */
  readonly section: Section;

  /**
   * The setting this credential proves, when it proves one.
   *
   * **Frame 3 pairs an identity with its proof** — `PMS username` beside
   * `PMS password`, `Client id` beside `Client secret` — and a two-column grid
   * only reproduces that if the pairs are adjacent. Emitting every setting and
   * then every secret puts the two identities in one row and the two proofs in
   * the next, which reads as four unrelated boxes.
   *
   * **Absent for `application-key`, and that absence is the point.** It proves
   * nothing: it addresses the tenancy, travelling as `x-app-key` beside
   * `x-hotelid` and `x-externalsystem` on every request. Having no partner is
   * what puts it last in Connection, which is exactly where the frame draws
   * it — so the layout follows from what the field *is* rather than from a
   * position somebody typed.
   */
  readonly proves?: string;
}

/** Each card's heading and the line under it — frame 3's `cardh`. */
export const SECTIONS: readonly { id: Section; title: string; note: string }[] = [
  {
    id: "connection",
    title: "Connection",
    // The three request headers plus the host. `x-app-key` is here because it
    // addresses the tenancy rather than authenticating to it.
    note: "where this property's OPERA lives, and what addresses it",
  },
  {
    id: "authentication",
    title: "Authentication",
    // The password grant, whole: the OAuth client that identifies the
    // integration and the OPERA user whose permissions a poll runs under.
    note: "the OAuth client, and the OPERA user a poll runs as",
  },
  { id: "polling", title: "Polling", note: "two tiers, per property" },
  { id: "scope", title: "Sync scope", note: "inbound only — ADR 0128 §4" },
];

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

  /**
   * The property's IANA zone — read-only, and there is nowhere to send it back.
   *
   * Frame 3 draws it locked because it is the PROPERTY's, not the
   * integration's: Core Administration configures it, and this connector's
   * tighter polling window is expressed in it. The submit message has no such
   * field, so the lock is the contract's rather than this form's discipline.
   */
  readonly propertyTimeZone?: string;
  readonly updatedAt?: string;
}

export const SETTINGS: readonly Setting[] = [
  // Frame 3's Connection card.
  { name: "endpoint", label: "OHIP host", hint: "https://ohip.example.com", section: "connection" },
  { name: "hotelCode", label: "Hotel id · property code", hint: "KOCHI01", section: "connection" },
  {
    name: "externalSystemCode",
    label: "External system code",
    hint: "HOTELOS",
    section: "connection",
  },

  // Frame 3's Authentication card, legible half. `clientId` and `pmsUsername`
  // identify; they do not prove, and the frame draws them in plain text.
  //
  // The frame's own order, PMS user before OAuth client — each is followed by
  // the secret that proves it, and `Secret.proves` is what puts it there.
  {
    name: "pmsUsername",
    label: "PMS username",
    hint: "hotelos_kochi",
    section: "authentication",
  },
  { name: "clientId", label: "Client id", hint: "hotelos_client", section: "authentication" },

  // Frame 3's Polling card — two tiers, because arrivals cluster.
  //
  // **Three hours ordinarily and fifteen minutes around check-in.** The queue
  // is emptied by reading, so a long interval makes a backlog rather than
  // saving work — which argues for polling harder WHEN THERE IS TRAFFIC, not
  // permanently. A single interval had to choose, and chose 30 seconds: 360
  // times the frame's figure, spent on empty reads against an API that
  // rate-limits.
  //
  // These hints are placeholders and govern nothing. The numbers that act are
  // `OhipPollingSchedule`'s defaults, which is where this round learned to put
  // them after correcting a hint and leaving the constant behind.
  {
    name: "pollNormalSeconds",
    label: "Normal interval (seconds)",
    hint: "10800",
    section: "polling",
  },
  {
    name: "pollTightSeconds",
    label: "Tighter interval (seconds)",
    hint: "900",
    section: "polling",
  },
  { name: "pollTightFrom", label: "Tighter window from", hint: "14:00", section: "polling" },
  { name: "pollTightUntil", label: "Tighter window until", hint: "16:00", section: "polling" },
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
  { name: "application-key", label: "Application key", section: "connection" },
  {
    name: "pms-password",
    label: "PMS password",
    section: "authentication",
    proves: "pmsUsername",
  },
  {
    name: "client-secret",
    label: "Client secret",
    section: "authentication",
    proves: "clientId",
  },
];

/** One subscription the property can turn on or off. */
export interface Toggle {
  readonly name: string;
  readonly label: string;
}

/**
 * Frame 3's Sync scope — what this integration sends us.
 *
 * Settings like any other, stored as `on` / `off`, because the Hub's map is
 * opaque to it and a boolean would need a schema the platform deliberately
 * does not have.
 */
export const TOGGLES: readonly Toggle[] = [
  { name: "syncReservations", label: "Reservations & stays" },
  { name: "syncRoomState", label: "Room & housekeeping state" },
  { name: "syncGuestProfiles", label: "Guest profiles" },
];

/**
 * What the frame draws as coming later, and why it is drawn at all.
 *
 * ADR 0128 §4 rules v1 inbound-only, so write-back does not exist. Frame 3
 * draws the row anyway, dimmed and tagged — and that is the point: omitting it
 * loses the signal that the capability exists and is coming, while drawing it
 * as a control somebody can press would be a control that lies. Deferred
 * honestly, never disabled and pretending.
 */
export const DEFERRED: readonly Toggle[] = [
  { name: "writeRoomStatusBack", label: "Write room status back to the PMS" },
];
