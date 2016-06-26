using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImageCompLibWin.Tasking
{
    public abstract class Task
    {
        private AutoResetEvent _taskEvent = new AutoResetEvent(false);

        protected Task(TaskManager manager)
        {
            Manager = manager;
        }

        public TaskManager Manager { get; }

        /// <summary>
        ///  Resources required by the task
        /// </summary>
        public abstract ICollection<IResource> RequiredResources
        {
            get;
        }

        public void Run()
        {
            Request();

            Perform();

            DeReferenceResources();
            Manager.TaskComplete(this);
        }

        public int GetRequestedSize() => RequiredResources.Where(x => !x.IsEngaged).Sum(x => x.Size);
        
        protected bool ThrunsAllReady() => RequiredResources.All(x => x.IsEngaged);

        private void Request()
        {
            lock(this)
            {
                if (ThrunsAllReady())
                {
                    ThrunsReferenceResources();
                    return;
                }
            }

            Manager.AddTask(this);

            // The event will be set only when the task is ready to run
            _taskEvent.WaitOne();
        }

        internal void SetToRun()
        {
            _taskEvent.Set();
        }

        internal void ThrunsReferenceResources()
        {
            foreach (var rr in RequiredResources)
            {
                rr.ReferenceCount++;
            }
        }

        internal void DeReferenceResources()
        {
            lock(this)
            {
                foreach (var rr in RequiredResources)
                {
                    rr.ReferenceCount--;
                }
            }
        }

        protected abstract void Perform();
    }
}
