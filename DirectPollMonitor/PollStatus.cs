using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectPollMonitor {
    enum PollStatus : int {
        Stopped = 0,
        Paused = 1,
        Running = 2
    }
}
