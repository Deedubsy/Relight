/** Provenance stamps for generated files (constitution rule 11, guardrails Step 6). Every file a script writes carries
 *  the git commit and the config hash it was made from; `npm run freshness:check` (packages/tools/src/freshness.ts)
 *  recomputes the hash from the current code and fails if either does not match.
 *
 *  A stamp names its config by a *recipe* (`ConfigRef`), not by value, so the checker can rebuild the config from the
 *  code that is checked out now: the same recipe on changed code gives a different hash, and the file is stale.
 *
 *    markdown: the first line is `<!-- source_commit: <sha>; config_hash: <hash>; config: <ConfigRef json> -->`
 *    JSON:     top-level keys `source_commit`, `config_hash`, `config_ref`
 *    PNG:      a sidecar `<name>.json` next to the image with the same three keys */
import { execSync } from 'node:child_process';
import { DEFAULT_CONFIG, SimConfig, configHash, protoCalibrated, CityPreset } from '@relight/sim';
import { CANON } from './run';
import { campaignConfig, CAMPAIGN_RULESET, LEGACY_RULESET, CAMPAIGN_RULES, OPENING_LAYOUT, DEFENCE, CAMPAIGN_THREAT, canonicalJson, fnv1a, type Ruleset, rulesetOf, EXPANSION, CAMPAIGN_POWER } from '@relight/sim';

export type ConfigRef =
  | { kind: 'campaign'; ruleset: typeof CAMPAIGN_RULESET; opening: typeof CAMPAIGN_RULES.opening }
  | { kind: 'experiments' }                                                   // packages/harness/src/run.ts CANON (docs/EXPERIMENTS.md, E*.json, nightly)
  | { kind: 'calibration'; map: 'lattice' | CityPreset; walk: boolean; economy: boolean; overrides: Record<string, unknown> }   // calibrate.ts
  | { kind: 'snapshot' }                                                      // snapshot.ts: the proto's config
  | { kind: 'section18' }                                                     // packages/tools/src/section18.ts: E8's config
  | { kind: 'seeds' };                                                        // packages/tools/src/seeds.ts: the city generator (config-independent; DEFAULT_CONFIG stands in)

export interface Stamp { source_commit: string; config_hash: string; config_ref: ConfigRef }

/** Deep merge of a config with a JSON overrides object (calibrate.ts `--overrides`). */
export function mergeConfig(base: Record<string, unknown>, over: Record<string, unknown>): Record<string, unknown> {
  const out: Record<string, unknown> = { ...base };
  for (const k of Object.keys(over)) {
    const b = base[k], o = over[k];
    out[k] = b && o && typeof b === 'object' && typeof o === 'object' && !Array.isArray(b) ? mergeConfig(b as Record<string, unknown>, o as Record<string, unknown>) : o;
  }
  return out;
}

/** The config a recipe names, built from the code that is checked out now. */
export function configOf(ref: ConfigRef): SimConfig {
  if (ref.kind !== 'campaign' && 'ruleset' in ref) throw new Error('legacy evidence must not carry campaign rules');
  switch (ref.kind) {
    case 'campaign':
      if (ref.ruleset !== CAMPAIGN_RULESET || ref.opening !== CAMPAIGN_RULES.opening) throw new Error('unsupported campaign evidence profile');
      return campaignConfig();
    case 'experiments': return CANON;
    case 'calibration': {
      const base = protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: ref.economy, walk: ref.walk });
      return mergeConfig(base as unknown as Record<string, unknown>, ref.overrides) as unknown as SimConfig;
    }
    case 'snapshot': return protoCalibrated({ ...DEFAULT_CONFIG, scatter: true, economy: true });
    case 'section18': return { ...DEFAULT_CONFIG, production: false, eco: { ...DEFAULT_CONFIG.eco } };
    case 'seeds': return DEFAULT_CONFIG;
  }
  throw new Error('unknown evidence configuration');
}

export function evidenceRuleset(ref: ConfigRef): Ruleset { configOf(ref); return ref.kind === 'campaign' ? CAMPAIGN_RULESET : LEGACY_RULESET; }
/** Preserve every legacy hash; campaign evidence also fingerprints its rules and geometry. */
export function evidenceHash(ref: ConfigRef): string {
  const config = configOf(ref);
  return ref.kind === 'campaign' ? fnv1a(canonicalJson({ config, ruleset: ref.ruleset, rules: CAMPAIGN_RULES, opening: OPENING_LAYOUT, expansion: EXPANSION, power: CAMPAIGN_POWER, defence: DEFENCE, threats: CAMPAIGN_THREAT })) : configHash(config);
}
export function evidenceProfileProblem(ref: ConfigRef, file: string, payload?: unknown): string {
  const profile = evidenceRuleset(ref), campaignPath = file.replace(/\\/g, '/').includes('/campaign/');
  if (campaignPath !== (profile === CAMPAIGN_RULESET)) return 'evidence path and campaign profile differ';
  const o = payload as { state?: unknown; finalState?: unknown; blocks?: unknown; ruleset?: Ruleset } | null;
  const state = (o?.state ?? o?.finalState ?? (o?.blocks ? o : null)) as { ruleset?: Ruleset } | null;
  if (state && rulesetOf(state) !== profile) return 'saved state and evidence profile differ';
  return '';
}

export function gitHead(cwd?: string): string {
  return execSync('git rev-parse HEAD', { cwd, encoding: 'utf8' }).trim();
}

export function stamp(ref: ConfigRef): Stamp {
  return { source_commit: gitHead(), config_hash: evidenceHash(ref), config_ref: ref };
}

export function stampLine(s: Stamp): string {
  return `<!-- source_commit: ${s.source_commit}; config_hash: ${s.config_hash}; config: ${JSON.stringify(s.config_ref)} -->`;
}

export function parseStampLine(line: string): Stamp | null {
  const m = /^<!-- source_commit: ([0-9a-f]{7,40}); config_hash: ([0-9a-f]{8}); config: (\{.*\}) -->$/.exec(line.trim());
  if (!m) return null;
  return { source_commit: m[1], config_hash: m[2], config_ref: JSON.parse(m[3]) as ConfigRef };
}
