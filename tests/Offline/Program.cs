using System.Reflection;
using Designer.Modules.ComponentAutoWidth;
using HarmonyLib;
using Pin = QSC.QSys.Design.Pin;
using Model = QSC.QSys.Design.PinProvider;
using Graphic = QSC.QSys.Graphic.PinProvider;
using Element = QSC.QSys.Graphic.Element;

internal static class Program
{
    private const string OtherId = "component-auto-width.tests.other";
    private static readonly List<string> Events = [];
    private static int checks;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static void Throws<T>(Action action, string name) where T : Exception
    {
        try { action(); }
        catch (T) { Check(true, name); return; }
        throw new InvalidOperationException(name + ": expected " + typeof(T).Name);
    }

    private static (Model Model, Graphic Graphic) Fixture(params Pin[] pins)
    {
        var model = new Model();
        model.Pins.AddRange(pins);
        return (model, new QSC.QSys.Graphic.Component(model));
    }

    private static void Rules()
    {
        Check(GeometryRules.TextWidth([]) == 54, "no exposed measurements retain minimum");
        Check(GeometryRules.TextWidth([0, -0.0, 52]) == 54, "padding fits within minimum at boundary");
        Check(GeometryRules.TextWidth([52.0001]) == 55, "fraction beyond boundary rounds upward");
        Check(GeometryRules.TextWidth([100.1, 80, 100.1]) == 103, "widest measurement wins independently of order and duplicates");
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.1 })
            Throws<ArgumentOutOfRangeException>(() => GeometryRules.TextWidth([90, invalid]), "invalid measurement rejected: " + invalid);
    }

    private static void Layout()
    {
        Element.Calls.Clear();
        Element.Width = _ => throw new Exception("An unexposed pin must not be measured.");
        var f = Fixture(new Pin { UserCreated = false, PrettyName = "Hidden" });
        f.Graphic.CreatePins();
        Check(Element.Calls.Count == 0, "built-in pins are excluded from measurement");
        Check(f.Graphic.NativeCalls == 1 && f.Graphic.NativeObservedWidth == 54, "original sizing runs once at native minimum");
        Check(f.Graphic.LabelText.Invalidations == 0, "unchanged minimum keeps cache");

        var widths = new Dictionary<string, double> { ["First"] = 70.2, ["Second "] = 100.1 };
        Element.Width = request => widths[request.Text];
        f.Model.Pins.Add(new() { UserCreated = true, PrettyName = "First", CodeName = "Unused" });
        f.Model.Pins.Add(new() { UserCreated = true, PrettyName = "Second " });
        string originalLabel = f.Model.Label;
        var originalPosition = f.Model.Position;
        f.Graphic.CreatePins();
        Check(f.Graphic.NativeObservedWidth == 103, "widest exposed measurement plus padding reaches native sizing");
        Check(Element.Calls.Select(c => c.Text).SequenceEqual(new[] { "First", "Second " }), "formatter receives exact pretty names including trailing whitespace");
        Check(Element.Calls.All(c => c.FontSize == 10 && c.Ppd == 1.25), "formatter receives pin font size and current PPD");
        Check(f.Graphic.LabelText.Invalidations == 1, "changed nominal width invalidates cached label once");
        Check(f.Graphic.LabelText.PPD == 1.25, "cache invalidation preserves PPD");
        Check(f.Model.Label == originalLabel && f.Model.Position == originalPosition, "component label separators and position remain untouched");
        Check(f.Model.Pins[1].CodeName == "Unused" && f.Model.Pins[2].PrettyName == "Second ", "pin labels remain untouched");

        f.Graphic.CreatePins();
        Check(f.Graphic.NativeCalls == 3, "repeated fit preserves one native call per request");
        Check(Element.Calls.Count == 4 && f.Graphic.LabelText.Invalidations == 1, "repeat measures again without unnecessary invalidation");

        widths["Second "] = 130;
        f.Graphic.CreatePins();
        Check(f.Graphic.NativeObservedWidth == 132 && f.Graphic.LabelText.Invalidations == 2, "changed measurement for the same label invalidates stale width");
        f.Model.Pins[2].PrettyName = "First";
        f.Graphic.CreatePins();
        Check(f.Graphic.NativeObservedWidth == 73 && f.Graphic.LabelText.Invalidations == 3, "renaming the longest pin shrinks width");

        f.Graphic.LabelText.PPD = 1.75;
        Element.Calls.Clear();
        widths["First"] = 95.2;
        f.Graphic.CreatePins();
        Check(Element.Calls.All(c => c.Ppd == 1.75), "later measurements use changed PPD");
        Check(f.Graphic.NativeObservedWidth == 98, "new host measurement controls width after PPD change");

        f.Model.Pins[1].UserCreated = false;
        f.Model.Pins[2].UserCreated = false;
        Element.Calls.Clear();
        f.Graphic.CreatePins();
        Check(f.Graphic.NativeObservedWidth == 54 && Element.Calls.Count == 0, "hiding exposed pins restores minimum without measuring them");
        f.Model.Pins.Clear();
        int invalidations = f.Graphic.LabelText.Invalidations;
        f.Graphic.CreatePins();
        Check(f.Graphic.NativeObservedWidth == 54 && f.Graphic.LabelText.Invalidations == invalidations, "removing all pins at minimum is stable");
        Check(f.Graphic.NativeCalls == 8, "all layout transitions preserve native invocation count");
    }

    private static void Names()
    {
        foreach (string? pretty in new string?[] { null, "" })
        {
            Element.Calls.Clear();
            Element.Width = _ => 90;
            var f = Fixture(new Pin { UserCreated = true, PrettyName = pretty, CodeName = "Fallback" });
            f.Graphic.CreatePins();
            Check(Element.Calls.Single().Text == "Fallback" && f.Graphic.NativeObservedWidth == 92, "empty pretty name uses code name: " + (pretty is null ? "null" : "empty"));
        }
        Element.Calls.Clear();
        Element.Width = _ => 0;
        var empty = Fixture(new Pin { UserCreated = true });
        empty.Graphic.CreatePins();
        Check(Element.Calls.Single().Text == "" && empty.Graphic.NativeObservedWidth == 54, "missing both names supplies empty text to formatter");

        foreach (string text in new[] { "  ", "café λ\u001d名前" })
        {
            Element.Calls.Clear();
            Element.Width = _ => 60.1;
            var f = Fixture(new Pin { UserCreated = true, PrettyName = text, CodeName = "Do not use" });
            f.Graphic.CreatePins();
            Check(Element.Calls.Single().Text == text && f.Graphic.NativeObservedWidth == 63, "nonempty whitespace or Unicode name is passed unchanged");
        }
    }

    private static void MeasurementFailures()
    {
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, -1d })
        {
            var f = Fixture(new Pin { UserCreated = true, PrettyName = "Good" }, new Pin { UserCreated = true, PrettyName = "Bad" });
            f.Graphic.LabelText.NominalWidth = 88;
            Element.Width = request => request.Text == "Good" ? 150 : invalid;
            int errors = Events.Count(e => e == "geometry-error");
            f.Graphic.CreatePins();
            Check(f.Graphic.NativeObservedWidth == 88 && f.Graphic.LabelText.Invalidations == 0, "invalid measurement prevents partial width change: " + invalid);
            Check(f.Graphic.NativeCalls == 1 && Events.Count(e => e == "geometry-error") == errors + 1, "invalid measurement reports error and retains native call");
        }
        var failed = Fixture(new Pin { UserCreated = true });
        Element.Width = _ => throw new InvalidOperationException("Synthetic formatter failure");
        failed.Graphic.CreatePins();
        Check(failed.Graphic.NativeCalls == 1 && failed.Graphic.NativeObservedWidth == 54, "formatter exception leaves original sizing active");
        Element.Width = _ => 80;
        failed.Graphic.CreatePins();
        Check(failed.Graphic.NativeCalls == 2 && failed.Graphic.NativeObservedWidth == 82, "later sizing recovers after formatter exception");
    }

    private static void Lifecycle(HostBindings bindings)
    {
        var owned = new Harmony(GeometryHooks.PatchId);
        owned.UnpatchAll(GeometryHooks.PatchId);
        GeometryHooks.Host = null;
        var unrelated = typeof(UnrelatedHost).GetMethod(nameof(UnrelatedHost.Run))!;
        var other = new Harmony(OtherId);
        other.Patch(unrelated, prefix: new HarmonyMethod(typeof(OtherPatch), nameof(OtherPatch.Prefix)));
        other.Patch(bindings.CreatePins, prefix: new HarmonyMethod(typeof(OtherPatch), nameof(OtherPatch.Prefix)));
        Throws<InvalidOperationException>(() => GeometryHooks.Install(bindings), "competing target patch prevents installation");
        Check(GeometryHooks.Host is null && !owned.GetPatchedMethods().Any(), "rejected install leaves no owned hooks or host binding");
        Check(Harmony.GetPatchInfo(bindings.CreatePins)!.Owners.Contains(OtherId), "rejected install preserves competing patch");
        Element.Calls.Clear();
        var rejected = Fixture(new Pin { UserCreated = true, PrettyName = "Still native" });
        rejected.Graphic.CreatePins();
        Check(rejected.Graphic.NativeCalls == 1 && rejected.Graphic.NativeObservedWidth == 54 && Element.Calls.Count == 0, "rejected install leaves target behavior native");

        other.Unpatch(bindings.CreatePins, HarmonyPatchType.All, OtherId);
        var active = GeometryHooks.Install(bindings);
        Check(active.GetPatchedMethods().SequenceEqual(new[] { bindings.CreatePins }), "install patches only width entry point without a connector dependency");
        Throws<InvalidOperationException>(() => GeometryHooks.Install(bindings), "duplicate install is rejected without removing existing hook");
        Element.Width = _ => 110;
        var f = Fixture(new Pin { UserCreated = true, PrettyName = "Wide" });
        f.Graphic.CreatePins();
        Check(f.Graphic.NativeObservedWidth == 112 && f.Graphic.NativeCalls == 1, "existing hook still fits after duplicate-install rejection");
        UnrelatedHost.Run();
        Check(UnrelatedHost.NativeCalls == 1 && Harmony.GetPatchInfo(unrelated)!.Owners.Contains(OtherId), "unrelated patched method still runs while width is enabled");

        active.UnpatchAll(GeometryHooks.PatchId);
        Element.Calls.Clear();
        var unpatched = Fixture(new Pin { UserCreated = true, PrettyName = "Wide" });
        unpatched.Graphic.CreatePins();
        Check(unpatched.Graphic.NativeObservedWidth == 54 && Element.Calls.Count == 0, "unpatching restores native behavior for a fresh graphic");
        Check(Harmony.GetPatchInfo(unrelated)!.Owners.Contains(OtherId), "removing owned hooks preserves unrelated patch");
        GeometryHooks.Install(bindings);
        unpatched.Graphic.CreatePins();
        Check(unpatched.Graphic.NativeObservedWidth == 112 && unpatched.Graphic.NativeCalls == 2, "reinstallation fits a previously unpatched graphic");
    }

    private static int Main()
    {
        try
        {
            Rules();
            GeometryHooks.Report = (kind, _) => Events.Add(kind);
            Throws<TypeLoadException>(() => new HostBindings(typeof(string).Assembly), "assembly without required host types is rejected");
            var bindings = new HostBindings(Assembly.GetExecutingAssembly());
            GeometryHooks.Install(bindings);
            Layout();
            Names();
            Check(!Events.Contains("geometry-error"), "valid layout and naming cases report no hook errors");
            MeasurementFailures();
            Lifecycle(bindings);
            string[] forbidden = ["Q-Sys Designer", "ModuleCommon", "PresentationCore", "PresentationFramework"];
            Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => forbidden.Contains(a.GetName().Name)), "no vendor or WPF assemblies loaded");
            Console.WriteLine($"{checks} offline checks passed. Synthetic host metrics; no WPF, Designer, or vendor assemblies loaded.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            new Harmony(GeometryHooks.PatchId).UnpatchAll(GeometryHooks.PatchId);
            new Harmony(OtherId).UnpatchAll(OtherId);
            GeometryHooks.Host = null;
            GeometryHooks.Report = null;
        }
    }
}
