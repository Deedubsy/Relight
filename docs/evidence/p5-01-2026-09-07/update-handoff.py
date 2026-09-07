"""Apply the P5-01 handoff once; preserve each document's original newline format."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[3]
def edit(name, fn):
    p = root / name
    data = p.read_bytes()
    newline = b'\r\n' if b'\r\n' in data else b'\n'
    old = data.decode('utf-8').replace('\r\n', '\n')
    new = fn(old)
    assert new != old, name
    p.write_bytes(new.replace('\n', newline.decode()).encode('utf-8'))

def progress(s):
    s = re.sub(r'^\| P5-01 \|.*$', '| P5-01 | Construction commands, drag paths, undo/redo and controls | agent | done | EX-09A | PHASE_5_SCOPE_REPORT.md P5-01: command-only factory actions, conserved path/undo operations, save/replay, catalogue/bindings/strings and actual browser controls | docs/P5_01_CONSTRUCTION_REPORT.md | 2026-09-07 |', s, flags=re.M)
    s = re.sub(r'(^\| P5-02 \|[^\n]*?\| agent \|) blocked ', r'\1 todo ', s, flags=re.M)
    s = re.sub(r'^\| T13 \|.*$', '| T13 | Retained Phase 5 placement and command milestone | claude | done | P5-01 | Review P5-01 command-only UI, paid in-reach construction, path/undo, hotbar, bindings and string-table evidence; D-SA-2 human sign-off is separately retained at phase exit | docs/P5_01_CONSTRUCTION_REPORT.md — technical review complete; human sign-off retained | 2026-09-07 |', s, flags=re.M)
    return s + '\n- 2026-09-07 — P5-01 and retained T13 technical review done under D-EX-22: ordinary factory command dispatch, atomic corner-aware belt paths, saved current-inventory undo/redo, shared catalogue/bindings/factory presentation and responsive controls. 203/203 tests, package/build/lint, legacy snapshot, actual browser controls at 1366/900 widths, documentation/freshness and evidence checks passed. Browser testing found and fixed omitted final-tick replay commands. P5-02 is todo. Human construction/gameplay gates and D-SA-2 sign-off remain outstanding; Phase 5 is not complete. See P5_01_CONSTRUCTION_REPORT.md.\n'
edit('docs/PROGRESS.md', progress)

def decisions(s):
    marker = '### Supersession boundaries'
    row = '| D-EX-22 | Continue the reconciled Phase 5 plan with construction commands and controls. | decided | owner (current user) | 2026-09-07 | “Looks good lets continue with that plan” after the EX-09A scope audit and P5-01 handoff. | P5-01 implementation/verification and retained T13 technical review. The 128-tile path, x-then-y skipped-pointer interpolation and 50-group saved history are provisional task defaults. Undo compensates current construction without rewinding gameplay. D-SA-2 human sign-off and later Phase 5/human work remain separate; no commit, merge or push implied. |\n\n'
    assert 'D-EX-22 |' not in s
    return s.replace(marker, row + marker, 1)
edit('docs/DECISIONS.md', decisions)

def state(s):
    s = s.replace('EX-09A reconciliation is complete; P5-01 construction commands and controls is next.', 'EX-09A and P5-01 construction commands/controls are complete; P5-02 routing primitives is next.')
    s = s.replace('Phase 5 has started with its scope audit complete; new factory implementation begins at P5-01.', 'Phase 5 has started with its scope audit and P5-01 construction increment complete; routing primitives are next.')
    s = s.replace('P5-01 is next for command-only construction, drag paths, undo/redo and controls;', '[P5_01_CONSTRUCTION_REPORT.md](P5_01_CONSTRUCTION_REPORT.md) completes command-only construction, drag paths, saved undo/redo and controls; P5-02 is next;')
    s = s.replace('and EX-09A audit are currently uncommitted', 'and EX-09A audit plus P5-01 are currently uncommitted')
    s = s.replace('The final suite passed 195/195 tests', 'The EX-07 suite passed 195/195 tests')
    s = s.replace('New factory implementation begins at P5-01.', 'P5-01 subsequently implemented the first factory increment.')
    marker = 'Untracked `.serena/`'
    update = 'P5-01 adds paid corner-aware belt paths, inventory/reach-safe construction history and command-dispatched factory controls. The optional history is saved; final-tick paused commands now replay correctly. Its final suite passed 203/203, all package typechecks/production build, lint and the unchanged legacy snapshot. Functional Chrome checks exercised actual controls at 1366×900 and 900×900, normal stock transfers, grouped paths, refusals, save/load and matching replay hashes. Documentation profiles, freshness, references/statuses and frozen-archive checks passed. See [P5-01 report](P5_01_CONSTRUCTION_REPORT.md) and its source/build manifest. Port 5176 serves the current local production build; port 5175 remains the frozen EX-08A checkpoint. Neither functional automation nor T13 technical review completes human Phase 5 exit. P5-02 is the next runnable task.\n\n'
    return s.replace(marker, update + marker)
edit('docs/PROGRAMME_STATE.md', state)
edit('CLAUDE.md', lambda s: s.replace('EX-09A records the reconciled scope and P5-01 is the first implementation increment.', 'EX-09A records the reconciled scope, P5-01 construction/controls is complete and P5-02 routing primitives is next.'))
edit('README.md', lambda s: s.replace('Phase 5 has begun with its factory scope audit.', 'Phase 5 has begun with its scope audit and construction increment complete.').replace('EX-09A is complete and P5-01 construction controls is next.', 'EX-09A and P5-01 are complete; P5-02 routing primitives is next.'))
edit('docs/PHASES.md', lambda s: s.replace('implementation and human exit remain outstanding.', 'P5-01 construction is complete, while remaining implementation and human exit are outstanding.').replace('P5-01 is next.', 'P5-01 construction is complete; P5-02 routing primitives is next.').replace('Phase 5 has no implementation or human completion claim from this sequencing change.', 'P5-01 has technical evidence in P5_01_CONSTRUCTION_REPORT.md; Phase 5 has no overall completion or human-pass claim.'))
def design(s):
    marker = '**Truck location/unlock adopted in D-EX-Q08'
    update = '**P5-01 implemented (D-EX-22):** factory controls dispatch ordinary commands. Belt drag previews a contiguous corner-aware path, commits atomically on release and cancels with Escape. Provisional defaults are 128 tiles, x-then-y skipped-pointer interpolation and 50 saved undo/redo groups. Undo packs current contents; rebuild uses carried machines or their normal price and restores settings. Current reach, occupancy, damage/repair, pockets, unfinished recipes and ammunition capacity can refuse the operation. Time and production do not rewind. Shared build/input/factory presentation tables and actual browser controls are verified; no translation, rebinding-screen or D-SA-2 human approval is claimed. See [implementation and evidence](P5_01_CONSTRUCTION_REPORT.md). Remaining Phase 5 features below are still intended work.\n\n'
    return s.replace(marker, update + marker, 1)
edit('docs/RELIGHT-design.md', design)
edit('docs/EXPLORATION_DEFENCE_PLAN.md', lambda s: s.replace('PROGRESS owns their status. Under D-EX-20', 'PROGRESS owns their status. [P5-01 evidence](P5_01_CONSTRUCTION_REPORT.md) records the implemented construction/controls increment; remaining factory work and human exit stay separate. Under D-EX-20'))

def standards(s):
    updates = {
      '1.3': '**built P5-01** — contiguous corner-aware preview, atomic path command and cancellation; P5_01_CONSTRUCTION_REPORT.md',
      '1.10': '**built P5-01** — 50 saved groups, current-stock/reach undo and redo, atomic refusal, no world rewind; P5_01_CONSTRUCTION_REPORT.md',
      '1.16': '**built P5-01 for current machines** — one catalogue for hotbar/menu references, purposes, prices, carried counts and locks; later machines extend it',
      '1.17': '**built P5-01** — shared binding table for game handlers/held movement and factory shortcut labels; rebinding screen remains Phase 12',
      '8.5': '**factory groundwork P5-01** — provisional English factory presentation table; sim-owned refusal/status descriptions retained, no translations or game-wide localisation completion; D-SA-2 human sign-off remains at phase exit',
    }
    lines = []
    for line in s.splitlines():
      cells = line.split('|')
      if len(cells) >= 8 and cells[1].strip() in updates:
        cells[4] = ' ' + updates[cells[1].strip()] + ' '
        line = '|'.join(cells)
      lines.append(line)
    return '\n'.join(lines) + '\n'
edit('docs/STANDARDS.md', standards)
print('Updated P5-01/T13 evidence, next task and authority handoff; historical scope report and human gates preserved.')
