public class CompoundCardData
{
    public CompoundCardData(CardData cardData)
    {
        compoundCardType = CompoundCardType.CommonCard;
        viewType = ViewType.Rarity;
        Status = AssetLoadStatus.None;
        this.cardData = cardData;
    }

    public CompoundCardData(UserMaterialCardData userMaterialCardData)
    {
        compoundCardType = CompoundCardType.MaterialCard;
        viewType = ViewType.Rarity;
        Status = AssetLoadStatus.None;
        this.userMaterialCardData = userMaterialCardData;
    }
}

// Util Function
public List<CompoundCardData> GetUserHaveRarityUpMaterialCard()
{
    List<CompoundCardData> ret = new List<CompoundCardData>();
    foreach (KeyValuePair<(ItemType type, int id), ItemInformation> item in userItemList)
    {
        if (item.Key.type == ItemType.MaterialCard)
        {
            ItemEntity entity = null;
            if (MasterDataManager.Instance.ItemMaster.TryGetItemEntity(item.Key.id, out entity) == false)
            {
                continue;
            }

            if ((Rarity)entity.EffectTarget >= Rarity.Epic)
            {
                continue;
            }

            for (int i = 0; i < item.Value.ItemNum; i++)
            {
                UserMaterialCardData userMaterial = new UserMaterialCardData(item.Key.id, 1);
                CompoundCardData compoundData = new CompoundCardData(userMaterial);                        
                ret.Add(compoundData);
            }
        }
    }

    ret.Sort((x, y) => x.userMaterialCardData.Index - y.userMaterialCardData.Index);

    return ret;
}