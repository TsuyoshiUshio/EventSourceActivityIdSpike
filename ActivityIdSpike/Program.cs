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
            
            EventSource.SetCurrentThreadActivityId(id);

            MyEventSource.Log.FooStart("starting ...");
            MyEventSource.Log.Message1($"Message1: id: {id}");
            PrintWithHeader("Main started");

            await Task.Delay(1000);
            PrintWithHeader("AfterListerStarted");
            var task1 = PropagateActivtyIdWithEventSource(1);
            var task3 = PropagateActivtyIdWithEventSource(2);
            await Task.WhenAll(task1, task3);
            PrintWithHeader("Main finished");
            MyEventSource.Log.FooStop("Stopping");

        }
       static async Task PropagateActivtyIdWithEventSource(int tid)
       {
            MyEventSource.Log.Message1("Message1");
            PrintWithHeader($"[{tid}] PropagateActivtyIdWithEventSource started");
         //   MyEventSource.Log.FooStart($"Child {tid} started");
            MyEventSource.Log.Message1("Message1");
            PrintWithHeader($"[{tid}] PropagateActivtyIdWithEventSource After starting propagation.");
            await Task.Yield();
            MyEventSource.Log.Message1("Message1");
            PrintWithHeader($"[{tid}] PropagateActivtyIdWithEventSource After Yield");
          //  MyEventSource.Log.FooStop($"Child {tid} stopped");   
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
            var log = $"[{DateTime.Now:HH:mm:ss.fff}] Tid: [{Thread.CurrentThread.ManagedThreadId}] CorrelationManager.ActivityId: [{Trace.CorrelationManager.ActivityId}] EventSource.CurrentThreadActivityId {EventSource.CurrentThreadActivityId} {message}";

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
