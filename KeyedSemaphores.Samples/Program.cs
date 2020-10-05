using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedSemaphores.Samples
{
    class Program
    {
        static async Task Main()
        {
            var tasks = Enumerable.Range(1, 4)
                .Select(SayHelloWorldAsync)
                .AsParallel();
            await Task.WhenAll(tasks);
            
            /*
             * Output:

            09:32:06.987 #001 Task 1: I am waiting for key 'Key1'
            09:32:06.996 #001 Task 1: Hello world! I have key 'Key1' now!
            09:32:07.001 #001 Task 2: I am waiting for key 'Key1'
            09:32:07.002 #001 Task 3: I am waiting for key 'Key2'
            09:32:07.002 #001 Task 3: Hello world! I have key 'Key2' now!
            09:32:07.002 #001 Task 4: I am waiting for key 'Key2'
            09:32:07.060 #006 Task 4: Hello world! I have key 'Key2' now!
            09:32:07.060 #007 Task 2: Hello world! I have key 'Key1' now!
            09:32:07.062 #005 Task 1: I have released 'Key1'
            09:32:07.062 #004 Task 3: I have released 'Key2'
            09:32:07.121 #004 Task 4: I have released 'Key2'
            09:32:07.121 #005 Task 2: I have released 'Key1'

             */
        }
        
        static async Task SayHelloWorldAsync(int i)
        {
            string key = "Key" + Math.Ceiling((double) i / 2);
            Log($"Task {i:0}: I am waiting for key '{key}'");
            using (await KeyedSemaphore.LockAsync(key))
            {
                Log($"Task {i:0}: Hello world! I have key '{key}' now!");
                await Task.Delay(50);
            }
            Log($"Task {i:0}: I have released '{key}'");
        }

        static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} #{Thread.CurrentThread.ManagedThreadId:000} {message}");
        }
    }
}