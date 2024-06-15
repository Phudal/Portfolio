using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CoffeeLibrary;
public partial class Popup_Entity
{
    WaitForSeconds delay01 = new WaitForSeconds(0.02f);

    Coroutine LevelupCO = null;

    bool setAutoLevelup = false;

    public void OnClick_AutoLevelup()
    {
        UserEntityData userEntityData = UserDataManager.Instance.GetUserEntityData(curSelectIndex);
        
        #region 초기 비용 계산
        if (userEntityData.IsLevelEnable())
        {
            EntityLevelData tmpData = userEntityData.GetEntityLevelData();

            if (ProjectUtil.IsLackResource(REWARD_TYPE.RESOURCE, DefineItem.exp_stone, tmpData.troopExp, true))
            {
                //Debug.Log($"LEVEL UP : {curSelectIndex}, {userEntityData.level} 재화가 부족합니다!!, 롱버튼 취소");
                gameobject_LevelupBtn.GetComponent<UIButtonLongPress>().OnPointerExit(null);
                return;
            }
        }
        else if (userEntityData.IsBreakEnable())
        {
            EntityBreakthroughData tmpData = userEntityData.GetEntityBreakthroughData();

            if (ProjectUtil.IsLackResource(tmpData.material, true))
            {
                return;
            }
        }
        // 초월할 단계
        else
        {
            return;
        }
        #endregion

        if (LevelupCO == null)
        {
            LevelupCO = StartCoroutine(AutoLevelupCO());
        }
        else
        {
            // UIManager.Instance.ShowToastMessage("_____코루틴이 실행중입니다.");
        }
    }

    IEnumerator AutoLevelupCO()
    {               
        int targetIndex = curSelectIndex;

        bool isLevelup = false;

        // 레벨업이 가능할 때만 하도록한다.        

        setAutoLevelup = true;

        gameobject_CancelAutoLevelupBtn.SetActive(true);
        

        // 자동 레벨업 조건        
        while (setAutoLevelup && targetIndex == curSelectIndex)
        {
            //RefreshEntity_LevelInfo();

            yield return delay01;

            if (targetIndex != curSelectIndex)
            {
                // 해당 시간동안 타겟이 바뀌었다면 즉시 나온다.
                break;
            }

            UserEntityData userEntityData = UserDataManager.Instance.GetUserEntityData(targetIndex);
            if (userEntityData == null)
                break;

            if (userEntityData.IsLevelEnable())
            {
                int grade = userEntityData.GetStarGrade();

                long levelUpPrice = 0;
                addLevel = 0;

                long hasCount = UserDataManager.Instance.GetHasCount(maxLevelPrice.type, maxLevelPrice.index);

                for (int level = userEntityData.level; level < userEntityData.GetMaxLevel(); level++)
                {
                    var levelData = SODataManager.Instance.GetEntityLevelData(userEntityData.GetRarity(), grade, level);
                    if (levelData == null)
                        continue;

                    // NOTE, 스킬레벨업에 필요한 재화를 1종이라고 가정한 상태
                    if (hasCount >= levelUpPrice + levelData.troopExp)
                    {
                        // 레벨업에 필요한 비용이 충분함
                        addLevel++;
                        levelUpPrice += levelData.troopExp;
                    }
                    else
                    {
                        // 비용이 이제 부족함
                        break;
                    }
                }

                maxLevelPrice.count = levelUpPrice;

                if (ProjectUtil.IsLackResource(maxLevelPrice, false))
                {
                    // MSG_NO_AUTO_LEVELUP : 레벨업 재료가 부족해서 자동 레벨업이 중지되었습니다. 
                    UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("MSG_NO_AUTO_LEVELUP"));
                    break;
                }

                // 올릴 수 있는 레벨이 없다.
                if (addLevel == 0)
                {
                    // MSG_NO_AUTO_LEVELUP : 레벨업 재료가 부족해서 자동 레벨업이 중지되었습니다. 
                    UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("MSG_NO_AUTO_LEVELUP"));
                    break;
                }
                
                MaxLevelUp_ByAuto();

                isLevelup = true;
            }
            else if (userEntityData.IsBreakEnable())
            {
                var tmpData = userEntityData.GetEntityBreakthroughData();

                if (ProjectUtil.IsLackResource(tmpData.material, false))
                {
                    // MSG_NO_AUTO_LEVELUP : 레벨업 재료가 부족해서 자동 레벨업이 중지되었습니다. 
                    UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("MSG_NO_AUTO_LEVELUP"));
                    break;
                }
                // OnClick_BreakThrough();
                BreakThrough_ByAuto();

