# Management window layout

The authored management surface uses a 960 x 640 reference rectangle. Its Canvas
Scaler uses Scale With Screen Size / Expand, and Management Background is a
centered, fixed reference rectangle. Native borders and the title bar are outside
the requested client area; WindowsWindowController accounts for them with
AdjustWindowRectExForDpi and refreshes the native frame when switching modes.

The inventory content rectangle includes the grid, equipment preview and stats.
Hero Stats Panel is authored in Main and referenced by ManagementUiController.
Its labels and More Stats button remain editable in the Hierarchy and Inspector.
The stats panel anchors to the content's upper right. Expanded secondary values
use separate rows to leave room for long stat names.

## Editor tools

- Taskbar Tactics / UI / Repair Management Window Layout: reapplies the bounded
  layout, checks panel containment and serialized references, then saves Main.
- Taskbar Tactics / UI / Build Repaired Windows Player: builds the saved scene
  directly into Builds/WindowsLayoutFix/TaskbarTactics.exe. It does not invoke the
  vertical-slice content generator.
- Temp/ManagementLayoutRepair.result records the most recent layout/build result.

For standalone visual checks, pass `-management-window` to the executable to open
management directly. Normal startup still opens the taskbar strip.

## Verification checklist

- Compile scripts and build Windows x64 without errors.
- Open Inventory and verify that both stat columns stay inside the wooden frame
  and do not overlap the equipment slots.
- Expand/collapse More Stats and check that the longer regeneration labels fit.
- Switch heroes and revisit Inventory from Party/Skills/Map.
- Switch between strip and management mode and check the same layout again.
- Repeat on actual Windows displays at 100%, 125% and 150% DPI when available;
  a Game View resize alone does not verify native Windows DPI behavior.

Existing missing-script/font/localization warnings are separate from this layout
repair and should not be treated as a successful clean-project warning audit.

## Verification performed (2026-10-25)

- Windows x64 player build succeeded with zero compiler/build errors.
- Main's two existing SceneArchitectureTests passed.
- Checked Inventory in Editor Play Mode and the actual Windows player, including
  expanded stats and switching from Warrior to Mage: text stayed inside the frame
  and clear of equipment slots.
- The Windows player started and exited without logged exceptions. The existing
  missing authored tooltip warning remains.
- Other physical Windows DPI configurations were not available for this check.
