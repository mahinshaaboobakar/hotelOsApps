/**
 * 7a · Windows & trigger — when the day is attempted, and who starts it
 * (S0's trigger, S5 c2). A window may cross midnight; nothing here is a UTC hour.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { act } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { versionLine, type SetupData } from "./index";

const LABEL: Record<string, string> = { MORNING: "Morning — departures, daily service, refresh", EVENING: "Turndown — evening" };

export function windows(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): void {
  const said = el("p", "said");
  const table = el("table");
  const head = el("tr");
  for (const name of ["Window", "On", "From", "To", "Assign outside it"]) head.append(el("th", undefined, name));
  table.append(head);
  const rows = data.windows.map((w) => {
    const on = el("input") as HTMLInputElement;
    on.type = "checkbox";
    on.checked = w.enabled;
    const from = el("input", "cell") as HTMLInputElement;
    from.type = "time";
    from.value = w.starts;
    const to = el("input", "cell") as HTMLInputElement;
    to.type = "time";
    to.value = w.ends;
    const outside = el("input") as HTMLInputElement;
    outside.type = "checkbox";
    outside.checked = w.allowAssignmentOutside;
    const tr = el("tr");
    const cells = [el("td", undefined, LABEL[w.window] ?? w.window), el("td"), el("td"), el("td"), el("td")];
    cells[1]!.append(on);
    cells[2]!.append(from);
    cells[3]!.append(to);
    cells[4]!.append(outside, document.createTextNode(" the supervisor may, for an arrival before it opens"));
    tr.append(...cells);
    table.append(tr);
    return { w, on, from, to, outside };
  });

  const trigger = el("section", "card");
  trigger.style.marginTop = "14px";
  trigger.append(el("h3", undefined, "Trigger — who prepares the day"));
  const mode = (value: string, text: string): HTMLElement => {
    const label = el("label");
    const radio = el("input") as HTMLInputElement;
    radio.type = "radio";
    radio.name = "trigger";
    radio.value = value;
    radio.checked = data.policy.triggerMode === value;
    label.append(radio, document.createTextNode(` ${text}`));
    return el("div").appendChild(label).parentElement as HTMLElement;
  };
  trigger.append(
    mode("PREPARE", "Prepare by button or HosPilot (default) — changes are collected; later presses add and never remove"),
    mode("AUTOMATIC", "Automatic — the decision runs on every change and places the work in the next window"),
    el("p", "dim", "HosPilot is always on — not a mode: it presses the button as the person asking (S7)."),
  );

  const save = async (): Promise<void> => {
    for (const r of rows) {
      const done = await act(host, "roomcare.configure", "saveWindow", { window: r.w.window, starts: r.from.value, ends: r.to.value, enabled: r.on.checked, allowAssignmentOutside: r.outside.checked, version: r.w.version });
      if (!done.ok) { said.className = "said bad"; said.textContent = done.because; return; }
    }
    const chosen = trigger.querySelector<HTMLInputElement>("input[name=trigger]:checked")?.value ?? data.policy.triggerMode;
    const done = await act(host, "roomcare.configure", "savePolicy", { triggerMode: chosen, version: data.policy.version });
    if (!done.ok) { said.className = "said bad"; said.textContent = done.because; return; }
    nav.show();
  };

  const foot = el("div", "row");
  foot.style.marginTop = "14px";
  foot.append(control("btn pri", "Save", () => void save()), control("btn", "Discard", nav.show), versionLine(host, data));
  body.append(table, el("div", "legend", `${data.windows.length} of ${data.windows.length} — a window may cross midnight (a night shift is 22:00 → 06:00, not two ranges)`), trigger, foot, said);
}