                isLevelup = true;
            }
            // 초월을 할 단계
            else
            {
                // MSG_MAX_AUTO_LEVELUP : 최대 레벨에 도달했습니다.
                UIManager.Instance.ShowToastMessage(SODataManager.Instance.GetStringTableValue("MSG_MAX_AUTO_LEVELUP"));
                break;
            }

            ShowMaxUpgradeBtn(true);
        }

        RefreshEntity_LevelInfo();

        if (isLevelup)
        {
            UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.ENTITY_HAVE, true);
            UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.ENTITY_PASSIVE, true);

            UserDataManager.Instance.UpdateBuff();

            UserDataManager.Instance.SaveData(new List<USER_DATA_TYPE>()
                {
                    USER_DATA_TYPE.USER_ENTITY,
                    USER_DATA_TYPE.USER_RESOURCES,
                });

            UserDataManager.Instance.SendNetworkSync(true);
        }

        gameobject_CancelAutoLevelupBtn.SetActive(false);

        LevelupCO = null;

        yield return null;
    }

    public void MaxLevelUp_ByAuto()
    {
        var userEntityData = UserDataManager.Instance.GetUserEntityData(curSelectIndex);

        if (ProjectUtil.IsLackResource(maxLevelPrice, true))
        {
            return;
        }

        if (userEntityData.IsLevelEnable() == false)
        {
            return;
        }

        UserDataManager.Instance.AddUserItemData(maxLevelPrice.type, maxLevelPrice.index, -1 * maxLevelPrice.count);
        userEntityData.IncLevel(addLevel);
        UserDataManager.Instance.AddQuestValue(QUEST_TYPE.ENTITY_LEVEL, addLevel);
        UserDataManager.Instance.AddQuestValue(QUEST_TYPE.ENTITY_LEVEL_UP, addLevel);
        UserDataManager.Instance.AddQuestValue(QUEST_TYPE.ENTITY_MAXLEVEL);
        UserDataManager.Instance.AddAchievementValue(EAchievementType.ENTITY_LEVEL_UP);        

        RefreshEntity_LevelInfo();


        // 팝업 화면 갱신
        Messenger<int>.Broadcast(EMessengerID.E_POPUP_ENTITY_SELECT, curSelectIndex);
    }

    public void BreakThrough_ByAuto()
    {
        var userEntityData = UserDataManager.Instance.GetUserEntityData(curSelectIndex);
        var tmpData = userEntityData.GetEntityBreakthroughData();

        if (ProjectUtil.IsLackResource(tmpData.material, true))
        {
            return;
        }

        int successRate = Mathf.Min(userEntityData.breakFailCount + tmpData.success, 100);
        UserDataManager.Instance.AddUserItemData(tmpData.material.type, tmpData.material.index, -1 * tmpData.material.count);

        UserDataManager.Instance.AddQuestValue(QUEST_TYPE.ENTITY_BREAK);
        UserDataManager.Instance.AddQuestValue(QUEST_TYPE.ENTITY_BREAK_LEVEL_TOTAL);

        var success = RandomUtil.Roll(successRate, 100);

        // 전체 효과 업데이트로 인한 유닛 버프 갱신
        if (success)
        {            
            userEntityData.upgrade++;
            userEntityData.breakFailCount = 0;

            //entitySimpleInfo.PlayLevelEffect();

            if (userEntityData.GetRarity() >= DefineRarity.SSS)
            {
                if (userEntityData.upgrade >= ProjectUtil.GetMaxBreakthroughLevel())
                {
                    var resultEntity = new RewardInfo();
                    resultEntity.type = REWARD_TYPE.ENTITY;
                    resultEntity.index = userEntityData.index;
                    resultEntity.count = (int)userEntityData.upgrade;

                    //ChatManager.Instance.sendSystemBreak(resultEntity);
                }
            }

            // 팝업 화면 갱신
            Messenger<int>.Broadcast(EMessengerID.E_POPUP_ENTITY_SELECT, curSelectIndex);
        }
        else
        {        
            Sound.Play(ePlaySoundType.ePST_EFFECT, SoundData.UI_FAIL);

            userEntityData.breakFailCount += tmpData.addPoint;
            
        }        
        RefreshEntity_LevelInfo();
    }

    private void StopAutoLevelUp()
    {
        setAutoLevelup = false;
        if (LevelupCO != null)
        {
            StopCoroutine(LevelupCO);
            LevelupCO = null;

            UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.ENTITY_HAVE, true);
            UserDataManager.Instance.UpdateBuffSync(BUFF_DETAIL_TYPE.ENTITY_PASSIVE, true);

            UserDataManager.Instance.UpdateBuff();

            UserDataManager.Instance.SaveData(new List<USER_DATA_TYPE>()
                {
                    USER_DATA_TYPE.USER_ENTITY,
                    USER_DATA_TYPE.USER_RESOURCES,
                });

            UserDataManager.Instance.SendNetworkSync(true);

            gameobject_CancelAutoLevelupBtn.SetActive(false);

            RefreshEntity_LevelInfo();
        }
    }

    public void OnClicked_CancelAutoLevelup()
    {
        StopAutoLevelUp();
    }
}