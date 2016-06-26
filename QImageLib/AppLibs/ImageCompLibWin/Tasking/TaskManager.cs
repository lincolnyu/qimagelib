using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static System.Diagnostics.Debug;

namespace ImageCompLibWin.Tasking
{
    public class TaskManager
    {
        public const int DefaultQuota = 1024 * 1024 * 1024;

        private bool _running;

        private Thread _processorThread;

        private AutoResetEvent _taskQueueEvent = new AutoResetEvent(false);
        private AutoResetEvent _taskCompleteEvent = new AutoResetEvent(false);

        private Mutex _taskMutex = new Mutex();

        public TaskManager(int quota, bool start = false)
        {
            Quota = quota;
            if (start)
            {
                Start();
            }
        }

        public static TaskManager Instance { get; } = new TaskManager(DefaultQuota);

        public LinkedList<Task> Tasks { get; } = new LinkedList<Task>();

        /// <summary>
        ///  In ascending order of size
        /// </summary>
        public List<IResource> CachedResources { get; } = new List<IResource>();

        public int Quota { get; }

        public int Used { get; private set; }

        public bool IsRunning => _running || _processorThread != null;

        public void Start()
        {
            if (IsRunning)
            {
                // already running or stuck
                return;
            }
            _running = true;
            _processorThread = new Thread(WorkingThread);
            _processorThread.Start();
        }

        // TODO implement immediate stop
        public void Stop()
        {
            if (_processorThread != null)
            {
                _running = false;
                _taskQueueEvent.Set();
                _processorThread.Join();
                CachedResources.Clear();
                Used = 0;
                _processorThread = null;
            }
        }

        public void AddTask(Task task)
        {
            lock (Tasks)
            {
                // TODO may need to reorder the task such that the smaller tasks
                // TODO gets performed first
                // TODO may use deque
                Tasks.AddLast(task);
                _taskQueueEvent.Set();
            }
        }

        public void TaskComplete(Task task)
        {
            _taskCompleteEvent.Set();
        }

        private void WorkingThread()
        {
            while (_running)
            {
                WaitForNewTasks();

                PrepareOneTask();
            }
        }

        private void WaitForNewTasks()
        {
            while (Tasks.Count == 0 && _running)
            {
                _taskQueueEvent.WaitOne();
            }
        }

        private void PrepareOneTask()
        {
            while (Tasks.Count > 0)
            {
                Task task = null;
                lock (Tasks)
                {
                    // TODO optimize this
                    task = Tasks.First.Value;
                }

                var reqrsrcs = task.RequiredResources.Where(x => !x.IsEngaged).ToList();
                var size = reqrsrcs.Sum(x => x.Size);
                if (size > Quota)
                {
                    throw new ArgumentException($"Quota ({Quota}) not enough for the requested size {size}");
                }
                var mustHold = GetMustHoldResources(task);
                var diff = Used + size - Quota;
                var canDo = diff <= 0 || ReleaseTasks(diff, mustHold);
                if (canDo)
                {
                    foreach (var r in reqrsrcs)
                    {
                        Assert(!r.IsEngaged);
                        r.IsEngaged = true;
                        if (r.IsEngaged)
                        {
                            Used += r.Size;
                            //Assert(!CachedResources.Contains(r));
                            CachedResources.Add(r);
                        }
                    }
                    lock (Tasks)
                    {
                        Tasks.RemoveFirst();
                    }
                    ClearMustHoldResources(task, mustHold);
                    task.ThrunsReferenceResources();
                    task.SetToRun();
                }
                else
                {
                    ClearMustHoldResources(task, mustHold);
                    _taskCompleteEvent.WaitOne();
                }
            }
        }

        private void ClearMustHoldResources(Task task, ISet<IResource> heldResources)
        {
            foreach (var hr in heldResources)
            {
                hr.HoldCount--;
            }
            heldResources.Clear();
        }

        private ISet<IResource> GetMustHoldResources(Task task)
        {
            var heldResources = task.RequiredResources.Where(x => x.IsEngaged);
            var heldResourceSet = new HashSet<IResource>();
            foreach (var hr in heldResources)
            {
                hr.HoldCount++;
                heldResourceSet.Add(hr);
            }
            return heldResourceSet;
        }

        private void ReorderCacheList()
        {
            CachedResources.Sort((a, b) =>
            {
                var c = a.ReferenceCount.CompareTo(b.ReferenceCount);
                if (c != 0) return -c;
                c = a.HoldCount.CompareTo(b.HoldCount);
                if (c != 0) return -c;
                c = a.Size.CompareTo(b.Size);
                return c;
            });
        }

        private bool ReleaseTasks(int required, ISet<IResource> keyHeld)
        {
            // TODO optimize this..
            // reordered such that the list is from least releasible to most
            ReorderCacheList();
            var removeList = new List<int>();
            var totalReleasible = 0;
            for (var i = CachedResources.Count - 1; i >= 0; i--)
            {
                var item = CachedResources[i];
                if (item.ReferenceCount > 0)
                {
                    break;
                }
                if (keyHeld.Contains(item))
                {
                    continue;
                }
                removeList.Add(i);
                totalReleasible += item.Size;
            }

            if (totalReleasible < required)
            {
                // TODO shall we just release them anyway?
                return false;
            }
            foreach (var ri in removeList)
            {
                var item = CachedResources[ri];
                Used -= item.Size;
                CachedResources.RemoveAt(ri);
                item.IsEngaged = false;
            }
            return true;
        }
    }
}
