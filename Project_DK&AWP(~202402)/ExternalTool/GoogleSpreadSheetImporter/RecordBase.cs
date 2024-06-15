using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordBase
{
    protected List<object> parameterList = new List<object>();

    public virtual void DataSet(List<object> list)
    {

    }

    public virtual List<object> DataGet()
    {
        return parameterList;
    }

    public virtual void RecordInitialize()
    {

    }

    public virtual void RecordInitialize(params string[] mylist)
    {

    }
}
