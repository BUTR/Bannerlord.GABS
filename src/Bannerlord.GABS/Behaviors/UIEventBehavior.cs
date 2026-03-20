using Bannerlord.GABS.Patches;

using Lib.GAB.Events;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles UI events: inquiries, conversations, menus, barter, persuasion, and incidents.
/// </summary>
public class UIEventBehavior : BridgeEventBehaviorBase
{
    private bool _hadIncident;

    public UIEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        // Conversations & menus
        Events.RegisterChannel("campaign/conversation_ended", "Conversation ended");
        Events.RegisterChannel("campaign/menu_opened", "Game menu opened");
        Events.RegisterChannel("campaign/inquiry_shown", "Popup inquiry/dialog appeared");

        // Barter
        Events.RegisterChannel("campaign/barter_accepted", "Barter deal accepted");
        Events.RegisterChannel("campaign/barter_canceled", "Barter deal rejected/canceled");

        // Persuasion
        Events.RegisterChannel("campaign/persuasion_progress", "Persuasion check result committed");

        // Random events
        Events.RegisterChannel("campaign/incident_resolved", "Random encounter/incident resolved");
    }

    public override void RegisterEvents()
    {
        // Conversations & menus
        CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);

        // Inquiry events (static hooks)
        InformationManager.OnShowInquiry += OnInquiryShown;
        MBInformationManager.OnShowMultiSelectionInquiry += OnMultiSelectionInquiryShown;

        // Barter
        CampaignEvents.OnBarterAcceptedEvent.AddNonSerializedListener(this, OnBarterAccepted);
        CampaignEvents.OnBarterCanceledEvent.AddNonSerializedListener(this, OnBarterCanceled);

        // Persuasion
        CampaignEvents.PersuasionProgressCommittedEvent.AddNonSerializedListener(this, OnPersuasionProgress);

#if v1313 || v1315
            // Random events (v1.3.x+)
            CampaignEvents.OnIncidentResolvedEvent.AddNonSerializedListener(this, OnIncidentResolved);
#endif

        // Tick-based incident detection (random event popups tracked via Harmony patches)
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
    }

    private void OnTick(float dt)
    {
        var hasIncident = InquiryState.CurrentIncident != null;
        if (hasIncident && !_hadIncident)
        {
#if v1313 || v1315
                var incident = InquiryState.CurrentIncident;
                Emit("campaign/inquiry_shown", new
                {
                    type = "incident",
                    title = incident?.Title?.ToString(),
                    description = incident?.Description?.ToString(),
                });
#else
            Emit("campaign/inquiry_shown", new
            {
                type = "incident",
            });
#endif
        }
        _hadIncident = hasIncident;
    }

    // --- Conversations & Menus ---

    private void OnConversationEnded(IEnumerable<CharacterObject?>? characters)
    {
        var names = characters?.Select(c => c?.Name?.ToString()).Where(n => n != null).ToList();
        Emit("campaign/conversation_ended", new
        {
            characters = names,
        });
    }

    private void OnGameMenuOpened(MenuCallbackArgs? args)
    {
        Emit("campaign/menu_opened", new
        {
            menuId = args?.MenuContext?.GameMenu?.StringId,
            text = args?.MenuTitle?.ToString(),
        });
    }

    private void OnInquiryShown(InquiryData data, bool pauseGame, bool prioritize)
    {
        Emit("campaign/inquiry_shown", new
        {
            type = "yes_no",
            title = data.TitleText,
            text = data.Text,
            affirmativeText = data.AffirmativeText,
            negativeText = data.NegativeText,
        });
    }

    private void OnMultiSelectionInquiryShown(MultiSelectionInquiryData data, bool pauseGame, bool prioritize)
    {
        Emit("campaign/inquiry_shown", new
        {
            type = "multi_selection",
            title = data.TitleText,
            text = data.DescriptionText,
            options = data.InquiryElements?.Select(e => e.Title).ToList(),
        });
    }

    // --- Barter ---

    private void OnBarterAccepted(Hero? offererHero, Hero? otherHero, List<Barterable?>? barters)
    {
        Emit("campaign/barter_accepted", new
        {
            offerer = offererHero?.Name?.ToString(),
            other = otherHero?.Name?.ToString(),
            items = barters?.Select(b => b?.GetType().Name).ToList(),
            involvesPlayer = offererHero == Hero.MainHero || otherHero == Hero.MainHero,
        });
    }

    private void OnBarterCanceled(Hero? offererHero, Hero? otherHero, List<Barterable?>? barters)
    {
        Emit("campaign/barter_canceled", new
        {
            offerer = offererHero?.Name?.ToString(),
            other = otherHero?.Name?.ToString(),
            involvesPlayer = offererHero == Hero.MainHero || otherHero == Hero.MainHero,
        });
    }

    // --- Persuasion ---

    private void OnPersuasionProgress(Tuple<PersuasionOptionArgs, PersuasionOptionResult>? progress)
    {
        Emit("campaign/persuasion_progress", new
        {
            result = progress?.Item2.ToString(),
        });
    }

    // --- Random Events ---

#if v1313 || v1315
        private void OnIncidentResolved(TaleWorlds.CampaignSystem.Incidents.Incident? incident)
        {
            Emit("campaign/incident_resolved", new
            {
                title = incident?.Title?.ToString(),
                description = incident?.Description?.ToString(),
            });
        }
#endif
}