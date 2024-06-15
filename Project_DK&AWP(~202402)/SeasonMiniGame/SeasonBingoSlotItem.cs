using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SeasonBingoSlotInfo
{
    public int index;

    public int frontIndex;

    public string imagePath;
    
    public RewardInfo rewardInfo;

    public bool isFlipped;
}


public class SeasonBingoSlotItem : MonoBehaviour
{
    public SeasonBingoSlotInfo cardData;

    [SerializeField] Image backImage;
    [SerializeField] Image frontImage;

    [SerializeField] TextMeshProUGUI tmp_randomIndex;
    [SerializeField] UISpineGraphic uISpineGraphic;

    Sequence bingoSeq;

    private void Awake()
    {
        uISpineGraphic.SetSkin("purple");
    }

    public void InitCardInfo(SeasonBingoSlotInfo info)
    {
        cardData = info;

        UpdateCardState();
    }

    public void RefreshCardState(bool isFlip)
    {
        cardData.isFlipped = isFlip;
    }

    private void UpdateCardState()
    {
        // 카드가 뒤집한 상태라면 아이콘을 표시하지 않음.
        backImage.gameObject.SetActive(!cardData.isFlipped);
        frontImage.gameObject.SetActive(cardData.isFlipped);
        if (cardData.isFlipped)
        {
            string fileName = $"{cardData.imagePath}_{(cardData.index - 1)}";
            frontImage.sprite = AtlasManager.Instance.GetSprite(fileName, AtlasType.Minigame);
        }

        tmp_randomIndex.gameObject.SetActive(!cardData.isFlipped);        
        tmp_randomIndex.text = cardData.frontIndex.ToString();
    }

    public void StartFlipAnim(TweenCallback callback = null)
    {
        var sequence = DOTween.Sequence();

        if (!cardData.isFlipped)
        {
            sequence.Append(transform.DORotate(new Vector3(0, 90, 0), 0.15f, RotateMode.Fast));
            sequence.AppendCallback(() =>
            {
                backImage.gameObject.SetActive(false);
                frontImage.gameObject.SetActive(true);
                tmp_randomIndex.gameObject.SetActive(false);
                string fileName = $"{cardData.imagePath}_{(cardData.index - 1)}";
                frontImage.sprite = AtlasManager.Instance.GetSprite(fileName, AtlasType.Minigame);

            });
            sequence.Append(transform.DORotate(new Vector3(0, 0, 0), 0.15f, RotateMode.Fast));
        }

        if (callback != null)            
            sequence.AppendCallback(callback);
    }

    public void StartBingoAnim(int timeIndex, TweenCallback callback = null)
    {
        bingoSeq.Kill();

        bingoSeq = DOTween.Sequence();
        if (timeIndex > 0)
            bingoSeq.AppendInterval(0.1f * timeIndex);

        bingoSeq.AppendCallback(() => {
            uISpineGraphic.gameObject.SetActive(true);
            uISpineGraphic.PlayAnimation("end", false, 1.0f, string.Empty );
        });
        bingoSeq.AppendInterval(2.5f + (CDefineData.max_bingo_line - timeIndex) * 0.1f);
        bingoSeq.AppendCallback(() => {            
            uISpineGraphic.gameObject.SetActive(false);
        });

        if (callback != null)
            bingoSeq.AppendCallback(callback);
    }
}
