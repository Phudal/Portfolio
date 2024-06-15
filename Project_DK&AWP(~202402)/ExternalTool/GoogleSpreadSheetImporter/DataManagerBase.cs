using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManagerBase //<T> where T : class, new()
{
    protected static object instance;

    public virtual object GetT()
    {
        return default;
    }
}