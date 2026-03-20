# Campaign Events — Expansion Plan

Analysis of all 274 `CampaignEvents` properties. Currently subscribed to 25 events. This document proposes new events worth adding, grouped by behavior class.

## Skipped Categories

### Tick/Frequent Events (fire every frame or very frequently)
| Event | Reason |
|-------|--------|
| `TickEvent` | Already subscribed (used for arrival/pursuit detection) |
| `DailyTickEvent` | Already subscribed |
| `HourlyTickEvent` | Every in-game hour — too frequent |
| `HourlyTickClanEvent` | Per-clan per-hour |
| `HourlyTickPartyEvent` | Per-party per-hour |
| `HourlyTickSettlementEvent` | Per-settlement per-hour |
| `DailyTickClanEvent` | Per-clan per-day |
| `DailyTickHeroEvent` | Per-hero per-day |
| `DailyTickPartyEvent` | Per-party per-day |
| `DailyTickSettlementEvent` | Per-settlement per-day |
| `DailyTickTownEvent` | Per-town per-day |
| `AiHourlyTickEvent` | AI processing tick |
| `MissionTickEvent` | Per-frame in 3D scenes |
| `TickPartialHourlyAiEvent` | Sub-hourly AI tick |
| `QuarterHourlyTickEvent` | Quarter-hour tick |
| `OnQuarterDailyPartyTick` | Quarter-day per-party |
| `WeeklyTickEvent` | Could be useful but adds little over DailyTick |

### Query/Callback Events (not actual notifications)
| Event | Reason |
|-------|--------|
| `CanBeGovernorOrHavePartyRoleEvent` | Game logic query |
| `CanHaveCampaignIssuesEvent` | Game logic query |
| `CanHeroBecomePrisonerEvent` | Game logic query |
| `CanHeroDieEvent` | Game logic query |
| `CanHeroEquipmentBeChangedEvent` | Game logic query |
| `CanHeroLeadPartyEvent` | Game logic query |
| `CanHeroMarryEvent` | Game logic query |
| `CanKingdomBeDiscontinuedEvent` | Game logic query |
| `CanMoveToSettlementEvent` | Game logic query |
| `CanPlayerMeetWithHeroAfterConversationEvent` | Game logic query |
| `IsSettlementBusyEvent` | Game logic query |
| `BarterablesRequested` | Callback to build barter list |
| `CollectAvailableTutorialsEvent` | Tutorial system callback |
| `OnCheckForIssueEvent` | Issue system check |

### Internal/Framework Events
| Event | Reason |
|-------|--------|
| `OnBeforeSaveEvent` / `OnSaveStartedEvent` / `OnSaveOverEvent` | Save lifecycle |
| `OnGameEarlyLoadedEvent` / `OnGameLoadedEvent` | Load lifecycle (already have `OnGameLoadFinished`) |
| `OnNewGameCreatedEvent` / `PartialFollowUp*` | New game lifecycle |
| `OnSessionLaunchedEvent` / `OnAfterSessionLaunchedEvent` | Session lifecycle |
| `OnCharacterCreationInitializedEvent` / `IsOverEvent` | Character creation |
| `OnConfigChangedEvent` | Settings change |
| `OnMapEventContinuityNeedsUpdateEvent` | Internal map event |
| `LocationCharactersAreReadyToSpawnEvent` / `SimulatedEvent` | Scene NPC spawning |
| `BeforePlayerAgentSpawnEvent` / `PlayerAgentSpawned` | Mission agent spawn |
| `ArmyOverlaySetDirtyEvent` | UI refresh |
| `MapInteractableCreated` / `Destroyed` | Map visual elements |
| `OnMapMarkerCreatedEvent` / `RemovedEvent` | Map markers |
| `CharacterPortraitPopUpOpenedEvent` / `ClosedEvent` | UI portraits |
| `OnHeroGetsBusyEvent` | Internal AI state |
| `OnHeroTeleportationRequestedEvent` | Internal movement |
| `OnHeroUnregisteredEvent` | Internal cleanup |
| `OnCollectLootsItemsEvent` / `OnLootDistributedToPartyEvent` | Loot distribution internal |
| `OnMobilePartyNavigationStateChangedEvent` | Every nav change — very frequent |
| `OnMobilePartyRaftStateChangedEvent` | Raft state toggle |

