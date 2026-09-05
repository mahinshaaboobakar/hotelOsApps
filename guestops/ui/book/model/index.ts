/**
 * The shapes the three screens read — the approved design's vocabulary, typed.
 *
 * These are **view shapes, not the domain**. The service's `RoomStay` carries a
 * lifecycle enum and a version; a table row carries the string a receptionist
 * reads. Keeping them apart is what lets the screens be built to the gold
 * frames while the wire contract is still `service.proto`'s.
 *
 * Every field here exists because a gold frame draws it. Nothing is here
 * speculatively — a field the design does not show is a field the module has no
 * way to render honestly.
 */

export * from "./availability";
export * from "./booking";
export * from "./attention";
export * from "./day";
export * from "./stay";
export * from "./tabs";
