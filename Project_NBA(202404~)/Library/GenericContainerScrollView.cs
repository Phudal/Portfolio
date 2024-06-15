// 데이터 세팅의 일부분
public void Setup(IMultiLoadGridScrollContext context, int resourceCountByOneCell, List<T> cardDataList, float barLimitSize = 0.05f)
{
    this.resourceCountByOneCell = resourceCountByOneCell;

    cellManager.SetResourceCountByOneCell(resourceCountByOneCell);
    
    cellManager.SetCardData(cardDataList);
    
    scroll.Set(context, barLimitSize);
    MakeScrollCell();
    ResetCardData(cardDataList);
    scroll.SetRefreshGrid(RefreshGrid);
    
    cellManager.CellList = new List<DoubleLoadGridScrollCell<T>>(content.GetComponentsInChildren<DoubleLoadGridScrollCell<T>>(true));
}

// 데이터 세팅 후 초기 로드
public async UniTask LoadInitCard(List<T> cardDataList, AssetLoader assetLoader, int loadNum = 24, int scrollIndex = 0)
{
    List<AsyncOperationHandle> handles = new();
    int startIndex = Math.Max(0, scrollIndex - loadNum / 2);
    List<T> loadCardDataList = cardDataList.Skip(startIndex).Take(loadNum).ToList();            
    foreach (T data in loadCardDataList)
    {
        for (int i = 0; i < resourceCountByOneCell; i++)
        {
            handles.Add(data.CardDataList[i].LoadSprite(assetLoader));
        }
    }

    for (int cellIndex = 0, handleIndex = 0; cellIndex < loadCardDataList.Count; cellIndex++)
    {
        await handles[handleIndex].Task;

        for (int dataIndex = 0; dataIndex < resourceCountByOneCell; dataIndex++) 
        {
            loadCardDataList[cellIndex].LoadSuccess(dataIndex, handles[handleIndex]);
            handleIndex++;
        }
    }
}

// 데이터 로딩의 일부분으로, handle에 로드할 데이터를 넣어 로드한다.
public async UniTask LoadCard(int count, int next, AssetLoader loader, CancellationToken token)
{
    ///
    /// ~~~
    /// 

    // Ignore loading if it's not complete.
    bool notSucceededContinue = false;
    foreach(var v in tmpLoadData[i].handle)
    {
        if (v.Status != AsyncOperationStatus.Succeeded)
        {
            notSucceededContinue = true;
            break;
        }
    }
    if (notSucceededContinue == true)
    {
        continue;
    }

    for (int handleIndex = 0; handleIndex < resourceCountByOneCell; handleIndex++)
    {
        tmpLoadData[i].data.LoadSuccess(handleIndex, tmpLoadData[i].handle[handleIndex]);
    }

    if (tmpLoadData[i].cell != null)
        tmpLoadData[i].cell.ReDraw();

    tmpLoadData[i].data = default(T);
    tmpLoadData[i].cell = default(U);

///
/// ~~~
///
}

// 스크롤 아이템이 상속받을 인터페이스로, 해당 인터페이스의 LoadSuccess등을 접근하여 완료된 리소스를 적용한다.
public interface IMultiLoadCellData
{
    public List<Sprite> CardAssetList { get; }

    public List<AssetLoadStatus> StatusList { get; }

    AsyncOperationHandle LoadSprite(int index, AssetLoader loader, bool smallFlag = false);
    UniTask GetSprite(int index, bool smallFlag = false);

    void LoadSuccess(int index, AsyncOperationHandle handle);
}

public interface IMultiLoadScrollCell<T> where T : IMultiLoadCellData
{
    T CellData { get; }
    GameObject gameObject { get; }
    void ReDraw();
}

// 해당 인터페이스를 상속받는 클래스를 제네릭으로 스크롤 매니저 구현
public class MultiLoadScrollCellManager<T, U> where T : IMultiLoadCellData where U : IMultiLoadScrollCell<T>
{
    private class MultiLoadData
    {
        public List<AsyncOperationHandle> handle;
        public T data = default(T);
        public U cell = default(U);

