/**
 * Settings — mockup 02: six tabs, one frame each, with the scope rail on the
 * concern-policy tab; numbering is a read-only line, not a tab.
 */

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { JOB_CONFIGURE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { subnav } from "../../chrome/tabs";
import { load, may, type Settings } from "../../board";
import { recordedSettings } from "../../board/recorded/settings";
import { concernPolicy, policies, policyFlow } from "./policies";
import { access, closing, holds, presence, whoIsTold } from "./tabs";

export const SETTINGS_TABS = ["Concern policy", "Shifts & presence", "Who is told", "Holds & reminders", "Closing & rating", "Access"];

/** What the settings screen is told and tells back. */
export interface SettingsPlace {
  tab: string;
  /** "list" (frame 7), "engineering" (frame 1), or a flow step "1" | "2" | "3" (frames 8–10). */
  view: string;
  onTab: (label: string) => void;
  onView: (view: string) => void;
}

export async function settings(host: HostApi, main: HTMLElement, place: SettingsPlace): Promise<void> {
  const got = await load(host, JOB_READ, "settings", recordedSettings);
  const s = got.value;
  const configure = may(host, JOB_CONFIGURE);
  const body = el("div", "body");
  body.append(subnav(SETTINGS_TABS.map((label) => ({ label })), place.tab, place.onTab, el("span", "mono", `Numbering · ${s.numbering}`)));
  body.append(tab(s, place, configure));
  if (!got.live) body.append(standIn("settings", got.because));
  main.replaceChildren(body);
}

function tab(s: Settings, place: SettingsPlace, configure: boolean): HTMLElement {
  switch (place.tab) {
    case "Shifts & presence": return presence(s, configure);
    case "Who is told": return whoIsTold(s, configure);
    case "Holds & reminders": return holds(s, configure);
    case "Closing & rating": return closing(s, configure);
    case "Access": return access(s);
    default:
      if (place.view === "list") return policies(s, configure, place.onView);
      if (place.view === "1" || place.view === "2" || place.view === "3") return policyFlow(s, place.view, place.onView);
      return concernPolicy(s, configure, place.onView);
  }
}
