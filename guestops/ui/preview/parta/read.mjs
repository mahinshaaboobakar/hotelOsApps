// Read a `--compare --json` report, and refuse anything that is not one.
//
// **Numbers that reach a certificate come from `--json`.** The readable report
// stays readable and is no longer a source: this module replaced a parser that
// read section headings out of prose, and that parser produced two wrong figures
// before it produced a right one — `0 unpaired` from grepping for a section the
// instrument never prints, and a `PAIRED (n, m by position)` header that a
// sibling stream's parser read as zero.
//
// The three rules below are the emitter's own, from `report.mjs:18-40`. Two are
// inherited and the third is implemented here.
//
// **1 · Check the schema, then read by name.** Every field stays present with
// its meaning within schema 1; a removal or a repurposing bumps the number. So a
// consumer that checks the number and then reads by name is safe across every
// improvement the instrument makes — and one that meets a number it was not
// written for must stop rather than guess which fields still mean what.
//
// **2 · Never enumerate and assert a closed set.** `counts` may gain buckets and
// `key` may gain facts; `valueRead` was added the day after `key` itself. A
// consumer requiring `Object.keys(counts)` to match a held list breaks on an
// addition that harms nothing.
//
// **3 · A named field that is absent is an error, never a zero.** This one is
// mine to implement and it is the fault it exists for: the prose parser searched
// for `UNPAIRED`, found nothing because the instrument prints
// `DRAWN, NOT IN THE BUILD`, and reported zero. A missing field and a measured
// zero are different facts, and only one of them is an answer.
//
// **And a count is not an array's length.** `unpaired.built` holds entries
// carrying a `count`, so four entries can be seven nodes. Read `counts.*` for a
// number and the array only for what the nodes are.

import { readFileSync } from "node:fs";

/** The schema this consumer was written against. */
const SCHEMA = 1;

/**
 * A named field, or an error naming the field and the file.
 *
 * The message names the path because the caller is a certificate: somebody
 * meeting this needs to know which field went missing from which run, not that
 * "something was undefined".
 */
function required(report, path, source) {
  let at = report;

  for (const step of path.split(".")) {
    if (at === null || at === undefined || !Object.hasOwn(at, step)) {
      throw new Error(
        `${source}: the report has no '${path}'. A named field that is absent is `
        + "an error, never a zero — report.mjs:18-40. Either the emitter changed "
        + "without bumping its schema, or this is not a --compare --json report.",
      );
    }

    at = at[step];
  }

  return at;
}

/**
 * One run's report, read by name.
 *
 * @param file a `--compare --json` output
 * @returns the fields this certificate quotes, and the differing nodes
 */
export function read(file) {
  const report = JSON.parse(readFileSync(file, "utf8"));
  const schema = required(report, "schema", file);

  if (schema !== SCHEMA) {
    throw new Error(
      `${file}: schema ${schema}, and this reads ${SCHEMA}. The number changes when `
      + "a field is removed or repurposed, so continuing would be reading fields "
      + "whose meaning is no longer the one assumed here.",
    );
  }

  // Read by name and nothing else. No `Object.keys` over `counts`, no assertion
  // that the set is what it was: a bucket added tomorrow must not fail today's
  // consumer, and a field this one does not name is none of its business.
  return {
    label: required(report, "label", file),

    key: {
      description: required(report, "key.description", file),
      step: required(report, "key.step", file),
      changedOn: required(report, "key.changedOn", file),
      comparable: required(report, "key.comparableWithRunsBefore", file),
    },

    counts: {
      drawnNodes: required(report, "counts.drawnNodes", file),
      builtNodes: required(report, "counts.builtNodes", file),
      paired: required(report, "counts.paired", file),
      identical: required(report, "counts.identical", file),
      differing: required(report, "counts.differing", file),
      collapsed: required(report, "counts.collapsed", file),
      refusedDrawn: required(report, "counts.refusedDrawn", file),
      refusedBuilt: required(report, "counts.refusedBuilt", file),
      unpairedDrawn: required(report, "counts.unpairedDrawn", file),
      unpairedBuilt: required(report, "counts.unpairedBuilt", file),
    },

    // The instrument's own arithmetic. It replaces the sum this certificate used
    // to compute by hand — which was right, and was right for one run only.
    closes: {
      drawn: required(report, "closes.drawn", file),
      built: required(report, "closes.built", file),
    },

    // Only the ones that differ. `differences` is `{property, drawn, built}`,
    // which is the shape the prose parser reconstructed with a regular
    // expression over aligned columns.
    differing: required(report, "comparisons", file)
      .filter((one) => one.differences.length > 0)
      .map((one) => ({
        text: one.text,
        tag: one.tag,
        properties: Object.fromEntries(
          one.differences.map((d) => [d.property, [d.drawn, d.built]]),
        ),
      })),
  };
}
