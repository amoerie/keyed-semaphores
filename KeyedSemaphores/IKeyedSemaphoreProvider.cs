namespace KeyedSemaphores
{
    internal interface IKeyedSemaphoreProvider
    {
        IKeyedSemaphore Provide(string key);
    }
}