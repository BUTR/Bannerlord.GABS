using Lib.GAB.Events;

using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles hero events: lifecycle, progression, quests, companions, relations, family, and romance.
/// </summary>
public class HeroEventBehavior : BridgeEventBehaviorBase
{
    public HeroEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        // Hero lifecycle
        Events.RegisterChannel("campaign/hero_killed", "Notable hero death");
        Events.RegisterChannel("campaign/hero_wounded", "Hero wounded");
        Events.RegisterChannel("campaign/hero_prisoner_taken", "Hero captured");
        Events.RegisterChannel("campaign/hero_prisoner_released", "Hero released from captivity");
        Events.RegisterChannel("campaign/hero_created", "New hero appeared in world");
        Events.RegisterChannel("campaign/hero_comes_of_age", "Child hero becomes adult");
        Events.RegisterChannel("campaign/hero_changed_clan", "Hero switched clans");

        // Player progression
        Events.RegisterChannel("campaign/clan_tier_changed", "Clan tier milestone reached");
        Events.RegisterChannel("campaign/renown_gained", "Renown earned");
        Events.RegisterChannel("campaign/hero_levelled_up", "Hero level up");
        Events.RegisterChannel("campaign/perk_opened", "Perk selected");
        Events.RegisterChannel("campaign/player_trait_changed", "Player personality trait changed");

        // Family & romance
        Events.RegisterChannel("campaign/marriage", "Heroes married");
        Events.RegisterChannel("campaign/romantic_state_changed", "Romance progressed between heroes");
        Events.RegisterChannel("campaign/child_conceived", "Pregnancy started");
        Events.RegisterChannel("campaign/child_born", "Birth event");

        // Companions & relations
        Events.RegisterChannel("campaign/companion_added", "New companion joined");
        Events.RegisterChannel("campaign/companion_removed", "Companion left the party");
        Events.RegisterChannel("campaign/relation_changed", "Relation with hero changed");
    }

    public override void RegisterEvents()
    {
        // Hero lifecycle
        CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
        CampaignEvents.HeroWounded.AddNonSerializedListener(this, OnHeroWounded);
        CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
        CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
        CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
        CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
        CampaignEvents.OnHeroChangedClanEvent.AddNonSerializedListener(this, OnHeroChangedClan);

        // Player progression
        CampaignEvents.ClanTierIncrease.AddNonSerializedListener(this, OnClanTierChanged);
        CampaignEvents.RenownGained.AddNonSerializedListener(this, OnRenownGained);
        CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnHeroLevelledUp);
        CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkOpened);
        CampaignEvents.PlayerTraitChangedEvent.AddNonSerializedListener(this, OnPlayerTraitChanged);

        // Family & romance
#if v1313 || v1315
            CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnMarriage);
#else
        CampaignEvents.HeroesMarried.AddNonSerializedListener(this, OnMarriage);
