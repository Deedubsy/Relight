/** Small formatting and aggregation helpers shared by the experiments. */
export const mean = (xs: number[]): number => xs.length ? xs.reduce((a, b) => a + b, 0) / xs.length : NaN;
export const clean = (xs: (number | null | undefined)[]): number[] => xs.filter((x): x is number => typeof x === 'number' && !Number.isNaN(x));
/** "mean [min–max]" over the seeds, like frontsim.py mr(). */
export function mr(vals: (number | null | undefined)[], p = 1): string {
  const v = clean(vals);
  if (!v.length) return 'n/a';
  const f = (x: number) => x.toFixed(p);
  return v.length === 1 ? f(v[0]) : `${f(mean(v))} [${f(Math.min(...v))}–${f(Math.max(...v))}]`;
}
export const fmt = (x: number, p = 2): string => Number.isNaN(x) ? '-' : x.toFixed(p);
export const min = (t: number, p = 1): string => t < 0 ? 'never' : (t / 60).toFixed(p);
export const minOrNever = (ts: number[], p = 1): string => {
  const v = ts.filter(t => t >= 0).map(t => t / 60);
  const never = ts.length - v.length;
  return (v.length ? mr(v, p) : 'never') + (never && v.length ? ` (never in ${never}/${ts.length})` : '');
};

export interface Check { name: string; pass: boolean; detail: string }
export interface Section { title: string; note?: string; header: string[]; rows: (string | number)[][] }
export interface ExperimentResult {
  id: string; title: string; pyNames: string[]; docRefs: string[]; setup: string;
  sections: Section[]; checks: Check[]; data: Record<string, unknown>;
}
export interface Ctx { seeds: number[]; hours: number; log: (s: string) => void; nightly: boolean; map: string; big: boolean }
export interface Experiment { id: string; title: string; run(ctx: Ctx): ExperimentResult }

/** A check: `pass` iff `value` lies in [lo, hi]. */
export function within(name: string, value: number, lo: number, hi: number, unit = ''): Check {
  const pass = !Number.isNaN(value) && value >= lo && value <= hi;
  return { name, pass, detail: `${Number.isNaN(value) ? 'n/a' : value.toFixed(2)}${unit} (expected ${lo}–${hi}${unit})` };
}
export function isTrue(name: string, pass: boolean, detail: string): Check { return { name, pass, detail }; }
