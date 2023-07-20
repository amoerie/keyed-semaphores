using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores.Samples;

internal class ExampleProgram
{
    public static async Task RunAsync()
    {
        var tasks = Enumerable.Range(1, 4)
            .Select(async i =>
            {
                var key = "Key" + Math.Ceiling((double)i / 2);
                Log($"Task {i:0}: I am waiting for key '{key}'");
                using (await KeyedSemaphore.LockAsync(key))
                {
                    Log($"Task {i:0}: Hello world! I have key '{key}' now!");
                    await Task.Delay(50);
                }

                Log($"Task {i:0}: I have released '{key}'");
            });
        await Task.WhenAll(tasks.AsParallel());

        void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId:000} {message}");
        }


        /*
         * Output:

        14:11:50.571 #001 Task 1: I am waiting for key 'Key1'
        14:11:50.584 #001 Task 1: Hello world! I have key 'Key1' now!
        14:11:50.585 #001 Task 2: I am waiting for key 'Key1'
        14:11:50.603 #001 Task 3: I am waiting for key 'Key2'
        14:11:50.603 #001 Task 3: Hello world! I have key 'Key2' now!
        14:11:50.604 #001 Task 4: I am waiting for key 'Key2'
        14:11:50.634 #009 Task 2: Hello world! I have key 'Key1' now!
        14:11:50.634 #010 Task 1: I have released 'Key1'
        14:11:50.664 #010 Task 3: I have released 'Key2'
        14:11:50.664 #009 Task 4: Hello world! I have key 'Key2' now!
        14:11:50.697 #009 Task 2: I have released 'Key1'
        14:11:50.727 #009 Task 4: I have released 'Key2'


         */
    }

    private static void Log(string message)
    {
        ;
    }
}
