// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.ContractsLight;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Pips.Operations;

namespace BuildXL.Scheduler.WorkDispatcher
{
    /// <summary>
    /// Dispatcher queue which fires tasks and is managed by <see cref="PipQueue"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public class DispatcherQueue : IDisposable
    {
        private PriorityQueue<RunnablePip> m_queue = new PriorityQueue<RunnablePip>();
        private readonly PipQueue m_pipQueue;

        private int m_numRunning;
        private int m_processesQueued;
        private readonly object m_startTasksLock = new object();

        /// <summary>
        /// Maximum parallelism degree
        /// </summary>
        public int MaxParallelDegree { get; private set; }

        /// <summary>
        /// Number of tasks running now
        /// </summary>
        public virtual int NumRunning => Volatile.Read(ref m_numRunning);

        /// <summary>
        /// Number of items waiting in the queue
        /// </summary>
        public virtual int NumQueued => m_queue.Count;

        /// <summary>
        /// Number of process pips queued
        /// </summary>
        public int NumProcessesQueued => Volatile.Read(ref m_processesQueued);

        /// <summary>
        /// Maximum number of tasks run at the same since the queue has been started
        /// </summary>
        public int MaxRunning { get; private set; }

        /// <summary>
        /// Whether the dispatcher is disposed or not
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public DispatcherQueue(PipQueue pipQueue, int maxParallelDegree)
        {
            m_pipQueue = pipQueue;
            MaxParallelDegree = maxParallelDegree;
            m_numRunning = 0;
            m_processesQueued = 0;
            m_stack = new ConcurrentStack<int>();
            for (int i = MaxParallelDegree - 1; i >= 0; i--)
            {
                m_stack.Push(i);
            }
        }

        private readonly ConcurrentStack<int> m_stack;

        /// <summary>
        /// Enqueues the given runnable pip
        /// </summary>
        public virtual void Enqueue(RunnablePip runnablePip)
        {
            Contract.Requires(!IsDisposed);

            m_queue.Enqueue(runnablePip.Priority, runnablePip);
            if (runnablePip.PipType == Pips.Operations.PipType.Process)
            {
                Interlocked.Increment(ref m_processesQueued);
            }
        }

        /// <summary>
        /// Starts all tasks until the queue becomes empty or concurrency limit is satisfied
        /// </summary>
        public virtual void StartTasks()
        {
            Contract.Requires(!IsDisposed);

            // Acquire the lock. This is needed because other threads (namely the ChooseWorker thread may call this
            // method).
            lock (m_startTasksLock)
            {
                RunnablePip runnablePip;
                int maxParallelDegree = MaxParallelDegree;
                while (maxParallelDegree > NumRunning && Dequeue(out runnablePip))
                {
                    StartTask(runnablePip);
                }
            }
        }

        /// <summary>
        /// Dequeues from the priority queue
        /// </summary>
        private bool Dequeue(out RunnablePip runnablePip)
        {
            if (m_queue.Count != 0)
            {
                runnablePip = m_queue.Dequeue();
                if (runnablePip.PipType == Pips.Operations.PipType.Process)
                {
                    Interlocked.Decrement(ref m_processesQueued);
                }
                return true;
            }

            runnablePip = null;
            return false;
        }

        /// <summary>
        /// Starts single task from the given runnable pip on the given dispatcher
        /// </summary>
        private void StartTask(RunnablePip runnablePip)
        {
            IncrementNumRunning();

            StartRunTaskAsync(runnablePip);
        }
        
        /// <summary>
        /// Runs pip asynchronously on a separate thread. This should not block the current
        /// thread on completion of the pip.
        /// </summary>
        [SuppressMessage("AsyncUsage", "AsyncFixer03:FireForgetAsyncVoid", Justification = "Fire and forget is intentional.")]
        protected virtual async void StartRunTaskAsync(RunnablePip runnablePip)
        {
            await Task.Yield();

            await RunCoreAsync(runnablePip);
        }

        /// <summary>
        /// Runs pip asynchronously
        /// </summary>
        protected async Task RunCoreAsync(RunnablePip runnablePip)
        {
            DispatcherReleaser releaser = new DispatcherReleaser(this);
            int tid = -1;
            if (runnablePip.IncludeInTracer)
            {
                m_stack.TryPop(out tid);
                runnablePip.ThreadId = tid;
            }

            try
            {
                // Unhandled exceptions (Catastrophic BuildXL Failures) during a pip's execution will be thrown here without an AggregateException.
                await runnablePip.RunAsync(releaser);
            }
            finally
            {
                if (tid != -1)
                {
                    m_stack.Push(tid);
                }
               
                releaser.Release();
                m_pipQueue.DecrementRunningOrQueuedPips(); // Trigger dispatching loop in the PipQueue
            }
        }

        /// <summary>
        /// Release resource for a pip
        /// </summary>
        public void ReleaseResource()
        {
            Interlocked.Decrement(ref m_numRunning); // Decrease the number of running tasks in the current queue.
            m_pipQueue.TriggerDispatcher();
        }

        /// <summary>
        /// Disposes the queue
        /// </summary>
        public virtual void Dispose()
        {
            m_queue = null;
            IsDisposed = true;
        }

        private void IncrementNumRunning()
        {
            MaxRunning = Math.Max(MaxRunning, Interlocked.Increment(ref m_numRunning));
        }

        /// <summary>
        /// Adjust the max parallel degree to decrease or increase concurrency
        /// </summary>
        internal virtual bool AdjustParallelDegree(int newParallelDegree)
        {
            if (MaxParallelDegree != newParallelDegree)
            {
                MaxParallelDegree = newParallelDegree;
                return true;
            }

            return false;
        }
    }
}
