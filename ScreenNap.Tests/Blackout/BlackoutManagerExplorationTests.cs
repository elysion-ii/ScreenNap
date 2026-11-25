using System.Globalization;
using ScreenNap.Blackout;
using ScreenNap.Monitors;
using ScreenNap.Native;
using ScreenNap.Tests.TestDoubles;
using ScreenNap.Tray;
using Xunit;

namespace ScreenNap.Tests.Blackout;

// Runs every sequence of up to four user actions and display changes against BlackoutManager
// and, after each step, compares it with a reference model of the specification: a blackout
// belongs to a physical monitor, or to a display device path for a monitor without a physical
// identity (INV-2), and only the user clears what is desired (INV-4)
public sealed class BlackoutManagerExplorationTests
{
    private const int Depth = 4;

    private static readonly CultureInfo s_english = CultureInfo.GetCultureInfo("en-US");
    private static readonly MonitorIdentity s_a = MonitorIdentity.For(null, "connection A");
    private static readonly MonitorIdentity s_b = MonitorIdentity.For(null, "connection B");

    // Physical monitors A and B have identities; N has none. Paths move between monitors,
    // a monitor changes its bounds, and the enumeration can be empty (MON-6)
    private static readonly MonitorInfo[][] s_topologies =
    [
        [Monitor("D1", "A", s_a, 0), Monitor("D2", "B", s_b, 1920)],
        [Monitor("D1", "A", s_a, 0), Monitor("D2", "B", s_b, 1920), Monitor("D3", "N", null, 3840)],
        [Monitor("D1", "A", s_a, 0)],
        [Monitor("D1", "N", null, 0), Monitor("D2", "B", s_b, 1920)],
        [Monitor("D1", "B", s_b, 0), Monitor("D2", "A", s_a, 1920)],
        [Monitor("D1", "A", s_a, -2560, 2560)],
        [],
    ];

    [Fact]
    [Trait("Requirement", "INV-2")]
    [Trait("Requirement", "INV-3")]
    [Trait("Requirement", "INV-4")]
    [Trait("Requirement", "BLK-4")]
    [Trait("Requirement", "DISP-2")]
    [Trait("Requirement", "DISP-3")]
    [Trait("Requirement", "DISP-4")]
    [Trait("Requirement", "DISP-5")]
    [Trait("Requirement", "DISP-6")]
    [Trait("Requirement", "DISP-7")]
    [Trait("Requirement", "MENU-6")]
    [Trait("Requirement", "HK-2")]
    internal void EverySequence_UpToFourSteps_KeepsTheSpecifiedState()
    {
        int sequences = 0;
        foreach (int start in new[] { 0, 1, 3 })
            Explore(new List<Step>(), start, ref sequences);

        Assert.True(sequences > 10_000, $"Only {sequences} sequences ran");
    }

    private static void Explore(List<Step> prefix, int start, ref int sequences)
    {
        sequences++;
        if (prefix.Count == Depth)
            return;

        foreach (Step step in NextSteps(prefix, start))
        {
            prefix.Add(step);
            Replay(prefix, start);
            Explore(prefix, start, ref sequences);
            prefix.RemoveAt(prefix.Count - 1);
        }
    }

    // The steps that apply after the prefix; replaying keeps the explorer free of shared state
    private static IEnumerable<Step> NextSteps(List<Step> prefix, int start)
    {
        World world = Replay(prefix, start);
        for (int i = 0; i < world.Topology.Length; i++)
            yield return new Step(StepKind.Hotkey, i);
        yield return new Step(StepKind.OpenMenu, 0);
        for (int i = 0; i < (world.MenuSnapshot?.Length ?? 0); i++)
            yield return new Step(StepKind.MenuSelect, i);
        yield return new Step(StepKind.ReleaseAll, 0);
        for (int i = 0; i < world.OpenWindowCount; i++)
            yield return new Step(StepKind.UserDismiss, i);
        for (int t = 0; t < s_topologies.Length; t++)
        {
            if (!ReferenceEquals(s_topologies[t], world.Topology))
                yield return new Step(StepKind.DisplayChange, t);
        }
    }

    private static World Replay(List<Step> steps, int start)
    {
        var world = new World(s_topologies[start]);
        for (int i = 0; i < steps.Count; i++)
        {
            world.Apply(steps[i]);
            world.AssertMatchesTheModel(steps.Take(i + 1));
        }
        return world;
    }

    private static MonitorInfo Monitor(string path, string name, MonitorIdentity? identity, int left, int width = 1920)
        => new(path, name, new RECT { Left = left, Right = left + width, Bottom = 1080 }, false, identity);

    private enum StepKind
    {
        Hotkey,
        OpenMenu,
        MenuSelect,
        ReleaseAll,
        UserDismiss,
        DisplayChange,
    }

    private readonly record struct Step(StepKind Kind, int Argument)
    {
        public override string ToString() => $"{Kind}({Argument})";
    }

