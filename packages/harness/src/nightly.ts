/** Nightly: the 10,000-seed distribution of the headline numbers (E7 shape ratios, E1 losses, first interior) at 5 h,
 *  and the 25 h runs (E8, E9) at ten seeds. Writes docs/experiments/nightly-<date>.json and nightly.md. */
import { writeFileSync, mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { runSim, DEFAULT_MAP } from './run';
import { E8 } from './experiments/e8';
import { E9 } from './experiments/e9';
import { Policy } from '@relight/sim';

function pct(xs: number[], p: number): number { const s = [...xs].sort((a, b) => a - b); return s[Math.min(s.length - 1, Math.floor(p * s.length))]; }
function dist(xs: number[]): { mean: number; p5: number; p50: number; p95: number; min: number; max: number } {
  return { mean: xs.reduce((a, b) => a + b, 0) / xs.length, p5: pct(xs, 0.05), p50: pct(xs, 0.5), p95: pct(xs, 0.95), min: Math.min(...xs), max: Math.max(...xs) };
}

export function nightly(o: { seedsN: number; outDir: string; log: (s: string) => void }): void {
  const t0 = Date.now();
  const per: Record<Policy, { mags: number[]; lost: number[]; firstInterior: number[] }> = {
    compact: { mags: [], lost: [], firstInterior: [] }, spike: { mags: [], lost: [], firstInterior: [] },
    cheapest: { mags: [], lost: [], firstInterior: [] }, balanced: { mags: [], lost: [], firstInterior: [] }, river: { mags: [], lost: [], firstInterior: [] }, turtle: { mags: [], lost: [], firstInterior: [] },
  };
  const ratios = { spikeCompact: [] as number[], cheapestCompact: [] as number[] };
  for (let seed = 1; seed <= o.seedsN; seed++) {
    const rs: Partial<Record<Policy, number>> = {};
    for (const policy of ['compact', 'spike', 'cheapest', 'balanced'] as Policy[]) {
      const r = runSim({ seed, policy, hours: 5 });
      per[policy].mags.push(r.totalMags); per[policy].lost.push(r.lost); per[policy].firstInterior.push(r.firstInterior);
      rs[policy] = r.totalMags;
    }
    ratios.spikeCompact.push(rs.spike! / rs.compact!); ratios.cheapestCompact.push(rs.cheapest! / rs.compact!);
    if (seed % 500 === 0) o.log(`${seed}/${o.seedsN} seeds, ${((Date.now() - t0) / 1000).toFixed(0)} s`);
  }
  const seeds10 = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  const e8 = E8.run({ seeds: seeds10, hours: 5, log: o.log, nightly: true, map: DEFAULT_MAP, big: true });
  const e9 = E9.run({ seeds: seeds10, hours: 5, log: o.log, nightly: true, map: DEFAULT_MAP, big: true });
  const date = new Date().toISOString().slice(0, 10);
  const summary = {
    date, seedsN: o.seedsN, seconds: (Date.now() - t0) / 1000,
    policies: Object.fromEntries((['compact', 'spike', 'cheapest', 'balanced'] as Policy[]).map(p => [p, { mags: dist(per[p].mags), lost: dist(per[p].lost), interiorNever: per[p].firstInterior.filter(x => x < 0).length }])),
    ratios: { spikeCompact: dist(ratios.spikeCompact), cheapestCompact: dist(ratios.cheapestCompact) },
    e8: e8.data, e9: e9.data,
  };
  mkdirSync(o.outDir, { recursive: true });
  writeFileSync(join(o.outDir, `nightly-${date}.json`), JSON.stringify(summary, null, 1));
  const md: string[] = [`# Nightly ${date}`, '', `${o.seedsN} seeds × 4 policies × 5 h; E8/E9 at ten seeds; ${summary.seconds.toFixed(0)} s.`, '',
    '| policy | total mags mean | p5 | p50 | p95 | lost mean | p95 | interior never |', '|---|---|---|---|---|---|---|---|'];
  for (const p of ['compact', 'spike', 'cheapest', 'balanced'] as Policy[]) {
    const s = summary.policies[p]; md.push(`| ${p} | ${s.mags.mean.toFixed(0)} | ${s.mags.p5.toFixed(0)} | ${s.mags.p50.toFixed(0)} | ${s.mags.p95.toFixed(0)} | ${s.lost.mean.toFixed(2)} | ${s.lost.p95} | ${s.interiorNever} |`);
  }
  md.push('', `spike/compact: mean ${summary.ratios.spikeCompact.mean.toFixed(2)}× p5 ${summary.ratios.spikeCompact.p5.toFixed(2)} p95 ${summary.ratios.spikeCompact.p95.toFixed(2)}`,
    `cheapest/compact: mean ${summary.ratios.cheapestCompact.mean.toFixed(2)}× p5 ${summary.ratios.cheapestCompact.p5.toFixed(2)} p95 ${summary.ratios.cheapestCompact.p95.toFixed(2)}`, '');
  for (const r of [e8, e9]) { md.push(`## ${r.id} ${r.title} (ten seeds)`, ''); for (const s of r.sections) { md.push(`| ${s.header.join(' | ')} |`, `|${s.header.map(() => '---').join('|')}|`); for (const row of s.rows) md.push(`| ${row.join(' | ')} |`); md.push(''); } }
  writeFileSync(join(o.outDir, 'nightly.md'), md.join('\n'));
  o.log(`wrote ${join(o.outDir, `nightly-${date}.json`)}`);
}
