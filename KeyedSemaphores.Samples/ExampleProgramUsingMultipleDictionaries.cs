using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores.Samples;

internal class ExampleProgramUsingMultipleDictionaries
{
    public static async Task RunAsync()
    {
        // You can create your own keyed semaphore dictionaries for advanced usage
        var dictionary1 = new KeyedSemaphoresDictionary<int>();
        var dictionary2 = new KeyedSemaphoresDictionary<int>();
        var dictionary1Tasks = Enumerable.Range(1, 4)
            .Select(async i =>
            {
                var key = (int) Math.Ceiling((double)i / 2);
                Log($"Dictionary 1 - Task {i:0}: I am waiting for key '{key}'");
                using (await dictionary1.LockAsync(key))
                {
                    Log($"Dictionary 1 - Task {i:0}: Hello world! I have key '{key}' now!");
                    await Task.Delay(50);
                }
                Log($"Dictionary 1 - Task {i:0}: I have released '{key}'");
            });
        var dictionary2Tasks = Enumerable.Range(1, 4)
            .Select(async i =>
            {
                var key = (int) Math.Ceiling((double)i / 2);
                Log($"Dictionary 2 - Task {i:0}: I am waiting for key '{key}'");
                using (await dictionary2.LockAsync(key))
                {
                    Log($"Dictionary 2 - Task {i:0}: Hello world! I have key '{key}' now!");
                    await Task.Delay(50);
                }

                Log($"Dictionary 2 - Task {i:0}: I have released '{key}'");
            });
        await Task.WhenAll(dictionary1Tasks.Concat(dictionary2Tasks).AsParallel());
        
        void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId:000} {message}");
        }

        /*
         * Output:

            14:11:50.903 #010 Dictionary 1 - Task 1: I am waiting for key '1'
            14:11:50.906 #010 Dictionary 1 - Task 1: Hello world! I have key '1' now!
            14:11:50.906 #010 Dictionary 1 - Task 2: I am waiting for key '1'
            14:11:50.916 #010 Dictionary 1 - Task 3: I am waiting for key '2'
            14:11:50.916 #010 Dictionary 1 - Task 3: Hello world! I have key '2' now!
            14:11:50.916 #010 Dictionary 1 - Task 4: I am waiting for key '2'
            14:11:50.932 #010 Dictionary 2 - Task 1: I am waiting for key '1'
            14:11:50.932 #010 Dictionary 2 - Task 1: Hello world! I have key '1' now!
            14:11:50.933 #010 Dictionary 2 - Task 2: I am waiting for key '1'
            14:11:50.947 #010 Dictionary 2 - Task 3: I am waiting for key '2'
            14:11:50.947 #010 Dictionary 2 - Task 3: Hello world! I have key '2' now!
            14:11:50.947 #010 Dictionary 2 - Task 4: I am waiting for key '2'
            14:11:50.963 #010 Dictionary 1 - Task 2: Hello world! I have key '1' now!
            14:11:50.963 #009 Dictionary 1 - Task 1: I have released '1'
            14:11:50.978 #010 Dictionary 1 - Task 4: Hello world! I have key '2' now!
            14:11:50.978 #009 Dictionary 1 - Task 3: I have released '2'
            14:11:50.994 #009 Dictionary 2 - Task 1: I have released '1'
            14:11:50.994 #010 Dictionary 2 - Task 2: Hello world! I have key '1' now!
            14:11:51.010 #010 Dictionary 2 - Task 3: I have released '2'
            14:11:51.010 #009 Dictionary 2 - Task 4: Hello world! I have key '2' now!
            14:11:51.026 #009 Dictionary 1 - Task 2: I have released '1'
            14:11:51.041 #009 Dictionary 1 - Task 4: I have released '2'
            14:11:51.057 #009 Dictionary 2 - Task 2: I have released '1'
            14:11:51.072 #009 Dictionary 2 - Task 4: I have released '2'

         */
    }
}
