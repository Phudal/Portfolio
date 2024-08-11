private async UniTask LoadAtUpdate()
{
    AsyncOperationHandle handle = new AsyncOperationHandle();

    if (mCellData.CardParam != null && mCellData.Status == AssetLoadStatus.None)
    {                
        mCellData.SetAssetLoadStatus(AssetLoadStatus.Loading);

        handle = GlobalAssetLoadManager.Instance.sceneAssetLoader.LoadPlayerCardSmallAsync(
            mCellData.CardParam.CardParamEntity.PlayerPicNo, mCellData.CardParam.CurrentRarity);

        await handle.Task;

        if (handle.IsValid())
        {
            mCellData.SetCardAsset(handle.Result as Sprite);
            mCellData.SetAssetLoadStatus(AssetLoadStatus.Complete);
        }
        else
        {
            mCellData.SetAssetLoadStatus(AssetLoadStatus.None);
        }                
    }

    else if (mCellData.CardParamCoach != null && mCellData.Status == AssetLoadStatus.None)
    {
        mCellData.SetAssetLoadStatus(AssetLoadStatus.Loading);

        handle = GlobalAssetLoadManager.Instance.sceneAssetLoader.LoadHeadCoachCardAsync(this.mCellData.CardParamCoach.CoachEntitiy.PicNo);

        await handle.Task;

        if (handle.IsValid())
        {
            mCellData.SetCardAsset(handle.Result as Sprite);
            mCellData.SetAssetLoadStatus(AssetLoadStatus.Complete);
        }
        else
        {
            mCellData.SetAssetLoadStatus(AssetLoadStatus.None);
        }
    }


    CardView.Card.RefreshCardLoadSuccess();
}

// 디바이스의 성능, 스크롤 속도 등의 이유로 로딩중이던 데이터가 화면에 보이지 않게될 경우,
// Release를 하여 리소스를 확보한다.
public override void UpdateContent(CardData itemData)
{
    if (mCellData != null)
    {
        if (mCellData != itemData && mCellData.Status == AssetLoadStatus.Loading)
        {
            if (mCellData.CardParam != null)
            {
                GlobalAssetLoadManager.Instance.sceneAssetLoader.ReleasePlayerCardSmallAsync(
                    mCellData.CardParam.CardParamEntity.PlayerPicNo, mCellData.CardParam.CurrentRarity);

                mCellData.SetAssetLoadStatus(AssetLoadStatus.None);                        
            }
            else if (mCellData.CardParamCoach != null)
            {
                GlobalAssetLoadManager.Instance.sceneAssetLoader.ReleaseHeadCoachCardAsync(mCellData.CardParamCoach.CoachEntitiy.PicNo);

                mCellData.SetAssetLoadStatus(AssetLoadStatus.None);                        
            }
        }
    }

    this.mCellData = itemData;

    UpdateContent();
}