/**
 * The card the guest signs. Frame 15.
 */

/** One field of the card, as the property configured it. */
export interface CardField {
  label: string;

  /**
   * What the guest gave. Null renders the placeholder.
   *
   * A masked value arrives already masked — `P•••••4412` — because the
   * document number is the guest's and the desk needs enough of it to check a
   * page against a record, not all of it on a screen behind a counter.
   */
  value: string | null;

  /** Shown when the value is null — `optional at this property`. */
  placeholder?: string;

  /** Pushed right inside the box: a chooser's caret, or a count. */
  aside?: string;

  /** Drawn tall, for an address or a paragraph. */
  tall?: boolean;
}

/**
 * One line of the card: a field on its own, or two side by side.
 *
 * **The order is the model, not the screen's.** The design interleaves — a name
 * across the sheet, two fields two-up, an address across it again — and a flat
 * list of fields plus a rule about which ones pair would put the card's order
 * in two places. A card is a legal record whose field order a property may
 * change; it must be expressible as data.
 */
export type CardRow =
  | { readonly kind: "one"; readonly field: CardField }
  | { readonly kind: "pair"; readonly fields: readonly [CardField, CardField] };

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

  /** Why it is showing — `shown because UAE is not this property's home country`. */
  because: string;

  /** Drawn in this order, two-up where the design pairs them. */
  rows: readonly CardRow[];
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
  /** `Checking in · Fatima Sheikh`. */
  who: string;

  /** `Room 506 · 31 Aug → 4 Sep`. */
  where: string;

  /** `GRC 2026/08/1152 · the property's series, next number taken on save`. */
  series: string;

  /** Everything above the conditional block, in the design's order. */
  rows: readonly CardRow[];

  /** Null for a guest whose nationality is the property's own. */
  foreign: ForeignBlock | null;

  /** The documents and signature rows, below the block. */
  closing: readonly CardRow[];

  /** Why the block is there, said in full under it. */
  note: string;

  /**
   * `Filing due 1 Sep (24 h after arrival)`.
   *
   * **The obligation is stated, never enforced** — S19b: an outstanding filing
   * never blocks a check-in. It is a deadline computed from an offset, like
   * every other deadline in this application.
   */
  obligation: string | null;
}
