/**
 * What Room Care's backend answers — the module views as the screens read
 * them, camelCase as ASP.NET writes them. One file of shapes, so a screen and
 * a test cannot disagree about a field's name.
 */

export interface Paging {
  page: number;
  pageSize: number;
  total: number;
}

export interface Outcome {
  kind: string;
  at: string | null;
  until: string | null;
  detail: string | null;
  days: number | null;
}

export interface Marks {
  soldTonight: boolean;
  dnd: boolean;
  disagreement: boolean;
  blocked: boolean;
  supervision: boolean;
  pending: boolean;
  inProgress: boolean;
  newSince: boolean;
  manual: boolean;
}

export interface BoardRoom {
  id: string;
  number: string;
  condition: string;
  source: string;
  setBy: string | null;
  setAt: string;
  occupancy: string;
  vacantDays: number | null;
  soldAt: string | null;
  taskId: string | null;
  taskVersion: number | null;
  service: string | null;
  reduction: string | null;
  earliestAt: string | null;
  priority: number | null;
  attendantId: string | null;
  attendant: string | null;
  outcome: Outcome;
  linen: string | null;
  marks: Marks;
  version: number;
}

export interface ZoneCounts {
  rooms: number;
  dirty: number;
  inProgress: number;
  ready: number;
  dnd: number;
  blocked: number;
  supervision: number;
  pending: number;
}

export interface ZoneGroup {
  zoneId: string | null;
  name: string;
  counts: ZoneCounts;
  rooms: BoardRoom[];
}

export interface Strip {
  rooms: number;
  dirty: number;
  inProgress: number;
  ready: number;
  pending: number;
  blocked: number;
  supervision: number;
  lastFactAt: string | null;
  silentSince: string | null;
  at: string;
  window: string | null;
}

export interface Board {
  strip: Strip;
  defaultView: string;
  zones: ZoneGroup[];
}

export interface StateRow {
  roomId: string;
  number: string;
  condition: string;
  occupancy: string;
  soldAt: string | null;
  stay: string;
  source: string;
  sourceBy: string | null;
  sourceAt: string;
  version: number;
  blocked: boolean;
}

export interface RoomStates {
  rooms: number;
  dirty: number;
  occupied: number;
  soldTonight: number;
  silentSince: string | null;
  defaultView: string;
  zones: { zoneId: string | null; name: string; rooms: StateRow[] }[];
}

export interface Operator {
  name: string | null;
  department: string;
  property: string | null;
}
