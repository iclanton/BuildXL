// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using BuildXL.Cache.ContentStore.Interfaces.FileSystem;
using BuildXL.Cache.Host.Configuration;
using RocksDbSharp;
using static BuildXL.Utilities.ConfigurationHelper;

#nullable enable

namespace BuildXL.Cache.ContentStore.Distributed.NuCache
{
    /// <summary>
    /// Configuration type for <see cref="ContentLocationDatabase"/> family of types.
    /// </summary>
    public abstract class ContentLocationDatabaseConfiguration
    {
        /// <summary>
        /// The touch interval for database entries.
        /// NOTE: This is set internally to the same value as <see cref="LocalLocationStoreConfiguration.TouchFrequency"/>
        /// </summary>
        internal TimeSpan TouchFrequency { get; set; }

        /// <summary>
        /// Interval between garbage collecting unreferenced content location entries and metadata in a local database.
        /// </summary>
        public TimeSpan GarbageCollectionInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Maximum allowed size of the Metadata column family.
        /// </summary>
        public double MetadataGarbageCollectionMaximumSizeMb { get; set; } = 20_000;

        /// <summary>
        /// Whether to clean the DB when it is corrupted.
        /// </summary>
        /// <remarks>
        /// Should be false for Content, true for Metadata.
        /// </remarks>
        public bool OnFailureDeleteExistingStoreAndRetry { get; set; } = false;

        /// <summary>
        /// Specifies whether the context operation guid is used when logging entry operations
        /// </summary>
        public bool UseContextualEntryOperationLogging { get; set; } = false;

        /// <summary>
        /// Specifies whether the context operation guid is used when logging entry operations
        /// </summary>
        public bool TraceOperations { get; set; } = true;

        /// <summary>
        /// Ges or sets log level from RocksDb emitted to Kusto.
        /// Null - the tracing is off.
        /// </summary>
        public LogLevel? RocksDbTracingLevel { get; set; }

        /// <summary>
        /// Specifies whether to trace touches or not.
        /// Tracing touches is expensive in terms of the amount of traffic to Kusto and in terms of memory traffic.
        /// </summary>
        public bool TraceTouches { get; set; } = true;

        /// <summary>
        /// Specifies whether to trace the cases when the call to SetMachineExistence didn't change the database's state.
        /// </summary>
        public bool TraceNoStateChangeOperations { get; set; } = false;
    }

    /// <summary>
    /// Configuration type for <see cref="RocksDbContentLocationDatabase"/>.
    /// </summary>
    public class RocksDbContentLocationDatabaseConfiguration : ContentLocationDatabaseConfiguration
    {
        /// <inheritdoc />
        public RocksDbContentLocationDatabaseConfiguration(AbsolutePath storeLocation) => StoreLocation = storeLocation;

        /// <summary>
        /// The directory containing the key-value store.
        /// </summary>
        public AbsolutePath StoreLocation { get; }

        /// <summary>
        /// Testing purposes only. Used to set location to load initial database data from.
        /// NOTE: This disables <see cref="CleanOnInitialize"/>
        /// </summary>
        public AbsolutePath? TestInitialCheckpointPath { get; set; }

        /// <summary>
        /// Gets whether the database is cleared on initialization. (Defaults to true because the standard use case involves restoring from checkpoint after initialization)
        /// </summary>
        public bool CleanOnInitialize { get; set; } = true;

        /// <summary>
        /// Whether the database should be open in read only mode. This will cause all write operations on the DB to
        /// fail.
        /// </summary>
        public bool OpenReadOnly { get; set; } = false;

        /// <summary>
        /// Specifies a opaque value which can be used to determine if database can be reused when <see cref="CleanOnInitialize"/> is false.
        /// </summary>
        /// <remarks>
        /// Do NOT change the default from null. The epoch is not stored by default, so the value is read as null. If
        /// you change this, open -> close -> open will always fail due to non-matching epoch values.
        /// </remarks>
        public string? Epoch { get; set; } = null;

        /// <summary>
        /// Number of keys to buffer on <see cref="RocksDbContentLocationDatabase.EnumerateEntriesWithSortedKeysFromStorage(ContentStore.Tracing.Internal.OperationContext, ContentLocationDatabase.EnumerationFilter, bool)"/>
        /// </summary>
        public long EnumerateEntriesWithSortedKeysFromStorageBufferSize { get; set; } = 100_000;

        /// <summary>
        /// Whether to use 'SetTotalOrderSeek' option during database enumeration.
        /// </summary>
        /// <remarks>
        /// Setting this flag is important in order to get the correct behavior for content enumeration of the database.
        /// When the prefix extractor is used by calling SetIndexType(BlockBasedTableIndexType.Hash) and SetPrefixExtractor(SliceTransform.CreateNoOp())
        /// then the full database enumeration may return already removed keys or the previous version for some values.
        ///
        /// Not setting this flag was causing issues during reconciliation because the database enumeration was producing values for already removed keys
        /// and some keys were missing.
        /// </remarks>
        public bool UseReadOptionsWithSetTotalOrderSeekInDbEnumeration { get; set; } = true;

        /// <summary>
        /// Whether to use 'SetTotalOrderSeek' option during database garbage collection.
        /// </summary>
        /// <remarks>
        /// See the remarks section for <see cref="UseReadOptionsWithSetTotalOrderSeekInDbEnumeration"/>.
        /// </remarks>
        public bool UseReadOptionsWithSetTotalOrderSeekInGarbageCollection { get; set; } = true;

        /// <summary>
        /// If true, then the locations will be merged without deserialization on the workers and sorted on the master.
        /// </summary>
        public bool SortMergeableContentLocations { get; set; }

        /// <nodoc />
        public RocksDbPerformanceSettings? RocksDbPerformanceSettings { get; set; } = null;

        /// <nodoc />
        public static RocksDbContentLocationDatabaseConfiguration FromDistributedContentSettings(
            DistributedContentSettings settings,
            AbsolutePath databasePath)
        {
            var configuration = new RocksDbContentLocationDatabaseConfiguration(databasePath)
            {
                UseContextualEntryOperationLogging = settings.UseContextualEntryDatabaseOperationLogging,
                TraceTouches = settings.TraceTouches,
            };

            configuration.RocksDbPerformanceSettings = settings.RocksDbPerformanceSettings;

            ApplyIfNotNull(settings.ContentLocationDatabaseRocksDbTracingLevel, v => configuration.RocksDbTracingLevel = (LogLevel)v);
            ApplyIfNotNull(settings.TraceStateChangeDatabaseOperations, v => configuration.TraceOperations = v);
            ApplyIfNotNull(settings.TraceNoStateChangeDatabaseOperations, v => configuration.TraceNoStateChangeOperations = v);

            ApplyIfNotNull(settings.ContentLocationDatabaseGcIntervalMinutes, v => configuration.GarbageCollectionInterval = TimeSpan.FromMinutes(v));
            ApplyIfNotNull(settings.ContentLocationDatabaseMetadataGarbageCollectionMaximumSizeMb, v => configuration.MetadataGarbageCollectionMaximumSizeMb = v);

            ApplyIfNotNull(settings.ContentLocationDatabaseOpenReadOnly, v => configuration.OpenReadOnly = (v && !settings.IsMasterEligible));
            ApplyIfNotNull(settings.SortMergeableContentLocations, v => configuration.SortMergeableContentLocations = v);

            ApplyIfNotNull(settings.ContentLocationDatabaseEnumerateEntriesWithSortedKeysFromStorageBufferSize, v => configuration.EnumerateEntriesWithSortedKeysFromStorageBufferSize = v);

            return configuration;
        }
    }
}
