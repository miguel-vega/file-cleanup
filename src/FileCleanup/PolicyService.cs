using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FileCleanup
{
    /// <summary>
    /// Service used to enforce file cleanup policies.
    /// </summary>
    public class PolicyService
    {
        private readonly ILogger<PolicyService> logger;

        /// <summary>
        /// Creates an instance of <see cref="PolicyService"/>.
        /// </summary>
        /// <param name="logger">Implementation of the <see cref="ILogger{PolicyService}"/> interface.</param>
        public PolicyService(ILogger<PolicyService> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Enforces the cleanup policies aysnchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<List<PolicyResult>> EnforcePoliciesAsync(PolicyConfiguration policyConfiguration)
        {
            var policyResults = new ConcurrentBag<PolicyResult>();
            if (policyConfiguration.Policies.Length == 0) return policyResults.ToList();

            var tasks = new List<Task>();
            using (var semaphoreSlim = new SemaphoreSlim(policyConfiguration.MaxThreads))
            {
                foreach (var policy in policyConfiguration.Policies)
                {
                    logger.LogInformation($"Thread count: {semaphoreSlim.CurrentCount}.");
                    if (semaphoreSlim.CurrentCount < 1)
                    {
                        logger.LogInformation($"Max thread count reached. Wating for a thread to complete...");
                    }
                    semaphoreSlim.Wait();

                    logger.LogInformation($"Enforcing policy for directory path: {policy.DirectoryPath}.");
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            policyResults.Add(EnforcePolicy(policy));
                        }
                        finally
                        {
                            semaphoreSlim.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks);
                logger.LogInformation("Finished enforcing all policies.");
            }

            return policyResults.ToList();
        }

        #region Helper Methods

        /// <summary>
        /// Enforces the specified policy.
        /// </summary>
        /// <returns>Returns the result of enforcing the policy as a <see cref="CleanupPolicyResult"/>.</returns>
        private PolicyResult EnforcePolicy(Policy policy)
        {
            var policyResult = new PolicyResult
            {
                DirectoryPath = policy.DirectoryPath
            };

            if (!Directory.Exists(policy.DirectoryPath))
            {
                logger.LogWarning($"Can't enforce policy on {policy.DirectoryPath} because the path does not exist.");
                return policyResult;
            }

            var lastWriteTimeInUtc = DateTime.UtcNow.AddDays(-policy.OlderThanInDays);

            var stopwatch = Stopwatch.StartNew();
            var cleanupResults = policy.IsRecursive
                ? CleanupRecursive(policy.DirectoryPath, policy.SearchPattern,
                    lastWriteTimeInUtc)
                : Cleanup(policy.DirectoryPath, policy.SearchPattern, lastWriteTimeInUtc);
            var cleanupDuration = stopwatch.Elapsed;

            policyResult.PolicyRuntime = cleanupDuration;
            policyResult.SuccessCount = cleanupResults.Item1;
            policyResult.FailureCount = cleanupResults.Item2;

            return policyResult;
        }

        /// <summary>
        /// Cleans up the directory recursively based on the specified parameters.
        /// </summary>
        /// <param name="directoryPath">The path of the directory where the policy will be enforced.</param>
        /// <param name="searchPattern">The search pattern to use that will determine which files to cleanup.</param>
        /// <param name="dateTimeInUtc">The date time in UTC that will be used to determine the retention of the files in the directory.</param>
        /// <returns>Returns a tuple with the number of files where the policy was successfully and unsuccessfully enforced.</returns>
        private Tuple<int, int> CleanupRecursive(string directoryPath, string searchPattern,
            DateTime dateTimeInUtc)
        {
            var successCount = 0;
            var failureCount = 0;

            var tuple1 = Cleanup(directoryPath, searchPattern, dateTimeInUtc);
            successCount += tuple1.Item1;
            failureCount += tuple1.Item2;

            foreach (var directory in Directory.GetDirectories(directoryPath))
            {
                /**
                 * DfsrPrivate folder is a staging folder used by DFSR to cache
                 * new and changed files for replication. Attempting to delete 
                 * this folder can have serious consequences and more often then 
                 * not will fail because of permissions.
                **/

                if (directory.Contains("DfsrPrivate")) continue;

                try
                {
                    var tuple2 = CleanupRecursive(directory, searchPattern, dateTimeInUtc);
                    successCount += tuple2.Item1;
                    failureCount += tuple2.Item2;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception.Message);
                    failureCount++;
                }
            }


            return new Tuple<int, int>(successCount, failureCount);
        }

        /// <summary>
        /// Cleans up the directory based on the specified parameters.
        /// </summary>
        /// <param name="directoryPath">The path of the directory where the policy will be enforced.</param>
        /// <param name="searchPattern">The search pattern to use that will determine which files to cleanup.</param>
        /// <param name="dateTimeInUtc">The date time in UTC that will be used to determine the retention of the files in the directory.</param>
        /// <returns>Returns a tuple with the number of files where the policy was successfully and unsuccessfully enforced.</returns>
        private Tuple<int, int> Cleanup(string directoryPath, string searchPattern, DateTime dateTimeInUtc)
        {
            var successCount = 0;
            var failureCount = 0;

            var files = Directory.GetFiles(directoryPath, searchPattern);
            foreach (var file in files)
            {
                try
                {
                    var lastWriteTimeInUtc = File.GetLastWriteTimeUtc(file);
                    if (lastWriteTimeInUtc >= dateTimeInUtc) continue;

                    logger.LogInformation($"Deleting file: {file}.");
                    File.Delete(file);
                    logger.LogInformation($"Successfully deleted: {file}.");
                    successCount++;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, $"Failed to delete: {file}.");
                    failureCount++;
                }
            }

            return new Tuple<int, int>(successCount, failureCount);
        }

        #endregion
    }
}
