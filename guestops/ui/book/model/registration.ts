/**
 * The card the guest signs. Frame 15.
 */

/**
 * One box of the card, as the property configured it.
 *
 * **`name` is the wire's `snake_case`, and it is the same name the property
 * configures.** `RegistrationRule` decides required-ness by these names and the
 * save sends them back under them, so a box's identity is one string all the
 * way through rather than a label matched by eye.
 */
export interface CardField {
  /** `passport_number` — what the property configures and the save sends. */
  name: string;

  label: string;

  /**
   * How it is captured: typed, a date, the property's own list of documents,
   * or `held` for a row this screen shows and cannot capture.
   *
   * **`held` is the honest state for the scans and the signature.** A scan
   * needs the platform's media service and a signature needs a pad; a text box
   * accepting a media reference typed by hand would be a worse lie than the
   * dead `Save` this card is replacing.
   */
  kind: "text" | "date" | "choice" | "held";

  /** What the card holds. Null renders the placeholder. */
  value: string | null;

  /**
   * The masked form, where the value is one the card does not show at rest.
   *
   * A document number arrives whole and masked both — `P•••••4412` beside the
   * number itself — because the desk with the passport in their hand has to be
   * able to retype it. The box shows this until somebody focuses it.
   */
  masked?: string | null;

  /** Shown when the value is null — `optional at this property`. */
  placeholder?: string | null;

  /** The property's accepted documents, for the one chooser on the card. */
  choices?: readonly string[] | null;

  /**
   * Whether this property wants it, for this guest.
   *
   * **Configuration, never the product's opinion.** Two sets exist — one for a
   * guest from the property's home country and one for a guest from anywhere
   * else — and the service applies whichever this guest falls under. Nothing in
   * this module decides it.
   */
  required: boolean;

  /** Drawn tall, for an address or a paragraph. */
  tall?: boolean;
}

/**
 * One line of the card: a box on its own, or two side by side.
 *
 * **The order is the model, not the screen's.** The design interleaves — a name
 * across the sheet, two fields two-up, an address across it again — and a flat
 * list plus a rule about which ones pair would put the card's order in two
 * places. A card is a legal record whose field order a property may change; it
 * must be expressible as data.
 */
export type CardRow =
  | { readonly kind: "one"; readonly field: CardField }
  | { readonly kind: "pair"; readonly fields: readonly CardField[] };

/**
 * The block shown only for a guest from outside the property's home country.
 *
 * **Conditional on the guest, not on the country the software runs in.** The
 * property sets its own home country and both field lists, so a hotel in Kochi
 * treats an Emirati guest this way and a hotel in Dubai treats an Indian guest
 * this way — **from the same product, with no country written into it**.
 */
export interface ForeignBlock {
  /** `Guest from outside`. */
  title: string;

  /** Why it is showing, in the two countries' own codes. */
  because: string;

  /** Drawn in this order, two-up where the design pairs them. */
  rows: readonly CardRow[];
}

/**
 * The card's number — taken, the next one, or none because this property has no
 * GuestOps settings row.
 *
 * **A union rather than a flag, so the third state cannot render a number.**
 * This was `{ number, taken }`, and a caller could take `.number` without
 * looking at `.taken` — which is how the card came to draw `GRC-1` on a property
 * where the capture answers 409 (owner decision B, 2026-10-01). With the number
 * living only on the two arms that have one, *rendering a number for an
 * unconfigured property is a compile error* instead of a rule somebody has to
 * remember.
 *
 * Minting advances the series, so a read never takes a number: a number taken
 * and not used is a gap in the series, and a gap is a question a property gets
 * asked at an inspection.
 *
 * **Proven load-bearing rather than asserted, at `8e7de314`.** Changing the
 * screen's `unconfigured` arm to `return card.series.number` does not compile:
 * `TS2339: Property 'number' does not exist on type '{ readonly state:
 * "unconfigured"; }'`. So the omission is INEXPRESSIBLE, which is a stronger
 * result than a test that would have caught it.
 */
export type CardSeries =
  /** The card exists and holds this number. `GRC-2026/08/1152`. */
  | { readonly state: "taken"; readonly number: string }

  /** No card yet; this is what the property would mint on save. */
  | { readonly state: "next"; readonly number: string }

  /**
   * No number, because nobody has set GuestOps up on this property.
   *
   * **Carries no `number` deliberately.** The series lives in the settings row,
   * so there is nothing to predict — and `MintCardNumber` refuses, which means a
   * desk shown a number here would be told 409 on save.
   */
  | { readonly state: "unconfigured" };

/** When a filing is owed, where this property owes one. */
export interface Obligation {
  /** ISO instant. Rendered where somebody is looking at it. */
  at: string;

  /** The property's configured offset, in hours after arrival. */
  hours: number;
}

/**
 * The registration card — frame 15.
 *
 * **A proposal the property tailors, not a form the platform imposes.** The
 * field list is the design's; which of them are *required* is configuration,
 * separately for domestic and foreign guests, because a resort taking weekend
 * guests and a city hotel taking business visas do not collect the same
 * things.
 *
 * **A field a property does not use is not deleted from the model.** A
 * registration card is a record that must stay readable for years, so an unused
 * field is simply not required.
 */
export interface RegistrationCard {
  stayId: string;

  /** The guest's name, or that nobody has been named. */
  who: string;

  /** The room, where one is assigned. Null is a stay with no room yet. */
  room: string | null;

  /** The version the stay was read at, which the check-in writes against. */
  version: number;

  /** Whether the guest is still expected — what makes checking in the action. */
  arriving: boolean;

  /** ISO day. The header's span is composed where somebody reads it. */
  arrive: string | null;

  /** ISO day, or null where no departure is recorded. */
  depart: string | null;

  /** The property's own home country, which the note explains the block by. */
  homeCountry: string;

  series: CardSeries;

  /** Everything above the conditional block, in the design's order. */
  rows: readonly CardRow[];

  /** Null for a guest whose nationality is the property's own. */
  foreign: ForeignBlock | null;

  /** The documents and signature rows, below the block. */
  closing: readonly CardRow[];

  /**
   * The configured fields this card still lacks.
   *
   * **A prompt, never a gate** — S19b. A guest at the desk at midnight is
   * served and the card is completed after, so this is what the sheet says is
   * wanted rather than what it refuses to save without.
   */
  missing: readonly string[];

  /**
   * When a filing is owed.
   *
   * **The obligation is stated, never enforced** — S19b: an outstanding filing
   * never blocks a check-in. Null where this property files nothing, or where
   * this guest falls outside its reporting scope.
   */
  obligation: Obligation | null;

  /** ISO instant, where the guest has signed. */
  signedAt: string | null;
}
