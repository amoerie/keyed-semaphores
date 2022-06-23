using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores.Samples;

internal class ExampleProgramUsingMultipleCollections
{
    public static async Task RunAsync()
    {
        // KeyedSemaphore.LockAsync(string key) is a shorthand that uses a static singleton KeyedSemaphoreCollection<string>
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

        13:23:41.284 #001 Collection 1 - Task 1: I am waiting for key '1'
        13:23:41.297 #001 Collection 1 - Task 1: Hello world! I have key '1' now!
        13:23:41.299 #001 Collection 1 - Task 2: I am waiting for key '1'
        13:23:41.302 #001 Collection 1 - Task 3: I am waiting for key '2'
        13:23:41.302 #001 Collection 1 - Task 3: Hello world! I have key '2' now!
        13:23:41.302 #001 Collection 1 - Task 4: I am waiting for key '2'
        13:23:41.303 #001 Collection 2 - Task 1: I am waiting for key '1'
        13:23:41.303 #001 Collection 2 - Task 1: Hello world! I have key '1' now!
        13:23:41.304 #001 Collection 2 - Task 2: I am waiting for key '1'
        13:23:41.304 #001 Collection 2 - Task 3: I am waiting for key '2'
        13:23:41.306 #001 Collection 2 - Task 3: Hello world! I have key '2' now!
        13:23:41.306 #001 Collection 2 - Task 4: I am waiting for key '2'
        13:23:41.370 #005 Collection 2 - Task 3: I have released '2'
        13:23:41.370 #008 Collection 1 - Task 3: I have released '2'
        13:23:41.370 #009 Collection 1 - Task 1: I have released '1'
        13:23:41.370 #007 Collection 2 - Task 1: I have released '1'
        13:23:41.371 #010 Collection 1 - Task 4: Hello world! I have key '2' now!
        13:23:41.371 #012 Collection 2 - Task 4: Hello world! I have key '2' now!
        13:23:41.371 #013 Collection 2 - Task 2: Hello world! I have key '1' now!
        13:23:41.371 #011 Collection 1 - Task 2: Hello world! I have key '1' now!
        13:23:41.437 #008 Collection 1 - Task 2: I have released '1'
        13:23:41.437 #009 Collection 2 - Task 2: I have released '1'
        13:23:41.437 #010 Collection 1 - Task 4: I have released '2'
        13:23:41.437 #011 Collection 2 - Task 4: I have released '2'

         */
    }
}