### Before/After Duplicates (already have the primary event)
| Event | Primary Event |
|-------|---------------|
| `BeforeGameMenuOpenedEvent` / `AfterGameMenuInitializedEvent` | `GameMenuOpened` |
| `BeforeSettlementEnteredEvent` / `AfterSettlementEntered` | `SettlementEntered` |
| `BeforeHeroKilledEvent` | `HeroKilledEvent` |
| `BeforeMissionOpenedEvent` | `OnMissionStartedEvent` (proposed) |
| `OnBeforeMainCharacterDiedEvent` | `HeroKilledEvent` + `OnGameOverEvent` |
| `OnBeforePlayerCharacterChangedEvent` | `OnPlayerCharacterChangedEvent` |

### Too Frequent for AI Agent
| Event | Reason |
|-------|--------|
| `OnHeroCombatHitEvent` | Every hit in combat |
| `OnSiegeBombardmentHitEvent` / `WallHitEvent` | Every bombardment hit |
| `OnPlayerPartyKnockedOrKilledTroopEvent` | Every troop kill |
| `GameMenuOptionSelectedEvent` | Every menu click |
| `OnPartySizeChangedEvent` | Every roster change |
| `OnPartyConsumedFoodEvent` | Every food consumption |
| `NearbyPartyAddedToPlayerMapEvent` | Every party enters vision |
| `OnClanInfluenceChangedEvent` | Frequent influence changes |
| `PartyVisibilityChangedEvent` | Fog of war changes |
| `TrackDetectedEvent` / `TrackLostEvent` | Tracking system |
| `OnItemConsumedEvent` / `OnItemProducedEvent` / `OnItemsRefinedEvent` | Economy simulation |
| `MercenaryNumberChangedInTown` / `MercenaryTroopChangedInTown` | Town garrison churn |
| `OnPartyAddedToMapEventEvent` | Every battle participant |
| `OnPlayerBodyPropertiesChangedEvent` | Character appearance |

---

## Proposed New Events

### WorldEventBehavior

#### Player Progression
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `ClanTierIncrease` | `(Clan clan, bool shouldNotify)` | `campaign/clan_tier_changed` | Clan tier milestone reached |
| `RenownGained` | `(Hero hero, int gainedRenown, bool doNotNotify)` | `campaign/renown_gained` | Renown earned (filter: player clan only) |
| `HeroLevelledUp` | `(Hero hero, bool shouldNotify)` | `campaign/hero_levelled_up` | Hero level up (filter: player + companions) |
| `PerkOpenedEvent` | `(Hero hero, PerkObject perk)` | `campaign/perk_opened` | Perk selected (filter: player + companions) |
| `PlayerTraitChangedEvent` | `(TraitObject trait, int previousLevel)` | `campaign/player_trait_changed` | Player personality trait changed |

#### Hero Lifecycle
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `HeroCreated` | `(Hero hero, bool isBornNaturally)` | `campaign/hero_created` | New hero appeared in world |
| `HeroComesOfAgeEvent` | `(Hero hero)` | `campaign/hero_comes_of_age` | Child hero becomes adult |
| `CharacterDefeated` | `(Hero winner, Hero loser)` | `campaign/character_defeated` | Hero defeated another in battle |
| `CompanionRemoved` | `(Hero hero, RemoveCompanionAction.RemoveCompanionDetail detail)` | `campaign/companion_removed` | Companion left the party |
| `OnHeroChangedClanEvent` | `(Hero hero, Clan oldClan)` | `campaign/hero_changed_clan` | Hero switched clans |

