﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;

namespace ReAgent.State;

[Api]
public class EntityInfo
{
    protected readonly GameController Controller;
    protected readonly Entity Entity;
    private readonly Lazy<StatDictionary> _stats;
    private string _baseEntityPath;

    public EntityInfo(GameController controller, Entity entity)
    {
        Controller = controller;
        Entity = entity;
        _stats = new Lazy<StatDictionary>(() => new StatDictionary(Entity.Stats ?? new Dictionary<GameStat, int>()), LazyThreadSafetyMode.None);
    }

    [Api]
    public string Path => Entity.Path;

    [Api]
    public string Metadata => Entity.Metadata;

    [Api]
    public string BaseEntityPath => _baseEntityPath ??= Entity.GetComponent<Animated>()?.BaseAnimatedObjectEntity?.Path;

    [Api]
    public Vector3 Position => Entity.Pos;

    [Api]
    public Vector2 Position2D => Position switch { var p => new Vector2(p.X, p.Y) };

    [Api]
    public float DistanceToCursor => Controller.IngameState.ServerData.WorldMousePosition.WorldToGrid().Distance(Entity.GridPos);

    [Api]
    public float Scale => Entity?.GetComponent<Positioned>()?.Scale ?? 0;

    [Api]
    public StatDictionary Stats => _stats.Value;

    [Api]
    public bool IsAlive => Entity.IsAlive;

    [Api]
    public bool IsTargeted => Entity.TryGetComponent<Targetable>(out var targetable) && targetable.isTargeted;

    [Api]
    public bool IsTargetable => Entity.TryGetComponent<Targetable>(out var targetable) && targetable.isTargetable;

    [Api]
    public float Distance => Entity.DistancePlayer;

    [Api]
    public bool IsUsingAbility => Entity.TryGetComponent<Actor>(out var actor) && actor.Action == ActionFlags.UsingAbility;

    [Api]
    public string PlayerName => Entity.GetComponent<Player>()?.PlayerName ?? string.Empty;

    [Api]
    public Vector2 ScreenPos => Controller.IngameState.Camera.WorldToScreen(Entity.Pos);

    [Api]
    public Vector2 RandomizedScreenPos(int minOffset = -5, int maxOffset = 5)
    {
        var random = new Random();
        return ScreenPos + new Vector2(
            random.Next(minOffset, maxOffset),
            random.Next(minOffset, maxOffset)
        );
    }

}

[Api]
public class MonsterInfo : EntityInfo
{
    protected readonly GameController Controller;
    private bool? _isInvincible;

    public MonsterInfo(GameController controller, Entity entity) : base(controller, entity)
    {
        Controller = controller;
        Vitals = new VitalsInfo(entity.GetComponent<Life>());
        Actor = new ActorInfo(entity);
        Skills = new SkillDictionary(controller, entity);
    }

    [Api]
    public VitalsInfo Vitals { get; }

    [Api]
    public ActorInfo Actor { get; }

    [Api]
    public bool IsInvincible => _isInvincible ??= Stats[GameStat.CannotBeDamaged].Value switch { 0 => false, _ => true };

    [Api]
    public MonsterRarity Rarity => Entity.Rarity switch
    {
        ExileCore2.Shared.Enums.MonsterRarity.White => MonsterRarity.Normal,
        ExileCore2.Shared.Enums.MonsterRarity.Magic => MonsterRarity.Magic,
        ExileCore2.Shared.Enums.MonsterRarity.Rare => MonsterRarity.Rare,
        ExileCore2.Shared.Enums.MonsterRarity.Unique => MonsterRarity.Unique,
        _ => MonsterRarity.Normal
    };

    [Api]
    public BuffDictionary Buffs => new BuffDictionary(Entity.GetComponent<Buffs>()?.BuffsList ?? [], null);

    [Api]
    public SkillDictionary Skills { get; }

    [Api]
    public bool IsMoving => Entity.TryGetComponent<Actor>(out var actor) && actor.isMoving;
}

public class NearbyMonsterInfo
{
    private readonly SortedDictionary<int, List<MonsterInfo>> _monsters;

    public NearbyMonsterInfo(ReAgent plugin)
    {
        _monsters = new SortedDictionary<int, List<MonsterInfo>>();
        var friendlyMonsters = new List<MonsterInfo>();
        if (!plugin.GameController.Player.HasComponent<Render>())
        {
            return;
        }

        foreach (var entity in plugin.GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster])
        {
            if (!IsValidMonster(plugin, entity, true))
            {
                continue;
            }

            var distance = (int)Math.Ceiling(entity.DistancePlayer);
            var monsterInfo = new MonsterInfo(plugin.GameController, entity);
            if (entity.IsHostile)
            {
                if (_monsters.TryGetValue(distance, out var list))
                {
                    list.Add(monsterInfo);
                }
                else
                {
                    _monsters[distance] = [monsterInfo];
                }
            }
            else
            {
                friendlyMonsters.Add(monsterInfo);
            }
        }

        FriendlyMonsters = friendlyMonsters;
    }

    public static bool IsValidMonster(ReAgent plugin, Entity entity, bool checkIsAlive) =>
        entity.DistancePlayer <= plugin.Settings.MaximumMonsterRange &&
        entity.HasComponent<Monster>() &&
        entity.HasComponent<Positioned>() &&
        entity.HasComponent<Render>() &&
        entity.TryGetComponent<Buffs>(out var buffs) &&
        !buffs.HasBuff("hidden_monster") &&
        entity.HasComponent<Life>() &&
        (!checkIsAlive || entity.IsAlive) &&
        entity.HasComponent<ObjectMagicProperties>();

    public IReadOnlyCollection<MonsterInfo> FriendlyMonsters { get; set; }

    public int GetMonsterCount(int range, MonsterRarity rarity) => GetMonsters(range, rarity).Count();

    public IEnumerable<MonsterInfo> GetMonsters(int range, MonsterRarity rarity) =>
        _monsters.TakeWhile(x => x.Key <= range).SelectMany(x => x.Value).Where(x => (x.Rarity & rarity) != 0);

    [Api]
    public double DistanceBetween(MonsterInfo monster1, MonsterInfo monster2)
    {
        var dx = monster1.Position.X - monster2.Position.X;
        var dy = monster1.Position.Y - monster2.Position.Y;
        var dz = monster1.Position.Z - monster2.Position.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

}