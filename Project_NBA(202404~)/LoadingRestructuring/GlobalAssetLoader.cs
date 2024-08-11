public class GlobalAssetLoadManager : SingletonMonoBehaviour<GlobalAssetLoadManager, GlobalAssetLoadManager>, ISingletonHandler
{
#if UNITY_EDITOR
    [SerializeField] private bool useDebugMode = false;

    public bool IsAssetLoaderDebugMode() => useDebugMode;
#endif

    // Use for frequently resource    
    public AssetLoader globalAssetLoader = new AssetLoader();

    // Use for one scene resource
    public AssetLoader sceneAssetLoader = new AssetLoader();
    
    // ~~~
}    