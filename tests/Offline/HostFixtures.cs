using System.Runtime.CompilerServices;

// The offline fixture supplies measured numbers, not a font renderer.
// Production GeometryHooks.cs and GeometryRules.cs are linked unchanged.
namespace System.Windows.Media
{
    public sealed class FormattedText(double width)
    {
        public double WidthIncludingTrailingWhitespace => width;
    }
}

namespace QSC.QSys.Design
{
    public sealed class Pin
    {
        public bool UserCreated;
        public string? PrettyName;
        public string? CodeName;
    }

    public sealed class PinProvider
    {
        public List<Pin> Pins { get; } = [];
        public string Label = "Component\u001dItalic label\u001d";
        public (double X, double Y) Position = (30, 40);
    }
}

namespace QSC.QSys.Graphic
{
    public sealed class ComponentLabelText
    {
        public double NominalWidth = 54;
        private double ppd = 1.25;
        public int Invalidations { get; private set; }
        public double PPD
        {
            get => ppd;
            set { ppd = value; Invalidations++; }
        }
    }

    public class PinProvider(Design.PinProvider model)
    {
        protected ComponentLabelText _LabelText = new();
        public Design.PinProvider InnerPinProvider => model;
        public ComponentLabelText LabelText => _LabelText;
        public int NativeCalls { get; private set; }
        public double NativeObservedWidth { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CreatePins() => _CreatePins();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void _CreatePins()
        {
            NativeCalls++;
            NativeObservedWidth = _LabelText.NominalWidth;
        }
    }

    public sealed class Component(Design.PinProvider model) : PinProvider(model) { }

    public static class Element
    {
        public readonly record struct Measurement(string Text, double FontSize, double Ppd);
        public static readonly List<Measurement> Calls = [];
        public static Func<Measurement, double> Width = _ => 0;

        public static System.Windows.Media.FormattedText GetFormattedText(string text, double size, double ppd)
        {
            var request = new Measurement(text, size, ppd);
            Calls.Add(request);
            return new(Width(request));
        }
    }
}

internal static class UnrelatedHost
{
    public static int NativeCalls;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run() => NativeCalls++;
}

internal static class OtherPatch
{
    public static int Calls;
    public static void Prefix() => Calls++;
}
