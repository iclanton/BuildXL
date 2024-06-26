// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BuildXL.Utilities.Instrumentation.Common;
using BuildXL.Utilities.Core;

namespace BuildXL.Tracing
{
    /// <summary>
    /// Extensions for logging <see cref="CounterCollection{TEnum}" /> counters via <see cref="Logger.Statistic" />.
    /// </summary>
    public static class CounterCollectionTracingExtensions
    {
        /// <summary>
        /// Logs all counters in the given collection via <see cref="Logger.Statistic"/>. Each counter is labelled as <c>NamePrefix.EnumName</c>.
        /// </summary>
        public static void LogAsStatistics<TEnum>(
            this CounterCollection<TEnum> counterCollection,
            string namePrefix,
            LoggingContext context) where TEnum : System.Enum
        {
            Logger.Log.BulkStatistic(context, counterCollection.AsStatistics(namePrefix));
        }
    }
}
