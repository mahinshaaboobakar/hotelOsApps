import { describe, expect, it } from "vitest";

import { developerNotes, readableText } from "../src";

/**
 * The guard's own positive and negative controls. An application's walk returning `[]` is only
 * a result if this reader and these shapes find a note when one IS there — so every shape is
 * planted here, including in the places a joined `textContent` went blind.
 */

const kinds = (text: string): string[] => developerNotes(text).map((one) => one.split(":")[0]!);

describe("each shape is found", () => {
  const PLANTED: readonly (readonly [string, string])[] = [
    ["a register id", "accepted with GUEST-Q6"],
    ["a register id", "see WF-Q18a"],
    ["an ADR", "as ADR 0174 requires"],
    ["an ADR", "ADR-0106"],
    ["an ADR", "ADR  0092"],
    ["a section reference", "Jobs' board (design §6)"],
    ["a section reference", "per §4.2"],
    ["a section reference", "see §"],
    ["a design-section reference", "the queue (S3 · approvals)"],
    ["a design-row reference", "drawn off (row 4)"],
    ["a chapter reference", "Chapter 11 says"],
    ["a design page", "see the design page"],
    ["a code identifier", "gives roomcare_manager"],
    ["a correlation id", "carries a correlation id"],
    ["a platform system", "Owned by Master Data"],
    ["a platform system", "the Kernel refused"],
    ["a platform system", "OpenFGA · Context Service · Integration Hub"],
  ];

  for (const [kind, text] of PLANTED) {
    it(`${kind} in "${text}"`, () => {
      expect(kinds(text)).toContain(kind);
    });
  }
});

describe("what a person at the property reads is not a note", () => {
  // Chosen to sit near the shapes: digits, capitals, a hyphenated code, a dot.
  const CLEAN = [
    "Check in · 48 h of arrival · ₹ 8 400.00 · BK-4471",
    "Casual · 3 days",
    "Front Office · HK · Room 204",
    "Every department is shown. Choosing one is not built yet.",
    "Nights 20:00 – 08:00",
  ];

  for (const text of CLEAN) {
    it(`"${text}"`, () => {
      expect(developerNotes(text)).toEqual([]);
    });
  }
});

describe("readableText", () => {
  function planted(html: string): HTMLElement {
    const root = document.createElement("div");
    root.innerHTML = html;
    return root;
  }

  it("keeps two elements' text apart, so a split note keeps its boundary", () => {
    const root = planted("<span>gives</span><b>roomcare_manager</b><i>X</i><b>WF-Q18</b>");
    expect(kinds(readableText(root))).toEqual(
      expect.arrayContaining(["a code identifier", "a register id"]));
  });

  it("reads a tooltip, a label, a description and a placeholder", () => {
    const root = planted(
      '<button title="ADR 0174">a</button>'
      + '<span aria-label="WF-Q2">b</span>'
      + '<span aria-description="per §9">c</span>'
      + '<input placeholder="Chapter 11">');
    expect(kinds(readableText(root))).toEqual(
      expect.arrayContaining(["an ADR", "a register id", "a section reference", "a chapter reference"]));
  });

  it("does not read a stylesheet or a script", () => {
    const root = planted("<style>.a_b{}</style><script>var x_y = 'ADR 1';</script><p>Rota</p>");
    expect(developerNotes(readableText(root))).toEqual([]);
  });
});
