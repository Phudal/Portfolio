#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System;
using System.Linq;

public class PlayerInfoViewerOdin : OdinMenuEditorWindow
{
    [MenuItem("GameManagement/PlayerInfoViewerOdin")]
    public static void OpenWindow()
    {
        GetWindow<PlayerInfoViewerOdin>().Show();
    }

    protected override OdinMenuTree BuildMenuTree()
    {       
        OdinMenuTree tree = new OdinMenuTree();

        tree.Selection.SupportsMultiSelect = false;

        if (!Application.isPlaying)
        {
            tree.Add("Info", new v_Info());
            return tree;
        }

        tree.Add("Player HP", new v_PlayerHP());
        tree.Add("Player Equipment", new v_PlayerEquipment());
        tree.Add("Player Pay", new v_PlayerPay());
        tree.Add("Player Stat", new v_PlayerStat());
        tree.Add("Character Stat", new v_CharacterStat());        
        return tree;
    }

    public class v_Info
    {
        [ReadOnly]
        public string Info = "You Can't View This, If You Don't Playing";
    }

    public class v_PlayerHP
    {
        public double TotalHP = 0.0f;

        public double CurrentHp = 0.0f;

        public v_PlayerHP()
        {
            if (UserDataManager.Instance.PlayerCharacter != null)
                UpdatePlayer();
        }

        [Button(ButtonSizes.Large)]
        public void UpdatePlayer()
        {
            if (UserDataManager.Instance.PlayerCharacter == null)
                return;

            TotalHP = UserDataManager.Instance.PlayerCharacter.CharacterParam.GetResultTotalCharacterParam(ECharacterParam.HP_I);
            CurrentHp = UserDataManager.Instance.PlayerCharacter.CharacterParam.CurrentHP;
        }

    }

    public class v_PlayerEquipment
    {
        private List<EquipedItemData> EquipedItemDataList = new List<EquipedItemData>();

        [TableList(AlwaysExpanded = true, DrawScrollView = false)]
        public List<v_PlayerEquipmentItem> PlayerEquipmentItemList = new List<v_PlayerEquipmentItem>();

        public v_PlayerEquipment()
        {
            if(UserDataManager.Instance.PlayerCharacter == null)
                return;

            UpdatePlayerEquipData();
        }

        [Button(ButtonSizes.Large)]
        public void UpdatePlayerEquipData()
        {
            if (UserDataManager.Instance.PlayerCharacter == null)
                return;

            PlayerEquipmentItemList.Clear();

            EquipedItemDataList = UserDataManager.Instance.PlayerCharacter.EquipedItemDataList;

            for (int i = 0; i < EquipedItemDataList.Count; i++)
            {
                PlayerEquipmentItemList.Add(new v_PlayerEquipmentItem(EquipedItemDataList[i].ItemIndex));
            }
        }
        
    }


    [Serializable]
    public class v_PlayerEquipmentItem
    {
        public EItemType ItemType;

        public int ItemIndex;

        [TextArea]
        public string ItemName;

        public v_PlayerEquipmentItem(int index)
        {
            ItemIndex = index;
            ItemName = TableManager.Instance.GetStringTableValue(ItemTable.Instance.m_ItemSheet.GetRecord(index).NameCode);
            ItemType = (EItemType)ItemTable.Instance.m_ItemSheet.GetRecord(index).ItemType;            
        }
    }


    public class v_PlayerPay
    {
        [TableList(AlwaysExpanded = true, DrawScrollView = false)]
        public List<v_PlayerPaytItem> PlayerPayDataList = new List<v_PlayerPaytItem>();

        public v_PlayerPay()
        {
            if (UserDataManager.Instance.PlayerCharacter == null)
                return;

            UpdatePlayerPayData();
        }

        [Button(ButtonSizes.Large)]
        public void UpdatePlayerPayData()
        {
            if (UserDataManager.Instance.PlayerCharacter == null)
                return;

            PlayerPayDataList.Clear();

            for (int currentPayType = 1; currentPayType < (int)EPayType.Max; currentPayType++)
            {
                // 비어있는 payType은 건너뛴다.
                if (currentPayType.ToString() == ((EPayType)currentPayType).ToString())
                    continue;

                PlayerPayDataList.Add(new v_PlayerPaytItem((EPayType)currentPayType));
            }
        }

    }


    [Serializable]
    public class v_PlayerPaytItem
    {
        public EPayType PayType;

        public int PayIndex;

        [TextArea]
        public string PayName;

        [TextArea]
        public string PayCount;

        public v_PlayerPaytItem(EPayType payType)
        {
            if (UserDataManager.Instance.UserData == null)
                return;

            PayCount = UserDataManager.Instance.UserData.GetPayValue(payType).ToString();

            PayIndex = (int)payType;
            PayName = TableManager.Instance.GetStringTableValue(ItemTable.Instance.m_PaySheet.GetRecord(PayIndex).NameCode);
            PayType = payType;
        }
    }

    [Serializable]
    public class v_PlayerStat
    {        
        [ShowInInspector]
        [TableMatrix(HorizontalTitle = "Reason Param", VerticalTitle = "Character Param", IsReadOnly = true)]        
        public string[,] CharacterStatMatrix; 

