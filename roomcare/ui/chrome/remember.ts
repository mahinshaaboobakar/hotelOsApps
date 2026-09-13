/**
 * A person's last view on this desk — the Board's Map/Wall and the Room states
 * chip (frame 4e: "the chip remembers the person's last choice on that desk",
 * page 64's local-preference case). The property's default applies until the
 * person picks; a realm with no storage simply uses the default.
 */

function store(): Storage | null {
  try {
    return globalThis.localStorage ?? null;
  } catch {
    return null;
  }
}

export function remembered(key: string, allowed: readonly string[], fallback: string): string {
  const value = store()?.getItem(`roomcare.${key}`) ?? null;
  return value !== null && allowed.includes(value) ? value : fallback;
}

export function remember(key: string, value: string): void {
  try {
    store()?.setItem(`roomcare.${key}`, value);
  } catch {
    // A realm without storage keeps the choice for this session only.
  }
}
