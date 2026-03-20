# Bannerlord Console Commands Reference

Complete reference of all console commands available in Mount & Blade II: Bannerlord, discovered via `[CommandLineFunctionality.CommandLineArgumentFunction]` attributes across game assemblies.

**Total: 133 commands across 17 groups** (+ 7 DedicatedServer-only ConsoleCommandMethod commands)

All `campaign.*` commands require cheats to be enabled. Pass `help` as argument to see format string.

---

## `campaign` group (88 commands)

### Gold / Economy
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.add_denars` | `[PositiveInteger]` | Adds gold denars to the player |
| `campaign.add_gold_to_hero` | `[HeroName] \| [PositiveInteger]` | Adds gold to a specific hero |
| `campaign.add_gold_to_all_heroes` | `[PositiveInteger]` | Adds gold to all heroes in the game |

### Troops / Party
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.add_troops` | `[TroopName] \| [Number]` | Adds troops to the player party |
| `campaign.give_troops` | `[TroopType] \| [Number]` | Gives troops to the main party (alternate) |
| `campaign.add_prisoners` | `[TroopName] \| [Number]` | Adds prisoners to the player party |
| `campaign.add_troops_xp` | `[Amount]` | Adds XP to all troops in the main party |
| `campaign.add_companions` | `[Number]` | Adds specified number of random companion heroes to player party |
| `campaign.add_random_hero_to_party` | _(no args)_ | Adds a random hero to the main party |
| `campaign.wound_all_troops` | _(no args)_ | Wounds all troops in the main party |
| `campaign.destroy_party` | `[PartyName]` | Destroys a mobile party |

### Food / Morale
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.add_food` | `[Amount]` | Adds food to the main party |
| `campaign.set_party_food` | `[Amount]` | Sets party food to a specific amount |
| `campaign.add_morale` | `[Amount]` | Adds morale to the main party |
| `campaign.boost_cohesion` | `[Amount]` | Boosts army cohesion |
| `campaign.add_courage` | `[TroopName] \| [Amount]` | Adds courage/morale to a specific troop |

### Hero / Character
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.heal_main_hero` | _(no args)_ | Fully heals the main hero |
| `campaign.set_main_hero_age` | `[Age]` | Sets the main hero's age |
| `campaign.set_hero_age` | `[HeroName] \| [Age]` | Sets a hero's age |
| `campaign.set_hero_culture` | `[HeroName] \| [CultureName]` | Sets a hero's culture |
| `campaign.change_hero_culture` | `[HeroName] \| [CultureName]` | Changes a hero's culture |
| `campaign.make_hero_fugitive` | `[HeroName]` | Makes a hero a fugitive |
| `campaign.take_hero_prisoner` | `[HeroName]` | Takes a hero prisoner |
| `campaign.kill_hero` | `[HeroName]` | Kills a hero _(SandBox.View.dll)_ |
| `campaign.adopt_hero` | `[HeroName]` | Makes a hero join the player clan |
| `campaign.make_main_hero_ill` | _(no args)_ | Makes the main hero ill |
| `campaign.list_heroes` | `[HeroName]` | Lists heroes matching a name |

### Skills / Attributes / Traits
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.add_skill_xp_to_hero` | `[HeroName] \| [SkillName] \| [Amount]` | Adds skill XP to a hero for a specific skill |
| `campaign.add_focus_points_to_skill` | `[SkillName] \| [Amount]` | Adds focus points to a specific skill |
| `campaign.set_all_skills` | `[Level]` | Sets all skills to specified level for main hero |
| `campaign.set_skill_level_of_hero` | `[HeroName] \| [SkillName] \| [Level]` | Sets a specific skill level for a hero |
| `campaign.set_hero_trait` | `[HeroName] \| [TraitName] \| [Level]` | Sets a hero's personality trait level |
| `campaign.print_player_traits` | _(no args)_ | Prints the player's trait levels and XP |

### Items / Inventory / Crafting
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.add_item` | `[ItemName] \| [Amount]` | Adds items to player inventory |
| `campaign.add_horse` | `[HorseName] \| [Amount]` | Adds horses to player party |
| `campaign.add_crafting_materials` | `[Amount]` | Adds crafting materials of all types |
| `campaign.give_all_crafting_recipes_to_main_hero` | _(no args)_ | Unlocks all crafting recipes for the player |
| `campaign.set_crafting_stamina` | `[Amount]` | Sets crafting stamina |

