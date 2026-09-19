/**
 * The five list states the app surface checklist audits, answered the way the
 * backend pages.
 *
 * ```text
 * E0  an empty list                     total 0
 * E1  an empty page of a non-empty list the list shrank while a person paged
 * 1P  a short single page               4 rows
 * MP  a full page of a multi-page list  page 0 of ~2.4 pages
 * ML  the short last page of it         reached by the drive, not by the answer
 * ```
 *
 * # Paged by the backend's arithmetic, not by a fixture's say-so
 *
 * A fixture that carries `total: 218` beside nine rows is a second contract with
 * the wire's field names — the closed loop in which every capture is evidence
 * about itself. So the harness never writes a total. It holds a dataset per
 * list and answers a request exactly as `HotelOS.Platform.Paging` does
 * (`packages/sdk-dotnet/HotelOS.Platform/Paging.cs`): the page floored at 0, the
 * size taken when positive and bounded by 500, **a request with no size gets
 * 500**, and — the fact E1 rests on — **no clamp to the last page**, so a page
 * past the rows answers none beside the whole total. {@link page} then asserts
 * its own answer is consistent, and refuses rather than serve one that is not.
 *
 * # E1 is reached the way a property reaches it
 *
 * The dataset loses its last page's rows after the first answer — a delete
 * while somebody paged — and the drive goes to the last page. The request that
 * follows asks for a page the list no longer has.
 */

/** The states, by the checklist's codes. */
export type ListState = "E0" | "E1" | "1P" | "MP" | "ML";

/** `Paging.MaxPageSize` — and the size applied when a request names none. */
const MAX_PAGE_SIZE = 500;

/** A request's paging, as the backend reads it — `Paging.Of`. */
function window(params: unknown): { page: number; size: number } {
  const body = (params ?? {}) as { page?: unknown; pageSize?: unknown };
  const asked = typeof body.pageSize === "number" ? body.pageSize : 0;
  const page = typeof body.page === "number" ? body.page : 0;

  return {
    page: Math.max(page, 0),
    size: asked > 0 ? Math.min(asked, MAX_PAGE_SIZE) : MAX_PAGE_SIZE,
  };
}

/**
 * One page of `all`, and the total, as the backend would answer.
 *
 * @throws when the answer is not self-consistent — a harness that served an
 *   impossible page would be photographing a state no property can reach.
 */
export function page<T>(all: readonly T[], params: unknown): { rows: T[]; total: number } {
  const { page: index, size } = window(params);
  const rows = all.slice(index * size, index * size + size);
  const total = all.length;

  const expected = Math.max(0, Math.min(size, total - index * size));
  if (rows.length !== expected || rows.length > size) {
    throw new Error(`harness paging is inconsistent: page ${index}, size ${size}, `
      + `total ${total}, ${rows.length} rows`);
  }

  return { rows, total };
}

/**
 * How many rows each state's list holds, from the screen's own page size.
 *
 * `after` is the size once the first answer has gone — only E1 shrinks, and it
 * shrinks by exactly its last page, so the last page a person can reach before
 * the delete is the first page past the rows after it.
 */
export function rowsFor(state: ListState, pageSize: number): { before: number; after: number } {
  if (state === "E0") return { before: 0, after: 0 };
  if (state === "1P") return { before: 4, after: 4 };

  // Two full pages and a short third: MP is page 0, ML is page 2.
  const before = pageSize * 2 + Math.max(1, Math.floor(pageSize * 0.4));
  return state === "E1"
    ? { before, after: pageSize * 2 }
    : { before, after: before };
}

/** `n` rows from a fixture's own, repeated in order — never invented. */
export function repeated<T>(rows: readonly T[], n: number): T[] {
  if (n === 0) return [];
  if (rows.length === 0) {
    throw new Error("a list state needs at least one fixture row to repeat");
  }
  return Array.from({ length: n }, (_, index) => rows[index % rows.length] as T);
}

/** Read `?list=` — a state the audit names, or none. */
export function listState(params: URLSearchParams): ListState | null {
  const state = params.get("list");
  return state === "E0" || state === "E1" || state === "1P" || state === "MP" || state === "ML"
    ? state
    : null;
}
