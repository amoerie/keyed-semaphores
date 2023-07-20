using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores.Samples;

internal class ExampleProgramUsingMultipleCollections
{
    public static async Task RunAsync()
    {
        // You can create your own keyed semaphore collections for advanced usage
        var collection1 = new KeyedSemaphoresCollection<int>();
        var collection2 = new KeyedSemaphoresCollection<int>();
        var collection1Tasks = Enumerable.Range(1, 4)
            .Select(async i =>
            {
                var key = (int) Math.Ceiling((double)i / 2);
                Log($"Collection 1 - Task {i:0}: I am waiting for key '{key}'");
                using (await collection1.LockAsync(key))
                {
                    Log($"Collection 1 - Task {i:0}: Hello world! I have key '{key}' now!");
                    await Task.Delay(50);
                }
                Log($"Collection 1 - Task {i:0}: I have released '{key}'");
            });
        var collection2Tasks = Enumerable.Range(1, 4)
            .Select(async i =>
            {
                var key = (int) Math.Ceiling((double)i / 2);
                Log($"Collection 2 - Task {i:0}: I am waiting for key '{key}'");
                using (await collection2.LockAsync(key))
                {
                    Log($"Collection 2 - Task {i:0}: Hello world! I have key '{key}' now!");
                    await Task.Delay(50);
                }

                Log($"Collection 2 - Task {i:0}: I have released '{key}'");
            });
        await Task.WhenAll(collection1Tasks.Concat(collection2Tasks).AsParallel());
        
        void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId:000} {message}");
        }

        /*
         * Output:

            14:11:50.731 #009 Collection 1 - Task 1: I am waiting for key '1'
            14:11:50.731 #009 Collection 1 - Task 1: Hello world! I have key '1' now!
            14:11:50.731 #009 Collection 1 - Task 2: I am waiting for key '1'
            14:11:50.743 #009 Collection 1 - Task 3: I am waiting for key '2'
            14:11:50.743 #009 Collection 1 - Task 3: Hello world! I have key '2' now!
            14:11:50.744 #009 Collection 1 - Task 4: I am waiting for key '2'
            14:11:50.759 #009 Collection 2 - Task 1: I am waiting for key '1'
            14:11:50.760 #009 Collection 2 - Task 1: Hello world! I have key '1' now!
            14:11:50.760 #009 Collection 2 - Task 2: I am waiting for key '1'
            14:11:50.774 #009 Collection 2 - Task 3: I am waiting for key '2'
            14:11:50.774 #009 Collection 2 - Task 3: Hello world! I have key '2' now!
            14:11:50.775 #009 Collection 2 - Task 4: I am waiting for key '2'
            14:11:50.791 #010 Collection 1 - Task 1: I have released '1'
            14:11:50.791 #009 Collection 1 - Task 2: Hello world! I have key '1' now!
            14:11:50.806 #009 Collection 1 - Task 3: I have released '2'
            14:11:50.806 #010 Collection 1 - Task 4: Hello world! I have key '2' now!
            14:11:50.822 #010 Collection 2 - Task 1: I have released '1'
            14:11:50.822 #009 Collection 2 - Task 2: Hello world! I have key '1' now!
            14:11:50.837 #009 Collection 2 - Task 3: I have released '2'
            14:11:50.837 #010 Collection 2 - Task 4: Hello world! I have key '2' now!
            14:11:50.853 #010 Collection 1 - Task 2: I have released '1'
            14:11:50.868 #010 Collection 1 - Task 4: I have released '2'
            14:11:50.884 #010 Collection 2 - Task 2: I have released '1'
            14:11:50.900 #010 Collection 2 - Task 4: I have released '2'


         */
    }
}
