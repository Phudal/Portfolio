using System;
using System.Collections.Generic;
using System.IO;

public struct PoolEnumComparer : IEqualityComparer<PoolType>
{
    public bool Equals(PoolType x, PoolType y)
    {
        return x == y;
    }
    public int GetHashCode(PoolType obj)
    {
        return (int)obj;
    }
}

public class PrefabScriptFinder
{
    // Prefab에 특정 스크립트가 있는지 검사합니다.
    public static void PrefabScriptFind()
    {
        string basePath = "";
        
        Dictionary<PoolType, string> basePrefabPath = new Dictionary<PoolType, string>(new PoolEnumComparer());        

        List<string> files = new List<string>();
        List<string> fileNames = new List<string>();

        int exceptPoolType = 10;

        for (int i = exceptPoolType; i < (int)PoolType.MAX; i++)
        {
            string curPath = basePath + basePrefabPath[(PoolType)i] + ".prefab";
            files.Add(File.ReadAllText(curPath));//, "*.prefab"));

            fileNames.Add(curPath);
        }

        Console.WriteLine($"총 {files.Count}개의 파일을 검사합니다.");

        List<string> missingStrings = new List<string>
        {
            "0e8b1ec63f644624c8b001392b1e9310",     // MagicProjectile.cs
            "1344501a2c36d604199e8bbe174a164d",     // ArrowProjectile.cs
            "3fc53d35ba8d48045afab1dc3f677ce8",     // BulletProjectile.cs
            "abf3a11a91e91974c85f4b7603020eca",     // LightingProjectile.cs
            "758a6120d2f4c4549b89d3cd0c97cb8c",     // ShieldProjectile.cs
            "c8d4364b72b566c47a78713f6f4e633e",     // SkillProjectile.cs
            "7a56687a80adf2b4e84a5f09cad807e4",     // MultiPoolItem.cs

        };

        for (int i = 0; i < files.Count; i++)
        {
            bool foundStrings = false;

            foreach (string searchString in missingStrings)
            {
                if (files[i].Contains(searchString))
                {
                    foundStrings = true;
                    break;
                }
            }
            if (!foundStrings)
            {
                Console.WriteLine($"파일 '{Path.GetFileName(fileNames[i])}'에 필요한 문자열이 누락되었습니다.");
            }
        }
    }
}
