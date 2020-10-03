using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace ArgsShare.Models.ArgsControllers
{
    public interface IArgsController : IDisposable
    {
        event EventHandler<string> ArgAddAsync;

        string MutexFullName { get; }
        string MemoryFileFullName { get; }
        IEnumerable<string> Args { get; }
        bool IsOwner { get; }
        bool IsFollowing { get; }

        void AddArg(string arg);
        void BeginFollow();
        void EndFollow();
    }
}
