using System.Threading.Tasks;

namespace KeyedSemaphores.Samples;

public class Program
{
    public static async Task Main()
    {
        await ExampleProgram.RunAsync();
        await ExampleProgramUsingMultipleCollections.RunAsync();
        await ExampleProgramUsingMultipleDictionaries.RunAsync();
    }
}
