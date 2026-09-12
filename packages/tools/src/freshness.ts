/** `npm run freshness:check` (constitution rule 11, guardrails Step 6): every generated file carries the git commit and
 *  the config hash it was made from; this reads them all and fails if a stamp is missing, if `source_commit` is not an
 *  ancestor of HEAD, or if `config_hash` differs from the hash of the same config recipe built from the current code
 *  (packages/harness/src/provenance.ts `configOf`).
 *
 *  Generated files: docs/EXPERIMENTS.md, docs/experiments/*.json|*.md (E*, E-*, calibration*, nightly*), the Scenario B
 *  snapshot(s) in packages/game/public/snapshots, and the PNGs docs/section18-*.png and docs/seeds/*.png through their
 *  `.json` sidecars. docs/experiments/lattice/ is the archive of the pre-D6 lattice results (REWORK_REPORT.md): its files
 *  are stamped with the commit that archived them and checked for that commit's ancestry only — their config hash is,
 *  by definition, not the current one and is reported, not compared.
 *
 *  Not covered (see docs/GUARDRAILS_REPORT.md §6): the D6 fixtures packages/sim/fixtures/city*.json (their bytes are the
 *  regression set and are compared by the tests instead), a dirty working tree at generation time, and code changes
 *  that leave the config hash unchanged (a rule change in step() with the same SimConfig) — those are what snapshot:check
 *  and the experiment suite catch. */
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { execSync } from 'node:child_process';
import { join, resolve, basename } from 'node:path';
import { evidenceHash, evidenceProfileProblem, parseStampLine, Stamp } from '@relight/harness/src/provenance';

const ROOT = resolve(process.env.INIT_CWD ?? process.cwd());
const ARCHIVES = ['docs/experiments/lattice'];   // frozen records: ancestry checked, config hash reported only

interface Row { file: string; kind: 'md' | 'json' | 'png'; archive: boolean; stamp: Stamp | null; problems: string[]; expected?: string }

const ls = (dir: string, re: RegExp): string[] => existsSync(join(ROOT, dir)) ? readdirSync(join(ROOT, dir)).filter(f => re.test(f)).sort().map(f => join(dir, f)) : [];
const files: { file: string; kind: Row['kind'] }[] = [
  { file: 'docs/EXPERIMENTS.md', kind: 'md' },
  ...ls('docs/experiments', /\.md$/).map(file => ({ file, kind: 'md' as const })),
  ...ls('docs/experiments', /\.json$/).map(file => ({ file, kind: 'json' as const })),
  ...ls('docs/experiments/campaign', /\.json$/).map(file => ({ file, kind: 'json' as const })),
  ...ls('docs/experiments/campaign', /\.md$/).map(file => ({ file, kind: 'md' as const })),
  ...ls('docs/experiments/lattice', /\.md$/).map(file => ({ file, kind: 'md' as const })),
  ...ls('docs/experiments/lattice', /\.json$/).map(file => ({ file, kind: 'json' as const })),
  ...ls('packages/game/public/snapshots', /\.json$/).map(file => ({ file, kind: 'json' as const })),
  ...ls('packages/game/public/snapshots/campaign', /\.json$/).map(file => ({ file, kind: 'json' as const })),
  ...ls('docs', /^section18-.*\.png$/).map(file => ({ file, kind: 'png' as const })),
  ...ls('docs/seeds', /\.png$/).map(file => ({ file, kind: 'png' as const })),
];

function readStamp(file: string, kind: Row['kind']): { stamp: Stamp | null; problem?: string } {
  const path = join(ROOT, file);
  if (!existsSync(path)) return { stamp: null, problem: 'missing' };
  if (kind === 'md') {
    const first = readFileSync(path, 'utf8').split('\n')[0];
    const s = parseStampLine(first);
    return s ? { stamp: s } : { stamp: null, problem: `first line is not a stamp: ${JSON.stringify(first.slice(0, 80))}` };
  }
  const jsonPath = kind === 'png' ? path.replace(/\.png$/, '.json') : path;
  if (!existsSync(jsonPath)) return { stamp: null, problem: `sidecar ${basename(jsonPath)} missing` };
  let o: Partial<Stamp>;
  try { o = JSON.parse(readFileSync(jsonPath, 'utf8')) as Partial<Stamp>; } catch (e) { return { stamp: null, problem: `not JSON: ${(e as Error).message}` }; }
  if (typeof o.source_commit !== 'string' || typeof o.config_hash !== 'string' || !o.config_ref) return { stamp: null, problem: 'no source_commit / config_hash / config_ref keys' };
  return { stamp: { source_commit: o.source_commit, config_hash: o.config_hash, config_ref: o.config_ref } };
}

const ancestorCache = new Map<string, boolean>();
function isAncestor(sha: string): boolean {
  const hit = ancestorCache.get(sha); if (hit !== undefined) return hit;
  let ok: boolean;
  try { execSync(`git merge-base --is-ancestor ${sha} HEAD`, { cwd: ROOT, stdio: 'ignore' }); ok = true; } catch { ok = false; }
  ancestorCache.set(sha, ok); return ok;
}

const rows: Row[] = files.map(({ file, kind }) => {
  const archive = ARCHIVES.some(a => file.replace(/\\/g, '/').startsWith(a + '/'));
  const { stamp, problem } = readStamp(file, kind);
  const row: Row = { file, kind, archive, stamp, problems: problem ? [problem] : [] };
  if (!stamp) return row;
  if (!isAncestor(stamp.source_commit)) row.problems.push(`source_commit ${stamp.source_commit.slice(0, 7)} is not an ancestor of HEAD`);
  try {
    row.expected = evidenceHash(stamp.config_ref);
    const payload = kind === 'json' ? JSON.parse(readFileSync(join(ROOT, file), 'utf8')) : undefined;
    const problem = evidenceProfileProblem(stamp.config_ref, file, payload);
    if (problem) row.problems.push(problem);
  } catch (e) { row.problems.push(`config_ref not understood: ${(e as Error).message}`); return row; }
  if (!archive && row.expected !== stamp.config_hash) row.problems.push(`config_hash ${stamp.config_hash} ≠ current ${row.expected} (${stamp.config_ref.kind})`);
  return row;
});

const head = execSync('git rev-parse --short HEAD', { cwd: ROOT, encoding: 'utf8' }).trim();
console.log(`freshness: ${rows.length} generated files against HEAD ${head}`);
for (const r of rows) {
  const tag = r.problems.length ? 'STALE' : r.archive ? 'archive' : 'fresh';
  const what = r.stamp ? `${r.stamp.source_commit.slice(0, 7)} ${r.stamp.config_hash}${r.archive ? ` (archived; current ${r.expected})` : ''}` : '';
  console.log(`  ${tag.padEnd(7)} ${r.file.padEnd(52)} ${what}${r.problems.length ? ' — ' + r.problems.join('; ') : ''}`);
}
const bad = rows.filter(r => r.problems.length);
if (bad.length) { console.error(`freshness: ${bad.length} stale or unstamped file(s); regenerate them on HEAD (experiments, calibrate, snapshot, section18, seeds)`); process.exit(1); }
console.log('freshness: every generated file was made on an ancestor of HEAD with the current config');
