namespace FileCleanup
{
    /// <summary>
    /// A policy that specifies what files in a directory will be cleaned up.
    /// </summary>
    public class Policy
    {
        /// <summary>
        /// The path of the directory where the policy will be enforced.
        /// </summary>
        public string DirectoryPath { get; set; }

        /// <summary>
        /// The search pattern used to determine which files to cleanup.
        /// </summary>
        public string SearchPattern { get; set; }

        /// <summary>
        /// Indicates if the policy should be recursively enforced on the directory.
        /// </summary>
        public bool IsRecursive { get; set; }

        /// <summary>
        /// The retention period of the files in days.
        /// </summary>
        public int OlderThanInDays { get; set; }
    }
}
