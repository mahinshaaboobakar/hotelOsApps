/**
 * Setup — seven tabs, the second level of the bar (mockup 02, frames 7a–7g).
 * Everything that reads like a fact about how a hotel works is a property
 * setting with a sensible default; every save is a new version.
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { subnav } from "../../chrome/bar";
import { READ, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { access } from "./access";
import { areas } from "./areas";
import { plan } from "./plan";
import { rules } from "./rules";
import { services } from "./services";
import { windows } from "./windows";
import { zones } from "./zones";

export const TABS = ["Windows & trigger", "Services & minutes", "Rules", "Assignment & zones", "Areas", "Deep clean plan", "Property-wide access"];

export interface Policy {
  triggerMode: string;
  whoLeads: string;
  staySource: string;
  boardDefaultView: string;
  statesDefaultView: string;
  onDepartureCondition: string;
  linenRuleKind: string;
  linenEveryDays: number;
  towels: string;
  turndownEnabled: boolean;
  refreshAfterDays: number;
  dndRecheckMinutes: number;
  supervisorAfterDays: number;
  priorityLadder: string[];
  assignmentStrategy: string;
  unsoldDeparture: string;
  version: number;
  changedAt: string | null;
}

export interface SetupData {
  policy: Policy;
  windows: { window: string; enabled: boolean; starts: string; ends: string; allowAssignmentOutside: boolean; version: number }[];
  changedBy: string | null;
}

export async function setup(host: HostApi, body: HTMLElement, nav: Nav, tab: string, go: (tab: string) => void): Promise<void> {
  body.append(subnav(TABS, tab, go));
  const got = await load<SetupData>(host, READ, "setup");
  if (!got.ok) {
    body.append(failed(host, got.failure, "the property's standard", nav.show));
    return;
  }

  const data = got.value;
  switch (tab) {
    case "Services & minutes": return services(host, body, nav, data);
    case "Rules": return rules(host, body, nav, data);
    case "Assignment & zones": return zones(host, body, nav, data);
    case "Areas": return areas(host, body, nav, data);
    case "Deep clean plan": return plan(host, body, nav, data);
    case "Property-wide access": return access(host, body, nav);
    default: return windows(host, body, nav, data);
  }
}