#endif
        CampaignEvents.RomanticStateChanged.AddNonSerializedListener(this, OnRomanticStateChanged);
        CampaignEvents.OnChildConceivedEvent.AddNonSerializedListener(this, OnChildConceived);
        CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);

        // Companions & relations
        CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnCompanionAdded);
        CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
        CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnRelationChanged);
    }

    // --- Hero Lifecycle ---

    private void OnHeroKilled(Hero? victim, Hero? killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
    {
        Emit("campaign/hero_killed", new
        {
            victim = victim?.Name?.ToString(),
            victimFaction = victim?.MapFaction?.Name?.ToString(),
            killer = killer?.Name?.ToString(),
            killerFaction = killer?.MapFaction?.Name?.ToString(),
            reason = detail.ToString(),
        });
    }

    private void OnHeroWounded(Hero? hero)
    {
        Emit("campaign/hero_wounded", new
        {
            hero = hero?.Name?.ToString(),
            faction = hero?.MapFaction?.Name?.ToString(),
            isPlayer = hero == Hero.MainHero,
        });
    }

    private void OnHeroPrisonerTaken(PartyBase? capturer, Hero? prisoner)
    {
        Emit("campaign/hero_prisoner_taken", new
        {
            prisoner = prisoner?.Name?.ToString(),
            prisonerFaction = prisoner?.MapFaction?.Name?.ToString(),
            capturer = capturer?.Name?.ToString(),
            capturerFaction = capturer?.MapFaction?.Name?.ToString(),
            isPlayer = prisoner == Hero.MainHero,
        });
    }

#if v1313 || v1315
        private void OnHeroPrisonerReleased(Hero? prisoner, PartyBase? party, IFaction? capturerFaction, EndCaptivityDetail detail, bool showNotification)
#else
    private void OnHeroPrisonerReleased(Hero? prisoner, PartyBase? party, IFaction? capturerFaction, EndCaptivityDetail detail)
#endif
    {
        Emit("campaign/hero_prisoner_released", new
        {
            prisoner = prisoner?.Name?.ToString(),
            faction = prisoner?.MapFaction?.Name?.ToString(),
            releasedFrom = capturerFaction?.Name?.ToString(),
            reason = detail.ToString(),
            isPlayer = prisoner == Hero.MainHero,
        });
    }

    private void OnHeroCreated(Hero? hero, bool isBornNaturally)
    {
        Emit("campaign/hero_created", new
        {
            hero = hero?.Name?.ToString(),
            clan = hero?.Clan?.Name?.ToString(),
            culture = hero?.Culture?.Name?.ToString(),
            isBornNaturally,
        });
    }

    private void OnHeroComesOfAge(Hero? hero)
    {
        Emit("campaign/hero_comes_of_age", new
        {
            hero = hero?.Name?.ToString(),
            clan = hero?.Clan?.Name?.ToString(),
            isPlayerClan = hero?.Clan == Clan.PlayerClan,
        });
    }

    private void OnHeroChangedClan(Hero? hero, Clan? oldClan)
    {
        Emit("campaign/hero_changed_clan", new
        {
            hero = hero?.Name?.ToString(),
            oldClan = oldClan?.Name?.ToString(),
            newClan = hero?.Clan?.Name?.ToString(),
        });
    }

    // --- Player Progression ---

    private void OnClanTierChanged(Clan? clan, bool shouldNotify)
    {
        Emit("campaign/clan_tier_changed", new
        {
            clan = clan?.Name?.ToString(),
            tier = clan?.Tier ?? 0,
            isPlayer = clan == Clan.PlayerClan,
        });
    }

    private void OnRenownGained(Hero? hero, int gainedRenown, bool doNotNotify)
    {
        if (hero?.Clan != Clan.PlayerClan) return;

        Emit("campaign/renown_gained", new
        {
            hero = hero.Name?.ToString(),
            gained = gainedRenown,
            totalRenown = hero.Clan?.Renown ?? 0,
        });
    }

    private void OnHeroLevelledUp(Hero? hero, bool shouldNotify)
    {
        if (hero?.Clan != Clan.PlayerClan && hero != Hero.MainHero) return;

        Emit("campaign/hero_levelled_up", new
        {
            hero = hero.Name?.ToString(),
            level = hero.Level,
            isPlayer = hero == Hero.MainHero,
        });
    }

    private void OnPerkOpened(Hero? hero, PerkObject? perk)
    {
        if (hero?.Clan != Clan.PlayerClan && hero != Hero.MainHero) return;

        Emit("campaign/perk_opened", new
        {
            hero = hero.Name?.ToString(),
            perk = perk?.Name?.ToString(),
            skill = perk?.Skill?.Name?.ToString(),
            isPlayer = hero == Hero.MainHero,
        });
    }

    private void OnPlayerTraitChanged(TraitObject? trait, int previousLevel)
    {
        Emit("campaign/player_trait_changed", new
        {
            trait = trait?.Name?.ToString(),
            previousLevel,
            newLevel = Hero.MainHero?.GetTraitLevel(trait) ?? 0,
        });
    }

    // --- Family & Romance ---

    private void OnMarriage(Hero? hero1, Hero? hero2, bool showNotification)
    {
        Emit("campaign/marriage", new
        {
            hero1 = hero1?.Name?.ToString(),
            hero2 = hero2?.Name?.ToString(),
            involvesPlayer = hero1 == Hero.MainHero || hero2 == Hero.MainHero,
        });
    }

    private void OnRomanticStateChanged(Hero? hero1, Hero? hero2, Romance.RomanceLevelEnum level)
    {
        Emit("campaign/romantic_state_changed", new
        {
            hero1 = hero1?.Name?.ToString(),
            hero2 = hero2?.Name?.ToString(),
            level = level.ToString(),
            involvesPlayer = hero1 == Hero.MainHero || hero2 == Hero.MainHero,
        });
    }

    private void OnChildConceived(Hero? mother)
    {
        Emit("campaign/child_conceived", new
        {
            mother = mother?.Name?.ToString(),
            father = mother?.Spouse?.Name?.ToString(),
            isPlayerFamily = mother?.Clan == Clan.PlayerClan,
        });
    }

    private void OnGivenBirth(Hero? mother, List<Hero?>? aliveChildren, int stillbornCount)
    {
        Emit("campaign/child_born", new
        {
            mother = mother?.Name?.ToString(),
            father = mother?.Spouse?.Name?.ToString(),
            children = aliveChildren?.Select(c => c?.Name?.ToString()).ToList(),
            stillbornCount,
            isPlayerFamily = mother?.Clan == Clan.PlayerClan,
        });
    }

    // --- Companions & Relations ---

    private void OnCompanionAdded(Hero? companion)
    {
        Emit("campaign/companion_added", new
        {
            name = companion?.Name?.ToString(),
            culture = companion?.Culture?.Name?.ToString(),
        });
    }

    private void OnCompanionRemoved(Hero? companion, RemoveCompanionAction.RemoveCompanionDetail detail)
    {
        Emit("campaign/companion_removed", new
        {
            name = companion?.Name?.ToString(),
            reason = detail.ToString(),
        });
    }

    private void OnRelationChanged(Hero? hero1, Hero? hero2, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero? effectorHero, Hero? effectOnHero)
    {
        if (hero1 != Hero.MainHero && hero2 != Hero.MainHero)
            return;

        var otherHero = hero1 == Hero.MainHero ? hero2 : hero1;
        Emit("campaign/relation_changed", new
        {
            hero = otherHero?.Name?.ToString(),
            change = relationChange,
            newRelation = otherHero != null ? Hero.MainHero.GetRelation(otherHero) : 0,
            reason = detail.ToString(),
        });
    }
}