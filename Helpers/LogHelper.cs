using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace TwitchClipper.Helpers
{
    public static class LogHelper
    {
        public static int Index = 0;

        public static async Task Log(string message, AsyncLock asyncLock)
        {
            using (await asyncLock.LockAsync())
            {
                Console.SetCursorPosition(0, Index);
                Console.Write(message);
            }
        }

        public static async Task Log(string message)
        {
            Console.SetCursorPosition(0, Index);
            await Task.Run(() => Console.Write(message));
            Index++;
        }
    }
}
