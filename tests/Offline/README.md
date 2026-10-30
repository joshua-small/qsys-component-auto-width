# Offline width regressions

Run with the repository's .NET 10 SDK on Linux or Windows:

```sh
dotnet run --project tests/Offline/OfflineTests.csproj -c Release --artifacts-path /tmp/component-auto-width-tests
```

On Windows, choose a separate Windows output directory instead of `/tmp`.
NuGet restore requires `Lib.Harmony` 2.4.2; subsequent runs can reuse the restored
dependency. Keep build outputs outside the source tree.

The executable links the production `GeometryHooks.cs` and `GeometryRules.cs`.
It uses real Harmony interception against small synthetic host types. The
formatter fixture returns explicit measured numbers and records its arguments;
it does not implement font measurement or Designer rendering.

Coverage includes minimum/rounding boundaries, invalid measurements, exposed-pin
filtering, exact name/fallback handling, changed measurements and PPD, cache
invalidation, shrinkage, measurement errors, native invocation counts, competing
and duplicate patch rejection, unpatch/reinstall, and unrelated patch ownership.
Tests preserve component labels, separators, pin names and positions.

These checks do not exercise `ComponentAutoWidthModule`, Mods Core host-hash
guards, startup ordering, actual Compact Connectors coexistence, partial Harmony
installation failure, native text/font fit, document loading, or save/reopen.
Unpatching a fresh synthetic graphic is not evidence that existing native
graphics shrink on disable. The Windows WPF harness remains separate.

Before claiming integration acceptance, use disposable design copies on the
supported Designer build and record:

- Startup/load order and enable/disable behavior with Compact Connectors both
  present and absent, including rejected host/competing patches and cleanup.
- Exposed-pin add/remove/rename, PrettyName fallback, whitespace, Unicode and
  label separators across representative component renderers.
- Real font/DPI measurements and actual pin-label fit, preserving labels and
  positions, including repeated layout and cached text invalidation.
- Save/reopen with width enabled and disabled. Stock Designer recalculates
  ordinary component width; these tests add no persistence guarantee.

No connected Core or hardware behavior is established by this suite.
