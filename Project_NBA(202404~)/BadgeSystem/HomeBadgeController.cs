using Cysharp.Threading.Tasks;
using Dimps.Application.API.Message;
using Dimps.Application.Global;
using Dimps.Application.Global.UserData;


using NBA.Common;
using NBA.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HomeBadgeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private List<HomeBadge> leftBadgeList = new List<HomeBadge>();
    [SerializeField] private List<HomeBadge> rightBadgeList = new List<HomeBadge>();

    private UserBadgeData userBadgeData;

    private List<HomeBadge> activeBadgeList = new();

    // 버튼 연타 방지
    public bool isOpeningPopup = false;

    private bool mIsInit = false;

    private int mVisible = 0;

    private void OnEnable()
    {
        Broadcaster.RegisterListener(EBroadcastListenerType.ADD_LISTENER, EBroadcastKey.UserBadgeRefresh, RefreshBadgeData);
        Broadcaster<bool>.RegisterListener(EBroadcastListenerType.ADD_LISTENER, EBroadcastKey.OpenBadgePopup, OpenBadgePopupCB);
    }

    private void OnDisable()
    {
        Broadcaster.RegisterListener(EBroadcastListenerType.REMOVE_LISTENER, EBroadcastKey.UserBadgeRefresh, RefreshBadgeData);
        Broadcaster<bool>.RegisterListener(EBroadcastListenerType.REMOVE_LISTENER, EBroadcastKey.OpenBadgePopup, OpenBadgePopupCB);
    }

    private void FixedUpdate()
    {
        if (this.mVisible == 0)
            return;

        if (this.mIsInit == false)
            return;

        foreach (HomeBadge l_badge in leftBadgeList)
        {
            bool inPeriod = l_badge.CheckInPeriod();

            l_badge.gameObject.SetActive(inPeriod);

            if (inPeriod)
                l_badge.RefreshBadge();
        }

        foreach (HomeBadge r_badge in rightBadgeList)
        {
            bool inPeriod = r_badge.CheckInPeriod();

            bool isBlock = r_badge.CheckDisplayBlock();

            r_badge.gameObject.SetActive(inPeriod && !isBlock);

            if (inPeriod && !isBlock)
                r_badge.RefreshBadge();

            if (r_badge.IsResponseDetailNull())
                continue;

            if (!inPeriod && r_badge.BM == eBMType.SeasonPass && r_badge.IsDisplayBlock() == false)
            {
                r_badge.SetDisplayBlock(true);

                Broadcaster<int>.Broadcast(EBroadcastKey.OnSeasonPassExpired, r_badge.badgeEntity.Id);
            }
        }
    }

    public void Show()
    {
        isOpeningPopup = false;

        if (this.mVisible == 1)
            return;

        this.mVisible = 1;

        mIsInit = false;

        IntializeBadgeData().Forget();

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (this.mVisible == 0)
            return;
            
        this.mVisible = 0;

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    public void InitialHideForce()
    {
        this.mVisible = 0;

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    public void RefreshBadgeData()
    {
        IntializeBadgeData(true).Forget();
    }

    private void OpenBadgePopupCB(bool isOpen)
    {
        isOpeningPopup = isOpen;
    }

    private List<int> GetInPeriodBadgeIndexList()
    {
        List<int> ret = new List<int>();

        var mstBadgeDatas = TableDataManager.Instance.GetDataList<BadgeTableData>();
        foreach (BadgeTableData badgeEntity in mstBadgeDatas)
        {
            // event type
            if (badgeEntity.Type == 0)
            {

                EventInfoTableData eventInfoEntity = TableDataManager.Instance.Extend.GetEventInfoByEventID(badgeEntity.TargetId);

                if (eventInfoEntity == null)
                    continue;

                if (GlobalFunction.CheckContainsPeriod(DateTime.Parse(eventInfoEntity.StartDate), DateTime.Parse(eventInfoEntity.EndDate)))
                    ret.Add(badgeEntity.TargetId);
            }

            // bm type
            else if (badgeEntity.Type == 1)
            {
                List<BmGroupTableData> bmGroupEntities = TableDataManager.Instance.Extend.GetBmGroupList(badgeEntity.TargetId);

                if(null != bmGroupEntities)
                {
                    foreach (BmGroupTableData bmGroupEntity in bmGroupEntities)
                    {
                        if (GlobalFunction.CheckContainsPeriod(DateTime.Parse(bmGroupEntity.StartDate), DateTime.Parse(bmGroupEntity.EndDate)))
                        {
                            ret.Add(badgeEntity.TargetId);

                            break;
                        }
                    }

                }
                
            }
        }

        return ret;
    }

    public async UniTask IntializeBadgeData(bool forceRefreshData = false)
    {
        foreach (HomeBadge l_Badge in leftBadgeList)
        {            
            l_Badge.gameObject.SetActive(false);
        }
        foreach (HomeBadge r_Badge in rightBadgeList)
        {            
            r_Badge.gameObject.SetActive(false);
        }
        activeBadgeList.Clear();

        int l_BadgeCount = 0;
        int r_BadgeCount = 0;

        IOrderedEnumerable<PBadgeDetail> sortedOrder = null;


        if (GlobalDataManager.Instance.GlobalUser.IsUserBadgeNull() || forceRefreshData)
        {
            userBadgeData = await GlobalDataManager.Instance.GlobalUser.GetUserBadgeData(true);
        }
        else
        {
            bool needRefresh = false;

            // 마스터 데이터 상에서 현재 진행 가능한 뱃지 인덱스 해쉬
            HashSet<int> inPeriodByMaster = new HashSet<int>(GetInPeriodBadgeIndexList());

            // 클라이언트가 현재 가지고 있는 뱃지 인덱스 해쉬
            HashSet<int> inPeriodByClientAndFinished = new HashSet<int>();

            userBadgeData = await GlobalDataManager.Instance.GlobalUser.GetUserBadgeData();

            sortedOrder = userBadgeData.UserBadeAllRes.List.OrderBy(v => TableDataManager.Instance.GetData<BadgeTableData>(v.Id).SortNum);

            foreach (PBadgeDetail badgeDetail in sortedOrder)
            {
                BadgeTableData badgeEntity = TableDataManager.Instance.GetData<BadgeTableData>(badgeDetail.Id);
                if (badgeEntity == null)
                {
                    DebugTool.LogError($"not found badge id - {badgeDetail.Id}");

                    needRefresh = true;
                    break;
                }
                 
                if (badgeDetail.Type == (int)eBadgeDetailType.Event &&
                    GlobalFunction.CheckContainsPeriod(badgeDetail.EventSummary.StartDate, badgeDetail.EventSummary.EndDate) == false)
                {
                    eBadgeEntryType eventBadgeType = (eBadgeEntryType)TableDataManager.Instance.GetData<EventInfoTableData>(badgeEntity.TargetId).Type;

                    // 요일 던전은 항상 떠있기 때문에 기간에 따른 전체 뱃지 정보 갱신 요청 하지 않음
                    if (eventBadgeType == eBadgeEntryType.DailyDungeon)
                    {
                        continue;
                    }

                    needRefresh = true;
                    break;
                }

                else if (badgeDetail.Type == (int)eBadgeDetailType.BM)
                {
                    // 가지고 있는 뱃지 데이터 안의 상품 중 하나라도 기간을 벗어나면 Refresh 진행                    
                    foreach (var v in badgeDetail.BillingSummary)
                    {
                        if (GlobalFunction.CheckContainsPeriod(v.StartDate, v.EndDate) == false)
                        {
                            needRefresh = true;
                            break;
                        }
                    }

                    if (needRefresh)
                    {
                        break;                        
                    }                                        
                }                

                inPeriodByClientAndFinished.Add(badgeEntity.TargetId);
            }            

            // 클라이언트에 캐시된 데이터 중 종료된 뱃지가 있거나
            // 마스터 데이터에서 현재 노출 가능한 뱃지 해쉬와 다를 경우
            // 업데이트 요청
            userBadgeData = await GlobalDataManager.Instance.GlobalUser.GetUserBadgeData(needRefresh || inPeriodByClientAndFinished.SetEquals(inPeriodByMaster) == false);            
        }

        sortedOrder = userBadgeData.UserBadeAllRes.List.OrderBy(v => TableDataManager.Instance.GetData<BadgeTableData>(v.Id).SortNum);

        foreach (PBadgeDetail badgeDetail in sortedOrder)
        {
            BadgeTableData badgeEntity = TableDataManager.Instance.GetData<BadgeTableData>(badgeDetail.Id);
            if (badgeEntity == null)
            {
                DebugTool.LogError($"not found badge id - {badgeDetail.Id}");
                continue;
            }

            if (badgeEntity.Position == 0 && leftBadgeList.Count > l_BadgeCount)
            {
                if (badgeDetail.IsDisplayBlocked == false)
                {
                    leftBadgeList[l_BadgeCount].gameObject.SetActive(true);
                    leftBadgeList[l_BadgeCount].Init(badgeDetail);
                }
                else
                {
                    leftBadgeList[l_BadgeCount].gameObject.SetActive(false);
                    leftBadgeList[l_BadgeCount].Init(badgeDetail);
                }

                activeBadgeList.Add(leftBadgeList[l_BadgeCount]);
                l_BadgeCount++;
                
            }
            else if (badgeEntity.Position == 1 && rightBadgeList.Count > r_BadgeCount)
            {
                if (badgeDetail.IsDisplayBlocked == false)
                {
                    rightBadgeList[r_BadgeCount].gameObject.SetActive(true);
                    rightBadgeList[r_BadgeCount].Init(badgeDetail);
                }
                else
                {
                    rightBadgeList[r_BadgeCount].gameObject.SetActive(false);
                    rightBadgeList[r_BadgeCount].Init(badgeDetail);
                }

                activeBadgeList.Add(rightBadgeList[r_BadgeCount]);
                r_BadgeCount++;
            }
        }

        // 남아있는 뱃지 데이터 clear
        for (int i = l_BadgeCount; i < leftBadgeList.Count; i++)
        {
            leftBadgeList[i].InitEmpty();
            leftBadgeList[i].gameObject.SetActive(false);
        }
        for (int i = r_BadgeCount; i < rightBadgeList.Count; i++)
        {
            rightBadgeList[i].InitEmpty();
            rightBadgeList[i].gameObject.SetActive(false);
        }

        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        mIsInit = true;
    }

    public HomeBadge GetHomeBadge(eBMType bmType)
    {
        foreach(var badge in activeBadgeList)
        {
            if (badge.BM == bmType)
                return badge;
        }

        return null;
    }

    public List<HomeBadge> GetActiveSeasonPassHomeBadgeList()
    {
        List<HomeBadge> ret = new List<HomeBadge>();

        foreach (var badge in activeBadgeList)
        {
            if (badge.responseDetail.Type == (int)eBadgeDetailType.BM && badge.BM == eBMType.SeasonPass && badge.responseDetail.IsDisplayBlocked == false)
            {
                ret.Add(badge);
            }
        }

        return ret;
    }

    // 정액권과 같이 하나의 groupid를 사용할 때는 불가능
    public HomeBadge GetHomeBadge(eBMType bmType, int link_id)
    {
        foreach (var badge in activeBadgeList)
        {
            List<BmGroupTableData> bmGroupTableDatas = TableDataManager.Instance.Extend.GetBmGroupList(badge.badgeEntity.TargetId);
            if (bmGroupTableDatas == null)
            {
                continue;
            }
            BmGroupTableData bmGroupEntity = bmGroupTableDatas.First();

            if (badge.BM == bmType && bmGroupEntity.LinkId == link_id)
                return badge;
        }

        return null;
    }

    public int GetActiveBadgeCount()
    {
        return activeBadgeList.Count;
    }
}