### Settlements
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.give_settlement_to_player` | `[SettlementName]` | Gives a settlement to the player clan |
| `campaign.give_settlement_to_kingdom` | `[SettlementName] \| [KingdomName]` | Gives a settlement to a kingdom |
| `campaign.give_workshop_to_player` | `[SettlementName]` | Gives a workshop to the player |
| `campaign.add_prosperity_to_settlement` | `[SettlementName] \| [Amount]` | Adds prosperity to a settlement |
| `campaign.add_loyalty_to_settlement` | `[SettlementName] \| [Amount]` | Adds loyalty to a settlement |
| `campaign.add_security_to_settlement` | `[SettlementName] \| [Amount]` | Adds security to a settlement |
| `campaign.add_militia_to_settlement` | `[SettlementName] \| [Amount]` | Adds militia to a settlement |
| `campaign.add_garrison_troops_to_settlement` | `[SettlementName] \| [Amount]` | Adds garrison troops to a settlement |
| `campaign.add_population_to_settlement` | `[SettlementName] \| [Amount]` | Adds population to a settlement |
| `campaign.add_hearths_to_settlement` | `[SettlementName] \| [Amount]` | Adds hearths to a village |
| `campaign.add_cattles_to_settlement` | `[SettlementName] \| [Amount]` | Adds cattle to a settlement |
| `campaign.add_food_to_settlement` | `[SettlementName] \| [Amount]` | Adds food to a settlement |
| `campaign.add_progress_to_current_building` | `[SettlementName] \| [Progress]` | Adds construction progress to current building |
| `campaign.set_prosperity_of_settlement` | `[SettlementName] \| [Value]` | Sets settlement prosperity to exact value |
| `campaign.set_loyalty_of_settlement` | `[SettlementName] \| [Value]` | Sets settlement loyalty to exact value |
| `campaign.set_security_of_settlement` | `[SettlementName] \| [Value]` | Sets settlement security to exact value |
| `campaign.set_militia_of_settlement` | `[SettlementName] \| [Value]` | Sets settlement militia to exact value |
| `campaign.set_hearth_of_settlement` | `[SettlementName] \| [Value]` | Sets village hearths to exact value |
| `campaign.set_food_of_settlement` | `[SettlementName] \| [Value]` | Sets settlement food to exact value |
| `campaign.clear_settlement_defense` | `[SettlementName]` | Clears all defensive structures from a settlement |
| `campaign.remove_militias_from_settlement` | `[SettlementName]` | Removes all militia from a settlement |
| `campaign.show_settlements` | _(no args)_ | Makes all settlements visible on the map |
| `campaign.hide_settlements` | _(no args)_ | Hides all settlements on the map |
| `campaign.show_hideouts` | _(no args)_ | Makes all hideouts visible on the map |
| `campaign.hide_hideouts` | _(no args)_ | Hides all hideouts on the map |
| `campaign.print_issues_in_settlement` | `[SettlementName]` | Prints all issues in a settlement |

### Diplomacy / Kingdom
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.declare_war` | `[Faction1Name] \| [Faction2Name]` | Declares war between two factions |
| `campaign.make_peace` | `[Faction1Name] \| [Faction2Name]` | Makes peace between two factions |
| `campaign.join_kingdom` | `[KingdomName]` | Player clan joins a kingdom as vassal |
| `campaign.join_kingdom_as_mercenary` | `[KingdomName]` | Player clan joins a kingdom as mercenary |
| `campaign.leave_kingdom` | _(no args)_ | Player clan leaves current kingdom |
| `campaign.create_player_kingdom` | _(no args)_ | Creates a kingdom for the player clan |
| `campaign.lead_kingdom` | `[KingdomName]` | Makes the player lead a kingdom |
| `campaign.lead_your_faction` | _(no args)_ | Makes the player lead their current faction |
| `campaign.make_trade_agreement` | `[Faction1] \| [Faction2]` | Creates a trade agreement between factions |
| `campaign.make_clan_mercenary_of_kingdom` | `[ClanName] \| [KingdomName] \| [Days]` | Makes a clan serve as mercenary _(SandBox.View.dll)_ |
| `campaign.add_influence` | `[PositiveInteger]` | Adds influence to the player clan |
| `campaign.add_renown_to_clan` | `[ClanName] \| [Renown]` | Adds renown to a specific clan |
| `campaign.activate_all_policies` | `[KingdomName]` | Activates all policies for a kingdom |
| `campaign.start_player_vs_world_war` | _(no args)_ | Declares war between player and all factions |
| `campaign.start_world_war` | _(no args)_ | Declares war between all factions |
| `campaign.print_criminal_ratings` | _(no args)_ | Prints criminal ratings for all factions |
| `campaign.print_strength_of_factions` | _(no args)_ | Prints military strength of all factions |
| `campaign.print_strength_of_lord_parties` | _(no args)_ | Prints strength of all lord parties |

