/**
 * Everything the design needs that the contract does not publish, derived.
 */

/**
 * Everything the design needs that the contract does not publish, derived.
 *
 * `color-mix` on a published token follows the theme the host injected, which
 * is the whole point: a tint written as `rgba(52,211,153,.12)` is a dark-theme
 * decision frozen into a module that a light property will also run.
 */
export const DERIVED = `
.go{
  --go-pms:var(--color-brand,#818cf8);
  --go-override:color-mix(in srgb, var(--color-brand,#818cf8) 62%, var(--color-bad,#f87171));
  --go-pms-wash:color-mix(in srgb, var(--color-brand,#818cf8) 13%, transparent);
  --go-override-wash:color-mix(in srgb, var(--go-override) 15%, transparent);
  /* The three state tints are PUBLISHED now — 894e230, docs/working/64 §1.
     These were hand-mixed at 14%, 12% and 10%: three numbers nobody chose
     together, in an application that is one of three doing the same thing.
     Aliased rather than deleted, because ten selectors name them and the
     shell's tone is the value either way; the fallback is the old mix, so a
     host that has not published the tones yet still renders. */
  --go-warn-wash:var(--color-warn-soft, color-mix(in srgb, var(--color-warn,#fbbf24) 14%, transparent));
  --go-ok-wash:var(--color-ok-soft, color-mix(in srgb, var(--color-ok,#34d399) 12%, transparent));
  --go-bad-wash:var(--color-bad-soft, color-mix(in srgb, var(--color-bad,#f87171) 10%, transparent));
  --go-brand-wash:color-mix(in srgb, var(--color-brand,#818cf8) 18%, transparent);
  --go-brand-edge:color-mix(in srgb, var(--color-brand,#818cf8) 50%, transparent);
  --go-bad-edge:color-mix(in srgb, var(--color-bad,#f87171) 45%, transparent);
  --go-warn-edge:color-mix(in srgb, var(--color-warn,#fbbf24) 35%, transparent);
  --go-ink-wash:color-mix(in srgb, var(--color-ink,#e8ebf4) 6%, transparent);
  --go-row-hover:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent);
  /* 135deg, not 120 — docs/working/64 §2. Jobs and GuestOps had independently
     derived the same fill down to the 62% and differed only in the angle;
     Jobs' is the baseline, because the geometry is. */
  --go-accent:linear-gradient(135deg,var(--go-pms),var(--go-override));
}
`;
