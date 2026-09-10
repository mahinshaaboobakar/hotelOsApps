/**
 * The approved example of the catalogue — frame 7: Marina Hotels' master,
 * Air conditioning › Not cooling opened, its aliases and resolutions.
 */

import type { Catalogue } from "../model";

export const recordedCatalogue: Catalogue = {
  organisation: "Marina Hotels",
  categories: [
    { id: "c-ac", name: "Air conditioning", department: "ENG", items: 6, activeHere: true },
    { id: "c-pl", name: "Plumbing", department: "ENG", items: 9, activeHere: true },
    { id: "c-li", name: "Lighting", department: "ENG", items: 4, activeHere: true },
    { id: "c-hk", name: "Housekeeping", department: "HK", items: 12, activeHere: true },
    { id: "c-gr", name: "Guest request", department: "FO", items: 7, activeHere: true },
    { id: "c-spa", name: "Spa", department: "SPA", items: 3, activeHere: false },
  ],
  items: [
    // **The frame's own item.** The Raise frame draws a bedside lamp in Room
    // 0817, and the harness held no such item — so a capture of that screen
    // could never reach the state the drawing specifies, and six nodes paired.
    // §8's fixture rule, extended to state (ARCH-Q20, 2026-09-10): a frame
    // drawn for measurement draws the fixture the harness holds, and the
    // harness is the side that moves.
    {
      id: "i-bl", categoryId: "c-li", name: "Bedside lamp dead", department: "Engineering (ENG) · inherited from the category",
      defaultPriority: "P3", dueWithinMinutes: 240, restricted: false,
      aliases: ["lamp not working", "bedside light dead"],
      activeAt: [{ property: "Marina Bay", on: true }],
      resolutions: [{ id: "r7", name: "Bulb replaced", noteRequired: false }, { id: "r8", name: "Fitting replaced", noteRequired: false }],
    },
    {
      id: "i-nc", categoryId: "c-ac", name: "Not cooling", department: "Engineering (ENG) · inherited from the category",
      defaultPriority: "P2", dueWithinMinutes: 40, restricted: false,
      aliases: ["AC not working", "room warm", "AC broken", "cooling"],
      activeAt: [{ property: "Marina Bay", on: true }, { property: "Marina Hills", on: true }, { property: "Marina Airport", on: false }],
      resolutions: [{ id: "r1", name: "Filter cleaned", noteRequired: false }, { id: "r2", name: "Filter replaced", noteRequired: false }, { id: "r3", name: "Refrigerant topped up", noteRequired: false }, { id: "r4", name: "Thermostat replaced", noteRequired: false }, { id: "r5", name: "Compressor fault — escalate to vendor", noteRequired: false }, { id: "r6", name: "No fault found", noteRequired: false }],
    },
    {
      id: "i-wd", categoryId: "c-ac", name: "Water dropping from unit", department: "Engineering (ENG)",
      defaultPriority: "P2", dueWithinMinutes: 60, restricted: false,
      aliases: ["AC leaking", "water from AC", "ceiling wet under AC"],
      activeAt: [{ property: "Marina Bay", on: true }, { property: "Marina Hills", on: true }, { property: "Marina Airport", on: true }],
      resolutions: [{ id: "r1", name: "Drain cleared", noteRequired: false }, { id: "r2", name: "Drain pipe replaced", noteRequired: false }, { id: "r3", name: "Condensate pump replaced", noteRequired: false }],
    },
  ],
};
