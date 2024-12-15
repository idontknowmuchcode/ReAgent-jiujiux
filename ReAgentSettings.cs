using System.Collections.Generic;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Newtonsoft.Json;

namespace ReAgent;

public sealed class ReAgentSettings : ISettings
{
    public ReAgentSettings()
    {
        PluginSettings = new PluginSettings();
    }

    public readonly Dictionary<string, Profile> Profiles = new();
    public string CurrentProfile = string.Empty;

    public ToggleNode ShowDebugWindow { get; set; } = new(false);
    public ToggleNode InspectState { get; set; } = new(false);

    [JsonIgnore]
    [Menu(null, "To clipboard")]
    public ButtonNode DumpState { get; set; } = new();

    [IgnoreMenu]
    public RangeNode<int> GlobalKeyPressCooldown { get; set; } = new(200, 0, 1000);

    [IgnoreMenu]
    public RangeNode<int> MaximumMonsterRange { get; set; } = new(200, 0, 500);

    [IgnoreMenu]
    public RangeNode<int> HistorySecondsToKeep { get; set; } = new(60, 0, 600);

    public TextNode ImageDirectory { get; set; } = new("textures/ReAgent");
    private PluginSettings _pluginSettings;

    public PluginSettings PluginSettings
    {
        get => _pluginSettings;
        set
        {
            value.Parent = this;
            _pluginSettings = value;
        }
    }

    public ToggleNode Enable { get; set; } = new(true);
}

[Submenu(CollapsedByDefault = true)]
public class PluginSettings
{
    internal ReAgentSettings Parent;

    public RangeNode<int> GlobalKeyPressCooldown
    {
        get => Parent.GlobalKeyPressCooldown;
        set => Parent.GlobalKeyPressCooldown = value;
    }

    public RangeNode<int> MaximumMonsterRange
    {
        get => Parent.MaximumMonsterRange;
        set => Parent.MaximumMonsterRange = value;
    }

    public RangeNode<int> HistorySecondsToKeep
    {
        get => Parent.HistorySecondsToKeep;
        set => Parent.HistorySecondsToKeep = value;
    }

    [Submenu]
    public class MouseMovementSettings
    {
        [Menu("Base Movement Speed", "Controls mouse movement speed\nHigher = faster, more direct\nLower = slower, smoother")]
        public RangeNode<int> BaseSpeed { get; set; } = new(25, 15, 100);

        [Menu("Minimum Steps", "Controls movement smoothness\nMore steps = smoother but slower\nFewer steps = faster but rigid")]
        public RangeNode<int> MinSteps { get; set; } = new(8, 4, 15);

        [Menu("Base Delay (ms)", "Time between movement steps\nHigher = more deliberate\nLower = faster\nRecommended: 18-25ms")]
        public RangeNode<int> BaseDelay { get; set; } = new(22, 15, 50);

        [Menu("Randomization Factor", "Controls movement variation\n0.12-0.18: Recommended\n<0.1: Too mechanical\n>0.2: Too erratic")]
        public RangeNode<float> RandomizationFactor { get; set; } = new(0.15f, 0.08f, 0.4f);

        [Menu("Noise Scale", "Controls the amount of micro-adjustments\nHigher = more shaky\nLower = smoother")]
        public RangeNode<float> NoiseScale { get; set; } = new(2.0f, 0.5f, 5.0f);
    }

    public MouseMovementSettings MouseMovement { get; set; } = new();

    public ToggleNode KeepEnableTogglesOnASingleLine { get; set; } = new(true);
    public ToggleNode ColorEnableToggles { get; set; } = new(true);
    public ToggleNode EnableVerticalGroupTabs { get; set; } = new(true);
    public RangeNode<int> VerticalTabContainerWidth { get; set; } = new(150, 0, 1000);
}