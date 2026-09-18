# Founders Court yard changes

## 1. Decisions file

Not reached: Step B1 stopped on the working-tree check below.

## 2. Generator changes

Not reached.

## 3. Generated and deleted objects

Not reached.

## 4. Verify

Not reached.

## 5. Capture

Not reached.

## 6. Temporary files

None.

## 7. Gaps in this pass

1. **Step B1: working tree not clean.** `git status --porcelain` from the repo root, run after the go message's plan-data changes were applied (office moved to (106,353) 12×7, gap object named "Works Yard broken fence", heading 13 updated, picture regenerated) and after this report was created with its seven headings. Ignoring the lines under `Unity/Docs/`, three lines remain: the Phase 4 generator, scene and baseline asset changes, uncommitted since the cleanup pass. The pass stopped here without touching the decisions file, the generator or the scene. The human must commit first, then send "go apply" again.

```
 M Unity/Docs/FOUNDERS-COURT-CLEANUP-CHANGES.md
 M Unity/Docs/FOUNDERS-COURT-DECISIONS.md
 M Unity/Docs/FOUNDERS-COURT-PLAN.json
 M Unity/Docs/FOUNDERS-COURT-PLAN.md
 M Unity/Docs/FOUNDERS-COURT-PLAN.png
 M Unity/Relight/Assets/Editor/FoundersCourtCompound.cs
 M Unity/Relight/Assets/Relight/Scenes/World.unity
 M Unity/Relight/Assets/Relight/World/Manual/SceneBaseGeometry.asset
?? Unity/Docs/FOUNDERS-COURT-AFTER-2.png
```

Working tree after this pass (`git status --porcelain`, not committed):

```
 M Unity/Docs/FOUNDERS-COURT-CLEANUP-CHANGES.md
 M Unity/Docs/FOUNDERS-COURT-DECISIONS.md
 M Unity/Docs/FOUNDERS-COURT-PLAN.json
 M Unity/Docs/FOUNDERS-COURT-PLAN.md
 M Unity/Docs/FOUNDERS-COURT-PLAN.png
 M Unity/Relight/Assets/Editor/FoundersCourtCompound.cs
 M Unity/Relight/Assets/Relight/Scenes/World.unity
 M Unity/Relight/Assets/Relight/World/Manual/SceneBaseGeometry.asset
?? Unity/Docs/FOUNDERS-COURT-AFTER-2.png
```
