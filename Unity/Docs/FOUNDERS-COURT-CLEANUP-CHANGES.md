# Founders Court cleanup changes

## 1. Decisions file

## 2. Baseline decor

## 3. Generator changes

## 4. Foreman workshop

## 5. Verify

## 6. Capture

## 7. Temporary files

## 8. Gaps in this pass

Step 1 item 1, `git status --porcelain` from the repository root, before any other change in this pass:

```
 M Unity/Docs/FOUNDERS-COURT-DECISIONS.md
 M Unity/Docs/FOUNDERS-COURT-PLAN.json
 M Unity/Docs/FOUNDERS-COURT-PLAN.md
 M Unity/Relight/Assets/Relight/Scenes/World.unity
?? Unity/Docs/FOUNDERS-COURT-AFTER.png
?? Unity/Docs/FOUNDERS-COURT-CLEANUP-CHANGES.md
?? Unity/Docs/FOUNDERS-COURT-COMPOUND-CHANGES.md
?? Unity/Relight/Assets/Editor/FoundersCourtCompound.cs
?? Unity/Relight/Assets/Editor/FoundersCourtCompound.cs.meta
```

The working tree is not clean. The human must commit first. Stopped at Step 1 item 2; Steps 1 item 4 onward, and Steps 2 to 7, were not run. The only line above created by this pass is this report itself (`FOUNDERS-COURT-CLEANUP-CHANGES.md`, created with its eight headings before Step 1 as the prompt requires). The other eight entries are the uncommitted Phase 3 Part B result (last commit `3a707efa` is the pre-apply commit).

Headings 1 to 7 are empty because the pass stopped before them.
