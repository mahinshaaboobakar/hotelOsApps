/**
 * Room Care's module — the bar, the sections a person holds, and where they are.
 *
 * The sections come from what this person was granted in this module
 * (`host.identity.capabilities` — the person's permissions intersected with the
 * application's admission). A supervisor sees the six-tab bar the frames lock:
 * Board · Prepare · Room states · Supervision · Deep clean · Setup, each only if
 * held. An attendant, who holds `room.clean` and none of the supervisor's
 * capabilities, sees one section: My rooms (frame 3).
 */

import type { Activate, HostApi, HostedModule } from "@hotelos/sdk";

import { head } from "./chrome/bar";
import { el } from "./chrome/element";
import { READ, holds, load } from "./chrome/load";
import { stylesheet } from "./chrome/styles";
import type { Read } from "./chrome/load";
import type { Operator } from "./model";
import { board } from "./screens/board";
import { deepClean } from "./screens/deepclean";
import { myRooms } from "./screens/myrooms";
import { prepare } from "./screens/prepare";
import { room } from "./screens/room";
import { setup } from "./screens/setup";
import { states } from "./screens/states";
import { supervision } from "./screens/supervision";

/** Each section and the capability that opens it. */
const SECTIONS: readonly (readonly [string, string])[] = [
  ["Board", "roomcare.read"],
  ["Prepare", "roomcare.assign"],
  ["Room states", "roomcare.amend"],
  ["Supervision", "roomcare.amend"],
  ["Deep clean", "roomcare.plan"],
  ["Setup", "roomcare.configure"],
];

const SUPERVISING = ["roomcare.assign", "roomcare.amend", "roomcare.configure", "roomcare.plan"];

/** Where a person is — kept while the module is mounted. */
export interface Place {
  section: string;
  roomId: string | null;
  taskId: string | null;
  page: number;
  setupTab: string;
}

/** The sections this person holds, in the bar's order. */
export function sectionsFor(host: HostApi): string[] {
  const supervisor = SUPERVISING.some((c) => holds(host, c));
  if (!supervisor && holds(host, "room.clean")) return ["My rooms"];
  return SECTIONS.filter(([, capability]) => holds(host, capability)).map(([label]) => label);
}

export const activate: Activate = (host: HostApi): HostedModule => {
  let root: HTMLElement | null = null;
  let operator: Read<Operator> | null = null;
  const sections = sectionsFor(host);
  const place: Place = { section: sections[0] ?? "Board", roomId: null, taskId: null, page: 0, setupTab: "Windows & trigger" };

  function show(): void {
    if (root === null) return;
    const frame = el("div", "rc");
    const body = el("main", "body");
    frame.append(head(sections, place.section, operator, go), body);
    root.replaceChildren(stylesheet(), frame);
    void draw(frame, body);
  }

  async function draw(frame: HTMLElement, body: HTMLElement): Promise<void> {
    const nav = { frame, show, openRoom, back };
    if (sections.length === 0) {
      body.append(el("div", "note", "This person holds no Room Care capability at this property."));
      return;
    }
    if (place.roomId !== null) return room(host, body, nav, place.roomId);
    switch (place.section) {
      case "Prepare": return prepare(host, body, nav, place.page, (p) => { place.page = p; show(); });
      case "Room states": return states(host, body, nav);
      case "Supervision": return supervision(host, body, nav, place.page, (p) => { place.page = p; show(); });
      case "Deep clean": return deepClean(host, body, nav, place.page, (p) => { place.page = p; show(); });
      case "Setup": return setup(host, body, nav, place.setupTab, (t) => { place.setupTab = t; show(); });
      case "My rooms": return myRooms(host, body, nav, place.taskId, (t) => { place.taskId = t; show(); });
      default: return board(host, body, nav);
    }
  }

  function openRoom(id: string): void {
    place.roomId = id;
    show();
  }

  function back(): void {
    place.roomId = null;
    show();
  }

  function go(section: string): void {
    place.section = section;
    place.roomId = null;
    place.taskId = null;
    place.page = 0;
    show();
  }

  return {
    mount(element) {
      root = element;
      show();
      void load<Operator>(host, READ, "me").then((got) => {
        operator = got;
        show();
      });
    },
    unmount() {
      root = null;
    },
  };
};

export default activate;
