# Swmarly Valheim Better Fists Weapons

This BepInEx mod gives every vanilla Unarmed/Fists weapon the normal vanilla knife special attack.

## What it changes

Fist weapons such as the Fenris claws no longer perform the unarmed kick when middle-clicking. Their secondary attack is replaced with the same attack definition used by Valheim's knives, including:

- the knife secondary animation and hit shape;
- the knife attack timing and stamina cost;
- the knife's special-attack damage multiplier;
- the normal weapon damage and Fists/Unarmed skill scaling of the equipped fist weapon.

The mod copies the live knife attack at runtime instead of hardcoding a multiplier. That keeps the behavior aligned with the installed Valheim build and with any knife-balance changes that happen before the item database is built.

## Multiplayer and dedicated servers

Install the mod on the dedicated server and on every client. The change is made to each peer's local item database; it does not add a new item, recipe, or network protocol. Having the same version everywhere keeps the attack definition and combat behavior consistent for all players.

The plugin also loads in the dedicated-server process. The server does not need to render the animation, but loading the same item-data patch there avoids having different weapon definitions between the server and clients.

## Compatibility

The patch runs after Valheim builds `ObjectDB`. It looks for a knife item with a real secondary attack, then applies a clone of that attack to every item whose skill type is `Skills.SkillType.Unarmed`. It safely skips non-item entries in `ObjectDB.m_items`.

If another mod deliberately changes knife or fist secondary attacks, the final definition present when `ObjectDB.Awake` completes is what this mod mirrors. Load order can therefore matter when another mod also edits these same attack fields.

## Building

The project targets .NET Framework 4.7.2 and expects these paths:

- Valheim: `C:\Program Files (x86)\Steam\steamapps\common\Valheim`
- BepInEx: the `BepInEx` directory inside the Valheim installation

Override them when necessary:

```powershell
dotnet build src -c Release `
  -p:ValheimManagedDir="D:\Games\Valheim\valheim_Data\Managed" `
  -p:BepInExCoreDir="D:\Games\Valheim\BepInEx\core"
python scripts/package.py
```

The GitHub Actions workflow downloads the current dedicated-server assemblies and builds the Thunderstore package automatically.

