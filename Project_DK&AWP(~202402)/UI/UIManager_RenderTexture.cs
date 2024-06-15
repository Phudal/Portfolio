using UnityEngine;
using CoffeeLibrary;

public partial class UIManager
{
    #region RenderTexture
    [NaughtyAttributes.HorizontalLine(color: NaughtyAttributes.EColor.Gray)]

    [SerializeField] Transform uiEntityCamTr;
    [SerializeField] GameObject baseSpumRT;
    [SerializeField] CommonPoolController uiEntityPoolCtrl;

    void OnDisable()
    {
        Messenger<bool>.RegisterListener(EMessengerListenerType.REMOVE_LISTENER, EMessengerID.E_RENDER_TEXTURE_SET, SetRenderTextureState);
    }

    public void RegisterMessenger()
    {
        Messenger<bool>.RegisterListener(EMessengerListenerType.ADD_LISTENER, EMessengerID.E_RENDER_TEXTURE_SET, SetRenderTextureState);
    }

    public void SetRenderTextureState(bool state)
    {
        if (state)
        {
            if( uiEntityCamTr.gameObject.activeSelf == false )
                uiEntityCamTr.gameObject.SetActive(true);
        }
        else
        {
            uiEntityCamTr.gameObject.SetActive(false);
            uiEntityPoolCtrl.Hide();
        }
    }


    /// <summary>
    /// RenderTexture로 쓸 Entity를 초기화 및 반환
    /// </summary>
    /// <param name="entityCover"></param>
    /// <param name="rideCover"></param>
    /// <param name="scale"></param>
    /// <param name="setSPUM"></param>
    /// <param name="setClickState"></param>
    /// <param name="cameraYOffset"></param>
    /// <param name="side"></param>
    /// <returns></returns>
    public UIEntityPrefabRT GetRTEntity(int entityCover, int rideCover, float scale, 
        float cameraYOffset = 5.0f, SIDE side = SIDE.LEFT)
    {
        var ret = uiEntityPoolCtrl.GetPoolItem<UIEntityPrefabRT>();
        if (ret == null)
        {
            Logger.LogError($"{System.Reflection.MethodBase.GetCurrentMethod().Name} is return null");
            return null;
        }

        ret.gameObject.SetActive(true);
        ret.gameObject.transform.SetParent(uiEntityPoolCtrl.gameObject.transform);
        ret.gameObject.transform.position = new Vector3(
            uiEntityCamTr.position.x,
            uiEntityCamTr.position.y - cameraYOffset,
            0.0f);

        ret.InitBaseEntity(entityCover, rideCover, scale, 
            baseSpumRT, side);

        SetRenderTextureState(true);

        return ret;
    }

    public UIEntityPrefabRT GetRTEntity_Reward(REWARD_TYPE type, int index, float scale,
        float cameraYOffset = 5.0f)
    {
        var ret = uiEntityPoolCtrl.GetPoolItem<UIEntityPrefabRT>();
        if (ret == null)
        {
            Logger.LogError($"{System.Reflection.MethodBase.GetCurrentMethod().Name} is return null");
            return null;
        }

        ret.gameObject.SetActive(true);
        ret.gameObject.transform.SetParent(uiEntityPoolCtrl.gameObject.transform);
        ret.gameObject.transform.position = new Vector3(
            uiEntityCamTr.position.x,
            uiEntityCamTr.position.y - cameraYOffset, 
            0.0f);

        ret.InitBaseEntity_Reward(type, index, scale, 
            baseSpumRT );

        SetRenderTextureState(true);

        return ret;
    }

    /// <summary>
    /// RenderTexture를 Pool에 다시 넣습니다. 팝업이 꺼지거나 할 때에 해당 함수가 아닌
    /// 브로드 캐스팅으로 SetRenderTextureState()를 호출해야합니다.
    /// </summary>
    /// <param name="ob"></param>
    public void ReturnPoolEntityRT(GameObject ob)
    {
        uiEntityPoolCtrl.ReturnPool(ob);
    }
    #endregion
}
