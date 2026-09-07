# Automated browser observation, 2026-09-07

Local Vite game, seed 3, exploration-v2, world view. The snapshots came from the labelled fixtures in campaignDiscovery.test.ts, exported via EX07_REVIEW_WINDUP_PATH and EX07_REVIEW_PENDING_PATH. This was automation, not owner play.

- Wind-up fixture at simulation second 0: world screenshot showed the optional workshop-records label, home/perception circles and yellow strike ring. The panel explained 5-tile activity perception and the 8-tile leash. “Recover field-repair schematic” was disabled: “Stalker guarding the records: draw it away or use your rifle or a supplied turret.”
- Defeated-guardian fixture at simulation second 5: recovery button was enabled, with the engineer at the cache. Clicking it while paused updated the panel immediately through the queued simulation command.
- Result: “Field-repair tool fitted · 40 HP in 2s for 2 steel + 1 copper; disabled core recovery 6s for 10 steel + 5 copper. Materials still required. Workshop records empty.” The button became disabled and its help text stated that the tool was already fitted.

The fixture’s modified stock/start position and the wind-up fixture’s deferred other threats are not pacing evidence. The snapshots and two temporary browser tabs were removed/closed, and the loopback development server stopped. Existing browser save slots were not written.
