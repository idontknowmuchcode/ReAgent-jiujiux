using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Numerics;
using ReAgent.State;
using System.Drawing;

namespace ReAgent.SideEffects;

[DynamicLinqType]
[Api]
public record DisplayTextAdvancedSideEffect : ISideEffect
{
    private readonly string Text;
    private readonly Vector2 Position;
    private readonly string TextColor;
    private readonly string BackgroundColor;
    private readonly float FontSize;

    public DisplayTextAdvancedSideEffect(string text, Vector2 position, string textColor, string backgroundColor, float fontSize)
    {
        Text = text;
        Position = position;
        TextColor = textColor;
        BackgroundColor = backgroundColor;
        FontSize = fontSize;
    }

    [Api]
    public static DisplayTextAdvancedSideEffect Create(string text, float x, float y, string textColor, string backgroundColor, float fontSize)
    {
        return new DisplayTextAdvancedSideEffect(text, new Vector2(x, y), textColor, backgroundColor, fontSize);
    }

    public SideEffectApplicationResult Apply(RuleState state)
    {
        state.InternalState.AdvancedTextToDisplay.Add((Text, Position, TextColor, BackgroundColor, FontSize));
        return SideEffectApplicationResult.AppliedUnique;
    }

    public override string ToString() => 
        $"Display \"{Text}\" at {Position} with color {TextColor}, background {BackgroundColor}, size {FontSize}";
}
