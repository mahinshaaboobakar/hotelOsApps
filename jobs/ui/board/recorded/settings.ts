/**
 * The approved example of the settings — mockup 02, frames 1 to 7: Marina
 * Bay's scopes, its seven policies, Engineering's clock, presence, who is
 * told, holds, closing, access.
 */

import type { Settings } from "../model";

export const recordedSettings: Settings = {
  scopes: [
    { label: "Marina Bay · property", state: "default", indent: 0 },
    { label: "Engineering", state: "override", indent: 0 },
    { label: "Housekeeping", state: "inherits", indent: 0 },
    { label: "Front Office", state: "inherits", indent: 0 },
    { label: "Food & Beverage", state: "override", indent: 0 },
    { label: "Housekeeping › Cleaning", state: "due 30 min", indent: 1 },
    { label: "Housekeeping › Bottle of water", state: "due 10 min", indent: 1 },
    { label: "› Deep clean (item)", state: "due 90 min", indent: 2 },
  ],
  policies: [
    { scope: "property", scopeLabel: "Marina Bay", name: "Property default", due: "60 min / 4 h / same shift", atRisk: "75 %", ladder: "assignee → supervisor → manager", usedBy: "FO, F&B and anything else" },
    { scope: "department", scopeLabel: "Engineering", name: "Engineering", due: "40 min / 2 h / same shift", atRisk: "75 %", ladder: "assignee → supervisor → manager → jobs manager", usedBy: "ENG jobs with no category policy" },
    { scope: "category", scopeLabel: "Engineering › AC not working", name: "AC — guest in room", due: "30 min / 90 min / 4 h", atRisk: "70 %", ladder: "assignee → supervisor → manager → jobs manager", usedBy: "every AC item, unless the item has its own" },
    { scope: "item", scopeLabel: "› Water dropping from unit", name: "AC leak — ceiling risk", due: "20 min / 60 min / —", atRisk: "60 %", ladder: "assignee → supervisor → manager → jobs manager", usedBy: "this item only" },
    { scope: "department", scopeLabel: "Housekeeping", name: "Housekeeping", due: "45 min / 2 h / same shift", atRisk: "75 %", ladder: "assignee → supervisor", usedBy: "HK jobs with no category policy" },
    { scope: "category", scopeLabel: "Housekeeping › Cleaning", name: "Cleaning", due: "30 min / 60 min / same shift", atRisk: "75 %", ladder: "assignee → supervisor", usedBy: "every cleaning item" },
    { scope: "category", scopeLabel: "Housekeeping › Bottle of water", name: "Water — 10 minutes", due: "10 min / 20 min / —", atRisk: "50 %", ladder: "assignee → supervisor → manager", usedBy: "every item of that category" },
  ],
  engineeringRules: [
    { priority: "P1", due: "40 min", atRisk: "75 %", notAccepted: "8 min", noSession: "15 min", ladder: "assignee → supervisor → manager → jobs manager", managerAtRisk: true },
    { priority: "P2", due: "2 h", atRisk: "75 %", notAccepted: "20 min", noSession: "45 min", ladder: "assignee → supervisor → manager", managerAtRisk: false },
    { priority: "P3", due: "same shift", atRisk: "80 %", notAccepted: "60 min", noSession: "—", ladder: "assignee → supervisor", managerAtRisk: false },
  ],
  presence: [
    { department: "Engineering", enabled: true, followShifts: true, hours: "07:00 – 23:00", now: "present · day shift since 07:00" },
    { department: "Housekeeping", enabled: true, followShifts: true, hours: "07:00 – 22:00", now: "present · since 07:00" },
    { department: "Food & Beverage", enabled: true, followShifts: false, hours: "06:00 – 00:00", now: "present · by hours" },
    { department: "Front Office", enabled: false, followShifts: false, hours: "—", now: "property clock · always running" },
  ],
  whoIsTold: [
    { role: "Assignee", atRisk: true, breached: "yes", stuck: "—", untriaged: false, repeat: "10 min", departments: "own jobs" },
    { role: "Department supervisor", atRisk: false, breached: "yes", stuck: "yes", untriaged: true, repeat: "15 min", departments: "own department" },
    { role: "Department manager", atRisk: false, breached: "P1 only", stuck: "> 30 min", untriaged: false, repeat: "30 min", departments: "own department" },
    { role: "Property jobs manager", atRisk: false, breached: "P1 only", stuck: "ladder's last step", untriaged: false, repeat: "30 min", departments: "all" },
  ],
  holds: [
    { k: "Requires", v: "a reason and a hold_until date" },
    { k: "Clock", v: "stopped while on hold" },
    { k: "Longest hold", v: "30 days · then STUCK → supervisor" },
  ],
  holdWarnings: [
    { when: "1 day before", who: "department supervisor" },
    { when: "on the day, 08:00", who: "assignee" },
    { when: "date passed, still on hold", who: "supervisor · repeat daily" },
  ],
  closing: [
    { scope: "Property default", hours: "4 h" },
    { scope: "Housekeeping", hours: "1 h" },
    { scope: "Engineering", hours: "4 h" },
  ],
  rating: [
    { k: "Note required", v: "when the resolution is \"Other\"" },
    { k: "Photo", v: "as the catalogue item says: none · optional · required" },
    { k: "Guest rating", v: "ask on close of guest-raised jobs, in the guest app" },
    { k: "Rating scale", v: "1–5 and a line of text" },
  ],
  access: [
    { label: "Property jobs manager", who: "Rohan Desai", from: "granted by the GM in Identity · 2026-08-28" },
    { label: "Department manager · ENG", who: "Kiran Bhat", from: "Workforce headship" },
    { label: "Department supervisor · ENG", who: "Priya Nair", from: "Workforce headship" },
    { label: "Department member · ENG", who: "9 people", from: "Workforce posting" },
    { label: "Department manager · HK", who: "Anjali Rao", from: "Workforce headship" },
  ],
  numbering: "MRN-ENG-… · next 145 · property code from Master Data",
};
