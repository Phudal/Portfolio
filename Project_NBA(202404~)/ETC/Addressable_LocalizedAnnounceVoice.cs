public static void Set_Sound_Addressable()
{
    var settings = AddressableAssetSettingsDefaultObject.Settings;
    var group_bgm = settings.FindGroup(GroupName_BGM); // BGM_
    var group_se = settings.FindGroup(GroupName_SE); // SE_
    var group_voice = settings.FindGroup(GroupName_VOICE); // vo_

    AddressableUtility.ClearAddressableEntries(group_bgm);
    AddressableUtility.ClearAddressableEntries(group_se);
    AddressableUtility.ClearAddressableEntries(group_voice);

    var guids = AssetDatabase.FindAssets($"t:{nameof(AudioClip)}");

    foreach (var guid in guids)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var fileName = Path.GetFileName(path);

        if (fileName.StartsWith("BGM_"))
        {
            var address = "BGM/" + fileName;
            var entry = AddressableMenu.CreateOrMoveEntry(settings, guid, group_bgm);
            entry.address = address;
        }
        else if (fileName.StartsWith("SE_"))
        {
            var address = "SE/" + fileName;
            var entry = AddressableMenu.CreateOrMoveEntry(settings, guid, group_se);
            entry.address = address;
        }
        else if (fileName.StartsWith("vo_", System.StringComparison.OrdinalIgnoreCase))
        {
            string address = string.Empty;
            if (path.Contains("JP", System.StringComparison.OrdinalIgnoreCase))
            {
                address = "VOICE/JP/" + fileName;                        
            }
            else
            {
                address = "VOICE/EN/" + fileName;                        
            }
            var entry = AddressableMenu.CreateOrMoveEntry(settings, guid, group_voice);
            entry.address = address;
        }
        else
        {
            DebugTool.LogWarning($"Ignore unknown path [{path}]");
        }
    }

    AddressableUtility.OrderAddressableEntries(group_bgm);
    AddressableUtility.OrderAddressableEntries(group_se);
    AddressableUtility.OrderAddressableEntries(group_voice);
   
    AssetDatabase.SaveAssets();
}

public void ReleaseCurAnnounceHandle()
{
    Queue<string> ReleaseQueue = new Queue<string>();    
    foreach(var pair in handles)
    {
        if (pair.Value.IsDone = true && pair.Key.StartsWith("VOICE", StringComparison.OrdinalIgnoreCase))
        {
            ReleaseQueue.EnQueue(pair.Key);            
            Addressable.Release(pair.Value);
        }
    }

    while (ReleaseQueue.Count > 0)
    {
        handles.Remove(ReleaseQueue.Dequeue());
    }
}