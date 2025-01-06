public void Init(PBadgeDetail badgeDetail)
{
    responseDetail = badgeDetail;
    badgeEntity = MasterDataManager.Instance.BadgeMaster.GetBadgeEntity(badgeDetail.Id);

    if (responseDetail.Type == (int)BadgeDetailType.Event)
    {
        gameObject_leftReddot.SetActive(badgeEntity.Position == 0 && responseDetail.EventSummary.IsEnableReward || responseDetail.IsNew);

        gameObject_rightReddot.SetActive(badgeEntity.Position == 1 && responseDetail.EventSummary.IsEnableReward || responseDetail.IsNew);
    }
    else if (responseDetail.Type == (int)BadgeDetailType.BM)
    {
        gameObject_leftReddot.SetActive(badgeEntity.Position == 0 && responseDetail.BillingSummary.IsEnableReward || responseDetail.IsNew);

        gameObject_rightReddot.SetActive(badgeEntity.Position == 1 && responseDetail.BillingSummary.IsEnableReward || responseDetail.IsNew);
    }
            
        
    button_onClick.onClick.RemoveAllListeners();
    button_onClick.onClick.AddListener(() =>
    {
        OnClicked_Badge().Forget();
    });
        

    // Design Type
    // 0 : Normal
    // 1 : Guage
    // 3 : BM

    gameObject_Gauge.SetActive(badgeEntity.DesignType == (int)BadgeDesignType.Gauge);

    RefreshBadge();
}
    
public void RefreshBadge()
{
    // Event Type
    if (responseDetail.Type == (int)BadgeDetailType.Event)
    {
        PEventSummary pEventSummary = responseDetail.EventSummary;

        int remainTime = (int)(pEventSummary.FixDate - GlobalDataManager.Instance.GlobalTime.CurrentTime) / 1000;
        if (remainTime > 0)
        {
            tmp_remainTime.text = GlobalFunction.GetRemainTimeText(remainTime);
        }
        else if (CheckInPeriod())
        {
            tmp_remainTime.text = "__FIN__";
        }
    }

    // BM Type
    else if (responseDetail.Type == (int)BadgeDetailType.BM)
    {
        PBillingSummary pBillingSummary = responseDetail.BillingSummary;

        int remainTime = (int)(pBillingSummary.EndDate - GlobalDataManager.Instance.GlobalTime.CurrentTime) / 1000;
        if (remainTime > 0)
            tmp_remainTime.text = GlobalFunction.GetRemainTimeText(remainTime);
        else
        {
            tmp_remainTime.text = "__FIN__";
        }
    }
}