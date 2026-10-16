# Component Auto Width

Automatically fit component widths to exposed control-pin labels without changing their text. It uses Designer's own font measurement and preserves component names and label separators.

This is the independent successor to the width feature in Automatic Geometry. Install it with Mods Core 0.1.7 or later within 0.1.x. Remove Automatic Geometry in the same Mod Manager transaction. Old Automatic Geometry releases and profile pins remain available for rollback. [Compact Connectors](https://github.com/joshua-small/qsys-compact-connectors) independently provides connector geometry and snapping.

There is no status-only menu action. Initialization details are at **File > Preferences > Mod Diagnostics**. Width receipts are under `%LOCALAPPDATA%/QsysSchematicTools/Designer-10.5/component-auto-width`.

Only the exact approved Designer 10.5 build is supported. The module retains Core's exact host guards and rejects competing patches. Tests cover label fitting, fallback names, cache invalidation, native call preservation and independent installation. These are synthetic WPF checks; see release notes for separate native acceptance.

Clone [Mods Core](https://github.com/joshua-small/qsys-mods-core) alongside this repository. Build with .NET 10.0.112 and a local `DesignerDir` containing vendor references. Run `dotnet run --project tests/GeometryTests.csproj` on Windows for the synthetic harness; pass the Designer directory for read-only native metadata checks. No vendor binaries are included.

Source extracted from qsys-automatic-geometry PR #7 (source checkpoint c5309a2). Source ownership now resides here; no new source license is granted. Third-party license notices and artwork provenance are retained.

## Geometry update acceptance (2026-10-16)

A disposable exact-build Designer 10.5 copy with redirected preferences exercised Containers, Channel Groups and a second Container nesting level. Twelve top/bottom pin operations covered main and exposed drawer pins; each verified equal native CanvasCenter coordinates and one-step undo/redo. Six registered pin commands and twelve existing routed boundary commands ran with native nested-canvas focus. Three checks confirmed six body/drawer snap corners and the Wiring snap domain on compact connectors. The fixture was saved to a separate output and the original hash stayed unchanged.

This is native API/routed-command evidence. Physical drag tolerance, physical shortcut input, connected wires, broader component/domain coverage and DSP/hardware are not established by it. Source and test fixtures contain no vendor code or binaries. The guarded driver is in Better Packing tests/PinProbe; it only runs with a matching disposable path, marker and fixture hash.
