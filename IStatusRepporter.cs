using System;
using System.Collections;
using System.Collections.Generic;

namespace PasswordFlagger
{
    public interface IStatusRepporter
    {
        event EventHandler ProcessStarted;

        ICollection<IStatusObject> GetStatusObjects();
    }
}