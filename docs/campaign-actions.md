# Campaign Actions Reference

> **Assembly:** `TaleWorlds.CampaignSystem.dll` (all actions are in this assembly — official game modules do not add additional Action classes)
> **Namespace:** `TaleWorlds.CampaignSystem.Actions`
> **Total Action Classes:** 61 (+ supporting enums/detail types)

All action classes in this namespace are `public static` classes containing static methods that modify campaign game state. They follow the pattern of calling an internal `ApplyInternal` method and then firing campaign events. These are the primary API for making game-state changes safely.

---

## Table of Contents

1. [Economy and Trade](#1-economy-and-trade)
2. [Diplomacy and War](#2-diplomacy-and-war)
3. [Kingdom and Clan Management](#3-kingdom-and-clan-management)
4. [Party Management](#4-party-management)
5. [Settlement Management](#5-settlement-management)
6. [Character and Hero Actions](#6-character-and-hero-actions)
7. [Companion and Relationship](#7-companion-and-relationship)
8. [Captivity and Prisoners](#8-captivity-and-prisoners)
9. [Military and Combat](#9-military-and-combat)
10. [Naval / Ships](#10-naval--ships)
11. [Miscellaneous](#11-miscellaneous)

---

## 1. Economy and Trade

### GiveGoldAction
Transfers gold between heroes, parties, and settlements. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyBetweenCharacters` | `(Hero giverHero, Hero recipientHero, Int32 amount, Boolean disableNotification)` | Transfer gold between two heroes |
| `ApplyForCharacterToSettlement` | `(Hero giverHero, Settlement settlement, Int32 amount, Boolean disableNotification)` | Hero donates gold to a settlement |
| `ApplyForSettlementToCharacter` | `(Settlement giverSettlement, Hero recipientHero, Int32 amount, Boolean disableNotification)` | Settlement pays gold to a hero |
| `ApplyForCharacterToParty` | `(Hero giverHero, PartyBase receipentParty, Int32 amount, Boolean disableNotification)` | Hero gives gold to a party |
| `ApplyForPartyToCharacter` | `(PartyBase giverParty, Hero recipientHero, Int32 amount, Boolean disableNotification)` | Party gives gold to a hero |
| `ApplyForPartyToSettlement` | `(PartyBase giverParty, Settlement settlement, Int32 amount, Boolean disableNotification)` | Party gives gold to a settlement |
| `ApplyForSettlementToParty` | `(Settlement giverSettlement, PartyBase recipientParty, Int32 amount, Boolean disableNotification)` | Settlement gives gold to a party |
| `ApplyForPartyToParty` | `(PartyBase giverParty, PartyBase receipentParty, Int32 amount, Boolean disableNotification)` | Transfer gold between two parties |

### GiveItemAction
Transfers items between heroes or parties.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyForHeroes` | `(Hero giver, Hero receiver, ItemRosterElement& itemRosterElement)` | Transfer item between two heroes |
| `ApplyForParties` | `(PartyBase giverParty, PartyBase receiverParty, ItemRosterElement& itemRosterElement)` | Transfer item between two parties |

### SellItemsAction
Sells items in a settlement marketplace.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement subject, Int32 number, Settlement currentSettlement)` | Execute an item sale transaction |

### SellGoodsForTradeAction
Trade goods sale by villager parties.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByVillagerTrade` | `(Settlement settlement, MobileParty villagerParty)` | Villager party sells goods at a settlement |

### SellPrisonersAction
Sells prisoners for gold. **Useful for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyForAllPrisoners` | `(PartyBase sellerParty, PartyBase buyerParty)` | Sell all prisoners from one party to another |
| `ApplyForSelectedPrisoners` | `(PartyBase sellerParty, PartyBase buyerParty, TroopRoster prisoners)` | Sell specific prisoners |
| `ApplyByPartyScreen` | `(TroopRoster prisoners)` | Sell prisoners via the party screen UI context |

### ChangeOwnerOfWorkshopAction
Changes workshop ownership.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByPlayerBuying` | `(Workshop workshop)` | Player buys a workshop |
| `ApplyByPlayerSelling` | `(Workshop workshop, Hero newOwner, WorkshopType workshopType)` | Player sells a workshop |
| `ApplyByBankruptcy` | `(Workshop workshop, Hero newOwner, WorkshopType workshopType, Int32 cost)` | Workshop changes hands due to bankruptcy |
| `ApplyByDeath` | `(Workshop workshop, Hero newOwner)` | Workshop inherited after owner death |
| `ApplyByWar` | `(Workshop workshop, Hero newOwner, WorkshopType workshopType)` | Workshop seized during war |

### ChangeProductionTypeOfWorkshopAction
Changes what a workshop produces.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Workshop workshop, WorkshopType newWorkshopType, Boolean ignoreCost)` | Change workshop production type |

### InitializeWorkshopAction
Sets up a new workshop.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByNewGame` | `(Workshop workshop, Hero workshopOwner, WorkshopType workshopType)` | Initialize a workshop at game start |

---

## 2. Diplomacy and War

### DeclareWarAction
Declares war between factions. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByDefault` | `(IFaction faction1, IFaction faction2)` | Generic war declaration |
| `ApplyByKingdomDecision` | `(IFaction faction1, IFaction faction2)` | War via kingdom council vote |
| `ApplyByPlayerHostility` | `(IFaction faction1, IFaction faction2)` | War triggered by player hostility |
| `ApplyByRebellion` | `(IFaction faction1, IFaction faction2)` | War due to rebellion |
| `ApplyByCrimeRatingChange` | `(IFaction faction1, IFaction faction2)` | War due to high crime rating |
| `ApplyByKingdomCreation` | `(IFaction faction1, IFaction faction2)` | War triggered by kingdom creation |
| `ApplyByClaimOnThrone` | `(IFaction faction1, IFaction faction2)` | War due to throne claim |
| `ApplyByCallToWarAgreement` | `(IFaction faction1, IFaction faction2)` | War via call-to-arms alliance |

### MakePeaceAction
Makes peace between factions. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(IFaction faction1, IFaction faction2)` | Simple peace agreement |
| `ApplyByKingdomDecision` | `(IFaction faction1, IFaction faction2, Int32 dailyTributeFrom1To2, Int32 dailyTributeDuration)` | Peace with tribute terms via kingdom decision |

### BeHostileAction
Applies hostile actions between parties, affecting relations and crime.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyHostileAction` | `(PartyBase attackerParty, PartyBase defenderParty, Single value)` | Generic hostile action with custom severity |
| `ApplyMinorCoercionHostileAction` | `(PartyBase attackerParty, PartyBase defenderParty)` | Minor coercion (e.g., threatening) |
| `ApplyMajorCoercionHostileAction` | `(PartyBase attackerParty, PartyBase defenderParty)` | Major coercion (e.g., forcing supplies) |
| `ApplyEncounterHostileAction` | `(PartyBase attackerParty, PartyBase defenderParty)` | Hostile encounter on the map |

### ChangeCrimeRatingAction
Modifies crime rating with a faction.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(IFaction faction, Single deltaCrimeRating, Boolean showNotification)` | Change crime rating by delta amount |

### PayForCrimeAction
Pay to clear crime rating.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(IFaction faction, PaymentMethod paymentMethod)` | Pay off crime with specified payment method |
| `GetClearCrimeCost` | `(IFaction faction, PaymentMethod paymentMethod) -> Single` | Calculate cost to clear crime |

### BribeGuardsAction
Bribes settlement guards.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Settlement settlement, Int32 gold)` | Bribe guards at a settlement |

---

## 3. Kingdom and Clan Management

### ChangeKingdomAction
Manages clan-kingdom membership. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByJoinToKingdom` | `(Clan clan, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil, Boolean showNotification)` | Clan joins a kingdom as vassal |
| `ApplyByJoinToKingdomByDefection` | `(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil, Boolean showNotification)` | Clan defects from one kingdom to another |
| `ApplyByCreateKingdom` | `(Clan clan, Kingdom newKingdom, Boolean showNotification)` | Clan creates a new kingdom |
| `ApplyByLeaveByKingdomDestruction` | `(Clan clan, Boolean showNotification)` | Clan leaves because kingdom was destroyed |
| `ApplyByLeaveKingdom` | `(Clan clan, Boolean showNotification)` | Clan voluntarily leaves kingdom |
| `ApplyByLeaveWithRebellionAgainstKingdom` | `(Clan clan, Boolean showNotification)` | Clan leaves via rebellion |
| `ApplyByJoinFactionAsMercenary` | `(Clan clan, Kingdom newKingdom, CampaignTime shouldStayInKingdomUntil, Int32 awardMultiplier, Boolean showNotification)` | Clan joins as mercenary |
| `ApplyByLeaveKingdomAsMercenary` | `(Clan mercenaryClan, Boolean showNotification)` | Mercenary clan leaves kingdom |
| `ApplyByLeaveKingdomByClanDestruction` | `(Clan clan, Boolean showNotification)` | Clan leaves due to being destroyed |

### ChangeRulingClanAction
Changes the ruling clan of a kingdom.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Kingdom kingdom, Clan clan)` | Set a new ruling clan for a kingdom |

### ChangeClanLeaderAction
Changes the leader of a clan.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyWithSelectedNewLeader` | `(Clan clan, Hero newLeader)` | Set a specific new clan leader |
| `ApplyWithoutSelectedNewLeader` | `(Clan clan)` | Auto-select a new clan leader |

### ChangeClanInfluenceAction
Modifies a clan's influence. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Clan clan, Single amount)` | Add or subtract influence from a clan |

### GainKingdomInfluenceAction
Awards kingdom influence for various actions.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyForBattle` | `(Hero hero, Single value)` | Influence gained from battle |
| `ApplyForGivingFood` | `(Hero hero1, Hero hero2, Single value)` | Influence for sharing food |
| `ApplyForDefault` | `(Hero hero, Single value)` | Generic influence gain |
| `ApplyForJoiningFaction` | `(Hero hero, Single value)` | Influence for joining a faction |
| `ApplyForDonatePrisoners` | `(MobileParty donatingParty, Single value)` | Influence for donating prisoners |
| `ApplyForRaidingEnemyVillage` | `(MobileParty side1Party, Single value)` | Influence for raiding |
| `ApplyForBesiegingEnemySettlement` | `(MobileParty side1Party, Single value)` | Influence for besieging |
| `ApplyForSiegeSafePassageBarter` | `(MobileParty side1Party, Single value)` | Influence for siege safe passage |
| `ApplyForCapturingEnemySettlement` | `(MobileParty side1Party, Single value)` | Influence for capturing settlement |
| `ApplyForLeavingTroopToGarrison` | `(Hero hero, Single value)` | Influence for garrisoning troops |
| `ApplyForBoardGameWon` | `(Hero hero, Single value)` | Influence for winning board game |

### GainRenownAction
Awards renown to a hero. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero hero, Single renownValue, Boolean doNotNotify)` | Grant renown to a hero |

### DestroyClanAction
Destroys a clan permanently.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Clan destroyedClan)` | Destroy a clan (generic) |
| `ApplyByFailedRebellion` | `(Clan failedRebellionClan)` | Destroy rebel clan after failed rebellion |
| `ApplyByClanLeaderDeath` | `(Clan destroyedClan)` | Destroy clan because leader died with no heir |

### DestroyKingdomAction
Destroys a kingdom permanently.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Kingdom destroyedKingdom)` | Destroy a kingdom (generic) |
| `ApplyByKingdomLeaderDeath` | `(Kingdom destroyedKingdom)` | Destroy kingdom because leader died |

### StartMercenaryServiceAction
Begins mercenary contract.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByDefault` | `(Clan clan, Kingdom kingdom, Int32 awardMultiplier)` | Start mercenary service for a clan |

### EndMercenaryServiceAction
Ends a mercenary contract.

| Method | Signature | Description |
|--------|-----------|-------------|
| `EndByDefault` | `(Clan clan)` | End mercenary service (generic) |
| `EndByLeavingKingdom` | `(Clan clan)` | End by leaving the kingdom |
| `EndByBecomingVassal` | `(Clan clan)` | End by becoming a full vassal |

### ClaimSettlementAction
Claims a settlement.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero claimant, Settlement claimedSettlement)` | Hero claims a settlement |

---

## 4. Party Management

### SetPartyAiAction
Sets AI behavior for a mobile party. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetActionForVisitingSettlement` | `(MobileParty owner, Settlement settlement, NavigationType navigationType, Boolean isFromPort, Boolean isTargetingPort)` | Move party to visit a settlement |
| `GetActionForPatrollingAroundSettlement` | `(MobileParty owner, Settlement settlement, NavigationType navigationType, Boolean isFromPort, Boolean isTargetingPort)` | Patrol around a settlement |
| `GetActionForPatrollingAroundPoint` | `(MobileParty owner, CampaignVec2 position, NavigationType navigationType, Boolean isFromPort)` | Patrol around a map position |
| `GetActionForRaidingSettlement` | `(MobileParty owner, Settlement settlement, NavigationType navigationType, Boolean isFromPort)` | Raid a village |
| `GetActionForBesiegingSettlement` | `(MobileParty owner, Settlement settlement, NavigationType navigationType, Boolean isFromPort)` | Besiege a settlement |
| `GetActionForEngagingParty` | `(MobileParty owner, MobileParty mobileParty, NavigationType navigationType, Boolean isFromPort)` | Engage/attack another party |
| `GetActionForGoingAroundParty` | `(MobileParty owner, MobileParty mobileParty, NavigationType navigationType, Boolean isFromPort)` | Move around (avoid) a party |
| `GetActionForDefendingSettlement` | `(MobileParty owner, Settlement settlement, NavigationType navigationType, Boolean isFromPort, Boolean isTargetingPort)` | Defend a settlement |
| `GetActionForEscortingParty` | `(MobileParty owner, MobileParty mobileParty, NavigationType navigationType, Boolean isFromPort, Boolean isTargetingPort)` | Escort another party |
| `GetActionForMovingToNearestLand` | `(MobileParty owner, Settlement settlement)` | Move party to nearest land from sea |

### DestroyPartyAction
Destroys a mobile party.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(PartyBase destroyerParty, MobileParty destroyedParty)` | Destroy a party (by another party) |
| `ApplyForDisbanding` | `(MobileParty disbandedParty, Settlement relatedSettlement)` | Destroy party by disbanding at a settlement |

### DisbandPartyAction
Begins or cancels the disbanding process.

| Method | Signature | Description |
|--------|-----------|-------------|
| `StartDisband` | `(MobileParty disbandParty)` | Begin disbanding a party |
| `CancelDisband` | `(MobileParty disbandParty)` | Cancel party disbanding |

### GatherArmyAction
Creates an army. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(MobileParty leaderParty, IMapPoint gatheringPoint)` | Gather an army at a map point |

### DisbandArmyAction
Disbands an army for various reasons.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByReleasedByPlayerAfterBattle` | `(Army army)` | Disband after player releases army post-battle |
| `ApplyByArmyLeaderIsDead` | `(Army army)` | Disband because army leader died |
| `ApplyByNotEnoughParty` | `(Army army)` | Disband due to insufficient parties |
| `ApplyByObjectiveFinished` | `(Army army)` | Disband after completing objective |
| `ApplyByPlayerTakenPrisoner` | `(Army army)` | Disband because player was taken prisoner |
| `ApplyByFoodProblem` | `(Army army)` | Disband due to food shortage |
| `ApplyByInactivity` | `(Army army)` | Disband due to inactivity |
| `ApplyByCohesionDepleted` | `(Army army)` | Disband because cohesion reached zero |
| `ApplyByNoActiveWar` | `(Army army)` | Disband because no active wars |
| `ApplyByUnknownReason` | `(Army army)` | Disband for unknown/generic reason |
| `ApplyByLeaderPartyRemoved` | `(Army army)` | Disband because leader party was removed |
| `ApplyByNoShip` | `(Army army)` | Disband because no ship available |

### AddHeroToPartyAction
Adds a hero to a party. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero hero, MobileParty party, Boolean showNotification)` | Add a hero to a mobile party |

---

## 5. Settlement Management

### EnterSettlementAction
Hero or party enters a settlement. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyForParty` | `(MobileParty mobileParty, Settlement settlement)` | Party enters a settlement |
| `ApplyForPartyEntersAlley` | `(MobileParty party, Settlement settlement, Alley alley, Boolean isPlayerInvolved)` | Party enters an alley in settlement |
| `ApplyForCharacterOnly` | `(Hero hero, Settlement settlement)` | Hero enters settlement (without party) |
| `ApplyForPrisoner` | `(Hero hero, Settlement settlement)` | Prisoner is placed in settlement |

### LeaveSettlementAction
Hero or party leaves a settlement. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyForParty` | `(MobileParty mobileParty)` | Party leaves settlement |
| `ApplyForCharacterOnly` | `(Hero hero)` | Hero leaves settlement (without party) |

### ChangeOwnerOfSettlementAction
Transfers settlement ownership. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByDefault` | `(Hero hero, Settlement settlement)` | Generic ownership change |
| `ApplyByKingDecision` | `(Hero hero, Settlement settlement)` | Ownership change by king's decision |
| `ApplyBySiege` | `(Hero newOwner, Hero capturerHero, Settlement settlement)` | Ownership by siege conquest |
| `ApplyByLeaveFaction` | `(Hero hero, Settlement settlement)` | Ownership transfer when clan leaves faction |
| `ApplyByBarter` | `(Hero hero, Settlement settlement)` | Ownership change via bartering |
| `ApplyByRebellion` | `(Hero hero, Settlement settlement)` | Ownership by rebellion |
| `ApplyByDestroyClan` | `(Settlement settlement, Hero newOwner)` | Ownership transfer when owning clan is destroyed |
| `ApplyByGift` | `(Settlement settlement, Hero newOwner)` | Settlement given as a gift |

### ChangeGovernorAction
Assigns or removes a settlement governor. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Town fortification, Hero governor)` | Assign a governor to a town |
| `RemoveGovernorOf` | `(Hero governor)` | Remove hero from their governor post |
| `RemoveGovernorOfIfExists` | `(Town town)` | Remove governor from a town if one exists |

### ChangeVillageStateAction
Changes the state of a village.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyBySettingToNormal` | `(Settlement settlement)` | Set village to normal state |
| `ApplyBySettingToBeingRaided` | `(Settlement settlement, MobileParty raider)` | Set village to being raided |
| `ApplyBySettingToBeingForcedForSupplies` | `(Settlement settlement, MobileParty raider)` | Village forced to give supplies |
| `ApplyBySettingToBeingForcedForVolunteers` | `(Settlement settlement, MobileParty raider)` | Village forced to give volunteers |
| `ApplyBySettingToLooted` | `(Settlement settlement, MobileParty raider)` | Set village to looted state |

### IncreaseSettlementHealthAction
Increases settlement health/prosperity.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Settlement settlement, Single percentage)` | Increase settlement health by percentage |

### BreakInOutBesiegedSettlementAction
Break in or out of a besieged settlement.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyBreakIn` | `(TroopRoster& casualties, Int32& armyCasualtiesCount, Boolean isFromPort)` | Break into a besieged settlement |
| `ApplyBreakOut` | `(TroopRoster& casualties, Int32& armyCasualtiesCount, Boolean isFromPort)` | Break out of a besieged settlement |

### LiftSiegeAction
Lifts a siege.

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetGameAction` | `(MobileParty side1Party)` | Lift siege by the besieging party |

### SiegeAftermathAction
Handles post-siege consequences.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyAftermath` | `(MobileParty attackerParty, Settlement settlement, SiegeAftermath aftermathType, Clan previousSettlementOwner, Dictionary partyContributions)` | Apply siege aftermath (devastate, pillage, mercy, etc.) |

---

## 6. Character and Hero Actions

### KillCharacterAction
Kills or removes a hero from the game. Has many variants for different causes.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByOldAge` | `(Hero victim, Boolean showNotification)` | Death by old age |
| `ApplyByWounds` | `(Hero victim, Boolean showNotification)` | Death by wounds |
| `ApplyByBattle` | `(Hero victim, Hero killer, Boolean showNotification)` | Death in battle |
| `ApplyByMurder` | `(Hero victim, Hero killer, Boolean showNotification)` | Death by murder |
| `ApplyInLabor` | `(Hero lostMother, Boolean showNotification)` | Death during childbirth |
| `ApplyByExecution` | `(Hero victim, Hero executer, Boolean showNotification, Boolean isForced)` | Death by execution |
| `ApplyByExecutionAfterMapEvent` | `(Hero victim, Hero executer, Boolean showNotification, Boolean isForced)` | Execution after a map event |
| `ApplyByRemove` | `(Hero victim, Boolean showNotification, Boolean isForced)` | Remove hero from game |
| `ApplyByDeathMark` | `(Hero victim, Boolean showNotification)` | Death from death mark |
| `ApplyByDeathMarkForced` | `(Hero victim, Boolean showNotification)` | Forced death from death mark |
| `ApplyByPlayerIllness` | `()` | Player dies of illness |

### DisableHeroAction
Disables a hero (removes from active play without killing).

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero hero)` | Disable a hero |

### MakeHeroFugitiveAction
Makes a hero a fugitive.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero fugitive, Boolean showNotification)` | Turn hero into a fugitive |

### TeleportHeroAction
Teleports a hero to a settlement or party. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyImmediateTeleportToSettlement` | `(Hero heroToBeMoved, Settlement targetSettlement)` | Instantly teleport hero to settlement |
| `ApplyImmediateTeleportToParty` | `(Hero heroToBeMoved, MobileParty party)` | Instantly teleport hero to party |
| `ApplyImmediateTeleportToPartyAsPartyLeader` | `(Hero heroToBeMoved, MobileParty party)` | Instantly teleport hero to party as its leader |
| `ApplyDelayedTeleportToSettlement` | `(Hero heroToBeMoved, Settlement targetSettlement)` | Delayed teleport to settlement (travel time) |
| `ApplyDelayedTeleportToParty` | `(Hero heroToBeMoved, MobileParty party)` | Delayed teleport to party |
| `ApplyDelayedTeleportToSettlementAsGovernor` | `(Hero heroToBeMoved, Settlement targetSettlement)` | Delayed teleport to settlement as governor |
| `ApplyDelayedTeleportToPartyAsPartyLeader` | `(Hero heroToBeMoved, MobileParty party)` | Delayed teleport to party as leader |

### ChangePlayerCharacterAction
Switches the player character.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero hero)` | Change the player character to a different hero |

### ApplyHeirSelectionAction
Handles heir selection after player death or retirement.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByDeath` | `(Hero heir)` | Select heir after player death |
| `ApplyByRetirement` | `(Hero heir)` | Select heir after player retirement |

### AdoptHeroAction
Adopts a hero into the player's clan.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero adoptedHero)` | Adopt a hero |

### MakePregnantAction
Makes a hero pregnant.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero mother)` | Make a female hero pregnant |

### MarriageAction
Arranges a marriage between two heroes.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero firstHero, Hero secondHero, Boolean showNotification)` | Marry two heroes |

---

## 7. Companion and Relationship

### AddCompanionAction
Adds a companion to a clan. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Clan clan, Hero companion)` | Add a companion hero to a clan |

### RemoveCompanionAction
Removes a companion from a clan.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByFire` | `(Clan clan, Hero companion)` | Fire a companion |
| `ApplyAfterQuest` | `(Clan clan, Hero companion)` | Remove companion after quest completion |
| `ApplyByDeath` | `(Clan clan, Hero companion)` | Remove companion due to death |
| `ApplyByByTurningToLord` | `(Clan clan, Hero companion)` | Remove companion who became a lord |

### ChangeRelationAction
Changes relations between heroes. **High utility for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyPlayerRelation` | `(Hero gainedRelationWith, Int32 relation, Boolean affectRelatives, Boolean showQuickNotification)` | Change player's relation with a hero |
| `ApplyRelationChangeBetweenHeroes` | `(Hero hero, Hero gainedRelationWith, Int32 relationChange, Boolean showQuickNotification)` | Change relation between two heroes |
| `ApplyEmissaryRelation` | `(Hero emissary, Hero gainedRelationWith, Int32 relationChange, Boolean showQuickNotification)` | Change relation via emissary |

### ChangeRomanticStateAction
Changes romantic state between two heroes.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Hero person1, Hero person2, RomanceLevelEnum toWhat)` | Set romance level between two heroes |

---

## 8. Captivity and Prisoners

### TakePrisonerAction
Takes a hero prisoner. **Useful for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(PartyBase capturerParty, Hero prisonerCharacter)` | Take a hero as prisoner |
| `ApplyByTakenFromPartyScreen` | `(FlattenedTroopRoster roster)` | Take prisoners via party screen |

### EndCaptivityAction
Releases a prisoner. **Useful for AI bridge.**

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByReleasedAfterBattle` | `(Hero character)` | Release after battle |
| `ApplyByRansom` | `(Hero character, Hero facilitator)` | Release by ransom payment |
| `ApplyByPeace` | `(Hero character, Hero facilitator)` | Release due to peace treaty |
| `ApplyByEscape` | `(Hero character, Hero facilitator, Boolean showNotification)` | Release by escape |
| `ApplyByDeath` | `(Hero character)` | End captivity due to death |
| `ApplyByReleasedByChoice` | `(FlattenedTroopRoster troopRoster)` | Release prisoners by player choice (roster) |
| `ApplyByReleasedByChoice` | `(Hero character, Hero facilitator)` | Release a specific hero by choice |
| `ApplyByReleasedByCompensation` | `(Hero character)` | Release by compensation |

#### EndCaptivityDetail (enum)
Values: `Ransom`, `ReleasedAfterPeace`, `ReleasedAfterBattle`, `ReleasedAfterEscape`, `ReleasedByChoice`, `Death`, `ReleasedByCompensation`

### TransferPrisonerAction
Transfers a prisoner between parties.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(CharacterObject prisonerTroop, PartyBase prisonerOwnerParty, PartyBase newParty)` | Transfer prisoner from one party to another |

---

## 9. Military and Combat

### StartBattleAction
Initiates combat encounters.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(PartyBase attackerParty, PartyBase defenderParty)` | Generic battle start |
| `ApplyStartBattle` | `(MobileParty attackerParty, MobileParty defenderParty)` | Field battle between mobile parties |
| `ApplyStartRaid` | `(MobileParty attackerParty, Settlement settlement)` | Start raiding a settlement |
| `ApplyStartSallyOut` | `(Settlement settlement, MobileParty defenderParty)` | Sally out from a settlement |
| `ApplyStartAssaultAgainstWalls` | `(MobileParty attackerParty, Settlement settlement)` | Assault settlement walls |

---

## 10. Naval / Ships

### ChangeShipOwnerAction
Transfers ship ownership.

| Method | Signature | Description |
|--------|-----------|-------------|
| `ApplyByTransferring` | `(PartyBase newOwner, Ship ship)` | Transfer ship to new owner |
| `ApplyByTrade` | `(PartyBase newOwner, Ship ship)` | Ship changes owner via trade |
| `ApplyByLooting` | `(PartyBase newOwner, Ship ship)` | Ship seized by looting |
| `ApplyByProduction` | `(PartyBase newOwner, Ship ship)` | New ship from production |
| `ApplyByMobilePartyCreation` | `(PartyBase newOwner, Ship ship)` | Ship assigned to new mobile party |

### DestroyShipAction
Destroys a ship.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Ship ship)` | Destroy a ship (generic) |
| `ApplyByDiscard` | `(Ship ship)` | Discard a ship |

### RepairShipAction
Repairs a ship.

| Method | Signature | Description |
|--------|-----------|-------------|
| `Apply` | `(Ship ship, Settlement repairPort)` | Repair ship at a port (with cost) |
| `ApplyForFree` | `(Ship ship)` | Repair ship for free |
| `ApplyForBanditShip` | `(Ship ship)` | Repair a bandit ship |

### RaftStateChangeAction
Toggles raft mode for a party (river navigation).

| Method | Signature | Description |
|--------|-----------|-------------|
| `ActivateRaftStateForParty` | `(MobileParty mobileParty)` | Put party on a raft |
| `DeactivateRaftStateForParty` | `(MobileParty mobileParty)` | Remove party from raft |

---

## 11. Miscellaneous

### ChangePlayerCharacterAction
See [Character and Hero Actions](#6-character-and-hero-actions).

---

## AI Bridge Priority Actions

The following actions are the most relevant for an AI agent controlling a Bannerlord campaign. They are ordered by expected frequency of use:

| Priority | Action Class | Why |
|----------|-------------|-----|
| **Critical** | `SetPartyAiAction` | Core party movement and targeting |
| **Critical** | `GiveGoldAction` | Economy management |
| **Critical** | `ChangeRelationAction` | Diplomacy with NPCs |
| **Critical** | `EnterSettlementAction` / `LeaveSettlementAction` | Settlement interaction |
| **High** | `DeclareWarAction` / `MakePeaceAction` | War and peace decisions |
| **High** | `ChangeKingdomAction` | Kingdom membership management |
| **High** | `GainRenownAction` | Clan progression |
| **High** | `ChangeClanInfluenceAction` | Political influence management |
| **High** | `TeleportHeroAction` | Hero positioning |
| **High** | `AddHeroToPartyAction` | Party composition |
| **High** | `GatherArmyAction` / `DisbandArmyAction` | Army management |
| **Medium** | `AddCompanionAction` / `RemoveCompanionAction` | Companion roster management |
| **Medium** | `ChangeGovernorAction` | Settlement governor assignment |
| **Medium** | `ChangeOwnerOfSettlementAction` | Settlement ownership |
| **Medium** | `TakePrisonerAction` / `EndCaptivityAction` | Prisoner management |
| **Medium** | `SellPrisonersAction` | Prisoner economy |
| **Medium** | `GiveItemAction` / `SellItemsAction` | Item/equipment management |
| **Medium** | `ChangeOwnerOfWorkshopAction` | Workshop investment |
| **Medium** | `StartBattleAction` | Combat initiation |
| **Medium** | `KillCharacterAction` | Execution decisions |
| **Low** | `MarriageAction` | Dynasty building |
| **Low** | `ChangeCrimeRatingAction` | Crime management |
| **Low** | `ChangeVillageStateAction` | Village state changes |
| **Low** | `ChangeShipOwnerAction` / `RepairShipAction` | Naval management |

---

## Key Types Referenced in Signatures

| Type | Description |
|------|-------------|
| `Hero` | A named character in the game world |
| `Clan` | A clan/family grouping of heroes |
| `Kingdom` | A kingdom faction containing clans |
| `IFaction` | Interface for factions (Kingdom, Clan, etc.) |
| `Settlement` | A map location (town, castle, village) |
| `Town` | A town/castle fortification (subset of Settlement) |
| `MobileParty` | A party moving on the campaign map |
| `PartyBase` | Base class for all party types (mobile and garrison) |
| `Army` | A collection of parties under one leader |
| `Ship` | A naval vessel |
| `Workshop` | A production workshop in a town |
| `WorkshopType` | The type of goods a workshop produces |
| `ItemRosterElement` | An item stack (item + count) |
| `TroopRoster` | A collection of troops |
| `FlattenedTroopRoster` | Flattened troop roster (individual entries) |
| `CampaignTime` | A time value in the campaign calendar |
| `CampaignVec2` | A 2D position on the campaign map |
| `IMapPoint` | Interface for any point on the map |
| `NavigationType` | Enum for land/sea navigation mode |
| `CharacterObject` | Template/definition for a character (troop type) |
| `RomanceLevelEnum` | Romance state between two heroes |
| `PaymentMethod` | How to pay for crimes (gold, influence, etc.) |
| `SiegeAftermath` | Post-siege policy (devastate, pillage, show mercy) |
