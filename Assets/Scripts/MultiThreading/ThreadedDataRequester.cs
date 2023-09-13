using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    Queue<ThreadInfo> threadInfos = new Queue<ThreadInfo>();
    private static ThreadedDataRequester instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        if(threadInfos.Count > 0)
        {
            for(int i = 0; i < threadInfos.Count; i++)
            {
                lock (threadInfos)
                {
                    ThreadInfo threadInfo = threadInfos.Dequeue();
                    threadInfo.callback.Invoke(threadInfo.parameter);
                }
            }
        }
    }
    
    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate
        {
            instance.DataThread(generateData, callback);
        };

        new Thread(threadStart).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        lock (threadInfos)
        {
            threadInfos.Enqueue(new ThreadInfo(callback, data));
        }
    }

    private struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
