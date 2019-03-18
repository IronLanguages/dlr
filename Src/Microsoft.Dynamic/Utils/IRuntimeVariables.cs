using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;

namespace System.Runtime.CompilerServices {
#if WINDOWS_UWP
    public interface IRuntimeVariables {
        object this[int index] { get; set; }
        int Count { get; }
    }
#endif
}
