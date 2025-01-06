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

    int l_BadgeCount = 0;
    int r_BadgeCount = 0;
    
    if (GlobalDataManager.Instance.GlobalUser.IsUserBadgeNull() || forceRefreshData || GlobalDataManager.Instance.GlobalCache.needBadgeInfoRefresh)
    {
        userBadgeData = await GlobalDataManager.Instance.GlobalUser.GetUserBadgeData(true);
    }
    else
    {
        bool needRefresh = false;

        // 마스터 데이터 상에서 현재 진행 가능한 뱃지 인덱스 해쉬
        HashSet<int> inPeriodByMaster = new HashSet<int>(MasterDataManager.Instance.BadgeMaster.GetInPeriodBadgeIndexList());

        // 클라이언트가 현재 가지고 있는 뱃지 인덱스 해쉬
        HashSet<int> inPeriodByClientAndFinished = new HashSet<int>();

        userBadgeData = await GlobalDataManager.Instance.GlobalUser.GetUserBadgeData();

        foreach (PBadgeDetail badgeDetail in userBadgeData.UserBadeAllRes.List)
        {
            BadgeEntity badgeEntity = MasterDataManager.Instance.BadgeMaster.GetBadgeEntity(badgeDetail.Id);
            if (badgeEntity == null)
            {
                DebugTool.LogError($"not found badge id - {badgeDetail.Id}");

                needRefresh = true;
                break;
            }
             
            if (badgeDetail.Type == 0 &&
                GlobalFunction.CheckContainsPeriod(badgeDetail.EventSummary.StartDate, badgeDetail.EventSummary.EndDate) == false)
            {
                needRefresh = true;
                break;
            }

            else if (badgeDetail.Type == 1 &&
                GlobalFunction.CheckContainsPeriod(badgeDetail.BillingSummary.StartDate, badgeDetail.BillingSummary.EndDate) == false)
            {
                needRefresh = true;
                break;
            }

            inPeriodByClientAndFinished.Add(badgeEntity.TargetId);
        }            

        // 클라이언트에 캐시된 데이터 중 종료된 뱃지가 있거나
        // 마스터 데이터에서 현재 노출 가능한 뱃지 해쉬와 다를 경우
        // 업데이트 요청
        userBadgeData = await GlobalDataManager.Instance.GlobalUser.GetUserBadgeData(needRefresh || inPeriodByClientAndFinished.SetEquals(inPeriodByMaster) == false);            
    }

    // 캐싱 초기화
    GlobalDataManager.Instance.GlobalCache.needBadgeInfoRefresh = false;
    cachedHideBadgeIndexHash = new HashSet<int>();


    foreach (PBadgeDetail badgeDetail in userBadgeData.UserBadeAllRes.List)
    {
        BadgeEntity badgeEntity = MasterDataManager.Instance.BadgeMaster.GetBadgeEntity(badgeDetail.Id);
        if (badgeEntity == null)
        {
            DebugTool.LogError($"not found badge id - {badgeDetail.Id}");
            continue;
        }

        // 구매, 컨텐츠 완료 등으로 UI로 띄우지 않을 뱃지
        if (badgeDetail.IsDisplayBlocked)
        {
            continue;
        }

        if (badgeEntity.Position == 0 && leftBadgeList.Count > l_BadgeCount)
        {
            leftBadgeList[l_BadgeCount].gameObject.SetActive(true);
            leftBadgeList[l_BadgeCount].Init(badgeDetail, ignoreButtonInitialize);

            l_BadgeCount++;
        }
        else if (badgeEntity.Position == 1 && rightBadgeList.Count > r_BadgeCount)
        {
            rightBadgeList[r_BadgeCount].gameObject.SetActive(true);
            rightBadgeList[r_BadgeCount].Init(badgeDetail, ignoreButtonInitialize);

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