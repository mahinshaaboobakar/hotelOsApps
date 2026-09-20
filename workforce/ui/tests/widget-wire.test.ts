import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { attendanceToday } from "../widgets/panel/attendance-today";
import { comingUp } from "../widgets/panel/coming-up";
import { pendingRequests } from "../widgets/panel/pending-requests";

/**
 * The widgets write the numbers the service now sends — NUM-Q1, ADR 0174.
 *
 * The wire used to carry every figure already written (`"34 of 38"`, `"22
 * min"`, `"5d"`, `"7 rostered"`), in the service's culture; it carries numbers
 * now (WidgetWireTests on the backend), and each form the drawing uses is
 * written here. Values are chosen so no two figures coincide.
 */

function host(method: string, answer: unknown): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (_capability: string, asked: string) => asked === method
      ? Promise.resolve(answer)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

const texts = (root: HTMLElement, selector: string): string[] =>
  Array.from(root.querySelectorAll(selector), (one) => one.textContent ?? "");

describe("widget numbers from the wire", () => {
  it("writes a figure out of a total, a count, a context and minutes", async () => {
    const card = await attendanceToday(host("attendanceToday", {
      figures: [
        { count: 34, of: 38, label: "present", tone: "ink" },
        { count: 3, of: null, label: "late", tone: "warn" },
        { count: 4, of: null, label: "absent", tone: "bad" },
      ],
      share: [{ count: 31, tone: "ok" }, { count: 3, tone: "warn" }, { count: 4, tone: "bad" }],
      byDepartment: [{
        name: "HK", meta: null, context: { count: 9, word: "rostered" },
        value: 2, form: "count", tone: "bad", opens: "attendance?department=HK",
      }],
      lateIn: [{
        name: "S. Kumar", meta: "HK", context: null, at: "07:00",
        value: 22, form: "minutes", tone: "warn", opens: "attendance?department=HK",
      }],
    }));

    expect(texts(card, ".wvalue")).toEqual(["34 of 38", "3", "4"]);
    // The late row carries the shift's start — `64g` §5, ruled sent — and the
    // separator is the screen's: the service sends `07:00`, never `HK · 07:00`.
    expect(texts(card, ".wmeta")).toEqual(["9 rostered", "HK · 07:00"]);
    expect(texts(card, ".wfig")).toEqual(["2", "22 min"]);
  });

  it("writes the shift's start in the property's own hour cycle", async () => {
    // The same wire, two readers. A service that had rendered this would have
    // shipped one property's hour cycle to every property (ADR 0175).
    const lateIn = [{
      name: "S. Kumar", meta: "HK", context: null, at: "15:00",
      value: 22, form: "minutes", tone: "warn", opens: "attendance?department=HK",
    }];

    const wire = {
      figures: [{ count: 34, of: 38, label: "present", tone: "ink" }],
      share: [{ count: 34, tone: "ok" }],
      byDepartment: [],
      lateIn,
    };

    const british = await attendanceToday(host("attendanceToday", wire));

    const american = await attendanceToday({
      ...host("attendanceToday", wire),
      property: { timezone: "Asia/Kolkata", locale: "en-US" },
    });

    expect(texts(british, ".wmeta")).toEqual(["HK · 15:00"]);
    expect(texts(american, ".wmeta")).toEqual(["HK · 03:00 PM"]);
  });

  it("writes days, and a count out of a total", async () => {
    const card = await comingUp(host("comingUp", {
      figures: [
        { count: 6, of: null, label: "overlaps", tone: "warn" },
        { count: 7, of: null, label: "expiring", tone: "warn" },
      ],
      overlaps: [{
        name: "Kitchen", on: "2026-10-07", meta: null, context: { count: 3, word: "away" },
        value: 5, form: "out-of", tone: "warn", opens: "leave?department=KIT",
      }],
      expiring: [{
        name: "Fire warden · Irfan Qadri", meta: null, context: null,
        value: 12, form: "days", tone: "warn", opens: "people?capability=expiring",
      }],
    }));

    expect(texts(card, ".wvalue")).toEqual(["6", "7"]);
    expect(texts(card, ".wmeta")).toEqual(["3 away"]);
    expect(texts(card, ".wfig")).toEqual(["of 5", "12d"]);
  });

  it("writes a waiting row's days beside its text meta", async () => {
    const card = await pendingRequests(host("pendingRequests", {
      figures: [
        { count: 8, of: null, label: "leave", tone: "ink" },
        { count: 1, of: null, label: "swaps", tone: "ink" },
      ],
      rows: [{
        name: "Fatima Noor", meta: "FO · with Irfan Qadri", context: null,
        value: 4, form: "days", tone: "warn", opens: "leave?department=FO",
      }],
    }));

    expect(texts(card, ".wvalue")).toEqual(["8", "1"]);
    expect(texts(card, ".wmeta")).toEqual(["FO · with Irfan Qadri"]);
    expect(texts(card, ".wfig")).toEqual(["4d"]);
  });
});