#### Kingdom & Politics
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `KingdomCreatedEvent` | `(Kingdom kingdom)` | `campaign/kingdom_created` | New kingdom formed |
| `KingdomDestroyedEvent` | `(Kingdom kingdom)` | `campaign/kingdom_destroyed` | Kingdom eliminated |
| `KingdomDecisionAdded` | `(KingdomDecision decision, bool isPlayerInvolved)` | `campaign/kingdom_decision_added` | New vote/proposal in kingdom |
| `KingdomDecisionConcluded` | `(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)` | `campaign/kingdom_decision_concluded` | Vote finished, outcome decided |
| `OnClanLeaderChangedEvent` | `(Hero oldLeader, Hero newLeader)` | `campaign/clan_leader_changed` | Clan leadership transferred |
| `RulingClanChanged` | `(Kingdom kingdom, Clan newRulingClan)` | `campaign/ruling_clan_changed` | Kingdom ruler changed |
| `OnClanDefectedEvent` | `(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)` | `campaign/clan_defected` | Clan defected to another kingdom |
| `OnAllianceStartedEvent` | `(Kingdom kingdom1, Kingdom kingdom2)` | `campaign/alliance_started` | Two kingdoms formed alliance |
| `OnAllianceEndedEvent` | `(Kingdom kingdom1, Kingdom kingdom2)` | `campaign/alliance_ended` | Alliance between kingdoms broken |

#### Military
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `MapEventStarted` | `(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)` | `campaign/battle_started` | Battle/raid/siege assault began on campaign map |
| `OnSiegeEventEndedEvent` | `(SiegeEvent siegeEvent)` | `campaign/siege_ended` | Siege concluded (relief or assault) |
| `OnPlayerBattleEndEvent` | `(MapEvent mapEvent)` | `campaign/player_battle_ended` | Player's battle finished |
| `OnPlayerSiegeStartedEvent` | `()` | `campaign/player_siege_started` | Player initiated a siege |
| `OnPartyJoinedArmyEvent` | `(MobileParty mobileParty)` | `campaign/party_joined_army` | Party joined an army |
| `OnPartyLeftArmyEvent` | `(MobileParty party, Army army)` | `campaign/party_left_army` | Party left an army |
| `VillageBeingRaided` | `(Village village)` | `campaign/village_being_raided` | Village raid in progress |
| `VillageLooted` | `(Village village)` | `campaign/village_looted` | Village fully looted |
| `RaidCompletedEvent` | `(BattleSideEnum winnerSide, RaidEventComponent raidEvent)` | `campaign/raid_completed` | Raid finished with outcome |

#### Settlement & Governance
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnGovernorChangedEvent` | `(Town fortification, Hero oldGovernor, Hero newGovernor)` | `campaign/governor_changed` | Settlement governor changed |
| `TownRebelliosStateChanged` | `(Town town, bool rebelliousState)` | `campaign/town_rebellion_state` | Town entering/leaving rebellious state |
| `RebellionFinished` | `(Settlement settlement, Clan oldOwnerClan)` | `campaign/rebellion_finished` | Rebellion concluded, ownership may have changed |
| `CrimeRatingChanged` | `(IFaction kingdom, float deltaCrimeAmount)` | `campaign/crime_rating_changed` | Player's crime rating changed (filter: significant changes) |

#### Family & Romance
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `RomanticStateChanged` | `(Hero hero1, Hero hero2, Romance.RomanceLevelEnum level)` | `campaign/romantic_state_changed` | Romance progressed between heroes |
| `OnChildConceivedEvent` | `(Hero mother)` | `campaign/child_conceived` | Pregnancy started |
| `OnGivenBirthEvent` | `(Hero mother, List<Hero> aliveChildren, int stillbornCount)` | `campaign/child_born` | Birth event |

#### Mercenary & Vassal Service
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnMercenaryServiceStartedEvent` | `(Clan mercenaryClan, ...)` | `campaign/mercenary_service_started` | Clan entered mercenary service |
| `OnMercenaryServiceEndedEvent` | `(Clan mercenaryClan, ...)` | `campaign/mercenary_service_ended` | Clan left mercenary service |

