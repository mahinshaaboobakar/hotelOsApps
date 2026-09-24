/**
 * Which overlay is open, and what opens it.
 *
 * **Extracted from `application.ts`, which this took over 300 code lines.**
 * It went 273 -> 314 as N1, C1, N2 and C2 landed, and NOTHING SAID SO: no
 * standards checker could see this repository until `scripts/check_standards.py`
 * (2026-09-24), and its first run found this. ADR 0036.
 *
 * **A move, not a rewrite.** The same chain in the same order, with `host`,
 * `main`, `where` and `show` as parameters where they were closure captures.
 * Output, interfaces and state handling are unchanged, so the build and the
 * existing suite are what verify it.
 *
 * **The boundary already existed.** This was one contiguous `const overlay =`
 * inside `draw`, and the extraction follows it rather than cutting where the
 * line count happened to fall — which is the half of ADR 0036 that stops a
 * ceiling producing a `helpers.ts`.
 *
 * The sheet stands OVER whatever screen is drawn, so it is appended after the
 * screen rather than instead of it — frame 10 is the day, dimmed, with the
 * walk-in on top of it.
 */

import type { HostApi } from "@hotelos/sdk";

import { cannot } from "./chrome/marks";
import { sheet } from "./chrome/overlay";
import { assignRoom } from "./screens/assign";
import { registrationCard } from "./screens/registration";
import { correctDialog } from "./screens/stay/correct";
import { nobodyCameDialog } from "./screens/stay/nobody-came";
import { walkIn } from "./screens/walkin";
import type { Place } from "./application";

/**
 * Draw whichever overlay the place names, over the screen already drawn.
 *
 * @param host the module host
 * @param main where the screen — and so the scrim — is drawn
 * @param where the place, which names the overlay and what it is about
 * @param show how to move, which is how every overlay closes
 */
export function overlay(
  host: HostApi,
  main: HTMLElement,
  where: Place,
  show: (next: Partial<Place>) => void,
): void {
    if (where.overlay === "walkin") {
      // **The sheet captures now.** `stay.create/walkIn` was declared,
      // served, and unreachable: this drew `recordedWalkIn` and called
      // `perform` nowhere. What kept it that way was not the write — it was
      // that nothing in this application could name a ROOM, and check-in
      // needs one (S8). `reservation.read/rooms` is that read.
      void walkIn(
        host,
        main,
        () => show({ overlay: null }),
        // In house: the sheet closes and the stay is opened, because the
        // service says the guest is there and the stay screen is what shows
        // it. A partial outcome does NOT come here — the sheet keeps itself
        // open and says the stay exists.
        (stayId) => show({ screen: "Stay", tab: "Overview", stayId, overlay: null }),
      );
    }

    // The card stands over the day, because that is where a check-in starts:
    // a receptionist opens it from the arrival they are looking at, and the
    // list stays behind it.
    // Frame 3's *Move room*, and the day list's `＋ assign` — one sheet, and
    // the service derives which of the two it is recording.
    if (where.overlay === "assign") {
      void assignRoom(
        host,
        main,
        where.stayId,
        () => show({ overlay: null }),
        // Recorded: the sheet closes and the screen behind redraws from the
        // service. It does not patch its own rows — the assignment is the
        // service's, and a client editing its copy would be a second place
        // it is decided.
        () => show({ overlay: null }),
      );
    }

    if (where.overlay === "nobodyCame" || where.overlay === "correct") {
      // Reached without a stay is a defect rather than an empty dialog —
      // every route here comes from one. Nothing was asked of the platform,
      // so there is no answer to report.
      if (where.stayId === "") {
        main.append(sheet({
          title: where.overlay === "correct" ? "Correct this stay" : "Record a no-show",
          subtitle: "no stay was chosen",
          body: [cannot(
            "No stay was chosen",
            "Open a stay and act from there. Nothing was asked of the "
            + "platform here, so there is no answer to report.",
          )],
          foot: null,
          actions: [{ label: "Close", onClick: () => show({ overlay: null }) }],
          onDismiss: () => show({ overlay: null }),
        }));
        return;
      }

      const open = where.overlay === "correct"
        ? correctDialog(
          host, main, where.stayId, host.property,
          () => show({ overlay: null }),
          // Recorded: the overlay closes and the screen behind redraws from
          // the service. It does not patch its own copy — the lifecycle is
          // the service's, and a client editing its own would be a second
          // place it is decided.
          () => show({ overlay: null }),
        )
        : nobodyCameDialog(
          host, main, where.stayId, host.property,
          () => show({ overlay: null }),
          () => show({ overlay: null }),
        );

      void open;
      return;
    }

    if (where.overlay === "registration") {
      // **Reached without a stay, which is a defect rather than an empty
      // card.** Every route here comes from a stay, and a card drawn without
      // one would be somebody's. Nothing was asked of the platform, so there
      // is no answer to report.
      if (where.stayId === "") {
        main.append(sheet({
          title: "Registration card",
          subtitle: "no stay was chosen",
          body: [cannot(
            "No stay was chosen",
            "Open a stay and check the guest in from there. Nothing was asked "
            + "of the platform here, so there is no answer to report.",
          )],
          foot: null,
          actions: [{ label: "Close", onClick: () => show({ overlay: null }) }],
          onDismiss: () => show({ overlay: null }),
        }));
        return;
      }

      void registrationCard(
        host,
        main,
        where.stayId,
        () => show({ overlay: null }),
        // Checked in: the card closes and the screen behind redraws from the
        // service. It does not patch its own rows — the lifecycle is the
        // service's, and a client editing its copy would be a second place
        // it is decided.
        () => show({ overlay: null }),
      );
    }
}
