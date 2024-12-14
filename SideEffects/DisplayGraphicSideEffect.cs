using System.Linq.Dynamic.Core.CustomTypeProviders;
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

    public DisplayGraphicSideEffect(string graphicFilePath, Vector2 position, Vector2 size, string colorTint)
    {
        GraphicFilePath = graphicFilePath;
        Position = position;
        Size = size;
        ColorTint = colorTint;
    }

    [Api]
    public static DisplayGraphicSideEffect Create(string graphicFilePath, float x, float y, float width, float height, string colorTint)
    {
        return new DisplayGraphicSideEffect(graphicFilePath, new Vector2(x, y), new Vector2(width, height), colorTint);
    }

    public SideEffectApplicationResult Apply(RuleState state)
    {
        state.InternalState.GraphicToDisplay.Add((GraphicFilePath, Position, Size, ColorTint));
        return SideEffectApplicationResult.AppliedUnique;
    }

    public override string ToString() => 
        $"Display \"{GraphicFilePath}\" at {Position} with {Size} and colortint {ColorTint}";
}