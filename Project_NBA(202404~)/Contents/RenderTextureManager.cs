
public void InitializeModel()
{
    ///
    ///...
    ///

    if (renderedDictionary.ContainsKey(modelID) == false)
    {
        AsyncOperationHandle[] handles = new AsyncOperationHandle[(int)AssetType[Max]];
        {
            handles[(int)AssetType.PlayerModel] = assetLoader.LoadPlayerModelAsync(modelID);
            handles[(int)AssetType.UniformMaterial] = assetLoader.LoadUniformMaterialAsync(uniformTeamId, IngameTeamType.Home);
            handles[(int)AssetType.NumberMaterial] = assetLoader.LoadNumberMaterialAsync(modelID, uniformNoText, IngameTeamType.Home);
        }

        await handles[(int)AssetType.PlayerModel].WithCancellation(cancellationToken: token);
        await handles[(int)AssetType.UniformMaterial].WithCancellation(cancellationToken: token);
        await handles[(int)AssetType.NumberMaterial].WithCancellation(cancellationToken: token);

        GameObject renderedObject = GameObject.Instantiate(handles[0].Result as GameObject, tr_renderedObject);
        renderedObject.AddComponent<PlayerControll>();

        PlayerControll playerControll = renderedObject.GetComponent<PlayerControll>();

        Material uniformMaterial = handles[(int)AssetType.UniformMaterial].Result as Material;
        Material numberMaterial = handles[(int)AssetType.numberMaterial].Result as Material;

        playerControll.SetupUniform(uniformMaterial, numberMaterial, uniformMaterial);

        // 선수 키차이에 따른 모델링 scale 조정
        playerControll.SetPlayerHeightScaleBone(); 
        playerControll.SetPlayerHeightScale(heightScale)
    }
}