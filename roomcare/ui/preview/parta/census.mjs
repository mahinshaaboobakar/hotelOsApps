// What each frame's comparison MEASURED, before anyone reads what it found.
//
// ```
// node preview/parta/census.mjs .parta
// ```
//
// A zero from a measurement is a claim about the instrument before it is a claim
// about the thing (GG's rule), and a denominator can be present, correct and
// still describe the wrong population (DD's). So this prints, per frame, the
// instrument's own counts — drawn, built, paired, and paired as a share of drawn
// — and the tag census of each side, which is what says whether the two could
// pair at all under a tag-and-text key: a frame that draws its tiles and chips
// as <div> cannot pair with a build that renders them as <button>, however
// faithfully the build draws them.
//
// It computes nothing the instrument did not report; the pairing share is the
// one division, and it is printed beside both of its operands.

import { readFileSync } from "node:fs";
import { join } from "node:path";

import { FRAMES } from "./frames.mjs";
import { read } from "./read.mjs";

const dir = process.argv[2];
if (dir === undefined) {
  process.stderr.write("usage: node preview/parta/census.mjs <dir>\n");
  process.exit(2);
}

function tags(file) {
  const counts = new Map();
  for (const row of JSON.parse(readFileSync(file, "utf8")).rows) counts.set(row.tag, (counts.get(row.tag) ?? 0) + 1);
  return [...counts].sort((a, b) => b[1] - a[1]).slice(0, 4).map(([t, n]) => `${t} ${n}`).join(", ");
}

const total = { drawn: 0, built: 0, paired: 0, identical: 0, differing: 0 };
process.stdout.write("frame  drawn built paired  share  identical differing   drawn tags  |  built tags\n");

for (const frame of FRAMES) {
  const report = read(join(dir, `cmp-${frame.id}.json`));
  const c = report.counts;
  total.drawn += c.drawnNodes;
  total.built += c.builtNodes;
  total.paired += c.paired;
  total.identical += c.identical;
  total.differing += c.differing;
  const share = `${((100 * c.paired) / c.drawnNodes).toFixed(1)}%`;
  process.stdout.write(
    `${frame.id.padEnd(5)} ${String(c.drawnNodes).padStart(6)} ${String(c.builtNodes).padStart(5)} ${String(c.paired).padStart(6)} ${share.padStart(6)} `
    + `${String(c.identical).padStart(10)} ${String(c.differing).padStart(9)}   ${tags(join(dir, `${frame.id}-drawn.json`))}  |  ${tags(join(dir, `${frame.id}-built.json`))}\n`);
}

process.stdout.write(
  `\nall    ${String(total.drawn).padStart(6)} ${String(total.built).padStart(5)} ${String(total.paired).padStart(6)} `
  + `${`${((100 * total.paired) / total.drawn).toFixed(1)}%`.padStart(6)} ${String(total.identical).padStart(10)} ${String(total.differing).padStart(9)}\n`);