### Relations / Marriage
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.add_relation` | `[HeroName] \| [Amount]` | Changes relation with a specific hero |
| `campaign.show_hero_relation` | `[HeroName]` | Shows relation level with a hero |
| `campaign.marry_player_with_hero` | `[HeroName]` | Marries the player with a hero |
| `campaign.marry_hero_with_hero` | `[Hero1Name] \| [Hero2Name]` | Marries two heroes |
| `campaign.print_heroes_suitable_for_marriage` | _(no args)_ | Lists heroes eligible for marriage |
| `campaign.is_hero_suitable_for_marriage_with_player` | `[HeroName]` | Checks if a hero is marriage-eligible |

### Pregnancy / Family
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.make_pregnant` | _(no args)_ | Makes player hero pregnant |
| `campaign.conceive_child` | _(no args)_ | Forces player hero to conceive a child |
| `campaign.end_pregnancy` | _(no args)_ | Forces end of current pregnancy |

### Time / Campaign Control
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.advance_time` | `[NumHours]` | Advances campaign time by specified hours |
| `campaign.fast_forward_campaign` | `[NumDays]` | Fast forwards campaign by specified days |
| `campaign.set_campaign_speed_multiplier` | `[Multiplier]` | Sets campaign map speed multiplier |
| `campaign.set_to_day` | `[DayNumber]` | Sets campaign time to a specific day |
| `campaign.set_to_season` | `[SeasonIndex]` | Sets campaign season (0-3) |
| `campaign.run_weekly_event` | _(no args)_ | Triggers the weekly campaign event |

### Movement / Map
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.move_main_party` | `[SettlementName]` | Teleports the player party to a settlement |
| `campaign.print_main_party_position` | _(no args)_ | Prints the main party's map position |
| `campaign.set_all_armies_and_parties_visible` | _(no args)_ | Makes all armies and parties visible on map |
| `campaign.toggle_information_restrictions` | _(no args)_ | Toggles fog-of-war style info restrictions |

### Quests
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.list_active_quests` | _(no args)_ | Lists all active quests |
| `campaign.print_all_issued_quests` | _(no args)_ | Prints all issued quests |
| `campaign.print_quests_of_hero` | `[HeroName]` | Prints quests related to a specific hero |
| `campaign.complete_active_quest` | `[QuestName]` | Completes an active quest |
| `campaign.cancel_active_quest` | `[QuestName]` | Cancels an active quest |
| `campaign.print_settlements_with_tournament` | _(no args)_ | Lists settlements with active tournaments |

### Siege / Battle Triggers
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.start_siege` | `[SettlementName]` | Starts a siege at a settlement |
| `campaign.set_main_party_attacker` | _(no args)_ | Sets main party as attacker in current siege |
| `campaign.raise_army` | `[KingdomName]` | Raises an army for a kingdom |
| `campaign.trigger_camp_encounter` | _(no args)_ | Triggers a camp encounter event |
| `campaign.trigger_mercenary_offer` | _(no args)_ | Triggers a mercenary offer |
| `campaign.trigger_vassalage_offer` | _(no args)_ | Triggers a vassalage offer |

