// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

public class TopLevelClass_JustAdded {
    public static string Flag = typeof(TopLevelClass_JustAdded).AssemblyQualifiedName;
}

public class TopLevelClass_ToBeRemained {
    public static string Flag = typeof(TopLevelClass_ToBeRemained).AssemblyQualifiedName;
}

namespace NormalNamespace {
    public class Class_JustAdded {
        public static string Flag = typeof(Class_JustAdded).AssemblyQualifiedName;
    }

    public class Class_ToBeRemained {
        public static string Flag = typeof(Class_ToBeRemained).AssemblyQualifiedName;
    }

    public class NormalClass {
        public class NestedClass_JustAdded {
            public static string Flag = typeof(NestedClass_JustAdded).AssemblyQualifiedName;
        }

        public class NestedClass_ToBeRemained {
            public static string Flag = typeof(NestedClass_ToBeRemained).AssemblyQualifiedName;
        }
    }
}

namespace Namespace_JustAdded {
    public class C {
        public static string Flag = typeof(C).AssemblyQualifiedName;
    }
}

namespace Namespace_ToBeRemained {
    public class C {
        public static string Flag = typeof(C).AssemblyQualifiedName;
    }
}

