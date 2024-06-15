using CodeStage.AntiCheat.ObscuredTypes;
using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public partial class UserDataManager
{
    public List<UserCardData> userCardData = new List<UserCardData>();

    public UserCardData GetUserCardDataFromEntityIndex(int entityindex)
    {
        var cardData = SODataManager.Instance.GetCardDataFromEntity(entityindex);

        if (cardData == null)
            return null;

        foreach (var item in userCardData)
        {
            if (item.index == cardData.index)
                return item;
        }

        return null;
    }

    public int GetUserSpeciesTotalCardLevel(SPECIES_TYPE species_type)
    {
        int totalLevel = 0;
        foreach (var item in userCardData)
        {
            if (item.GetCardData().species.ToUpper() == species_type.ToString())
                totalLevel += item.level;
        }

        return totalLevel;
    }

    public UserCardData GetUserCardData(int index)
    {
        foreach (var item in userCardData)
        {
            if (item.index == index)
                return item;
        }

        return null;
    } 

    /// ------------------------ Summon Card --------------------------


    // 카드 리롤 횟수
    public int summon_card_reroll_count = 0;

    public int summon_card_exp = 0;

    public int summon_card_ratio = -1;

    // key는 뽑기 인덱스, bool은 뽑았는지 안뽑았는지 
    public Dictionary<int, bool> summonCardTargetInfo = new Dictionary<int, bool>();

    public void GenerateSummonCardInfo()
    {
        summonCardTargetInfo.Clear();

        // 소환된 카드의 인덱스를 저장하고 다음 카드 소환에서 빼는 용도다.
        List<int> summonedIndexList = new List<int>();

        int summonCount = SODataManager.Instance.define.CARD_SHOW_BASE;

        List<CardDrawData> curAngelDrawRewardList = SODataManager.Instance.GetCardDrawList(SODataManager.Instance.define.CARD_DRAW_ANGEL_GROUP_INDEX);
        int totalAngelProb = SODataManager.Instance.GetCardDrawTotalProb(SODataManager.Instance.define.CARD_DRAW_ANGEL_GROUP_INDEX);

        #region 천장 시스템
        // 소환 경험치가 일정 이상 쌓였을 경우에는 이계 카드를 확정적으로 하나 넣어준다.
        //while (summon_card_exp >= SODataManager.Instance.define.CARD_GET_ANGEL_MAX && summonCount > 0)
        //{
        //    int prob = Random.Range(0, totalAngelProb + 1);
        //    int weight = 0;
        //    foreach (CardDrawData record in curAngelDrawRewardList)
        //    {
        //        weight += record.prob;
        //        if (prob <= weight)
        //        {
        //            if (summonedIndexList.Contains(record.rewardInfo.index))
        //            {
        //                // 동일한 카드를 뽑아, 다시 진행합니다.
        //                Logger.Log("동일한 카드를 뽑아, 다시 뽑기 로직을 진행합니다.");
        //                break;
        //            }

        //            summonCardTargetInfo.Add(record.index, false);
        //            summonedIndexList.Add(record.rewardInfo.index);

        //            summon_card_exp -= SODataManager.Instance.define.CARD_GET_ANGEL_MAX;

        //            summonCount--;
        //            break;
        //        }
        //    }
        //}
        #endregion

        List<CardDrawData> curDrawRewardList = SODataManager.Instance.GetCardDrawList(SODataManager.Instance.define.CARD_DRAW_GROUP_INDEX);
        int totalProb = SODataManager.Instance.GetCardDrawTotalProb(SODataManager.Instance.define.CARD_DRAW_GROUP_INDEX);

        while (summonCount > 0)
        {
            int prob = Random.Range(0, totalProb + 1);
            int weight = 0;
            foreach (CardDrawData record in curDrawRewardList)
            {
                weight += record.prob;
                if (prob <= weight)
                {
                    if (summonedIndexList.Contains(record.rewardInfo.index))
                    {
                        // 동일한 카드를 뽑아, 다시 진행합니다.
                        Logger.Log("동일한 카드를 뽑아, 다시 뽑기 로직을 진행합니다.");
                        break;
                    }

                    summonCardTargetInfo.Add(record.index, false);
                    summonedIndexList.Add(record.rewardInfo.index);

                    summonCount--;

                    break;
                }
            }
        }
    }

    public int GetSummonRatio()
    {
        if (summon_card_ratio < 1)
            summon_card_ratio = 1;

        return summon_card_ratio;
    }

    public ObscuredLong GetRerollCost()
    {
        ObscuredLong rerollCost = (ObscuredLong)SODataManager.Instance.define.CARD_REROLL_BASE;
        rerollCost += (ObscuredLong)SODataManager.Instance.define.CARD_REROLL_BASE * UserDataManager.Instance.summon_card_reroll_count;
        if (rerollCost > SODataManager.Instance.define.CARD_REROLL_MAX)
            rerollCost = (ObscuredLong)SODataManager.Instance.define.CARD_REROLL_MAX;

        return rerollCost;
    }

    public int GetSummonableCardCount()
    {
        return summonCardTargetInfo.Where(v => v.Value == false).ToList().Count;
    }

    public void SetSummonCard(int index)
    {
        if (summonCardTargetInfo.ContainsKey(index))
        {
            summonCardTargetInfo[index] = true;
        }
        else
        {
            Logger.LogError($"Error - index is not Contains Target Info {index}");
        }
    }
}
