// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using BuildXL.Utilities.Core;

namespace BuildXL.Cache.ContentStore.Distributed.NuCache.EventStreaming
{
    /// <summary>
    /// Performance counters available for <see cref="ContentLocationDatabase"/>.
    /// </summary>
    public enum ContentLocationEventStoreCounters
    {
        //
        // Dispatch events counters
        //

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        DispatchAddLocations = 1,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        DispatchRemoveLocations,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        DispatchTouch,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        DispatchBlob,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        DispatchUpdateMetadata,

        /// <nodoc />
        DispatchAddLocationsHashes,

        /// <nodoc />
        DispatchRemoveLocationsHashes,

        /// <nodoc />
        DispatchTouchHashes,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        ProcessEvents,

        /// <summary>
        /// The number database mutations while processing 'AddLocation' event.
        /// </summary>
        DatabaseAddedLocations,

        /// <summary>
        /// The number database mutations while processing 'RemoveLocation' event.
        /// </summary>
        DatabaseRemovedLocations,

        /// <summary>
        /// The number database mutations while processing 'TouchContent' event.
        /// </summary>
        DatabaseTouchedLocations,

        /// <summary>
        /// The number database mutations while processing 'UpdateMetadata' event.
        /// </summary>
        DatabaseUpdatedMetadata,

        /// <nodoc />
        ReceivedEventHubEventsCount,

        /// <nodoc />
        ReceivedEventBatchCount,

        /// <nodoc />
        ReceivedEventsCount,

        /// <nodoc />
        ReceivedMessagesTotalSize,

        /// <nodoc />
        MessagesWithoutSenderMachine,

        /// <nodoc />
        FilteredEvents,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        Deserialization,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        DispatchEvents,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        GetAndDeserializeEventData,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        DispatchBlobEventData,

        //
        // Send events counters
        //

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        PublishAddLocations,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        PublishUpdateContentHashList,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        PublishRemoveLocations,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        PublishTouchLocations,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        PublishReconcile,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        PublishLargeEvent,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        StartProcessing,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        SuspendProcessing,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        SendEvents,

        /// <nodoc />
        [CounterType(CounterType.Stopwatch)]
        Serialization,

        /// <nodoc />
        SentEventBatchCount,

        /// <nodoc />
        SentEventsCount,

        /// <nodoc />
        SentMessagesTotalSize,

        /// <nodoc />
        SentAddLocationsEvents,

        /// <nodoc />
        SentAddLocationsHashes,

        /// <nodoc />
        SentRemoveLocationsEvents,

        /// <nodoc />
        SentRemoveLocationsHashes,

        /// <nodoc />
        SentTouchLocationsEvents,

        /// <nodoc />
        SentTouchLocationsHashes,

        /// <nodoc />
        SentStoredEvents,

        /// <nodoc />
        SentUpdateMetadataEntryEvents,
    }
}
