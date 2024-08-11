using Cysharp.Threading.Tasks;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Common.Dialog;
using Dimps.Application.Common.UI;
using Dimps.Application.Global;
using Dimps.Application.MasterData;
using Dimps.Application.MasterData.Types;
using Dimps.Application.Scene;
using Dimps.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GVNC.Application.Trade
{
    public class TradeFilterMenu : SceneBase<TradeFilterMenu.Param>
    {
        public class Param : ISceneParameter
        {
            public CardListRule rules = null;  
            public Action<CardListRule> close = null;
            public string titleId;
            public Param(CardListRule rules, string titleId, Action<CardListRule> close = null)
            {
                this.rules = rules;
                this.titleId = titleId;
                this.close = close;
            }
        }

        public enum Type
        {
            None = -1,
            Possession,
            Storage,
            Album,
            Upgrade
        }

        [SerializeField] private UILocalizeText tmp_Title;

        [SerializeField] private UICustomButton cancelButton = null;
        [SerializeField] private UICustomButton okButton = null;
        [SerializeField] private UICustomButton resetButton = null;
        
        [Header("Player Name Filter")]
        [SerializeField] private UICustomButton filterPlayerButton = null;
        [SerializeField] private GameObject[] playerNameLabel = null;
        [SerializeField] private TextMeshProUGUI playerName = null;
        [SerializeField] private TextMeshProUGUI playerName_FilterOn = null;
        [SerializeField] private UICustomScrollButton playerNameSelectAllToggle = null;

        [Header("Filter Menu")]        
        [SerializeField] private GameObject filterRarityToggleGroup = null;
        [SerializeField] private GameObject filterPositionToggleGroup = null;
        [SerializeField] private GameObject filterTeamNoToggleGroup = null;
        [SerializeField] private GameObject filterSkillTypeToggleGroup = null;
        [SerializeField] private UICustomScrollButton[] filterSelectAllToggles = null;
        [SerializeField] private ScrollRect filterScrollRect = null;

        [Header("Content Object")]
        [SerializeField] private GameObject TeamGridGroup = null;

        private int filterPlayerId = -1;
        private UICustomScrollButton[] filterRarityToggles = null;
        private UICustomScrollButton[] filterPositionToggles = null;
        private UICustomScrollButton[] filterTeamNoToggles = null;
        private UICustomScrollButton[] filterSkillTypeToggles = null;


        private Action<CardListRule> close = null;     
        private CardListRule resultCardListRule = new();    
        private HashSet<int> playerIdSet = null;


        private Dictionary<int, Dictionary<int, UICustomScrollButton>> filterButtonDic = new();

        /// <summary>本メニュー設定できるフィルタルール</summary>
        private Dictionary<int, IList> filterRules = new Dictionary<int, IList>()
        {
            {(int)FilterRule.Rarity,
                new List<Rarity>(){
                    Rarity.Common,      // filterRarityToggles[0]
                    Rarity.UnCommon,    // filterRarityToggles[1]
                    Rarity.Rare,        // filterRarityToggles[2]
                    Rarity.UltraRare,   // filterRarityToggles[3]
                    Rarity.Epic,        // filterRarityToggles[4]
                    Rarity.Epic_secret, // filterRarityToggles[5]
                    Rarity.Legendary    // filterRarityToggles[6]
                }
            },
            {(int)FilterRule.Position,
                new List<PlayerPosition>(){
                    PlayerPosition.PG,  // filterPositionToggles[0]
                    PlayerPosition.SG,  // filterPositionToggles[1]
                    PlayerPosition.SF,  // filterPositionToggles[2]
                    PlayerPosition.PF,  // filterPositionToggles[3]
                    PlayerPosition.C    // filterPositionToggles[4]
                }
            },
            {(int)FilterRule.TeamNo,
                new List<TeamNo>(){
                        TeamNo.Boston_Celtics,          // filterTeamNoToggles[0]
                        TeamNo.Brooklyn_Nets,           // filterTeamNoToggles[1]
                        TeamNo.NewYork_Knicks,          // filterTeamNoToggles[2]
                        TeamNo.Philadelphia_76ers,      // filterTeamNoToggles[3]
                        TeamNo.Toronto_Raptors,         // filterTeamNoToggles[4]
                        TeamNo.Chicago_Bulls,           // filterTeamNoToggles[5]
                        TeamNo.Cleveland_Cavaliers,     // filterTeamNoToggles[6]
                        TeamNo.Detroit_Pistons,         // filterTeamNoToggles[7]
                        TeamNo.Indiana_Pacers,          // filterTeamNoToggles[8]
                        TeamNo.Milwaukee_Bucks,         // filterTeamNoToggles[9]
                        TeamNo.Atlanta_Hawks,           // filterTeamNoToggles[10]
                        TeamNo.Charlotte_Hornets,       // filterTeamNoToggles[11]
                        TeamNo.Miami_Heat,              // filterTeamNoToggles[12]
                        TeamNo.Orlando_Magic,           // filterTeamNoToggles[13]
                        TeamNo.Washington_Wizards,      // filterTeamNoToggles[14]
                        TeamNo.Dallas_Mavericks,        // filterTeamNoToggles[15]
                        TeamNo.Houston_Rockets,         // filterTeamNoToggles[16]
                        TeamNo.Memphis_Grizzlies,       // filterTeamNoToggles[17]
                        TeamNo.NewOrleans_Pelicans,     // filterTeamNoToggles[18]
                        TeamNo.SanAntonio_Spurs,        // filterTeamNoToggles[19]
                        TeamNo.Denver_Nuggets,          // filterTeamNoToggles[20]
                        TeamNo.Minnesota_Timberwolves,  // filterTeamNoToggles[21]
                        TeamNo.OklahomaCity_Thunder,    // filterTeamNoToggles[22]
                        TeamNo.PortlandTrail_Blazers,   // filterTeamNoToggles[23]
                        TeamNo.Utah_Jazz,               // filterTeamNoToggles[24]
                        TeamNo.GoldenState_Warriors,    // filterTeamNoToggles[25]
                        TeamNo.LosAngeles_Clippers,     // filterTeamNoToggles[26]
                        TeamNo.LosAngeles_Lakers,       // filterTeamNoToggles[27]
                        TeamNo.Phoenix_Suns,            // filterTeamNoToggles[28]
                        TeamNo.Sacramento_Kings,        // filterTeamNoToggles[29]
                }
            },
            {(int)FilterRule.SkillType,
                new List<SkillIconType>(){
                    SkillIconType.ClutchPass,
                    SkillIconType.ClutchDrive,
                    SkillIconType.ClutchShoot,
                    SkillIconType.ClutchOffIntercept,
                    SkillIconType.ClutchSteal,
                    SkillIconType.ClutchBlock,
                    SkillIconType.ClutchPassCut,
                    SkillIconType.ClutchDefIntercept,
                    SkillIconType.PassiveBuff,
                    SkillIconType.ActiveBuff,
                    SkillIconType.PassiveDeBuff,
                    SkillIconType.ActiveDeBuff,
                    SkillIconType.Reward,
                }
            }
        };

        public static void Open(Param param)
        {
            SceneProvider.Instance.AddScene(SceneContent.Type.TradeFilterMenu, param);
        }

        protected override Param OnRootStart()
        {
            var param = new Param(new CardListRule(SortRule.Name, true), string.Empty);
            return param;
        }


        protected override void OnInitialize(Param param)
        {
            tmp_Title.SetLocalizeText(param.titleId);

            filterRarityToggles = filterRarityToggleGroup.GetComponentsInChildren<UICustomScrollButton>();
            filterPositionToggles = filterPositionToggleGroup.GetComponentsInChildren<UICustomScrollButton>();
            filterTeamNoToggles = filterTeamNoToggleGroup.GetComponentsInChildren<UICustomScrollButton>();
            filterSkillTypeToggles = filterSkillTypeToggleGroup.GetComponentsInChildren<UICustomScrollButton>();

            filterRarityToggles[(int)Rarity.Epic_secret].gameObject.SetActive(false);

            // ボタン登録
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() => CloseDialog());
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(() => SendRules());
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(() => ResetButtonState());
            
            filterPlayerButton.onClick.RemoveAllListeners();
            filterPlayerButton.onClick.AddListener(() => { OpenPlayerNameList(); });
            playerNameSelectAllToggle.onClick.RemoveAllListeners();
            playerNameSelectAllToggle.onClick.AddListener(() => {
                SetPlayerNameLabel(-1);
            });


            foreach (var (rule, elementList) in filterRules)
            {
                Dictionary<int, UICustomScrollButton> tmpDictionary = new();
                switch (rule)
                {
                    case (int)FilterRule.Rarity:
                        for (int i = 0; i < elementList.Count; i++)
                        {
                            tmpDictionary.Add((int)elementList[i], filterRarityToggles[i]);
                        }
                        break;
                    case (int)FilterRule.Position:
                        for (int i = 0; i < elementList.Count; i++)
                        {
                            tmpDictionary.Add((int)elementList[i], filterPositionToggles[i]);
                        }
                        break;
                    case (int)FilterRule.TeamNo:
                        for (int i = 0; i < elementList.Count; i++)
                        {
                            tmpDictionary.Add((int)elementList[i], filterTeamNoToggles[i]);
                        }
                        break;
                    case (int)FilterRule.SkillType:
                        for (int i = 0; i < elementList.Count; i++)
                        {
                            tmpDictionary.Add((int)elementList[i], filterSkillTypeToggles[i]);
                        }
                        break;
                    default:
                        DebugTool.LogError($"unknown {nameof(FilterRule)}:{(FilterRule)rule}");
                        break;
                }
                filterButtonDic.Add(rule, tmpDictionary);
            }

            foreach (var rule in filterButtonDic.Keys)
            {
                filterSelectAllToggles[rule].onClick.RemoveAllListeners();
                filterSelectAllToggles[rule].onClick.AddListener(() => SelectFilterAll((FilterRule)rule, filterSelectAllToggles[rule]));
            }

            foreach (var (rule, toggles) in filterButtonDic)
            {
                foreach (var toggle in toggles.Values)
                {
                    toggle.onClick.AddListener(() => CheckFilterState((FilterRule)rule));
                }
            }

            foreach (var (rule, list) in param.rules.FilterRules)
            {
                if (!filterRules.Keys.Contains(rule) && rule != (int)FilterRule.Player)
                {
                    resultCardListRule.SetFilterRule((FilterRule)rule, list);
                }
            }

            close = param.close;

            RulesToButtonState(param.rules);
        }

        protected override async UniTask OnLoad(CancellationToken token)
        {
            await UniTask.Yield();
        }

        protected override async UniTask OnActivate(CancellationToken token)
        {
            await UniTask.Yield();
        }


        protected override void OnExecute(Param param)
        {
            GetPlayerIdList();
        }


        protected override void OnDispose()
        {

        }

        private void GetPlayerIdList()
        {
            var userDisp = MasterDataManager.Instance.PlayerCardMaster.GetPlayerCardDispListExceptTradeBlock();
            playerIdSet = userDisp.Values.Select(value => value.PersonalId).ToHashSet();            
            TeamGridGroup.SetActive(false);
        }


        private void ButtonStateToRules()
        {
            if (filterPlayerId > 0)
            {
                resultCardListRule.SetFilterRule(FilterRule.Player, new List<int> { filterPlayerId });
            }
            foreach (var (rule, dic) in filterButtonDic)
            {
                List<int> tmpElements = new List<int>();
                foreach (var (element, button) in dic)
                {
                    if (button.Toggle)
                    {
                        tmpElements.Add(element);
                    }
                }

                resultCardListRule.SetFilterRule((FilterRule)rule, tmpElements);
            }
        }

        private void RulesToButtonState(CardListRule rules)
        {
            ResetButtonState();               
            if (rules.FilterRules.TryGetValue((int)FilterRule.Player, out var idList))
            {
                if (idList != null && idList.Count > 0)
                    SetPlayerNameLabel(idList[0]);
            }
            foreach (var (rule, dic) in filterButtonDic)
            {
                if (rules.FilterRules.TryGetValue(rule, out var elements))
                {
                    foreach (var element in elements)
                    {
                        dic[element].Toggle = true;
                    }
                    CheckFilterState((FilterRule)rule);
                }
            }
        }


        private void ResetButtonState()
        {            
            ResetFilterButtonState();
        }


        private void ResetFilterButtonState()
        {
            SetPlayerNameLabel(-1);
            foreach (var (rule, dic) in filterButtonDic)
            {
                foreach (var toggle in dic.Values)
                {
                    toggle.Toggle = false;
                }
                CheckFilterState((FilterRule)rule);
            }
        }

        private void SelectFilterAll(FilterRule rule, UICustomScrollButton button)
        {
            if (filterButtonDic.TryGetValue((int)rule, out var dic))
            {
                foreach (var toggle in dic.Values)
                {
                    toggle.Toggle = button.Toggle;
                }
            }
        }


        private void CheckFilterState(FilterRule rule)
        {
            if (filterButtonDic.TryGetValue((int)rule, out var dic))
            {
                filterSelectAllToggles[(int)rule].Toggle = dic.All(data => data.Value.Toggle);
            }
        }

        private void OpenPlayerNameList()
        {            
            PlayerNameList.Open(new PlayerNameList.Param(
                playerIdSet, filterPlayerId, (value) => { SetPlayerNameLabel(value); }));
        }

        private void SetPlayerNameLabel(int id)
        {
            filterPlayerId = id;
            playerNameLabel[0].gameObject.SetActive(id < 0);  
            playerNameLabel[1].gameObject.SetActive(id >= 0);  
            playerNameSelectAllToggle.Toggle = id < 0;       
            playerNameSelectAllToggle.enabled = id >= 0;        
            if (id >= 0)
            {
                if (MasterDataManager.Instance.PlayerPersonalMaster.TryGetPlayerPersonalEntity(id, out var entity))
                {
                    playerName.text = LanguageManager.Instance.GetOSTText(entity.H32_PlayerNameId);
                    playerName_FilterOn.text = LanguageManager.Instance.GetOSTText(entity.H32_PlayerNameId);
                }
            }
        }

        private void SendRules()
        {
            ButtonStateToRules();
            close?.Invoke(resultCardListRule);
            CloseDialog();
        }
    }

}

