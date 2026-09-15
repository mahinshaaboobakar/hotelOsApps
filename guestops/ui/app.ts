/**
 * What this application is called, where a person reads it.
 *
 * **Its own module so that both bundles can have it and neither pays for the
 * other.** The screens reach it through `book`, which carries the whole
 * reservation model; a widget is a separate bundle showing four numbers and
 * must not pull that in to learn its own name. A file holding one string is
 * imported by both and costs a widget nothing.
 *
 * **Not `host.identity.id`.** That is the package identifier — `guestops` — and
 * a failure sentence reading *guestops did not answer* names the right thing in
 * the wrong register to somebody standing at a desk. The SDK asks for this
 * separately for exactly that reason.
 */

/** The name a person reads. */
export const APP = "GuestOps";
