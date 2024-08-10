using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;

namespace ActivityIdSpike
{
    internal class Program
    {
        static async Task Main(string[] args)
        { 

            MyEventListener listener = new MyEventListener();
            Guid id = Guid.NewGuid();
            //EventSource.SetCurrentThreadActivityId(id);
    //        Trace.CorrelationManager.ActivityId = id;
   //            MyEventSource.Log.FooStart("starting");

            PrintWithHeader("Main started");




            await Task.Delay(1000);
            PrintWithHeader("AfterListerStarted");
          //  Trace.CorrelationManager.ActivityId = Guid.NewGuid();
//            var task1 = NoPropagateActivityID(1);
            var task1 = PropagateByCorrelationManager(1);
//            var task2 = NoPropagateActivityID(2);
//            var task3 = NoPropagateActivityID(3);
            var task3 = PropagateByCorrelationManager(3);
 //           var task4 = NoPropagateActivityID(4);
            //await Task.WhenAll(task1, task2, task3, task4);
            await Task.WhenAll(task1, task3);
            PrintWithHeader("Main finished");
     //       MyEventSource.Log.FooStop("Stopping");
        }


        static async Task PropagateActivityID(int tid)
        {
            PrintWithHeader($"[{tid}] PropagateActivityID started");
            Guid id = Guid.NewGuid();
            // EventSource.SetCurrentThreadActivityId(id);
            Trace.CorrelationManager.ActivityId = id;

            PrintWithHeader($"[{tid}] PropagateActivityID updatedActivityId");

            MyEventSource.Log.FooStart("starting");
            PrintWithHeader($"[{tid}] PropagateActivityID After starting propagation.");
            await Task.Yield();
            PrintWithHeader($"[{tid}] PropagateActivityID After Yield");

            await RandomWait($"[{tid}] PropagateActivityID");
            PrintWithHeader($"[{tid}] PropagateActivityID After RandomWait");

            await RandomWaitWithConfigureAwaitFalse($"[{tid}] PropagateActivityID");
            PrintWithHeader($"[{tid}] PropagateActivityID After RandomWaitWithConfigureAwaitFalse");

            MyEventSource.Log.FooStop("Stopping");
           // Debug.Assert(EventSource.CurrentThreadActivityId == id);
        }

        static async Task NoPropagateActivityID(int tid)
        {
            PrintWithHeader($"[{tid}] NoPropagateActivityID started");
            Guid id = Guid.NewGuid();
            //EventSource.SetCurrentThreadActivityId(id);
            Trace.CorrelationManager.ActivityId = id;
            PrintWithHeader($"[{tid}] NoPropagateActivityID updatedActivityId");
            await Task.Yield();
            PrintWithHeader($"[{tid}] NoPropagateActivityID After Yield");
            await RandomWait($"[{tid}] NoPropagateActivityID");
            PrintWithHeader($"[{tid}] NoPropagateActivityID After RandomWait");

            await RandomWaitWithConfigureAwaitFalse($"[{tid}] NoPropagateActivityID");
            PrintWithHeader($"[{tid}] NoPropagateActivityID After RandomWaitWithConfigureAwaitFalse");

            Debug.Assert(EventSource.CurrentThreadActivityId != id);
        }

        static async Task PropagateByCorrelationManager(int tid)
        {
            Guid id = Guid.NewGuid();
            Trace.CorrelationManager.ActivityId = id;
            PrintWithHeader($"[{tid}] PropagateByCorrelationManager started");

            PrintWithHeader($"[{tid}] PropagateByCorrelationManager updatedActivityId");
            await Task.Yield();
            PrintWithHeader($"[{tid}] PropagateByCorrelationManager After Yield");
            await RandomWait($"[{tid}] PropagateByCorrelationManager", false, false);
            PrintWithHeader($"[{tid}] PropagateByCorrelationManager After RandomWait");

            await RandomWaitWithConfigureAwaitFalse($"[{tid}] PropagateByCorrelationManager", false);
            PrintWithHeader($"[{tid}] PropagateByCorrelationManager After RandomWaitWithConfigureAwaitFalse");

            await RandomWait($"[{tid}] PropagateByCorrelationManager user false", false, false);

        }


        static async Task RandomWait(string parent, bool restoreActivityId = false, bool generateNewIdAndSetToThread = false)
        {
            if (generateNewIdAndSetToThread)
            {
               var activityId = Guid.NewGuid();
                EventSource.SetCurrentThreadActivityId(activityId);
                Trace.CorrelationManager.ActivityId = activityId;
            }

            if (restoreActivityId)
            {
                var activityId = Trace.CorrelationManager.ActivityId;
                EventSource.SetCurrentThreadActivityId(activityId);
            }
            Random random = new Random();
            int delay = random.Next(1000, 5000);
            PrintWithHeader($"RandomWait Parent:{parent} start");
            await Task.Delay(delay);
            PrintWithHeader($"RandomWait Parent:{parent} end");
        }

        static async Task RandomWaitWithConfigureAwaitFalse(string parent, bool restoreActivityId = false)
        {
            if (restoreActivityId)
            {
                var activityId = Trace.CorrelationManager.ActivityId;
                EventSource.SetCurrentThreadActivityId(activityId);
            }
            Random random = new Random();
            int delay = random.Next(1000, 5000);
            PrintWithHeader($"RandomWaitConfiguraAwaitFalse Parent:{parent} start");
            await Task.Delay(delay).ConfigureAwait(false);
            PrintWithHeader($"RandomWaitConfiguraAwaitFalse Parent:{parent} end");
        }

        static void PrintWithHeader(string message)
        {
//            var log = $"[{DateTime.Now:HH:mm:ss.fff}] Tid: [{Thread.CurrentThread.ManagedThreadId}] ActivityId: [{Trace.CorrelationManager.ActivityId}] ActivitId ES: [{EventSource.CurrentThreadActivityId}] {message}";
            var log = $"[{DateTime.Now:HH:mm:ss.fff}] Tid: [{Thread.CurrentThread.ManagedThreadId}] CorrelationManager.ActivityId: [{Trace.CorrelationManager.ActivityId}] {message}";

            //    Console.WriteLine(log);
            MyEventSource.Log.Message1(log);
        }}

        [EventSource(Name = "MyEventSource")]
    class MyEventSource : EventSource
    {
        public static MyEventSource Log = new MyEventSource();

        [Event(1)]
        public void Message1(string arg1) => WriteEvent(1, arg1);

        [Event(2)]
        public void FooStart(string arg1) => WriteEvent(2, arg1);

        [Event(3)]
        public void FooStop(string arg1) => WriteEvent(3, arg1);
    }

    class MyEventListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
            {
                Console.WriteLine("TplEventSource found");
                EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
            }
            else if (eventSource.Name == "MyEventSource")
            {
                Console.WriteLine("{0,-5} {1,-40} {1,-40} {2,-15} {3}", "TID", "Activity ID", "RelativeActivtyId", "Event", "Arguments");
                EnableEvents(eventSource, EventLevel.Informational);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (this)
            {
                Console.Write("{0,-5} {1,-40} {1,-40} {2,-15} ", eventData.OSThreadId, eventData.ActivityId, eventData.RelatedActivityId, eventData.EventName);
                if (eventData.Payload.Count == 1)
                {
                    Console.WriteLine($"OnEventWritten {eventData.Payload[0]}");
                }
                else
                {
                    Console.WriteLine();
                }
            }
        }
    }
}
