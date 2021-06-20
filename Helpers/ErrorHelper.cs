using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchClipper.Helpers
{
    public static class ErrorHelper
    {
        public static async Task LogAndExit(string error)
        {
            await LogHelper.Log(error);
            Environment.Exit(-1);
        }
    }
}
