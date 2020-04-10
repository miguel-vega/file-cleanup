using System;

namespace FileCleanup
{
    /// <summary>
    /// Result for a policy that was attempted to be enforced.
    /// </summary>
    public class PolicyResult
    {
        /// <summary>
        /// The path of the directory where the policy will be enforced.
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// The amount of time it took to enforce the policy.
        /// </summary>
        public TimeSpan PolicyRuntime { get; set; }

        /// <summary>
        /// The number of files successfully cleaned according to its policy.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// The number of files failed to be cleaned according to its policy.
        /// </summary>
        public int FailureCount { get; set; }
    }
}
