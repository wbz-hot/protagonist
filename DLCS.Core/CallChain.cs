using System;
using System.Threading.Tasks;
using DLCS.Core.Guard;

namespace DLCS.Core
{
    /// <summary>
    /// Contains helper methods for calling consecutive requests.
    /// </summary>
    public class CallChain
    {
        /// <summary>
        /// Execute all of the provided functions until one returns false, at which point execution stops.
        /// </summary>
        /// <param name="commands">List of commands to execute.</param>
        /// <returns></returns>
        public static async Task<bool> ExecuteInSequence(params Func<Task<bool>>[] commands)
        {
            commands.ThrowIfNullOrEmpty(nameof(commands));
            
            foreach (var command in commands)
            {
                if (!await command())
                    return false;
            }

            return true;
        }
    }
}