#### Game State
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnGameOverEvent` | `()` | `campaign/game_over` | Game ended (player died with no heir, etc.) |
| `OnMainPartyStarvingEvent` | `()` | `campaign/party_starving` | Player party has no food |

### PlayerNavigationEventBehavior

| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnMissionStartedEvent` | `(IMission mission)` | `campaign/mission_started` | Entered a 3D scene (town, battle, etc.) |
| `OnMissionEndedEvent` | `(IMission mission)` | `campaign/mission_ended` | Left a 3D scene |
| `OnHideoutSpottedEvent` | `(PartyBase party, PartyBase hideoutParty)` | `campaign/hideout_spotted` | Bandit hideout discovered |

### UIEventBehavior

#### Offers & Proposals (AI must respond)
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnPeaceOfferedToPlayerEvent` | `(IFaction opponentFaction, int tributeAmount, int tributeDurationInDays)` | `campaign/peace_offered` | Enemy faction offers peace terms |
| `OnMarriageOfferedToPlayerEvent` | `(Hero suitor, Hero maiden)` | `campaign/marriage_offered` | Marriage proposal received |
| `OnRansomOfferedToPlayerEvent` | `(Hero captiveHero)` | `campaign/ransom_offered` | Ransom offer for captured hero |
| `OnVassalOrMercenaryServiceOfferedToPlayerEvent` | `(Kingdom offeredKingdom)` | `campaign/service_offered` | Kingdom offers mercenary/vassal contract |

#### Barter & Trade
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnBarterAcceptedEvent` | `(Hero offererHero, Hero otherHero, List<Barterable> barters)` | `campaign/barter_accepted` | Barter deal accepted |
| `OnBarterCanceledEvent` | `(Hero offererHero, Hero otherHero, List<Barterable> barters)` | `campaign/barter_canceled` | Barter deal rejected/canceled |

