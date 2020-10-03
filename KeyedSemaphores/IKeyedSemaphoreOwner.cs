namespace KeyedSemaphores
{
    internal interface IKeyedSemaphoreOwner
    {
        void Return(IKeyedSemaphore keyedSemaphore);
    }
}