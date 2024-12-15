using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.Shared.Enums;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ReAgent.State;

[Api]
public class RuleState
{
    private readonly Lazy<NearbyMonsterInfo> _nearbyMonsterInfo;
    private readonly Lazy<List<EntityInfo>> _miscellaneousObjects;
    private readonly Lazy<List<EntityInfo>> _noneEntities;
    private readonly RuleInternalState _internalState;
    private readonly Lazy<List<EntityInfo>> _ingameiconObjects;
    
    private readonly Lazy<List<EntityInfo>> _effects;
    private readonly Lazy<List<MonsterInfo>> _allMonsters;
    private readonly Lazy<List<MonsterInfo>> _allPlayers;
    private readonly Lazy<List<MonsterInfo>> _corpses;
    private readonly GameController _gameController;
    private readonly ReAgent _plugin;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private Vector2 GetCursorPosition()
    {
        GetCursorPos(out POINT point);
        return new Vector2(point.X, point.Y);
    }

    public RuleInternalState InternalState
    {
        get
        {
            if (_internalState.AccessForbidden)
            {
                throw new Exception("Access denied");
            }

            return _internalState;
        }
    }

    public RuleState(ReAgent plugin, RuleInternalState internalState)
    {
        _internalState = internalState;
        _gameController = plugin.GameController;
        _plugin = plugin;
        if (_gameController != null)
        {
            IsInHideout = plugin.GameController.Area.CurrentArea.IsHideout;
            IsInTown = plugin.GameController.Area.CurrentArea.IsTown;
            IsInPeacefulArea = plugin.GameController.Area.CurrentArea.IsPeaceful;
            AreaName = plugin.GameController.Area.CurrentArea.Name;

            var player = _gameController.Player;
            if (player.TryGetComponent<Buffs>(out var playerBuffs))
            {
                Ailments = plugin.CustomAilments
                    .Where(x => x.Value.Any(playerBuffs.HasBuff))
                    .Select(x => x.Key)
                    .ToHashSet();
            }


            if (player.TryGetComponent<Life>(out var lifeComponent))
            {
                Vitals = new VitalsInfo(lifeComponent);
            }

            if (player.TryGetComponent<Actor>(out var actorComponent))
            {
                Animation = actorComponent.Animation;
                IsMoving = actorComponent.isMoving;
                Skills = new SkillDictionary(_gameController, player);
                AnimationId = actorComponent.AnimationController?.CurrentAnimationId ?? 0;
                AnimationStage = actorComponent.AnimationController?.CurrentAnimationStage ?? 0;
            }

            Buffs = new BuffDictionary(playerBuffs?.BuffsList ?? [], Skills);

            Flasks = new FlasksInfo(_gameController, InternalState);
            Player = new MonsterInfo(_gameController, player);
            _nearbyMonsterInfo = new Lazy<NearbyMonsterInfo>(() => new NearbyMonsterInfo(plugin), LazyThreadSafetyMode.None);
            _miscellaneousObjects = new Lazy<List<EntityInfo>>(() => 
                _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.MiscellaneousObjects]
                    .Select(x => new EntityInfo(_gameController, x)).ToList(), 
                LazyThreadSafetyMode.None);
            _noneEntities = new Lazy<List<EntityInfo>>(() => 
                _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.None]
                    .Select(x => new EntityInfo(_gameController, x)).ToList(), 
                LazyThreadSafetyMode.None);
            _ingameiconObjects = new Lazy<List<EntityInfo>>(() => 
                _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.IngameIcon]
                    .Select(x => new EntityInfo(_gameController, x)).ToList(), 
                LazyThreadSafetyMode.None);
            _allMonsters = new Lazy<List<MonsterInfo>>(() => _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
                .Where(e => NearbyMonsterInfo.IsValidMonster(plugin, e, false))
                .Select(x => new MonsterInfo(_gameController, x)).ToList(), LazyThreadSafetyMode.None);
            _corpses = new Lazy<List<MonsterInfo>>(() => _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
                .Where(e => NearbyMonsterInfo.IsValidMonster(plugin, e, false))
                .Where(x=>x.IsDead)
                    .Select(x => new MonsterInfo(_gameController, x)).ToList(), LazyThreadSafetyMode.None);
            _effects = new Lazy<List<EntityInfo>>(() => _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.Effect].Select(x => new EntityInfo(_gameController, x)).ToList(), LazyThreadSafetyMode.None);
            _allPlayers = new Lazy<List<MonsterInfo>>(() => _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.Player]
                    .Where(e => NearbyMonsterInfo.IsValidMonster(plugin, e, false))
                    .Select(x => new MonsterInfo(_gameController, x)).ToList(), LazyThreadSafetyMode.None);
        }
    }


    [Api]
    public bool IsMoving { get; }

    [Api]
    public BuffDictionary Buffs { get; }

    [Api]
    public AnimationE Animation { get; }

    [Api]
    public int AnimationId { get; }

    [Api]
    public int AnimationStage { get; }

    [Api]
    public IReadOnlyCollection<string> Ailments { get; } = new List<string>();

    [Api]
    public SkillDictionary Skills { get; } = new SkillDictionary(null, null);

    [Api]
    public VitalsInfo Vitals { get; }

    [Api]
    public FlasksInfo Flasks { get; }

    [Api]
    public MonsterInfo Player { get; }

    [Api]
    public bool IsInHideout { get; }

    [Api]
    public bool IsInTown { get; }

    [Api]
    public bool IsInPeacefulArea { get; }

    [Api]
    public string AreaName { get; }

    [Api]
    public int MonsterCount(int range, MonsterRarity rarity) => _nearbyMonsterInfo.Value.GetMonsterCount(range, rarity);

    [Api]
    public int MonsterCount(int range) => MonsterCount(range, MonsterRarity.Any);

    [Api]
    public int MonsterCount() => MonsterCount(int.MaxValue);

    [Api]
    public IEnumerable<MonsterInfo> Monsters(int range, MonsterRarity rarity) => _nearbyMonsterInfo.Value.GetMonsters(range, rarity);

    [Api]
    public IEnumerable<MonsterInfo> FriendlyMonsters => _nearbyMonsterInfo.Value.FriendlyMonsters;

    [Api]
    public IEnumerable<MonsterInfo> Monsters(int range) => Monsters(range, MonsterRarity.Any);

    [Api]
    public IEnumerable<MonsterInfo> Monsters() => Monsters(int.MaxValue);

    [Api]
    public IEnumerable<EntityInfo> MiscellaneousObjects => _miscellaneousObjects.Value;

    [Api]
    public IEnumerable<EntityInfo> NoneEntities => _noneEntities.Value;

    [Api]
    public IEnumerable<EntityInfo> IngameIcons => _ingameiconObjects.Value;

    [Api]
    public IEnumerable<MonsterInfo> AllMonsters => _allMonsters.Value;

    [Api]
    public IEnumerable<MonsterInfo> Corpses => _corpses.Value;

    [Api]
    public IEnumerable<MonsterInfo> AllPlayers => _allPlayers.Value;

    [Api]
    public MonsterInfo PlayerByName(string name) => _allPlayers.Value.FirstOrDefault(p=>p.PlayerName.Equals(name));

    [Api]
    public IEnumerable<EntityInfo> Effects => _effects.Value;

    [Api]
    public bool IsKeyPressed(Keys key) => Input.IsKeyDown(key);

    [Api]
    public bool SinceLastActivation(double minTime) =>
        (_internalState.CurrentGroupState.ConditionActivations.GetValueOrDefault(_internalState.CurrentGroupState.CurrentRule)?.Elapsed.TotalSeconds ??
         double.PositiveInfinity) > minTime;

    [Api]
    public bool IsFlagSet(string name) => _internalState.CurrentGroupState.Flags.GetValueOrDefault(name);

    [Api]
    public float GetNumberValue(string name) => _internalState.CurrentGroupState.Numbers.GetValueOrDefault(name);

    [Api]
    public float GetTimerValue(string name) => (float?)_internalState.CurrentGroupState.Timers.GetValueOrDefault(name)?.Elapsed.TotalSeconds ?? 0f;

    [Api]
    public bool IsTimerRunning(string name) => _internalState.CurrentGroupState.Timers.GetValueOrDefault(name)?.IsRunning ?? false;

    [Api]
    public bool IsChatOpen => _internalState.ChatTitlePanelVisible;

    [Api]
    public bool IsLeftPanelOpen => _internalState.LeftPanelVisible;

    [Api]
    public bool IsRightPanelOpen => _internalState.RightPanelVisible;

    [Api]
    public bool IsAnyFullscreenPanelOpen => _internalState.FullscreenPanelVisible;
    
    [Api]
    public bool IsAnyLargePanelOpen => _internalState.LargePanelVisible; 

    [Api]
    public List<(string Text, Vector2 Position, string TextColor, string BackgroundColor, float FontSize)> AdvancedTextToDisplay { get; } = new();

    private class MouseMovement
    {
        public static Vector2 GetNextPoint(Vector2 start, Vector2 end, float progress)
        {
            // Using Bezier curve with some Gaussian noise for natural movement
            var control1 = start + (end - start) * 0.3f * (1 + (float)Random.Shared.NextDouble());
            var control2 = start + (end - start) * 0.7f * (1 + (float)Random.Shared.NextDouble());
            
            var t = progress;
            var bezierPoint = new Vector2(
                (float)(Math.Pow(1 - t, 3) * start.X + 
                       3 * Math.Pow(1 - t, 2) * t * control1.X + 
                       3 * (1 - t) * Math.Pow(t, 2) * control2.X + 
                       Math.Pow(t, 3) * end.X),
                (float)(Math.Pow(1 - t, 3) * start.Y + 
                       3 * Math.Pow(1 - t, 2) * t * control1.Y + 
                       3 * (1 - t) * Math.Pow(t, 2) * control2.Y + 
                       Math.Pow(t, 3) * end.Y)
            );
            
            // Add small Gaussian noise
            var u1 = 1.0 - Random.Shared.NextDouble();
            var u2 = 1.0 - Random.Shared.NextDouble();
            var noise = new Vector2(
                (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2)),
                (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2))
            ) * 2f;
            
            return bezierPoint + noise;
        }
    }

    private bool IsWithinScreenBounds(Vector2 position)
    {
        if (_gameController?.Window?.GetWindowRectangleTimeCache == null) return false;
        
        var screenSize = _gameController.Window.GetWindowRectangleTimeCache.Size;
        return position.X >= 0 && position.X < screenSize.X && 
               position.Y >= 0 && position.Y < screenSize.Y;
    }

    private async Task MoveCursorWithHumanization(Vector2 targetPos, int delayMs = 1000)
    {
        // Validate target position is within screen bounds
        if (!IsWithinScreenBounds(targetPos))
        {
            DebugWindow.LogError($"Target position {targetPos} is outside screen bounds");
            return;
        }

        var startPos = GetCursorPosition();
        var distance = Vector2.Distance(startPos, targetPos);
        
        // Use settings for step calculation
        var settings = _plugin.Settings.PluginSettings.MouseMovement;
        var baseSteps = Math.Max(settings.MinSteps.Value, (int)(distance / settings.BaseSpeed.Value));
        var randomFactor = settings.RandomizationFactor.Value;
        var steps = baseSteps + Random.Shared.Next(-(int)(baseSteps * randomFactor), (int)(baseSteps * randomFactor));

        // Generate deviation for the control points
        var midPoint = (startPos + targetPos) / 2f;
        var perpendicular = new Vector2(-(targetPos.Y - startPos.Y), targetPos.X - startPos.X);
        perpendicular = Vector2.Normalize(perpendicular) * (distance * 0.25f * (float)(Random.Shared.NextDouble() * 0.8 + 0.2));
        
        var control1 = midPoint + perpendicular * (float)(Random.Shared.NextDouble() * 0.5 + 0.5);
        var control2 = midPoint - perpendicular * (float)(Random.Shared.NextDouble() * 0.5 + 0.5);

        for (int i = 0; i < steps; i++)
        {
            var t = (i + 1f) / steps;
            
            // Cubic Bezier curve calculation
            var oneMinusT = 1 - t;
            var oneMinusTSquared = oneMinusT * oneMinusT;
            var tSquared = t * t;
            
            var nextPos = new Vector2(
                oneMinusT * oneMinusTSquared * startPos.X +
                3 * oneMinusTSquared * t * control1.X +
                3 * oneMinusT * tSquared * control2.X +
                tSquared * t * targetPos.X,
                
                oneMinusT * oneMinusTSquared * startPos.Y +
                3 * oneMinusTSquared * t * control1.Y +
                3 * oneMinusT * tSquared * control2.Y +
                tSquared * t * targetPos.Y
            );

            // Add Gaussian noise
            var u1 = 1.0 - Random.Shared.NextDouble();
            var u2 = 1.0 - Random.Shared.NextDouble();
            var standardDeviation = settings.NoiseScale.Value * Math.Min(distance * 0.01f, 2.0f); // Use noise scale setting
            var noise = new Vector2(
                (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2) * standardDeviation),
                (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2) * standardDeviation)
            );

            nextPos += noise;

            // Ensure position is within bounds
            if (IsWithinScreenBounds(nextPos))
            {
                Input.SetCursorPos(nextPos);
                
                // Variable delay with Gaussian distribution
                var baseDelay = settings.BaseDelay.Value;
                var delayVariation = Math.Max(2.0, Math.Sqrt(-2.0 * Math.Log(Random.Shared.NextDouble())) * 
                                   Math.Cos(2.0 * Math.PI * Random.Shared.NextDouble()) * 3.0);
                var stepDelay = (int)(baseDelay + delayVariation);
                await Task.Delay(Math.Max(1, stepDelay));
            }
        }

        // Ensure we reach the exact target
        Input.SetCursorPos(targetPos);
        await Task.Delay(delayMs);
    }

    [Api]
    public async Task MoveCursorToMonsterAndReturnToCenter(MonsterInfo monster, int delayMs = 1000)
    {
        await MoveCursorWithHumanization(monster.RandomizedScreenPos());
        var screenCenter = new Vector2(
            _gameController.Window.GetWindowRectangleTimeCache.Size.X / 2f,
            _gameController.Window.GetWindowRectangleTimeCache.Size.Y / 2f
        );
        await MoveCursorWithHumanization(screenCenter, delayMs);
    }

    [Api]
    public async Task MoveCursorToMonsterAndReturnToPrevious(MonsterInfo monster, int delayMs = 1000)
    {
        var originalPos = GetCursorPosition();
        await MoveCursorWithHumanization(monster.RandomizedScreenPos());
        await MoveCursorWithHumanization(originalPos, delayMs);
    }
}
