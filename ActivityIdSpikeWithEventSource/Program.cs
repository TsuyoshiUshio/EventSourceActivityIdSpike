using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace ActivityIdSpikeWithEventSource
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var task1 = Task.Run(async () => await HandleRequestAsync(Guid.NewGuid(), 1));
            var task2 = Task.Run(async () => await HandleRequestAsync(Guid.NewGuid(), 2));
            await Task.WhenAll(task1, task2);
        }

        static async Task HandleRequestAsync(Guid activityId, int num)
        {
            Console.WriteLine($"Starting request ({num}) with ID: " + activityId);

         //   MyActivityTracker.SetActivityId(activityId);
         using var activity = StartActivityId(activityId);
            DoSomeWork(num);
            Task.Run(() => DoSomeWork(num * 10 + 0));
            await NestedWork(num * 100);
            await Task.Delay(1000);
            await Task.Yield();
            Task.Run(() => DoSomeWork(num * 10 + 1));
            await NestedWork(num * 1000);
        //    MyActivityTracker.SetActivityId(Guid.Empty);
        }

        static IDisposable StartActivityId(Guid activityId)
        {
            MyActivityTracker.SetActivityId(activityId);
            return new CompleteActivity();
        }

        static async Task NestedWork(int num)
        {
//            MyActivityTracker.SetActivityId(Guid.NewGuid());
            using var activity = StartActivityId(Guid.NewGuid());
            Task.Run(() => DoSomeWork(num * 10 + 0));
            await Task.Delay(1000);
            await Task.Yield();
            Task.Run(() => DoSomeWork(num * 10 + 1));
  //          MyActivityTracker.SetActivityId(Guid.Empty);
        }

        static void DoSomeWork(int num)
        {
            Console.WriteLine($"({num})  ThreadId: {Thread.CurrentThread.ManagedThreadId} ActivityId: {EventSource.CurrentThreadActivityId}");
        }

    }

    class CompleteActivity : IDisposable
    {
        public void Dispose()
        {
            MyActivityTracker.SetActivityId(Guid.Empty);
        }
    }

    static class MyActivityTracker
    {
        static AsyncLocal<Guid> _activityId = new AsyncLocal<Guid>(ActivityIdChanged);

        public static void SetActivityId(Guid activityId)
        {
            _activityId.Value = activityId;
        }

        private static void ActivityIdChanged(AsyncLocalValueChangedArgs<Guid> args)
        {
            EventSource.SetCurrentThreadActivityId(args.CurrentValue);
        }
    }
}
