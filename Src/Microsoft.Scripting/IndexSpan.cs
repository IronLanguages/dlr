﻿
using System;

using Microsoft.Scripting.Utils;

namespace Microsoft.Scripting {
    /// <summary>
    /// This structure represents an immutable integer interval that describes a range of values, from Start to End. 
    /// 
    /// It is closed on the left and open on the right: [Start .. End). 
    /// </summary>
    public struct IndexSpan : IEquatable<IndexSpan> {
        private readonly int _start, _length;

        public IndexSpan(int start, int length) {
            ContractUtils.Requires(length >= 0, nameof(length));
            ContractUtils.Requires(start >= 0, nameof(start));

            _start = start;
            _length = length;
        }

        public int Start {
            get {
                return _start;
            }
        }

        public int End {
            get {
                return _start + _length;
            }
        }

        public int Length {
            get {
                return _length;
            }
        }

        public bool IsEmpty {
            get {
                return _length == 0;
            }
        }

        public override int GetHashCode() {
            return Length.GetHashCode() ^ Start.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj is IndexSpan span) {
                return Equals(span);
            }
            return false;
        }

        public static bool operator ==(IndexSpan self, IndexSpan other) {
            return self.Equals(other);
        }

        public static bool operator !=(IndexSpan self, IndexSpan other) {
            return !self.Equals(other);
        }

        #region IEquatable<IndexSpan> Members

        public bool Equals(IndexSpan other) {
            return _length == other._length && _start == other._start;
        }

        #endregion
    }
}
