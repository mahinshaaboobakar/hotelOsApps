/**
 * The property the capture harnesses tell the module about.
 *
 * **Unestablished by default, and only ever stated by the URL.** A property with
 * no locale and no zone is a real state — the SDK types both nullable because
 * Master Data may not have answered yet — and it is the state that shows whether
 * a screen falls back honestly (page 64 §11, I4; §12, U2). So nothing here
 * invents one. `?locale=en-IN&tz=Asia/Kolkata` states one, for the captures that
 * need a property's own form: the choice is written in the address of the
 * capture that made it, never defaulted in code.
 *
 * Both harnesses — the screen's and the widgets' — read it here, so the two
 * cannot disagree about which property a capture was taken for.
 */

import type { PropertyEnvironment } from "@hotelos/sdk";

/** The property this capture was asked for, or the unestablished one. */
export function statedProperty(search: string = location.search): PropertyEnvironment {
  const params = new URLSearchParams(search);
  return { locale: params.get("locale"), timezone: params.get("tz") };
}
