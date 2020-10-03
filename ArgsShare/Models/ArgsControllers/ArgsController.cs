using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArgsShare.Models.ArgsControllers
{
    public partial class ArgsController : IArgsController
    {
        public event EventHandler<string> ArgAddAsync;

        private HashSet<string> args;
        private MemoryMappedFile memoryFile;
        private Mutex mutex;
        private bool disposed;
        private Task updateTask;
        private CancellationTokenSource tokenSource;

        public string MutexFullName { get; }
        public string MemoryFileFullName { get; }
        public IEnumerable<string> Args
        {
            get
            {
                mutex.WaitOne();
                var argsArr = args.ToArray();
                mutex.ReleaseMutex();
                return argsArr;
            }
        }
        public bool IsOwner { get; }
        public bool IsFollowing { get; private set; }

        public ArgsController(string mutexPrefix, string memoryFilePrefix, long fileMaxSize = 65_536)
        {
            disposed = false;
            args = new HashSet<string>();

            MutexFullName = $"{mutexPrefix}_Args";
            MemoryFileFullName = $"{memoryFilePrefix}_Args";
            mutex = new Mutex(true, MutexFullName, out bool isCreated);
            IsOwner = isCreated;
            memoryFile = MemoryMappedFile.CreateOrOpen(MemoryFileFullName, fileMaxSize);
            if (IsOwner)
                mutex.ReleaseMutex();
        }

        public void AddArg(string arg)
        {
            mutex.WaitOne();
            long offset = 0;
            using (var reader = new StreamReader(memoryFile.CreateViewStream()))
            {
                while (reader.Peek() > 0)
                {
                    reader.Read();
                    offset++;
                }
            }
            using (var writer = new StreamWriter(memoryFile.CreateViewStream(offset, 0)))
            {
                writer.WriteLine(arg);
            }
            mutex.ReleaseMutex();
        }
        public void BeginFollow()
        {
            if (!IsFollowing && IsOwner)
            {
                tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;
                updateTask = Task.Run(() =>
                {
                    long offset = 0;

                    while (!token.IsCancellationRequested)
                    {
                        UpdateArgsCollection(ref offset);
                        try
                        {
                            Task.Delay(200, token).Wait();
                        }
                        catch (TaskCanceledException)
                        {
                            return;
                        }
                    }
                });

                IsFollowing = true;
            }
        }
        public void EndFollow()
        {
            if (IsFollowing)
            {
                if (updateTask != null)
                {
                    tokenSource.Cancel();
                    updateTask.Wait();
                    updateTask = null;
                }

                IsFollowing = false;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                EndFollow();
                if (IsOwner)
                    mutex.WaitOne();

                memoryFile.Dispose();
                mutex.Dispose();

                disposed = true;
            }
        }

        private void UpdateArgsCollection(ref long offset)
        {
            mutex.WaitOne();
            using (var reader = new StreamReader(memoryFile.CreateViewStream(offset, 0)))
            {
                while (reader.Peek() > 0)
                {
                    string arg = reader.ReadLine();
                    args.Add(arg);
                    ArgAddAsync?.Invoke(this, arg);
                    offset += reader.CurrentEncoding.GetByteCount(arg + "\n\r");
                }
            }
            mutex.ReleaseMutex();
        }

        ~ArgsController()
        {
            Dispose(false);
        }
    }
}