        public v_PlayerStat()
        {
            CharacterStatMatrix = new string[(int)EReasonParam.Max + 2, (int)ECharacterParam.Max + 2];

            if (UserDataManager.Instance.PlayerCharacter == null)
                return;

            UpdatePlayerStatData();
        }

        [Button(ButtonSizes.Large)]
        public void UpdatePlayerStatData()
        {
            if (UserDataManager.Instance.PlayerCharacter == null)
                return;

            CharacterStatMatrix = new string[(int)EReasonParam.Max + 2, (int)ECharacterParam.Max + 2];

            List<ReasonCharacterParam> reasonList =
                UserDataManager.Instance.PlayerCharacter.CharacterParam.ReasonParamMap.Values.ToList();

            for (int currentReason = 1; currentReason < (int)EReasonParam.Max; currentReason++)
            {
                for (int currentParam = 1; currentParam < (int)ECharacterParam.Max; currentParam++)
                {
                    CharacterStatMatrix[currentReason + 1, currentParam] = reasonList[currentReason - 1].CharParam[(ECharacterParam)currentParam].ToString();
                }
            }

            for (int i = 1; i < (int)ECharacterParam.Max; i++)
            {
                EnumStringRecord enumStringRecord = TableManager.Instance.GetEnumStringRecord("StatParam", i);

                if (enumStringRecord != null)
                    CharacterStatMatrix[0, i] = TableManager.Instance.GetStringTableValue(enumStringRecord.NameCode); // ((ECharacterParam)i).ToString();

                else
                    CharacterStatMatrix[0, i] = ((ECharacterParam)i).ToString();
                    
                CharacterStatMatrix[1, i] = UserDataManager.Instance.PlayerCharacter.CharacterParam.TotalCharacterParamMap[((ECharacterParam)i)].GetValue().ToString();
            }

            CharacterStatMatrix[1, 0] = "Total";

            for (int i = 1; i < (int)EReasonParam.Max; i++)
            {
                // EnumStringRecord enumStringRecord = TableManager.Instance.GetEnumStringRecord("ReasonParam", i);
                
                CharacterStatMatrix[i + 1, 0] = ((EReasonParam)i).ToString();                
            }

        }

    }

    [Serializable]
    public class v_CharacterStat
    {
        [ShowInInspector]
        public int targetCharacterKey;

        [ShowInInspector]
        [TableMatrix(HorizontalTitle = "Reason Param", VerticalTitle = "Character Param", IsReadOnly = true)]
        public string[,] CharacterStatMatrix;

        BattleWorld battleWorld;

        public v_CharacterStat()
        {
            CharacterStatMatrix = new string[(int)EReasonParam.Max + 2, (int)ECharacterParam.Max + 2];

            battleWorld = BattleManager.Instance.CurrentBattleWorld;

            if (battleWorld == null)
                return;

            if (battleWorld.CharacterMap == null)
                return;

            targetCharacterKey = battleWorld.CharacterMap.First().Value.CharacterKey;

            UpdatePlayerStatData();
        }

        [Button(ButtonSizes.Large)]
        public void UpdatePlayerStatData()
        {
            battleWorld = BattleManager.Instance.CurrentBattleWorld;

            if (battleWorld == null)
                return;

            if (battleWorld.CharacterMap == null)
                return;

            Character targetCharacter = null;

            foreach (Character c in battleWorld.CharacterMap.Values)
            {
                if (c.CharacterKey == targetCharacterKey)
                    targetCharacter = c;
            }

            if (targetCharacter == null)
                return;

            CharacterStatMatrix = new string[(int)EReasonParam.Max + 2, (int)ECharacterParam.Max + 2];

            List<ReasonCharacterParam> reasonList =
                targetCharacter.CharacterParam.ReasonParamMap.Values.ToList();

            for (int currentReason = 1; currentReason < (int)EReasonParam.Max; currentReason++)
            {
                for (int currentParam = 1; currentParam < (int)ECharacterParam.Max; currentParam++)
                {
                    CharacterStatMatrix[currentReason + 1, currentParam] = reasonList[currentReason - 1].CharParam[(ECharacterParam)currentParam].ToString();
                }
            }

            for (int i = 1; i < (int)ECharacterParam.Max; i++)
            {
                EnumStringRecord enumStringRecord = TableManager.Instance.GetEnumStringRecord("StatParam", i);

                if (enumStringRecord != null)
                    CharacterStatMatrix[0, i] = TableManager.Instance.GetStringTableValue(enumStringRecord.NameCode); // ((ECharacterParam)i).ToString();

                else
                    CharacterStatMatrix[0, i] = ((ECharacterParam)i).ToString();

                CharacterStatMatrix[1, i] = targetCharacter.CharacterParam.TotalCharacterParamMap[((ECharacterParam)i)].GetValue().ToString();
            }

            CharacterStatMatrix[1, 0] = "Total";

            for (int i = 1; i < (int)EReasonParam.Max; i++)
            {
                // EnumStringRecord enumStringRecord = TableManager.Instance.GetEnumStringRecord("ReasonParam", i);

                CharacterStatMatrix[i + 1, 0] = ((EReasonParam)i).ToString();
            }

        }

    }
}
#endif