### Camera / Focus (SandBox.View.dll)
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.focus_tournament` | _(no args)_ | Focuses camera on a settlement with an active tournament |
| `campaign.focus_hostile_army` | _(no args)_ | Focuses camera on a random hostile army |
| `campaign.focus_mobile_party` | `[PartyName]` | Focuses camera on a specific mobile party |
| `campaign.focus_hero` | `[HeroName]` | Focuses camera on a specific hero's location |
| `campaign.focus_infested_hideout` | `[Optional: NumTroops]` | Focuses camera on a random infested hideout |
| `campaign.focus_issue` | `[IssueName]` | Focuses camera on a specific issue's location |

### Miscellaneous
| Command | Parameters | Description |
|---------|-----------|-------------|
| `campaign.print_gameplay_statistics` | _(no args)_ | Prints gameplay statistics summary |
| `campaign.add_supporters_for_main_hero` | `[Amount]` | Adds supporters for the main hero |
| `campaign.control_party_ai_by_cheats` | `[0/1]` | Toggles player party AI cheat control |
| `campaign.set_rebellion_enabled` | `[0/1]` | Enables/disables rebellions |
| `campaign.export_main_hero` | _(no args)_ | Exports main hero data |
| `campaign.import_main_hero` | _(no args)_ | Imports main hero data |
| `campaign.kick_companion_from_party` | `[CompanionName]` | Kicks a companion from the party |
| `campaign.add_power_to_notable` | `[NotableName] \| [Amount]` | Adds power to a notable |
| `campaign.add_development` | `[SettlementName] \| [Amount]` | Adds development points to a settlement |

---

## `mission` group (16 commands)

### Combat / Agents (TaleWorlds.MountAndBlade.dll)
| Command | Parameters | Description |
|---------|-----------|-------------|
| `mission.flee_enemies` | _(no args)_ | Makes all enemy agents flee |
| `mission.flee_team` | `[TeamIndex]` | Makes all agents of a specific team flee |
| `mission.kill_n_allies` | `[Number]` | Kills N allied troops |
| `mission.kill_all_allies` | _(no args)_ | Kills all allied troops |
| `mission.killAgent` | `[AgentIndex]` | Kills a specific agent by index |
| `mission.toggleDisableDying` | _(no args)_ | Toggles invincibility for the player agent |
| `mission.toggleDisableDyingTeam` | _(no args)_ | Toggles invincibility for the entire player team |
| `mission.set_facial_anim_to_agent` | `[AgentIndex] \| [AnimName]` | Sets facial animation on a specific agent |

### Siege Equipment
| Command | Parameters | Description |
|---------|-----------|-------------|
| `mission.set_battering_ram_speed` | `[Speed]` | Sets the battering ram speed multiplier |
| `mission.set_siege_tower_speed` | `[Speed]` | Sets the siege tower speed multiplier |
| `mission.set_deployment_visualization_selector` | `[Bitmask]` | Sets deployment visualization (1=Undeployed, 2=Line, 4=Arc, 8=Banner, 16=Path, 32=Ghost, 64=Contour, 128=LiftLadders, 256=Light) |

### Hideout
| Command | Parameters | Description |
|---------|-----------|-------------|
| `mission.kill_all_sentries` | _(no args)_ | Kills all sentries in hideout ambush _(SandBox.dll)_ |

### Camera (TaleWorlds.MountAndBlade.View.dll)
| Command | Parameters | Description |
|---------|-----------|-------------|
| `mission.fix_camera_toggle` | _(no args)_ | Toggles fixed (debug) camera on/off |
| `mission.set_shift_camera_speed` | `[Multiplier]` | Sets the shift-camera speed multiplier (default 3) |
| `mission.set_camera_position` | `[X] [Y] [Z]` | Sets camera position to exact coordinates |
| `mission.get_face_and_helmet_info_of_followed_agent` | _(no args)_ | Copies face/helmet info of followed agent to clipboard |

---

## `ai` group (1 command)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `ai.formation_speed_adjustment_enabled` | `[0/1]` | Toggles AI formation speed adjustment on/off |

---

## `console` group (3 commands)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `console.clear` | _(no args)_ | Clears the debug console |
| `console.echo_command_window` | `[text]` | Echoes text to the command window |
| `console.echo_command_window_test` | _(no args)_ | Test async echo countdown (debug) |

---

## `chatlog` group (2 commands)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `chatlog.clear` | _(no args)_ | Clears all chat log messages |
| `chatlog.can_focus_while_in_mission` | `[0/1]` | Sets whether chat can gain focus during missions |

---

## `facegen` group (2 commands)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `facegen.show_debug` | _(no args)_ | Toggles FaceGen debug values display |
| `facegen.toggle_update_deform_keys` | _(no args)_ | Toggles FaceGen deform key updates |

---

## `game` group (1 command)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `game.reload_managed_core_params` | _(no args)_ | Reloads managed core parameters from XML |

---

## `module` group (1 command)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `module.get_item_mesh_names` | _(no args)_ | Gets and prints all item mesh names |

---

## `mp_host` group (2 commands)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `mp_host.kill_player` | `[UserName]` | Kills a player in multiplayer |
| `mp_host.end_warmup` | _(no args)_ | Ends the multiplayer warmup phase |

---

## `mp_perks` group (2 commands)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `mp_perks.raise_event` | _(unknown)_ | Raises a multiplayer perk event |
| `mp_perks.tick_perks` | _(unknown)_ | Manually ticks multiplayer perks |

---

## `scoreboard` group (1 command)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `scoreboard.force_toggle` | `[0/1]` | Forces scoreboard toggle behavior (press vs hold) |

---

## `storymode` group (1 command)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `storymode.add_family_members` | _(no args)_ | Adds story mode family members to player party/clan |

---

## `ui` group (1 command)

| Command | Parameters | Description |
|---------|-----------|-------------|
| `ui.toggle_ui` | _(no args)_ | Toggles UI visibility on/off |

---

## `benchmark` group (2 commands) _(CustomBattle module)_

| Command | Parameters | Description |
|---------|-----------|-------------|
| `benchmark.cpu_benchmark` | _(no args)_ | Runs a CPU benchmark test |
| `benchmark.cpu_benchmark_mission` | _(no args)_ | Runs a CPU benchmark in a mission context |

---

## `state_string` group (2 commands) _(CustomBattle module)_

| Command | Parameters | Description |
|---------|-----------|-------------|
| `state_string.save` | `[FileName]` | Saves the current state string to a file |
| `state_string.load` | `[FileName]` | Loads a state string from a file |

---

## `replay_mission` group (1 command) _(CustomBattle module)_

| Command | Parameters | Description |
|---------|-----------|-------------|
| `replay_mission.play` | `[FileName]` | Replays a saved mission |

---

## `mp_admin` group (6 commands) _(Multiplayer module)_

| Command | Parameters | Description |
|---------|-----------|-------------|
| `mp_admin.announcement` | `[Message]` | Sends a server-wide announcement |
| `mp_admin.kick_player` | `[PlayerName]` | Kicks a player from the server |
| `mp_admin.ban_player` | `[PlayerName]` | Bans a player from the server |
| `mp_admin.change_welcome_message` | `[Message]` | Changes the server welcome message |
| `mp_admin.change_class_restriction` | `[Class] \| [0/1]` | Changes class restrictions |
| `mp_admin.restart_game` | _(no args)_ | Restarts the current game |

---

## `customserver` group (1 command) _(Multiplayer module)_

| Command | Parameters | Description |
|---------|-----------|-------------|
| `customserver.gettoken` | _(no args)_ | Gets the custom server authentication token |

---

## DedicatedServer `ConsoleCommandMethod` Commands _(Multiplayer module)_

These use the secondary `[ConsoleCommandMethod]` attribute and are only active in dedicated server mode:

| Command | Description |
|---------|-------------|
| `list` | Lists available server commands |
| `set_winner_team` | Sets the winning team |
| `set_server_bandwidth_limit_in_mbps` | Sets server bandwidth limit |
| `set_server_tickrate` | Sets server tick rate |
| `stats` | Shows server statistics |
| `open_monitor` | Opens server monitor |
| `crash_game` | Intentionally crashes the game (debug) |

---

## Summary by Source Assembly

| Assembly | Commands |
|----------|----------|
| TaleWorlds.CampaignSystem.dll | 80 |
| TaleWorlds.MountAndBlade.dll | 17 |
| SandBox.View.dll | 9 |
| TaleWorlds.MountAndBlade.GauntletUI.dll | 5 |
| TaleWorlds.Engine.dll | 4 |
| TaleWorlds.MountAndBlade.View.dll | 4 |
| CustomBattle.dll _(CustomBattle module)_ | 5 |
| TaleWorlds.MountAndBlade.Multiplayer.dll _(Multiplayer module)_ | 7 |
| SandBox.dll | 1 |
| StoryMode.dll | 1 |
| **Total (CommandLineArgumentFunction)** | **133** |
| TaleWorlds.MountAndBlade.DedicatedCustomServer.dll _(ConsoleCommandMethod)_ | 7 |

## Notes

- `ConsoleCommandMethod` attribute exists in TaleWorlds.MountAndBlade.dll — only used by DedicatedCustomServer (7 server admin commands, not relevant for single-player bridge)
- Parameter separator for multi-arg commands is ` | ` (space-pipe-space)
- Commands return a string result ("Success" or an error message)
- All `campaign.*` commands check `CampaignCheats.CheckCheatUsage()` first — cheats must be enabled
- The `CommandLineFunctionality.CollectCommandLineFunctions()` method scans ALL loaded assemblies, so **our mod can register new commands** simply by adding the attribute to static methods
