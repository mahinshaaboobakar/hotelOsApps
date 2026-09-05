/**
 * Settings — mockup 02: six tabs, one frame each, with the scope rail on the
 * concern-policy tab; numbering is a read-only line, not a tab.
 */

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { JOB_CONFIGURE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { subnav } from "../../chrome/tabs";
import { saying } from "../../chrome/form";
import { act, load, may, type Settings } from "../../board";
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

  /** Redraw from the service after a save — and what Discard goes back to. */
  onChanged: () => void;
}

export async function settings(host: HostApi, main: HTMLElement, place: SettingsPlace): Promise<void> {
  const got = await load(host, JOB_READ, "settings", recordedSettings);
  const s = got.value;
  const configure = may(host, JOB_CONFIGURE);
  const body = el("div", "body");
  body.append(subnav(SETTINGS_TABS.map((label) => ({ label })), place.tab, place.onTab, el("span", "mono", `Numbering · ${s.numbering}`)));
  const said = saying();
  const save = (method: string, params: unknown): void => {
    void act(host, JOB_CONFIGURE, method, params).then((done) => {
      if (done.ok) place.onChanged();
      else said.say(done.refused ?? "the setting was not saved");
    });
  };

  body.append(tab(s, place, configure, save, place.onChanged), said.line);
  if (!got.live) body.append(standIn("settings", got.because));
  main.replaceChildren(body);
}

function tab(
  s: Settings,
  place: SettingsPlace,
  configure: boolean,
  save: (method: string, params: unknown) => void,
  discard: () => void,
): HTMLElement {
  switch (place.tab) {
    case "Shifts & presence": return presence(s, configure, save, discard);
    case "Who is told": return whoIsTold(s, configure);
    case "Holds & reminders": return holds(s, configure, save, discard);
    case "Closing & rating": return closing(s, configure, save, discard);
    case "Access": return access(s);
    default:
      if (place.view === "list") return policies(s, configure, place.onView);
      if (place.view === "1" || place.view === "2" || place.view === "3") return policyFlow(s, place.view, place.onView);
      return concernPolicy(s, configure, place.onView);
  }
}
