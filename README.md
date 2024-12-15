# ReAgent

## Note

This fork is maintained for personal use only. While you're welcome to use it, I won't be accepting feature requests.

If you like it, you can donate to the author of ReAgent via:

BTC: bc1qke67907s6d5k3cm7lx7m020chyjp9e8ysfwtuz

ETH: 0x3A37B3f57453555C2ceabb1a2A4f55E0eB969105

## Changed & Additional Features

### Advanced Text Display

The `AdvancedTextToDisplay` feature allows you to display text on screen with more customization options than the basic text display:

```csharp
DisplayTextAdvancedSideEffect.Create(
    text: "Your Text Here",
    x: 100,                    // X position on screen
    y: 100,                    // Y position on screen
    textColor: "Red",          // Color of the text
    backgroundColor: "Black",  // Color of the background
    fontSize: 20              // Size of the font
)
```

### Graphic Display

The `GraphicToDisplay` feature allows you to display images on screen with optional text:

```csharp
DisplayGraphicSideEffect.Create(
    graphicFilePath: "path/to/image.png",  // Path to image file
    x: 100,                        // X position on screen
    y: 100,                        // Y position on screen
    width: 64,                     // Width of image
    height: 64,                    // Height of image
    colorTint: "White",           // Color tint for the image
    text: "Optional Text",        // Text to display below image (optional)
    textColor: "Red",            // Color of the text (optional)
    fontSize: 25,                // Size of the text (optional)
    backgroundColor: "White"     // Background color of the text (optional)
)
```

### Example of using GraphicToDisplay and SingleSideEffect (Place your image in the ReAgent/Images folder)

```csharp
var groundEffects = State.Effects
    ?.Where(e => e != null &&
           !string.IsNullOrEmpty(e.Metadata) &&
           !string.IsNullOrEmpty(e.BaseEntityPath) &&
           !e.BaseEntityPath.ToLower().Contains("rig.ao") &&
           (e.Metadata.ToLower().Contains("ground_effects") ||
            e.Metadata.ToLower().Contains("monsters_effects") ||
            e.BaseEntityPath.ToLower().Contains("cursespell") ||
            e.BaseEntityPath.ToLower().Contains("on_death") ||
            e.BaseEntityPath.ToLower().Contains("monsters_effects")))
    ?.ToList();

if (groundEffects != null && groundEffects.Any())
{
    foreach (var effect in groundEffects)
    {
        if (effect?.BaseEntityPath == null || effect.ScreenPos == null)
            continue;

        try {
            // Extract the effect name from the BaseEntityPath
            var effectName = effect.BaseEntityPath
                .Split('/')
                .LastOrDefault()
                ?.Replace(".ao", "");

            if (string.IsNullOrEmpty(effectName))
                continue;

            // Create the display effect
            return ReAgent.SideEffects.DisplayGraphicSideEffect.Create(
                "danger.png",
                effect.ScreenPos.X,
                effect.ScreenPos.Y - 20,
                64,
                64,
                "Green",
                effectName,
                "Black",
                30,
                "White");
        }
        catch {
            continue; // Skip this effect if any error occurs
        }
    }
}
return null;
```

### Example of drawing monster effects with png and text

![Graphic Display](/screenshots/GraphicToDisplay.png)

### Cursor Movement Features

Three methods are available for automated cursor movement with humanization:

1. `MoveCursorToMonsterAndReturnToCenter`: Moves the cursor to a monster's position and then returns it to the center of the screen after a delay.

```csharp
 MoveCursorToMonsterAndReturnToCenter(monster, delayMs: 1000)
```

### Example of using MoveCursorToMonsterAndReturnToCenter to cast Pain Offering on skeleton minion

