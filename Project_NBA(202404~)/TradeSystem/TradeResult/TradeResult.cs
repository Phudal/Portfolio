using Cysharp.Threading.Tasks;
using Dimps.Application.API;
using Dimps.Application.AssetBundle;
using Dimps.Application.Card;
using Dimps.Application.Common;
using Dimps.Application.Common.Button;
using Dimps.Application.Flow;
using Dimps.Application.Scene;
using Dimps.Application.Sound;
using GVNC.Application.Upgrade;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;


namespace GVNC.Application.Trade
{
    public class TradeResult : SceneBaseFlow<TradeResult.Param>
    {
        public class Param : ISceneParameter
        {
            public CardParamPlayer cardParm;

            public Param(CardParamPlayer cardParam)
            {
                this.cardParm = cardParam;
            }
        }

        [SerializeField]
        UICustomButton customButtonOK;

        [SerializeField]
        Image cardImage;

        [SerializeField]
        UIPlayable uiPlayable;

        [Header("연출 애니메이션")]
        [SerializeField]
        AnimationClip animationClipIn;

        [SerializeField]
        float cardOpenSoundDelay = 0f;

        AssetLoader assetLoader = new AssetLoader();

        private Param curParam;

        public static void Open(Param param)
        {
            SceneProvider.Instance.ChangeScene(SceneContent.Type.TradeResult, param);
        }

        protected override Param OnRootStart()
        {
            var param = new Param(null);
            return param;
        }

        protected override void OnInitialize(Param param)
        {
            curParam = param;

            if (customButtonOK != null)
                customButtonOK.onClick.AddListener(() => { TradeTop.Open(new TradeTop.Param(TradeTop.TradeTopTab.ProposeList)); });
        }

        protected override async UniTask OnLoad(CancellationToken token)
        {
            //仮のログイン※Editor専用
            await APIUtility.EditorSimpleLogin();

            await LoadCard();
        }

        protected override async UniTask OnActivate(CancellationToken token)
        {
            await UniTask.Yield();
        }

        protected override void OnExecute(Param param)
        {
            General.Instance.Footer.Hide();
            StartCoroutine(StartAnimationCor());
        }

        protected override void OnFlowEnd(IFlowEndParameter param)
        {
            
        }

        protected override void OnDispose()
        {
            if (assetLoader != null)
            {
                assetLoader.Dispose();
                assetLoader = null;
            }
        }

        protected override void OnEscape()
        {
            if (customButtonOK != null)
                customButtonOK.onClick?.Invoke();
        }

        private IEnumerator StartAnimationCor()
        {
            uiPlayable?.Play(animationClipIn, AnimFinish);
            yield return new WaitForSeconds(cardOpenSoundDelay);
            SoundMgr.Instance().PlayIngameUiSeWithKeyType(SoundSeInfo.KeyType.offer_get_high_rare);
        }

        private void AnimFinish()
        { 
            General.Instance.Footer.Show();
        }

        private async UniTask LoadCard()
        {
            AsyncOperationHandle handle = assetLoader.LoadPlayerCardAsync(curParam.cardParm.CardId, curParam.cardParm.CurrentRarity);
            await handle.Task;
            cardImage.sprite = handle.Result as Sprite;
        }
    }
}

