# P8-02 — Saved blueprint library and ghost orders

Date: 2026-09-08. Branch `codex/tram-expansion`, parent HEAD `8e54ed602da159905b0428f316f6e75cb25ae351`. Authority: D-EX-41, the owner's “continue with P8-02”. **Implementation and automated verification are complete.** Human play and phase approval remain outstanding. Changes are local; no commit or push. PROGRESS owns next task P8-03.

## Player behaviour

After recruiting the Foreman, copy a layout and choose **Save clipboard** with a name, folder and catalogue machine icon tag. Select/filter saved layouts, edit their metadata, load one into the clipboard, or use **Export text** / **Import text** to share portable settings. Native text shortcuts remain native in the input fields. Deleting an entry leaves its clipboard and previously queued orders intact.

**Queue ghosts** previews a plan in cyan. A click saves its independent definition without spending stock, reserving machine occupancy, adding power, ammunition or production. Real obstructions and absent supplies may remain underneath a plan; overlapping waiting plans are refused. Each visible order shows a stable ID, location, matching parts, missing material quantities after carried machines and current blockers. Build matching parts manually or use **Build remaining** for one ordinary, atomic construction group. Reach, unlocks, terrain, current inventory, intended underground connections and normal prices still apply. Existing matching machines are neither duplicated nor charged again.

Cancel an order to discard its remaining ghosts while retaining already built machines. Completed/cancelled records are terminal; undo, damage or removal does not silently resurrect a finished order. Remove a finished record to free queue space. Truck loading, supply-chest selection and automated travel/building are P8-03.

## Data and provisional limits

Optional `campaign.plans` schema 1 leaves campaign metadata 10/recruits 4 and earlier geometry unchanged. Old saves keep plans absent until first successful use. Save validation rejects malformed definitions, keys, identities, lifecycle times, footprints and overlapping waiting orders. Library and order definitions are independent deep copies and current saves replay through ordinary commands.

- 32 library entries and 32 total order records, including terminal history; safe monotonic IDs are never recycled.
- Names 1–120 characters, single folder labels up to 64 characters; empty folder means Unfiled. Icons use catalogue machine kinds.
- Portable `relight-blueprint` version 1 JSON, up to 262144 characters; no content, cargo, inventory, rewards, identities or executable code are imported.
- Existing 1–128 entities and normalized 128×128 tile bounds. A maximum freight-rich station layout also round-trips within the text bound.
- Only Foreman-recruited campaigns can mutate plans. Imported locked machines remain data until ordinary machine unlocks permit real construction.

These are agent-selected reversible implementation defaults under D-EX-41, not owner-adopted balance targets.

## Verification

- **292/292 full simulation tests passed**, 404.6 seconds: [full log](evidence/p8-02-2026-09-08/full-test.log). The full suite uses the final simulation/test source; subsequent UI refinements and generated-reference edits are covered by the final build and browser checks.
- **21/21 focused clipboard/library tests passed**, 26.0 seconds: [focused log](evidence/p8-02-2026-09-08/focused-final.log). Coverage includes Foreman/unlock gates, malformed and overlapping imports, bounds and stable IDs, exact shortages/carried machines, inert queue creation, partial manual completion, terminal cancellation/completion, undo, hostile saves, intended tunnel pairing, maximum freight-rich export/import, conservation and an ordinary opening recruitment/library/build/replay sequence. Isolated unlock/stock definitions are labelled fixtures; the end-to-end case walks and pays normally.
- All package typechecks, production build and lint passed: [build](evidence/p8-02-2026-09-08/typecheck.log), [lint](evidence/p8-02-2026-09-08/lint.log). The existing Vite large-bundle advisory remains.
- Campaign reference generation, both-profile documentation checks and evidence freshness passed: [generation](evidence/p8-02-2026-09-08/docsync-generate.log), [docsync](evidence/p8-02-2026-09-08/docsync.log), [freshness](evidence/p8-02-2026-09-08/freshness.log).
- Browser controls passed at **1366×900 and 900×900**: [runner](evidence/p8-02-2026-09-08/browser.cjs), [log](evidence/p8-02-2026-09-08/browser.log), [results](evidence/p8-02-2026-09-08/browser-result.json), [ghost view](evidence/p8-02-2026-09-08/ghosts-1366.png), [compact library](evidence/p8-02-2026-09-08/library-900.png), [completed orders](evidence/p8-02-2026-09-08/completed-1366.png). Real DOM/pointer/keyboard actions cover recruitment/copy, save/update/filter (including a literal asterisk folder), export/import and malformed refusal, library deletion independent of orders, inert ghosts, overlap refusal, native text shortcuts, partial progress, Ctrl+S/O, paid remaining completion, cancellation/removal and complete-session replay. No page errors or horizontal overflow. Only stock/travel/source-build preparation uses ordinary recorded developer helpers; no gameplay-state edits.

Two five-second live lamp/pole samples with the library and a waiting ghost plan present completed **40/40 edits**. Wide: 16.7 ms mean / 16.8 ms maximum frame interval and 14.6 ms mean light paint. Compact: 16.8 / 33.4 ms frames and 20.5 ms light paint. Saved ghosts outside the viewport are culled. These are small local headless samples, not a maximum-queue benchmark, human play or closure of existing larger-network stalls.

The [manifest](evidence/p8-02-2026-09-08/manifest.json) archives **188 source inputs and 6 production files** in [source.zip](evidence/p8-02-2026-09-08/source.zip) and [build.zip](evidence/p8-02-2026-09-08/build.zip). [Freeze script](evidence/p8-02-2026-09-08/freeze.py), [consistency checker](evidence/p8-02-2026-09-08/check.py), [result](evidence/p8-02-2026-09-08/consistency.log) and [verification hashes](evidence/p8-02-2026-09-08/verification.json) identify this checkpoint, validate local references/task handoff and protect earlier evidence. The mutable development preview is on port 5178; frozen pre-P8 port 5179 remains unchanged.

## Findings and boundaries

Early checks found and corrected a module-initialization cycle: sim now invokes plan completion through the existing tile hook boundary instead of importing the flow-backed plan module. Typechecking caught map height belonging to ground rather than FlowState, narrowing around a mutating sync function and a DOM collection without iterable typings. The tunnel test initially omitted copper from its supply fixture; that fixture now takes real copper from the chest. Portable text bounds were raised from the initial 65536-character draft so the largest legal freight-rich library entry can export and re-import itself. A browser review also led to placing the clipboard before the library and refreshing form metadata when selecting, loading, importing or reloading a saved entry. Folder filtering also distinguishes a literal asterisk folder from All folders. The interrupted initial suite is retained in [its log](evidence/p8-02-2026-09-08/full-test-initial.log); the corrected full run passes.

No human usability result, reference-machine performance certification, broader performance finding closure or P6-AMMO fix is inferred. Shared human observations remain after Phase 8 and EX-08D. Earlier EX-08C, P8-00, P8-01, P7-05 and EX-08B reports and evidence remain protected.
