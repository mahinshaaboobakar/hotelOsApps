/**
 * What every screen is handed to move about: redraw, open a room, come back,
 * and the frame an overlay sits in (a sibling of the body, page 64 §9).
 */

export interface Nav {
  frame: HTMLElement;
  show(): void;
  openRoom(id: string): void;
  back(): void;
}
