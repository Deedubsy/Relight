# Build hover descriptions — 2026-09-15

The Build menu now explains the purpose of each of its 25 buildable items. Crafting machines also list the unique outputs of their currently supported recipes, using ProductionRules.RecipesFor and the catalogue item names. Mining machines explain extraction; the Generator explains electricity production. Logistics, lighting and defences describe their function without suggesting they craft items.

The existing tooltip retains the 300 ms hover delay, construction costs, power requirements, availability, pointer transparency and screen-edge placement. Detailed technical stats remain in the Build detail pane. No gameplay rules, recipes, costs, assets or scene settings changed.

Changed code: Assets/Relight/Sim/UI/Build/BuildCatalogue.cs and Assets/Relight/UI/Build/BuildPanelController.cs.

Validation: Unity compiled without errors. Live synthetic UI Toolkit enter/leave events exercised all 25 build cards through their category tabs, checking purpose text, output text, hiding on leave and closing Build. Foundry metals, Assembler ammunition/components and electricity-independent belts were checked explicitly. Clean Foundry and Assembler tooltips were captured and checked within the screen bounds at 1920x1080 and 1280x720. The 720p Assembler capture was visually inspected for readable wrapping and clipping. Unity's current console reported zero errors and zero warnings.

Evidence: [live checks](evidence/build-tooltips/live-checks.txt), [layout checks](evidence/build-tooltips/visual-checks.txt), [720p screenshot](evidence/build-tooltips/assembler-1280.png). Scripts alongside the evidence reproduce the checks. Test-session autosaves were disabled and no player save files were accessed. Original Game view size was restored. Native mouse and standalone build acceptance were not run; no broad simulation rerun was needed for these UI descriptions.

Documentation tooling: docsync:check and freshness:check were attempted; both are blocked by the existing missing tsx executable. Scoped git diff --check found no whitespace errors (only the repository's LF-to-CRLF notice).
