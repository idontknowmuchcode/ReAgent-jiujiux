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

namespace ReAgent.State;

[Api]
public class RuleState
{
    private readonly Lazy<NearbyMonsterInfo> _nearbyMonsterInfo;
    private readonly Lazy<List<EntityInfo>> _miscellaneousObjects;
    private readonly RuleInternalState _internalState;
    private readonly Lazy<List<EntityInfo>> _ingameiconObjects;
    
    private readonly Lazy<List<EntityInfo>> _effects;
    private readonly Lazy<List<MonsterInfo>> _allMonsters;
    private readonly Lazy<List<MonsterInfo>> _allPlayers;
    private readonly Lazy<List<MonsterInfo>> _corpses;
    private readonly GameController _gameController;

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
            _miscellaneousObjects = new Lazy<List<EntityInfo>>(() => _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.MiscellaneousObjects].Select(x => new EntityInfo(_gameController, x)).ToList(), LazyThreadSafetyMode.None);
            _ingameiconObjects = new Lazy<List<EntityInfo>>(() => _gameController.EntityListWrapper.ValidEntitiesByType[EntityType.IngameIcon].Select(x => new EntityInfo(_gameController, x)).ToList(), LazyThreadSafetyMode.None);
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
    public void MoveCursorToMonster(MonsterInfo monster, int delayMs = 0)
    {
        if (delayMs > 0)
        {
            System.Threading.Tasks.Task.Delay(delayMs).ContinueWith(_ => 
                Input.SetCursorPos(monster.RandomizedScreenPos()));
        }
        else
        {
            Input.SetCursorPos(monster.RandomizedScreenPos());
        }
    }

    [Api]
    public void MoveCursorToMonsterAndReturnToCenter(MonsterInfo monster, int delayMs = 1000)
    {
        MoveCursorToMonster(monster);
        var screenCenter = new Vector2(
            _gameController.Window.GetWindowRectangleTimeCache.Size.X / 2f,
            _gameController.Window.GetWindowRectangleTimeCache.Size.Y / 2f);
        var randomOffset = new Vector2(
            Random.Shared.Next(-5, 5),
            Random.Shared.Next(-5, 5));
        System.Threading.Tasks.Task.Delay(delayMs).ContinueWith(_ => 
            Input.SetCursorPos(screenCenter + randomOffset));
    }

    [Api]
    public void MoveCursorToMonsterAndReturnToPrevious(MonsterInfo monster, int delayMs = 1000)
    {
        var originalPos = GetCursorPosition();
        MoveCursorToMonster(monster);
        var randomOffset = new Vector2(
            Random.Shared.Next(-5, 5),
            Random.Shared.Next(-5, 5));
        System.Threading.Tasks.Task.Delay(delayMs).ContinueWith(_ => 
            Input.SetCursorPos(originalPos + randomOffset));
    }
}
