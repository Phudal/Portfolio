using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EBroadcastKey
{
    ConsecPurchaseAnim,
    
    // Quest
    QuestDetailItemSelected,

    // Badge
    UserBadgeRefresh,
    OpenBadgePopup,    

    // Shop
    ShopPopupExtendClicked,
    ShopPopupItemClicked,
    PlayShopPopupOpenAnim,
    ProductPurchaseSuccess,

    // Guild
    SelectGuildLogoItem,
    RefreshModifiedGuildInfo,
    SelectGuildMemberList,
    SelectGuildApplicantList,
    GuildApplicantConfirm,
    GuildApplicantReject,
    OnGuildMembersUpdate,
    GuildMemberGradeModified,
    GuildApplyCancelConfirm,

    OnGuildMemberProfileSelected,
    OnGuildApplicantProfileSelected,

    // Exchange
    OnExchangeConditionPlayerSelected,
    OnExchangeConditionPlayerRefresh,
    OnExchangeConditionItemSelected,
    OnExchangeConditionItemRefresh,
    ExchangePopupRefresh,
    OnExchangeRegisterAtExpired,

    // Search
    PlayerFilterSuccess,
    OnFilterTeamSelected,    

    // SeasonPass
    OnSeasonPassItemSelected,
    OnSeasonPassRewardGetSuccess,
    OnSeasonPassExpired,
    OnSeasonPassBalloonShow,
    OnSeasonPassBalloonHide,

    // TeamEdit
    OnPlayerChangeSelected,
    OnPlayerChangeLongSelected,
    OnPlayerChangeLongSelectEnd,

    // Inventory(= Trunk)
    OnInventoryItemUseClicked,

    // Socket
    SOCKET_OnChatMessageRecv,   

    MAX
}