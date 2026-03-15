// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

public class TopLevelClass_ToBeRemoved {
    public static string Flag = typeof(TopLevelClass_ToBeRemoved).AssemblyQualifiedName;
}

public class TopLevelClass_ToBeRemained {
    public static string Flag = typeof(TopLevelClass_ToBeRemained).AssemblyQualifiedName;
}

namespace NormalNamespace {
    public class Class_ToBeRemoved {
        public static string Flag = typeof(Class_ToBeRemoved).AssemblyQualifiedName;
    }

    public class Class_ToBeRemained {
        public static string Flag = typeof(Class_ToBeRemained).AssemblyQualifiedName;
    }

    public class NormalClass {
        public class NestedClass_ToBeRemoved {
            public static string Flag = typeof(NestedClass_ToBeRemoved).AssemblyQualifiedName;
        }

        public class NestedClass_ToBeRemained {
            public static string Flag = typeof(NestedClass_ToBeRemained).AssemblyQualifiedName;
        }
    }
}

namespace Namespace_ToBeRemoved {
    public class C {
        public static string Flag = typeof(C).AssemblyQualifiedName;
    }
}

namespace Namespace_ToBeRemained {
    public class C {
        public static string Flag = typeof(C).AssemblyQualifiedName;
    }
}