        public MultiLoadData(int resourceCountByOneCell, T data, AssetLoader loader)
        {
            if (data == null)
            {
                this.data = default(T);
                return;
            }

            this.data = data;

            handle = new List<AsyncOperationHandle>();
            for (int i = 0; i < resourceCountByOneCell; i++) 
            {
                this.handle.Add(this.data.LoadSprite(i, loader, true));
            }
        }
    }

    public List<T> DataList => mDataList;
    public List<U> CellList { get => mCellList; set => mCellList = value; }

    private List<T> mDataList = null;
    private List<U> mCellList = null;
    private bool enabledLoading = true;

    private int resourceCountByOneCell = 1;
}

// 상속받은 인터페이스의 사용 예시
public class MultiLoadScrollDataConatainer : IMultiLoadCellData
{
    public List<CardData> CardDataList { get; set; }

    public List<Sprite> CardAssetList { get; private set; }

    public List<AssetLoadStatus> StatusList { get; set; }

    public List<Action<Sprite>> LoadSuccessAction = new List<Action<Sprite>>();

    public MultiLoadScrollDataConatainer(List<CardData> cardDatas)
    {
        this.CardDataList = cardDatas;
        this.StatusList = new List<AssetLoadStatus>();
        this.CardAssetList = new List<Sprite>();
        this.LoadSuccessAction = new List<Action<Sprite>>();
        for (int i = 0; i <  CardDataList.Count; i++)
        {
            StatusList.Add(AssetLoadStatus.None);
            CardAssetList.Add(null);
            LoadSuccessAction.Add(null);
        }
    }

    public AsyncOperationHandle LoadSprite(int index, AssetLoader loader, bool smallFlag = false)
    {
        StatusList[index] = AssetLoadStatus.Loading;
        if (smallFlag)
        {
            return loader.LoadPlayerCardSmallAsync(CardDataList[index].CardParam.CardParamEntity.PlayerPicNo, CardDataList[index].CardParam.CurrentRarity);
        }
        else
        {
            return loader.LoadPlayerCardAsync(CardDataList[index].CardParam.CardParamEntity.PlayerPicNo, CardDataList[index].CardParam.CurrentRarity);
        }
    }

    public async UniTask GetSprite(int index, bool smallFlag = false)
    {
        await UniTask.Yield(PlayerLoopTiming.Update);
    }

    public void LoadSuccess(int index, AsyncOperationHandle handle)
    {
        StatusList[index] = AssetLoadStatus.Complete;
        CardAssetList[index] = handle.Result as Sprite;
        CardDataList[index].SetCardAsset(CardAssetList[index]);
        LoadSuccessAction[index]?.Invoke(CardAssetList[index]);
    }
}

//////////////////////////////////////////////////////

// 탭 전환 딜레이 최적화
// 현재 초기 탭에서 스크롤이 존해나는 탭을 이동할 때 해당 탭에 속해있는 스크롤의 리소스를 불러오는 과정에서 프레임 드랍이 생긴다. 
// 리소스를 조금만 불러올 때는 상관이 없지만, 많은 리소스를 한꺼번에 불러오고자할 때에는 프레임 드랍이 크게 생긴다. 
// 이를 개선하여 사용될 수 있는 리소스들을 해당 탭이 열리지 않아도 미리 로딩할 수 있도록 만든다.
protected override async UniTask OnActivate(CancellationToken token)
{
	// ~~
	// ~~
	PreLoadProposeResources().Forget();
}

private async UniTask PreLoadProposeResources()
{
    foreach (ProposeListScrollDataConatainer container in proposedContainerList)
    {
        foreach (CardData cardData in container.CardDataList)
        {
            AsyncOperationHandle handle = cardData.LoadSprite(assetLoader, true);
            await handle.Task;
            cardData.LoadSuccess(handle);
        }
    }
}

// 결과
// 적용 전 288, 303, 242, 264, 262 Milliseconds, 평균 271.8Milliseconds
// 적용 후 142, 270, 150, 148, 150 Milliseconds, 평균 172Milliseconds
// 평균 약 99Milliseconds 감소