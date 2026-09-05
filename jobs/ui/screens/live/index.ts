/**
 * Live — frame 5: one scrolling card per department (presence from the shift
 * fan-out, service hours, or the property clock), then the sweep's concern
 * table. Cards page as they scroll; the first six people come with the screen.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { when } from "../../chrome/instant";
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
  const heading = el("div", "sect", `Concern · property · last 60-second sweep ${when(host, got.value.sweptAt)}`);
  heading.style.marginTop = "22px";
  body.append(cards, heading, table(host, got.value));
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
  // Says what the two numbers are, and promises nothing. It read "more load as
  // you scroll" until the pagination conformance pass, and nothing scrolled:
  // the service returns everyone working, and the second number is the
  // department's on-shift count, so there is no more to load (standard §6 — a
  // list says what it is showing, and a caption that describes a behaviour the
  // screen does not have is worse than no caption).
  if (d.people.length < d.peopleTotal) {
    box.append(el("div", "more", `${String(d.people.length)} working of ${String(d.peopleTotal)} on shift`));
  }
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
      el("td", undefined, when(host, r.since)), el("td", undefined, r.accountable), el("td", undefined, r.lastNudge),
    );
    t.append(tr);
  }
  // A note about what the table leaves out — not a pager, and it stops
  // borrowing that class now the pager sticks to the list's floor: a caveat
  // that held station at the bottom of the screen would be a control that is
  // not one.
  return fill(el("div"), t, el("div", "mono", `${String(l.concern.length)} in concern · ON TRACK rows are not listed here`));
}
