using System;

namespace PasswordFlagger
{
    public interface IStatusObject
    {
        public bool IsCompleted { get; set; }
        event EventHandler ObjectCompleted;

        string GetName();
    }
}