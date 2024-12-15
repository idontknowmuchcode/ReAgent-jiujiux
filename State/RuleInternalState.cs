﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ReAgent.State;

public class RuleInternalState
{
    public bool CanPressKey { get; set; }
    public Keys? KeyToPress { get; set; }
    public List<Keys> KeysToHoldDown { get; set; } = [];
    public List<Keys> KeysToRelease { get; set; } = [];
    public List<(string GraphicFilePath, Vector2 Position, Vector2 Size, string TintColor, string Text, string TextColor, float FontSize, string BackgroundColor)> GraphicToDisplay { get; } = new();
    public List<(string Text, Vector2 Position, string Color)> TextToDisplay { get; } = new();
    public List<(string Text, Vector2 Position, string TextColor, string BackgroundColor, float FontSize)> AdvancedTextToDisplay { get; } = new();
    public List<(string Text, Vector2 Position, Vector2 Size, float Fraction, string Color, string BackgroundColor, string TextColor)> ProgressBarsToDisplay { get; } = new();
    public bool AccessForbidden { get; set; }
    public RuleGroup CurrentGroup { get; private set; }
    public Dictionary<int, (bool WasActive, DateTime DeactivationTime)> TinctureUsageTracker { get; } = [];

    public bool ChatTitlePanelVisible { get; set; }

    public bool LeftPanelVisible { get; set; }
    public bool RightPanelVisible { get; set; }
    public bool FullscreenPanelVisible { get; set; }
    public bool LargePanelVisible { get; set; }

    [JsonProperty]
    private Dictionary<RuleGroup, PerGroupInternalState> PerGroupStates { get; } = new();

    [JsonIgnore]
    public PerGroupInternalState CurrentGroupState =>
        PerGroupStates.TryGetValue(CurrentGroup, out var state)
            ? state
            : PerGroupStates[CurrentGroup] = new PerGroupInternalState();

    public IDisposable SetCurrentGroup(RuleGroup group)
    {
        return new RuleGroupRegistration(this, group);
    }

    private class RuleGroupRegistration : IDisposable
    {
        public RuleGroupRegistration(RuleInternalState state, RuleGroup group)
        {
            _state = state;
            _oldGroup = _state.CurrentGroup;
            _state.CurrentGroup = group;
        }

        public void Dispose()
        {
            _state.CurrentGroup = _oldGroup;
        }

        private readonly RuleInternalState _state;
        private readonly RuleGroup _oldGroup;
    }
}