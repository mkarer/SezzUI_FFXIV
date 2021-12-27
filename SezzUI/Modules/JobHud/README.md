# JobHud Configuration

Actions will automatically be adjusted to the level-appropiate one by [GetAdjustedActionId](https://goatcorp.github.io/Dalamud/api/FFXIVClientStructs.FFXIV.Client.Game.ActionManager.html#FFXIVClientStructs_FFXIV_Client_Game_ActionManager_GetAdjustedActionId_System_UInt32_), so its best to use the initial action that isn't affected by any traits.
There are plugins (XIVCombo and similar ones) that hook into GetAdjustedActionId and mess with the result, those actions can be manually added to the SpellHelper class.

All the datamined IDs for actions and spells can be found here:
- <https://zeffuro.github.io/SimpleTriggernometryTriggerCreator/>
- <https://github.com/xivapi/ffxiv-datamining/blob/master/csv/Action.csv>
- <https://github.com/xivapi/ffxiv-datamining/blob/master/csv/Status.csv>

## AuraAlert Examples

## BarIcon Examples

### Ley Lines

This is a special case, it should show the cooldown of Lay Lines, the duration of Lay Lines (the buff we get that indicates that "Between the Lines" is available despite its cooldown) and it should glow while we're not standing in the Circle of Power:

```cs
bar.Add(new Icon(bar) { TextureActionId = 3573, CooldownActionId = 3573, StatusId = 737, MaxStatusDuration = 30, StatusTarget = Enums.Unit.Player, GlowBorderStatusId = 738, GlowBorderInvertCheck = true, GlowBorderStatusIdForced = 737, GlowBorderUsable = true, Level = 52 });
```

Full explanation:

| Texture Property | Description |
| --- | --- |
| `TextureActionId = 3573` | Show the icon of Ley Lines action (affected by GetAdjustedActionId) |

| Cooldown Property | Description |
| --- | --- |
| `CooldownActionId = 3573` | Show cooldown of Ley Lines action (affected by GetAdjustedActionId) |

| Status Property | Description |
| --- | --- |
| `StatusId = 737` | Check for Ley Lines status (NOT affected by GetAdjustedActionId, if this would be needed we could use StatusActionId instead) |
| `MaxStatusDuration = 30` | A maximum duration has to be specified because it is not available in the [Status](https://goatcorp.github.io/Dalamud/api/FFXIVClientStructs.FFXIV.Client.Game.Status.html) object |
| `StatusTarget = Enums.Unit.Player` | Check for Ley Lines on the player | 

| Border Glow Property | Description |
| --- | --- |
| `GlowBorderInvertCheck = true` | Invert Status + Usable condition |
| `GlowBorderStatusId = 738` | Check for NOT HAVING Circle of Power status (GlowBorderInvertCheck!) |
| `GlowBorderStatusIdForced = 737` | Check for Ley Lines status (not affected by GlowBorderInvertCheck) |
| `GlowBorderUsable = true` | Check if Ley Lines action is NOT USABLE (GlowBorderInvertCheck!) |

| Level Property | Description |
| --- | --- |
| `Level = 52` | Ley Lines is only available at level 52, don't display the icon when not at least at that level. |
