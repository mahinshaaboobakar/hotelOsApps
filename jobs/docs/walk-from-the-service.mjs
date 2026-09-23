/**
 * What the Jobs service saw while somebody walked the screens.
 *
 * ```
 * node jobs/docs/walk-from-the-service.mjs                    # since the process last started
 * node jobs/docs/walk-from-the-service.mjs 2026-09-23T06:21   # since an instant, in UTC
 * ```
 *
 * **Every time it prints is UTC**, which is what the log's own `instant` carries;
 * the service's bracketed prefix is the machine's local time, and mixing the two
 * gave a first-seen later than a last-seen on the first run of this script.
 *
 * **This is the service's account, not the screen's.** It reads the installed
 * application's own log — every module call, its status and its duration — and
 * says nothing about what was drawn, what was pressed, or what a person
 * understood. A walk reported from here is a walk of the wire. The screen's
 * account needs eyes on the screen, and the two are not substitutes: a call
 * that answered 200 can still have drawn nothing a person could use.
 *
 * It exists because the alternative was worse. This stream cannot drive the
 * installed desktop — its bearer is refused by the real Kernel, and the
 * WebView has no debugging port — so the choice was between the service's
 * account, honestly labelled, and a harness walk dressed as a live one.
 *
 * What it cannot see, stated so a green line is not read as more than it is:
 * a control that was never pressed (no call, no line), a screen that drew a
 * failure from a cached read, anything the UI decided without asking the
 * service, and every refusal the Kernel made before the request reached Jobs.
 */

import { readFileSync, existsSync } from "node:fs";
import { join } from "node:path";

const LOG = join(process.env.LOCALAPPDATA ?? "", "HotelOS", "logs", "apps", "jobs", "current.jsonl");

/** `POST http://127.0.0.1:53588/module/job.read/board - 200` and the like. */
const FINISHED = /Request finished HTTP\/1\.1 POST \S+\/module\/([a-z.]+)\/([A-Za-z]+) - (\d{3})/;
const STARTED = /Application started/;
const REFUSED = /refused|denied|forbidden|InvalidRequest|Unauthorized/i;

function lines() {
  if (!existsSync(LOG)) {
    console.error(`no Jobs log at ${LOG} — is the application installed and running?`);
    process.exit(2);
  }
  return readFileSync(LOG, "utf8").split("\n").filter((l) => l.startsWith("{")).map((l) => {
    try { return JSON.parse(l); } catch { return null; }
  }).filter((entry) => entry !== null);
}

const since = process.argv[2];
const entries = lines();

// Default window: whatever the service has done since it last started, because a
// walk of an application that restarted mid-walk is two walks.
let from = 0;
if (since === undefined) {
  for (const [i, entry] of entries.entries()) if (STARTED.test(entry.message ?? "")) from = i;
} else {
  from = entries.findIndex((entry) => (entry.instant ?? "") >= since);
  if (from < 0) { console.error(`no line at or after ${since} (UTC, as the log records it)`); process.exit(2); }
}

const window = entries.slice(from);
const calls = new Map();
const refusals = [];

for (const entry of window) {
  const message = entry.message ?? "";
  const call = FINISHED.exec(message);
  if (call !== null) {
    const [, capability, method, status] = call;
    const key = `${capability}/${method}`;
    const seen = calls.get(key) ?? { ok: 0, statuses: new Map(), first: entry.instant, last: entry.instant };
    seen.statuses.set(status, (seen.statuses.get(status) ?? 0) + 1);
    if (status === "200") seen.ok += 1;
    seen.last = entry.instant;
    calls.set(key, seen);
  } else if (REFUSED.test(message)) {
    refusals.push(`${(entry.instant ?? "").slice(11, 19)}  ${message.slice(0, 160)}`);
  }
}

const started = window.find((entry) => STARTED.test(entry.message ?? ""));
console.log(`# The service's account of the walk\n`);
console.log(`Log: ${LOG}`);
console.log(`Window: ${window.length} lines${started ? `, from the service's start at ${started.instant}` : ""}`);
console.log(`\n**This is what the service saw. It is not what the screen drew.**\n`);

if (calls.size === 0) {
  console.log("No module call in the window — nothing was asked of Jobs. Either nobody opened it, or the desktop never reached this service.");
} else {
  console.log("| Call | Answered | Statuses | First (UTC) | Last (UTC) |");
  console.log("|---|---|---|---|---|");
  for (const [key, seen] of [...calls.entries()].sort()) {
    const statuses = [...seen.statuses.entries()].map(([s, n]) => `${s}×${n}`).join(" · ");
    console.log(`| \`${key}\` | ${seen.ok} | ${statuses} | ${(seen.first ?? "").slice(11, 19)} | ${(seen.last ?? "").slice(11, 19)} |`);
  }
}

console.log(`\n## Refusals and faults in the window: ${refusals.length}`);
for (const line of refusals.slice(-40)) console.log(`    ${line}`);

// The ledger names 44 operations; what a walk never asked for is as much a
// finding as what failed, and only a person can say whether they pressed it.
const asked = new Set([...calls.keys()].map((k) => k.split("/")[1]));
console.log(`\n## Never asked in this window\n`);
console.log([...asked].length === 0 ? "everything" : `${asked.size} of Jobs' 44 operations were asked for. The rest were not — which says nothing about whether their controls were pressed, only that no call reached the service.`);
