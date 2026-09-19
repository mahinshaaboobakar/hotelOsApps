import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { SUPERVISOR, click, host, mount, settle } from "./host";

/**
 * Page 64 §2 (checklist C11): "A primary action with nothing to send is drawn
 * `off`, with the reason beside it — never live-and-refusing." Every Setup tab
 * ended with a live Save whatever had changed, so pressing it with nothing
 * edited wrote an unchanged new version. Found by the page-64 audit, 2026-09-19.
 */
const TABS = ["Windows & trigger", "Services & minutes", "Rules", "Assignment & zones", "Areas", "Deep clean plan"];

async function tab(name: string): Promise<HTMLElement> {
  const root = mount(activate, host(SUPERVISOR));
  await settle();
  click(root, "button.tab", "Setup");
  await settle();
  click(root, ".subnav button.tab", name);
  await settle();
  return root;
}

describe("a Setup tab's Save", () => {
  for (const name of TABS) {
    it(`on ${name}, is drawn off, saying why, until something on the tab changes`, async () => {
      const root = await tab(name);
      const line = root.querySelector<HTMLElement>(".save")!;
      expect(line.querySelector(".btn.pri")).toBeNull();
      expect(line.querySelector(".btn.off")?.textContent).toBe("Save — nothing changed");

      const field = root.querySelector<HTMLInputElement | HTMLSelectElement>(".body input:not([type=radio]), .body select:not(.btn)")!;
      field.dispatchEvent(new Event("change", { bubbles: true }));
      expect(line.querySelector(".btn.pri")?.textContent).toBe("Save");
      expect(line.querySelector(".btn.off")).toBeNull();
    });
  }

  it("goes live after a reorder sheet keeps a new order, which moves no field", async () => {
    const root = await tab("Rules");
    click(root, "button", "Reorder…");
    click(root, ".sheet button", "↓");
    click(root, ".sheet .df button", "Keep this order");
    await settle();
    expect(root.querySelector(".sheet")).toBeNull();
    expect(root.querySelector(".save .btn.pri")?.textContent).toBe("Save");
  });
});
