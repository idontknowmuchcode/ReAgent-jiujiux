﻿using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Numerics;
using ReAgent.State;

namespace ReAgent.SideEffects;

[DynamicLinqType]
[Api]
public record DisplayGraphicSideEffect : ISideEffect
{
    private readonly string GraphicFilePath;
    private readonly Vector2 Position;
    private readonly Vector2 Size;
    private readonly string ColorTint;
    private readonly string Text;
    private readonly string TextColor;
    private readonly float FontSize;
    private readonly string BackgroundColor;


    public DisplayGraphicSideEffect(string graphicFilePath, Vector2 position, Vector2 size, string colorTint, string text, string textColor, float fontSize, string backgroundColor)
    {
        GraphicFilePath = graphicFilePath;
        Position = position;
        Size = size;
        ColorTint = colorTint;
        Text = text;
        TextColor = textColor;
        FontSize = fontSize;
        BackgroundColor = backgroundColor;
    }

    [Api]
    public static DisplayGraphicSideEffect Create(string graphicFilePath, float x, float y, float width, float height, string colorTint, string text = "", string textColor = "Red", float fontSize = 25f, string backgroundColor = "White")
    {
        return new DisplayGraphicSideEffect(graphicFilePath, new Vector2(x, y), new Vector2(width, height), colorTint, text, textColor, fontSize, backgroundColor);
    }

    public SideEffectApplicationResult Apply(RuleState state)
    {
        state.InternalState.GraphicToDisplay.Add((GraphicFilePath, Position, Size, ColorTint, Text, TextColor, FontSize, BackgroundColor));
        return SideEffectApplicationResult.AppliedUnique;
    }

    public override string ToString() => 
        $"Display \"{GraphicFilePath}\" at {Position} with {Size} and colortint {ColorTint}" + 
        (string.IsNullOrEmpty(Text) ? "" : $" and text \"{Text}\"");
}