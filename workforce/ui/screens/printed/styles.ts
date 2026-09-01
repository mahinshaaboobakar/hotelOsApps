/**
 * The printed sheet's rules — ink on paper, and no colour at all.
 *
 * Deliberately not on published tokens: this artifact is for a monochrome
 * photocopier, so it fixes black on white rather than following the viewer's
 * theme. A themed print sheet would come out of the machine as grey on grey.
 */
export const PRINTED_CSS = `
.sheet{background:#fff;color:#111;padding:26px 30px;min-height:100vh;
       font:12px/1.5 "Times New Roman",Georgia,serif}
.phead{display:flex;justify-content:space-between;align-items:flex-end;
       border-bottom:2px solid #111;padding-bottom:8px;margin-bottom:12px}
.pt{font-size:17px;font-weight:700;letter-spacing:-.01em}
.psub{font-size:10.5px;color:#444}

.pgrid{display:grid;grid-template-columns:190px repeat(7,1fr);
       border:1px solid #111;border-right:0;border-bottom:0}
.pcell{border-right:1px solid #111;border-bottom:1px solid #111;
       padding:5px 7px;font-size:11px;text-align:center}
.ph{font-weight:700;background:#eee}
.pwho{text-align:left}
.pwho b{display:block;font-weight:700}
.pwho s{text-decoration:none;display:block;font-size:10px;color:#444}
.pmod{background:#f6f6f6;font-size:10px;font-weight:600;text-align:left}

.plegend{display:flex;gap:18px;flex-wrap:wrap;margin-top:10px;font-size:10.5px}
.pchanges{margin-top:14px;border-top:1px solid #999;padding-top:8px;font-size:10.5px}
.pct{font-weight:700;margin-bottom:4px}
`;