#### Tournaments
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnPlayerJoinedTournamentEvent` | `(Town town, bool isParticipant)` | `campaign/tournament_joined` | Player entered a tournament |
| `TournamentFinished` | `(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)` | `campaign/tournament_finished` | Tournament concluded |
| `PlayerEliminatedFromTournament` | `(int round, Town town)` | `campaign/tournament_eliminated` | Player knocked out of tournament |

#### Random Events
| Event | Signature | Channel | Description |
|-------|-----------|---------|-------------|
| `OnIncidentResolvedEvent` | `(Incident incident)` | `campaign/incident_resolved` | Random encounter/incident resolved |
| `PersuasionProgressCommittedEvent` | `(Tuple<PersuasionOptionArgs, PersuasionOptionResult> progress)` | `campaign/persuasion_progress` | Persuasion check result committed |

---

## Events Considered But Not Recommended

These are borderline events that could be added later if needed:

| Event | Reason to Skip |
|-------|----------------|
| `HeroGainedSkill` | Fires for every hero every skill XP gain — very frequent |
| `HeroOccupationChangedEvent` | Rare, low value for AI gameplay |
| `OnHeroJoinedPartyEvent` | Party roster changes — could be noisy |
| `MobilePartyCreated` | Bandits/caravans spawn constantly |
| `OnPartyRemovedEvent` | Parties removed constantly |
| `OnNewIssueCreatedEvent` / `OnIssueUpdatedEvent` | Already have quest started/completed |
| `QuestLogAddedEvent` / `IssueLogAddedEvent` | Too granular — log entries |
| `MobilePartyQuestStatusChanged` | Internal quest state |
| `BanditPartyRecruited` | Minor world detail |
| `OnTroopRecruitedEvent` / `OnUnitRecruitedEvent` | Every recruit action — frequent |
| `OnTroopsDesertedEvent` | Could be useful but infrequent edge case |
| `HeroOrPartyGaveItem` / `HeroOrPartyTradedGold` | Every trade — very frequent |
| `OnItemSoldEvent` / `OnItemsDiscardedByPlayerEvent` | Every item sale |
| `PlayerInventoryExchangeEvent` | Every inventory action |
| `OnEquipmentSmeltedByHeroEvent` / `CraftingPartUnlockedEvent` / `OnNewItemCraftedEvent` | Crafting details |
| `SiegeEngineBuiltEvent` / `OnSiegeEngineDestroyedEvent` | Siege equipment details |
| `OnBlockadeActivatedEvent` / `DeactivatedEvent` | Naval blockade (DLC) |
| `OnShipCreatedEvent` / `DestroyedEvent` / `OwnerChanged` / `Repaired` | Ship events (DLC) |
| `OnBuildingLevelChangedEvent` | Settlement building changes |
| `WorkshopOwnerChangedEvent` / `TypeChangedEvent` | Workshop management |
| `OnCaravanTransactionCompletedEvent` | Caravan trade detail |
| `OnPlayerEarnedGoldFromAssetEvent` / `OnPlayerTradeProfitEvent` | Income tracking |
| `OnClanEarnedGoldFromTributeEvent` | Periodic tribute income |
| `AlleyOwnerChanged` / `Cleared` / `Occupied` | Alley control (minor) |
| `ArmyGathered` | Already have `ArmyCreated` / `ArmyDispersed` |
| `PartyAttachedAnotherParty` / `PartyRemovedFromArmyEvent` | Army composition detail |
| `PlayerDesertedBattleEvent` | Fleeing combat — niche |
| `PlayerStartRecruitmentEvent` / `PlayerStartTalkFromMenu` | UI interaction triggers |
| `PlayerUpgradedTroopsEvent` | Troop upgrade actions |
| `OnPrisonerDonatedToSettlementEvent` / `OnPrisonerSoldEvent` / `OnPrisonerTakenEvent` | Prisoner management (hero versions already covered) |
| `OnMainPartyPrisonerRecruitedEvent` | Prisoner recruit detail |
| `PrisonersChangeInSettlement` | Garrison prisoner changes |
| `OnCallToWarAgreementStartedEvent` / `EndedEvent` | Diplomacy pact detail |
| `OnTradeAgreementSignedEvent` | Trade pact — infrequent |
| `ChildEducationCompletedEvent` / `HeroGrowsOutOfInfancyEvent` / `HeroReachesTeenAgeEvent` | Child growth stages |
| `OnHeirSelectionRequestedEvent` / `OverEvent` | Succession events |
| `OnPlayerCharacterChangedEvent` | Post-succession identity change |
| `RebelliousClanDisbandedAtSettlement` | Post-rebellion cleanup |
| `OnPlayerBoardGameOverEvent` | Tavern mini-game |
| `OnTutorialCompletedEvent` | Tutorial system |
| `PerkResetEvent` | Perk respec — very rare |
| `CharacterBecameFugitiveEvent` | Fugitive status — niche |
| `OnFigureheadUnlockedEvent` | Cosmetic unlock |
| `ForceSuppliesCompletedEvent` / `ForceVolunteersCompletedEvent` | Forced action completions |
| `OnHomeHideoutChangedEvent` / `OnHideoutDeactivatedEvent` | Hideout lifecycle |
| `OnHideoutBattleCompletedEvent` | Already covered by `MapEventEnded` |
| `OnTradeRumorIsTakenEvent` | Trade rumor system |
| `OnAgentJoinedConversationEvent` | Mid-conversation agent join |
| `OnVassalOrMercenaryServiceOfferCanceledEvent` | Offer cancellation |
| `OnMarriageOfferCanceledEvent` / `OnPeaceOfferResolvedEvent` / `OnRansomOfferCancelledEvent` | Offer resolution (already have the offer event) |
| `OnPartyDisbandStartedEvent` / `CanceledEvent` / `DisbandedEvent` | Party disband lifecycle |
| `OnPartyLeaderChangedEvent` / `LeaderChangeOfferCanceledEvent` | Party leader changes |
| `OnPlayerArmyLeaderChangedBehaviorEvent` | Army behavior change |

---

## Summary

| Category | Currently | Proposed | Total |
|----------|-----------|----------|-------|
| WorldEventBehavior | 20 | +35 | 55 |
| PlayerNavigationEventBehavior | 5 | +3 | 8 |
| UIEventBehavior | 3 | +10 | 13 |
| **Total** | **28** | **+48** | **76** |

Skipped: ~198 events (ticks, queries, internal, too frequent, duplicates, low value).
