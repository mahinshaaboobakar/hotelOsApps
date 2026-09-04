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
    {
      id: "i-nc", categoryId: "c-ac", name: "Not cooling", department: "Engineering (ENG) · inherited from the category",
      defaultPriority: "P2", dueWithinMinutes: 40, restricted: false,
      aliases: ["AC not working", "room warm", "AC broken", "cooling"],
      activeAt: [{ property: "Marina Bay", on: true }, { property: "Marina Hills", on: true }, { property: "Marina Airport", on: false }],
      resolutions: ["Filter cleaned", "Filter replaced", "Refrigerant topped up", "Thermostat replaced", "Compressor fault — escalate to vendor", "No fault found"],
    },
    {
      id: "i-wd", categoryId: "c-ac", name: "Water dropping from unit", department: "Engineering (ENG)",
      defaultPriority: "P2", dueWithinMinutes: 60, restricted: false,
      aliases: ["AC leaking", "water from AC", "ceiling wet under AC"],
      activeAt: [{ property: "Marina Bay", on: true }, { property: "Marina Hills", on: true }, { property: "Marina Airport", on: true }],
      resolutions: ["Drain cleared", "Drain pipe replaced", "Condensate pump replaced"],
    },
  ],
};
