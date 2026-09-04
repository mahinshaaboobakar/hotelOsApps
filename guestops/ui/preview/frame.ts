/**
 * One module instance, in one realm.
 *
 * Each pane of the harness is a real `<iframe>`, because that is the shape the
 * module actually gets (ADR 0128 §7). It matters visually: the module sizes
 * itself with `100vh`, which is its realm's height in production and would be
 * the whole scrolling page if it were mounted into a plain `<div>` — the
 * capture would then show a layout no property will ever see.
 *
 * # It fakes the host and nothing else
 *
 * The identity, the granted capabilities and the answers to `host.call` are
 * this file's. The module's own code, its stylesheet and its token references
 * are the shipped ones, so what appears here is what a property would see.
 *
 * The answers are `book/recorded.ts` — the approved frames' own data — served
 * as though the platform had returned them. That exercises the **live** path,
 * which is the one a property will use; the harness's fourth pane grants
 * nothing and exercises the fallback.
 */

import type { HostApi } from "@hotelos/sdk";

import { activate } from "../application";
import { recordedAttention, recordedStay, recordedToday } from "../book";

/**
 * A host that grants what the manifest requests and answers from the fixtures.
 *
 * `granted` is a parameter so the harness can show the refusal path too: the
 * module renders a stand-in banner when a capability was not granted, and that
 * banner is a design element the audit has to be able to see.
 */
function host(granted: readonly string[]): HostApi {
  return {
    identity: { id: "guestops", version: "0.1.0", capabilities: granted },

    // The host tells a module its property's zone and locale. Both are `null`
    // here on purpose: the SDK types them nullable because a property that has
    // not been configured is a real state, and a double that invented
    // "Asia/Kolkata" would hide every place this module forgets to handle it.
    property: { timezone: null, locale: null },

    call(capability: string, method: string): Promise<unknown> {
      if (method === "today") return Promise.resolve(recordedToday);
      if (method === "attention") return Promise.resolve(recordedAttention);
      if (method === "stay") return Promise.resolve(recordedStay);
      return Promise.reject(new Error(`unhandled ${capability}/${method}`));
    },

    on(): () => void {
      return () => {};
    },
  };
}

const params = new URLSearchParams(location.search);
const screen = params.get("screen") ?? "today";

const granted = params.get("granted") === "none"
  ? []
  : ["reservation.read", "stay.override", "registration.capture", "request.handle"];

activate(host(granted)).mount(document.body);

/** Click the first element matching `selector` whose text contains `text`. */
function click(selector: string, text: string): void {
  for (const node of Array.from(document.querySelectorAll<HTMLElement>(selector))) {
    if (node.textContent?.includes(text) === true) {
      node.click();
      return;
    }
  }
}

/**
 * Drive this realm to the screen it was asked for, then say so.
 *
 * The flag is what the capture waits on. A screenshot taken on a timer catches
 * a half-rendered screen often enough to be believed, and a loading state
 * photographs well.
 */
function drive(): void {
  if (screen === "attention") click(".ri", "Attention");
  if (screen === "stay") click(".tr.act", "Rajesh Pillai");

  // Two frames: one for the click's own render, one for the screen it opened.
  requestAnimationFrame(() =>
    requestAnimationFrame(() =>
      setTimeout(() => document.documentElement.setAttribute("data-ready", "true"), 40)));
}

setTimeout(drive, 60);
