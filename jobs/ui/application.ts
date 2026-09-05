/**
 * Jobs' desktop module — repairs and tasks, raised from anywhere.
 *
 * # What this is
 *
 * A package's UI, not the shell's: built in the package, shipped in `ui/`, run
 * in its own iframe realm. Its only connection to HotelOS is `@hotelos/sdk`
 * and the port the host transfers in — no ambient capability, so no database,
 * no tuple writer, no route past the Hub.
 *
 * # It is styled, never themed
 *
 * Every colour is a `var()` on the tokens the shell publishes (ADR 0106).
 *
 * # This file composes and holds no screen
 *
 * ADR 0042. Each screen is a directory of its own; what they share is
 * `chrome/` for the drawing and `board/` for the single data seam.
 */

import type { Activate, HostApi, HostedModule } from "@hotelos/sdk";

import { el } from "./chrome/element";
import { MARKS_CSS } from "./chrome/marks";
import { stylesheet } from "./chrome/styles";
import { head, type Tab } from "./chrome/tabs";
import { JOB_READ } from "./chrome/permissions";
import { load, type Operator } from "./board";
import { recordedMe } from "./board/recorded/me";
import { board } from "./screens/board";
import { catalogue } from "./screens/catalogue";
import { job } from "./screens/job";
import { live } from "./screens/live";
import { raise } from "./screens/raise";
import { resolve } from "./screens/resolve";
import { scheduled } from "./screens/scheduled";
import { settings } from "./screens/settings";

/** The five top tabs — the current chrome era (mockup 01). */
const TABS: readonly Tab[] = [
  { label: "Board" }, { label: "Live" }, { label: "Scheduled" }, { label: "Catalogue" }, { label: "Settings" },
];

/** Where the module is, in one object — no screen keeps its own copy. */
interface Place {
  tab: string;
  /** Set while a job is open over the Board tab. */
  jobId: string | null;
  jobTab: string;
  /** "board" | "raise" | "resolve" — what the Board tab is showing. */
  mode: string;
  boardFilter: string;
  boardPage: number;
  settingsTab: string;
  settingsView: string;
}

export const activate: Activate = (host: HostApi): HostedModule => {
  let root: HTMLElement | null = null;

  // Held, not appended once: `show` replaces the root's children on every
  // change, so a stylesheet appended at mount would be deleted by the first
  // render and the module would draw itself unstyled.
  const style = stylesheet([MARKS_CSS]);

  // Drawn only once the service has said who is looking (audit finding,
  // 2026-09-04): the module has no user of its own to name.
  let operator: Operator | null = null;

  const place: Place = {
    tab: "Board", jobId: null, jobTab: "Overview", mode: "board",
    boardFilter: "My departments · ENG", boardPage: 0,
    settingsTab: "Concern policy", settingsView: "engineering",
  };

  function show(): void {
    if (root === null) return;

    const frame = el("div", "jb");
    const main = el("div", "main");
    main.style.display = "flex";
    main.style.flexDirection = "column";
    main.style.minHeight = "0";
    frame.append(head(TABS, place.tab, operator, go), main);
    root.replaceChildren(style, frame);
    void draw(main);
  }

  async function draw(main: HTMLElement): Promise<void> {
    switch (place.tab) {
      case "Live": return live(host, main);
      case "Scheduled": return scheduled(host, main);
      case "Catalogue": return catalogue(host, main, show);
      case "Settings":
        return settings(host, main, {
          tab: place.settingsTab, view: place.settingsView,
          onTab: (label) => { place.settingsTab = label; place.settingsView = label === "Concern policy" ? "engineering" : "list"; show(); },
          onView: (view) => { place.settingsView = view; show(); },
          onChanged: show,
        });
      default:
        if (place.mode === "raise") return raise(host, main, back);
        if (place.mode === "resolve" && place.jobId !== null) {
          return resolve(host, main, place.jobId, () => { place.mode = "board"; show(); });
        }
        if (place.jobId !== null) {
          return job(host, main, {
            jobId: place.jobId, tab: place.jobTab,
            onTab: (label) => { place.jobTab = label; show(); },
            onResolve: () => { place.mode = "resolve"; show(); },
            onBack: back,
            onChanged: show,
          });
        }

        return board(host, main, {
          filter: place.boardFilter, page: place.boardPage,
          onFilter: (label) => { place.boardFilter = label; place.boardPage = 0; show(); },
          onPage: (n) => { place.boardPage = n; show(); },
          onOpen: (id) => { place.jobId = id; place.jobTab = "Overview"; show(); },
          onRaise: () => { place.mode = "raise"; show(); },
        });
    }
  }

  function back(): void {
    place.mode = "board";
    place.jobId = null;
    show();
  }

  function go(label: string): void {
    place.tab = label;
    place.mode = "board";
    place.jobId = null;
    show();
  }

  return {
    mount(element) {
      root = element;
      show();
      void load(host, JOB_READ, "me", recordedMe).then((got) => {
        // Only what the platform actually established. Every screen that
        // stands in says so in a note; the chrome has nowhere to say it, so a
        // name that is not the property's own is not drawn at all.
        if (!got.live) return;

        operator = got.value;
        show();
      });
    },

    unmount() {
      root = null;
    },
  };
};

export default activate;
