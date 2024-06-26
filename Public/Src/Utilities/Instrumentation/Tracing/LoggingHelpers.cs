// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.ContractsLight;
using BuildXL.Utilities;
using BuildXL.Utilities.Core;
using BuildXL.Utilities.Instrumentation.Common;

namespace BuildXL.Tracing
{
    /// <summary>
    /// Logging helper methods
    /// </summary>
    public static class LoggingHelpers
    {
        /// <summary>
        /// Logs a PerfCollector
        /// </summary>
        public static void LogPerformanceCollector(PerformanceCollector.Aggregator aggregator, LoggingContext loggingContext, string description, long? duration = null)
        {
            Contract.RequiresNotNull(aggregator);
            Contract.RequiresNotNull(loggingContext);
            Contract.RequiresNotNullOrWhiteSpace(description);

            // Only log if there was more than one sample taken
            if (aggregator.ProcessThreadCount.Count > 0)
            {
                int processAverageThreadCount = ConvertToInt(aggregator.ProcessThreadCount.Average);
                int processMaximumPrivateMegaBytes = ConvertToInt(aggregator.ProcessPrivateMB.Maximum);
                int processMaximumWorkingSetMegaBytes = ConvertToInt(aggregator.ProcessWorkingSetMB.Maximum);
                int processAverageWorkingSetMegaBytes = ConvertToInt(aggregator.ProcessWorkingSetMB.Average);
                int processMaximumHeldMegaBytess = ConvertToInt(aggregator.ProcessHeldMB.Maximum);
                int processAverageCPUTime = ConvertToInt(aggregator.ProcessCpu.Average);
                int machineAverageCPUTime = ConvertToInt(aggregator.MachineCpu.Average);
                int machineAverageCPUWMITime = ConvertToInt(aggregator.MachineCpuWMI.Average);
                int jobObjectAverageCPUTime = ConvertToInt(aggregator.JobObjectCpu.Average);

                int machineMinimumAvailableMemoryMegabytes = ConvertToInt(aggregator.MachineAvailablePhysicalMB.Minimum);

                int machineActiveTcpConnections = ConvertToInt(aggregator.MachineActiveTcpConnections.Latest);
                int machineOpenFileDescriptors = ConvertToInt(aggregator.MachineOpenFileDescriptors.Latest);

                Dictionary<string, long> dict = new Dictionary<string, long>(8 + aggregator.DiskStats.Count);
                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_ProcessAverageThreadCount), processAverageThreadCount);
                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_ProcessMaximumPrivateMB), processMaximumPrivateMegaBytes);
                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_ProcessMaximumWorkingSetPrivateMB), processMaximumWorkingSetMegaBytes);
                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_ProcessAverageWorkingSetPrivateMB), processAverageWorkingSetMegaBytes);
                if (processMaximumHeldMegaBytess > 0)
                {
                    dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_ProcessMaximumHeldMB), processMaximumHeldMegaBytess);
                }

                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_ProcessAverageCPUTime), processAverageCPUTime);
                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_MachineAverageCPUTime), machineAverageCPUTime);
                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_MachineAverageCPUWMITime), machineAverageCPUWMITime);
                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_JobObjectAverageCPUTime), jobObjectAverageCPUTime);

                dict.Add(GetCategorizedStatisticName(description, Statistics.Counter_MachineMinimumAvailableMemoryMB), machineMinimumAvailableMemoryMegabytes);

                if (duration != null)
                {
                    dict.Add(GetCategorizedStatisticName(description, "DurationMs"), duration.Value);
                }

                foreach (var diskStat in aggregator.DiskStats)
                {
                    var activeTime = diskStat.CalculateActiveTime(lastOnly: false);
                    dict.Add(GetCategorizedStatisticName(GetCategorizedStatisticName(description, Statistics.MachineAverageDiskActiveTime), diskStat.Drive), activeTime);
                }
                dict.Add(GetCategorizedStatisticName(description, Statistics.MachineActiveTcpConnections), machineActiveTcpConnections);

                // Log the information only for linux builds.
                if (OperatingSystemHelper.IsLinuxOS)
                {
                    dict.Add(GetCategorizedStatisticName(description, Statistics.MachineOpenFileDescriptors), machineOpenFileDescriptors);
                }

                Logger.Log.BulkStatistic(loggingContext, dict);
            }
        }

        /// <summary>
        /// Logs a statistic
        /// </summary>
        public static void LogCategorizedStatistic(LoggingContext loggingContext, string categorization, string itemName, int value)
        {
            Contract.RequiresNotNull(loggingContext);
            Contract.RequiresNotNullOrWhiteSpace(categorization);
            Contract.RequiresNotNullOrWhiteSpace(itemName);

            Logger.Log.Statistic(
                loggingContext,
                new Statistic()
                {
                    Name = GetCategorizedStatisticName(categorization, itemName),
                    Value = value,
                });
        }

        /// <summary>
        /// Logs a set of statistics
        /// </summary>
        public static void LogCategorizedStatistics(LoggingContext loggingContext, string categorization, ICollection<KeyValuePair<string, int>> statistics)
        {
            Dictionary<string, long> dict = new Dictionary<string, long>(statistics.Count);
            foreach (var statistic in statistics)
            {
                dict[GetCategorizedStatisticName(categorization, statistic.Key)] = statistic.Value;
            }

            Logger.Log.BulkStatistic(loggingContext, dict);
        }

        private static string GetCategorizedStatisticName(string categorization, string itemName)
        {
            return categorization + "." + itemName;
        }

        /// <summary>
        /// Converts a float to int replacing the result by 0 on overflows
        /// </summary>
        private static int ConvertToInt(double input)
        {
            if (double.IsNaN(input))
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(input);
            }
            catch (OverflowException)
            {
                return 0;
            }
        }
    }
}
