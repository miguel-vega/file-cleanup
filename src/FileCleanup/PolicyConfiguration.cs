namespace FileCleanup
{
    /// <summary>
    /// Configuration information for all policies.
    /// </summary>
    public class PolicyConfiguration
    {
        /// <summary>
        /// The maximum number of threads that will be used to enforce the policy. 
        /// Each policy is enforced on one thread.
        /// </summary>
        public int MaxThreads { get; set; }

        /// <summary>
        /// Collection of file cleanup policies.
        /// </summary>
        public Policy[] Policies { get; set; }
    }
}
