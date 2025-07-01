using System;
using System.Collections.Generic;


public delegate void BroadcastCallback();
public delegate void BroadcastCallback<T>(T arg1);
public delegate void BroadcastCallback<T, U>(T arg1, U arg2);
public delegate void BroadcastCallback<T, U, V>(T arg1, U arg2, V arg3);

public enum EBroadcastMode : byte
{
    DONT_REQUIRE_LISTENER,
    REQUIRE_LISTENER,
}

public enum EBroadcastListenerType
{
    ADD_LISTENER,
    REMOVE_LISTENER
}

internal static class BroadcasterInternal
{    
    public static Dictionary<EBroadcastKey, List<Delegate>> SubscriberDic = new Dictionary<EBroadcastKey, List<Delegate>>();
    public static readonly EBroadcastMode DEFAULT_MODE = EBroadcastMode.DONT_REQUIRE_LISTENER;

    public static void RemoveAllListener()
    {
        foreach (var kv in SubscriberDic)
        {
            kv.Value.Clear();
        }
        SubscriberDic.Clear();
    }

    public static void AddListener(EBroadcastKey brKey, Delegate listener)
    {
        if (listener == null)
        {
            return;
        }

        List<Delegate> delegateList;
        if (SubscriberDic.TryGetValue(brKey, out delegateList))
        {
            if (delegateList.Contains(listener))
            {
                return;
            }

            if (delegateList.Count > 0)
            {
                Delegate d = delegateList[0];

                if (d != null && d.GetType() != listener.GetType())
                {
                    throw new Exception(string.Format("Attempting to add listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being added has type {2}",
                                                       brKey, d.GetType().Name, listener.GetType().Name));
                }
            }

            delegateList.Add(listener);
        }
        else
        {
            delegateList = new List<Delegate>();
            delegateList.Add(listener);

            SubscriberDic.Add(brKey, delegateList);
        }
    }

    public static void RemoveListener(EBroadcastKey brKey, Delegate listener)
    {
        if (listener == null)
        {
            return;
        }

        List<Delegate> delegateList;
        if (SubscriberDic.TryGetValue(brKey, out delegateList))
        {
            delegateList.Remove(listener);

            if (delegateList.Count <= 0)
            {
                SubscriberDic.Remove(brKey);
            }
        }
    }
}

public static class Broadcaster
{
    private static Dictionary<EBroadcastKey, List<Delegate>> SubscriberDic = BroadcasterInternal.SubscriberDic;

    public static void RegisterListener(EBroadcastListenerType val, EBroadcastKey brKey, BroadcastCallback handler)
    {
        if (val == EBroadcastListenerType.ADD_LISTENER)
        {
            BroadcasterInternal.AddListener(brKey, handler);
        }
        else if (val == EBroadcastListenerType.REMOVE_LISTENER)
        {
            BroadcasterInternal.RemoveListener(brKey, handler);
        }
    }

    public static void Broadcast(EBroadcastKey brKey)
    {
        Broadcast(brKey, BroadcasterInternal.DEFAULT_MODE);
    }

    public static void Broadcast(EBroadcastKey brKey, EBroadcastMode mode)
    {
        List<Delegate> delegateList;
        if (SubscriberDic.TryGetValue(brKey, out delegateList))
        {
            for (int i = 0; i < delegateList.Count; ++i)
            {
                BroadcastCallback callback = delegateList[i] as BroadcastCallback;
                if (callback != null)
                {
                    callback();
                }
                else
                {
                    throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", brKey));
                }
            }
        }
        else
        {
            if (mode == EBroadcastMode.REQUIRE_LISTENER)
            {
                throw new Exception(string.Format("Broadcasting message {0} but no listener found.", brKey));
            }
        }
    }
}


public static class Broadcaster<T>
{
    private static Dictionary<EBroadcastKey, List<Delegate>> SubscriberDic = BroadcasterInternal.SubscriberDic;

    public static void RegisterListener(EBroadcastListenerType val, EBroadcastKey brKey, BroadcastCallback<T> handler)
    {
        if (val == EBroadcastListenerType.ADD_LISTENER)
        {
            BroadcasterInternal.AddListener(brKey, handler);
        }
        else if (val == EBroadcastListenerType.REMOVE_LISTENER)
        {
            BroadcasterInternal.RemoveListener(brKey, handler);
        }
    }

