using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Numerics;
using ReAgent.State;

namespace ReAgent.SideEffects;

[DynamicLinqType]
[Api]
public record DisplayTextSideEffect : ISideEffect
{
    private readonly string Text;
    private readonly Vector2 Position;
    private readonly string Color;

    public DisplayTextSideEffect(string text, Vector2 position, string color)
    {
        Text = text;
        Position = position;
        Color = color;
    }

    [Api]
    public static DisplayTextSideEffect Create(string text, float x, float y, string color)
    {
        return new DisplayTextSideEffect(text, new Vector2(x, y), color);
    }

    public SideEffectApplicationResult Apply(RuleState state)
    {
        state.InternalState.TextToDisplay.Add((Text, Position, Color));
        return SideEffectApplicationResult.AppliedUnique;
    }

    public override string ToString() => $"Display \"{Text}\" at {Position} with color {Color}";
}