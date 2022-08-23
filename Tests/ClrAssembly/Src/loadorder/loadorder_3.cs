// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace First {
    public class Generic1<K, V> {
        public static string Flag = typeof(Generic1<,>).FullName;
    }
}

//#region The above non-generic type will loaded followed by each type below

//// non-generic type, which has different namespace, same name from First.Generic1
//namespace Second {
//    public class Generic1 {
//        public static string Flag = typeof(Generic1).FullName;
//    }
//}

//// non-generic type, which has different namespace, different name from First.Generic1
//namespace Second {
//    public class Generic2 {
//        public static string Flag = typeof(Generic2).FullName;
//    }
//}

//// non-generic type, which has same namespace, same name from First.Generic1
//namespace First {
//    public class Generic1 {
//        public static string Flag = typeof(Generic1).Name;
//    }
//}

//// non-generic type, which has same namespace, different name from First.Generic1
//namespace First {
//    public class Generic2 {
//        public static string Flag = typeof(Generic2).FullName;
//    }
//}

//// generic type, which has different namespace, same name from First.Generic1
//namespace Second {
//    public class Generic1<K, V> {
//        public static string Flag = typeof(Generic1<,>).FullName;
//    }
//}

//// generic type, which has different namespace, different name from First.Generic1
//namespace Second {
//    public class Generic2<K, V> {
//        public static string Flag = typeof(Generic2<,>).FullName;
//    }
//}

//// generic type, which has same namespace, same name from First.Generic1
//namespace First {
//    public class Generic1<K, V> {
//        public static string Flag = typeof(Generic1<,>).FullName + "_Same";
//    }
//}

//// generic type, which has same namespace, same name from First.Generic1
//namespace First {
//    public class Generic1<T> {
//        public static string Flag = typeof(Generic1<>).FullName;
//    }
//}

//// generic type, which has same namespace, different name from First.Generic1
//namespace First {
//    public class Generic2<T> {
//        public static string Flag = typeof(Generic2<>).FullName;
//    }
//}

//#endregion