    public static void Broadcast(EBroadcastKey brKey, T arg1)
    {
        Broadcast(brKey, arg1, BroadcasterInternal.DEFAULT_MODE);
    }

    public static void Broadcast(EBroadcastKey brKey, T arg1, EBroadcastMode mode)
    {
        List<Delegate> delegateList;
        if (SubscriberDic.TryGetValue(brKey, out delegateList))
        {
            for (int i = 0; i < delegateList.Count; ++i)
            {
                BroadcastCallback<T> callback = delegateList[i] as BroadcastCallback<T>;
                if (callback != null)
                {
                    callback(arg1);
                }
                else
                {
                    throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", brKey));
                }
            }
        }
        else
        {
            if (mode == EBroadcastMode.REQUIRE_LISTENER)
            {
                throw new Exception(string.Format("Broadcasting message {0} but no listener found.", brKey));
            }
        }
    }
}


public static class Broadcaster<T, U>
{
    private static Dictionary<EBroadcastKey, List<Delegate>> SubscriberDic = BroadcasterInternal.SubscriberDic;

    public static void RegisterListener(EBroadcastListenerType val, EBroadcastKey brKey, BroadcastCallback<T, U> handler)
    {
        if (val == EBroadcastListenerType.ADD_LISTENER)
        {
            BroadcasterInternal.AddListener(brKey, handler);
        }
        else if (val == EBroadcastListenerType.REMOVE_LISTENER)
        {
            BroadcasterInternal.RemoveListener(brKey, handler);
        }
    }

    public static void Broadcast(EBroadcastKey brKey, T arg1, U arg2)
    {
        Broadcast(brKey, arg1, arg2, BroadcasterInternal.DEFAULT_MODE);
    }

    public static void Broadcast(EBroadcastKey brKey, T arg1, U arg2, EBroadcastMode mode)
    {
        List<Delegate> delegateList;
        if (SubscriberDic.TryGetValue(brKey, out delegateList))
        {
            for (int i = 0; i < delegateList.Count; ++i)
            {
                BroadcastCallback<T, U> callback = delegateList[i] as BroadcastCallback<T, U>;
                if (callback != null)
                {
                    callback(arg1, arg2);
                }
                else
                {
                    throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", brKey));
                }
            }
        }
        else
        {
            if (mode == EBroadcastMode.REQUIRE_LISTENER)
            {
                throw new Exception(string.Format("Broadcasting message {0} but no listener found.", brKey));
            }
        }
    }
}


public static class Broadcaster<T, U, V>
{
    private static Dictionary<EBroadcastKey, List<Delegate>> SubscriberDic = BroadcasterInternal.SubscriberDic;

    public static void RegisterListener(EBroadcastListenerType val, EBroadcastKey brKey, BroadcastCallback<T, U, V> handler)
    {
        if (val == EBroadcastListenerType.ADD_LISTENER)
        {
            BroadcasterInternal.AddListener(brKey, handler);
        }
        else if (val == EBroadcastListenerType.REMOVE_LISTENER)
        {
            BroadcasterInternal.RemoveListener(brKey, handler);
        }
    }

    public static void Broadcast(EBroadcastKey brKey, T arg1, U arg2, V arg3)
    {
        Broadcast(brKey, arg1, arg2, arg3, BroadcasterInternal.DEFAULT_MODE);
    }

    public static void Broadcast(EBroadcastKey brKey, T arg1, U arg2, V arg3, EBroadcastMode mode)
    {
        List<Delegate> delegateList;
        if (SubscriberDic.TryGetValue(brKey, out delegateList))
        {
            for (int i = 0; i < delegateList.Count; ++i)
            {
                BroadcastCallback<T, U, V> callback = delegateList[i] as BroadcastCallback<T, U, V>;
                if (callback != null)
                {
                    callback(arg1, arg2, arg3);
                }
                else
                {
                    throw new Exception(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", brKey));
                }
            }
        }
        else
        {
            if (mode == EBroadcastMode.REQUIRE_LISTENER)
            {
                throw new Exception(string.Format("Broadcasting message {0} but no listener found.", brKey));
            }
        }
    }
}