```csharp
if (!State.SinceLastActivation(1) ||
    (State.MonsterCount(100, ReAgent.State.MonsterRarity.Normal) < 10 &&
     State.MonsterCount(100, ReAgent.State.MonsterRarity.AtLeastMagic) < 1))
{
    return false;
}

var nonRagingSpiritMinions = State.FriendlyMonsters
    .Where(monster =>
        (!monster.Metadata.Contains("RagingSpiritPlayerSummoned") &&
         !monster.Metadata.Contains("BoneConstruct")) &&
        monster.IsUsingAbility)
    .ToList();

// Early exit if no minions or if most already have the buff
if (nonRagingSpiritMinions.Count == 0 ||
    nonRagingSpiritMinions.Count(m => !m.Buffs.Has("pain_offering_buff")) < nonRagingSpiritMinions.Count * 0.5)
{
    return false;
}

const float DISTANCE_WEIGHT = 0.7f; // Lower weight means skeletondistance from playeris more important
const float NEARBY_COUNT_WEIGHT = 0.3f; // Higher weight means other nearby skeletons count is more important
const int NEARBY_RADIUS = 50; // Radius to check for nearby skeletons

// Get the skeleton with the best combined score of proximity to player and nearby skeleton count
var targetSkeleton = State.FriendlyMonsters
    .Where(monster =>
        monster.Metadata.Contains("Skeletons") &&
        monster.IsUsingAbility)
    .Select(skeleton => new
    {
        Skeleton = skeleton,
        DistanceScore = 1.0f / (1.0f + skeleton.Distance),
        NearbyScore = State.FriendlyMonsters.Count(other =>
            other.Metadata.Contains("Skeletons") &&
            other.IsUsingAbility &&
            other.DistanceTo(skeleton) <= NEARBY_RADIUS) / 10.0f
    })
    .OrderByDescending(data =>
        (data.DistanceScore * DISTANCE_WEIGHT) +
        (data.NearbyScore * NEARBY_COUNT_WEIGHT))
    .Select(data => data.Skeleton)
    .FirstOrDefault();

if (targetSkeleton != null)
{
    State.MoveCursorToMonsterAndReturnToCenter(targetSkeleton, 200);
    return true;
}

return false;
```

2. `MoveCursorToMonsterAndReturnToPrevious`: Moves the cursor to a monster's position and then returns it to the previous position after a delay.

```csharp
 MoveCursorToMonsterAndReturnToPrevious(monster, delayMs: 1000)
```

3. `MoveCursorToMonster`: Moves the cursor to a monster's position.

```csharp
 MoveCursorToMonster(monster)
```

### Example of using MoveCursorToMonster

```csharp
if (!State.SinceLastActivation(1))
{
    return false;
}

// Check for targetable corpses near cursor and player
bool hasNearbyCorpse = State.Corpses
    .Any(corpse =>
        corpse.DistanceToCursor < 50 &&
        corpse.Distance < 50 &&
        corpse.IsTargetable);

// Check for monsters near player
int nearbyMonsterCount = State.MonsterCount(50);

// If we don't have both corpses and monsters, exit early
if (!hasNearbyCorpse || nearbyMonsterCount == 0)
{
    return false;
}

// Find the closest targetable corpse to cursor
var targetCorpse = State.Corpses
    .Where(corpse =>
        corpse.Distance < 50 &&
        corpse.IsTargetable)
    .OrderBy(corpse => corpse.DistanceToCursor)
    .FirstOrDefault();

// If we found a valid corpse that's also near the cursor
if (targetCorpse != null &&
    targetCorpse.DistanceToCursor < 20 &&
    targetCorpse.IsTargetable)
{
    State.MoveCursorToMonster(targetCorpse);
    return true;
}

return false;
```

![MouseMenu](/screenshots/MouseMenu.png)

### Monster Distance Calculation

The `DistanceTo` method allows you to calculate the distance between any two monsters (including the player):

```csharp
// Calculate distance between two monsters
double distance = monster1.DistanceTo(monster2);
```

This is useful for positioning checks, range-based skill usage, and minion management.
