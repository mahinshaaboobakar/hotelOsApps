/**
 * Live — frame 5: one scrolling card per department (presence from the shift
 * fan-out, service hours, or the property clock), then the sweep's concern
 * table. Cards page as they scroll; the first six people come with the screen.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { clock, when } from "../../chrome/instant";
import { concern } from "../../chrome/marks";
import { JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { load, type Live, type LiveDepartment } from "../../board";
import { recordedLive } from "../../board/recorded/live";

export async function live(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load(host, JOB_READ, "live", recordedLive);
  const body = el("div", "body");
  const cards = el("div", "cols3");
  for (const d of got.value.departments) cards.append(department(d));
  body.append(cards, el("div", "sect", `Concern · property · last 60-second sweep ${when(host, got.value.sweptAt)}`), table(host, got.value));
  if (!got.live) body.append(standIn("live board", got.because));
  main.replaceChildren(body);
}

function department(d: LiveDepartment): HTMLElement {
  const box = el("div", "card");
  const title = el("h3");
  const presence = d.presence === "off" ? el("span", "pill", "no presence") : el("span", "pill ok", "present");
  title.append(document.createTextNode(`${d.name} · `), presence);
  box.append(title);
  if (d.presence === "off") box.append(el("div", "mono", d.presenceLine));
  else box.append(el("div", "mono", d.presenceLine));

  const list = el("div", "scroll");
  for (const p of d.people) {
    const row = el("div", "wrow");
    row.append(el("span", undefined, p.name), p.tone === "dim" ? el("span", "mono", p.doing) : el("span", `pill ${p.tone}`, p.doing));
    list.append(row);
  }
  box.append(list);
  if (d.people.length < d.peopleTotal) box.append(el("div", "more", `${String(d.people.length)} of ${String(d.peopleTotal)} · more load as you scroll`));
  const bar = fill(el("div", "bar"), el("i", d.breached > 0 ? "bad" : ""));
  (bar.firstElementChild as HTMLElement).style.width = `${String(Math.min(100, Math.round((d.open / 20) * 100)))}%`;
  box.append(bar, el("div", "mono", `${String(d.open)} open · ${String(d.breached)} breached`));
  return box;
}

function table(host: HostApi, l: Live): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of ["Job", "Dept", "State", "Since", "Accountable now", "Last nudge"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const r of l.concern) {
    const tr = el("tr");
    tr.append(
      el("td", "num", r.number), el("td", undefined, r.department), fill(el("td"), concern(r.concern)),
      el("td", undefined, clock(host, r.since)), el("td", undefined, r.accountable), el("td", undefined, r.lastNudge),
    );
    t.append(tr);
  }
  return fill(el("div"), t, el("div", "pager", `${String(l.concern.length)} of ${String(l.concern.length)} in concern · ON TRACK rows are not listed here`));
}