    private sealed class World
    {
        private readonly FakeBlackoutWindowFactory _factory = new();
        private readonly BlackoutManager _manager;
        private readonly ContextMenu _menu;
        // The model: which monitors are blacked out and which physical monitors are desired
        private readonly HashSet<string> _live = new(StringComparer.Ordinal);
        private readonly HashSet<string> _desired = new(StringComparer.Ordinal);
        private int _trayCount;

        internal World(MonitorInfo[] topology)
        {
            Topology = topology;
            _manager = new BlackoutManager(_factory, new FakeUserNotifier(), s_english);
            _manager.ActiveCountChanged += () => _trayCount = _manager.ActiveCount;
            _menu = new ContextMenu(_manager, () => Topology, new FakePopupMenuApi(), () => { }, s_english);
        }

        internal MonitorInfo[] Topology { get; private set; }
        internal MonitorInfo[]? MenuSnapshot { get; private set; }
        internal int OpenWindowCount => _factory.Windows.Count(window => window.IsOpen);

        internal void Apply(Step step)
        {
            switch (step.Kind)
            {
                case StepKind.Hotkey:
                    _manager.Toggle(Topology[step.Argument]);
                    ModelToggle(Topology[step.Argument]);
                    break;
                case StepKind.OpenMenu:
                    _menu.Show(IntPtr.Zero);
                    MenuSnapshot = Topology;
                    break;
                case StepKind.MenuSelect:
                    _menu.HandleCommand(MenuCommandIds.MonitorBase + step.Argument);
                    MonitorInfo selected = MenuSnapshot![step.Argument];
                    // MENU-6: a live blackout closes; otherwise the monitor as it is now, if present
                    MonitorInfo? target = _live.Contains(KeyOf(selected))
                        ? selected
                        : Array.Find(Topology, monitor => KeyOf(monitor) == KeyOf(selected));
                    if (target is not null)
                        ModelToggle(target);
                    break;
                case StepKind.ReleaseAll:
                    _manager.ReleaseAll();
                    _live.Clear();
                    _desired.Clear();
                    break;
                case StepKind.UserDismiss:
                    FakeBlackoutWindow window = _factory.Windows.Where(open => open.IsOpen).ElementAt(step.Argument);
                    _live.Remove(KeyOf(window.Identity, window.DevicePath));
                    if (window.Identity is { } identity)
                        _desired.Remove(identity.Value);
                    window.DismissByUser();
                    break;
                case StepKind.DisplayChange:
                    Topology = s_topologies[step.Argument];
                    _manager.Reconcile(Topology);
                    _live.IntersectWith(Topology.Select(KeyOf));
                    foreach (MonitorInfo monitor in Topology)
                    {
                        if (monitor.Identity is { } desired && _desired.Contains(desired.Value))
                            _live.Add(KeyOf(monitor));
                    }
                    break;
            }
        }

        internal void AssertMatchesTheModel(IEnumerable<Step> steps)
        {
            string sequence = string.Join(" -> ", steps);
            List<FakeBlackoutWindow> open = [.. _factory.Windows.Where(window => window.IsOpen)];

            Assert.True(_manager.ActiveCount == _live.Count, $"{sequence}: {_manager.ActiveCount} live blackouts, the model has {_live.Count}");
            Assert.True(_trayCount == _manager.ActiveCount, $"{sequence}: the tray shows {_trayCount}, {_manager.ActiveCount} are live (INV-3)");
            Assert.True(open.Count == _manager.ActiveCount, $"{sequence}: {open.Count} windows are open, {_manager.ActiveCount} are tracked");
            Assert.True(
                open.Select(window => KeyOf(window.Identity, window.DevicePath)).Distinct(StringComparer.Ordinal).Count() == open.Count,
                $"{sequence}: two blackouts cover one monitor (INV-2)");

            foreach (MonitorInfo monitor in Topology)
            {
                bool expected = _live.Contains(KeyOf(monitor));
                Assert.True(_manager.IsActive(monitor) == expected, $"{sequence}: {monitor.FriendlyName} on {monitor.DevicePath} should be {(expected ? "" : "not ")}blacked out");
                FakeBlackoutWindow? window = open.Find(candidate => KeyOf(candidate.Identity, candidate.DevicePath) == KeyOf(monitor));
                if (window is not null)
                    Assert.True(window.Bounds.Equals(monitor.Bounds), $"{sequence}: the blackout of {monitor.FriendlyName} does not cover its monitor (DISP-7): window {window.Bounds.Left}..{window.Bounds.Right}, monitor {monitor.Bounds.Left}..{monitor.Bounds.Right}");
            }
        }

        private void ModelToggle(MonitorInfo monitor)
        {
            string key = KeyOf(monitor);
            if (_live.Remove(key))
            {
                if (monitor.Identity is { } identity)
                    _desired.Remove(identity.Value);
                return;
            }

            _live.Add(key);
            if (monitor.Identity is { } desired)
                _desired.Add(desired.Value);
        }

        private static string KeyOf(MonitorInfo monitor) => KeyOf(monitor.Identity, monitor.DevicePath);

        private static string KeyOf(MonitorIdentity? identity, string devicePath) => identity?.Value ?? $"path {devicePath}";
    }
}
