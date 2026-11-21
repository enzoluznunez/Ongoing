using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace AccountingImmersiveAnalytics.App
{
    public class MainThread : MonoBehaviour
    {

        private static readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public static Task Run(Action action)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    tcs.SetException(e);
                }
            });
            return tcs.Task;
        }

        private void Update()
        {
            while (_queue.TryDequeue(out var action)) action();
        }
    }
}
