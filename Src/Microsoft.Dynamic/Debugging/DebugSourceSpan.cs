﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Scripting.Debugging {
    /// <summary>
    /// Combines source file and span.  Also provides Contains and Intersects functionality.
    /// </summary>
    [DebuggerDisplay("{LineStart}, {ColumnStart} - {LineEnd}, {ColumnEnd}")]
    internal sealed class DebugSourceSpan {
        private readonly DebugSourceFile _sourceFile;
        private readonly int _lineStart;
        private readonly int _columnStart;
        private readonly int _lineEnd;
        private readonly int _columnEnd;

        internal DebugSourceSpan(DebugSourceFile sourceFile, int lineStart, int columnStart, int lineEnd, int columnEnd) {
            _sourceFile = sourceFile;
            _lineStart = lineStart;
            _columnStart = columnStart;
            _lineEnd = lineEnd;
            _columnEnd = columnEnd;
        }

        internal DebugSourceSpan(DebugSourceFile sourceFile, SourceSpan dlrSpan)
            : this(sourceFile, dlrSpan.Start.Line, dlrSpan.Start.Column, dlrSpan.End.Line, dlrSpan.End.Column) {
        }

        internal DebugSourceFile SourceFile {
            get { return _sourceFile; }
        }

        internal int LineStart {
            get { return _lineStart; }
        }

        internal int ColumnStart {
            get { return _columnStart; }
        }

        internal int LineEnd {
            get { return _lineEnd; }
        }

        internal int ColumnEnd {
            get { return _columnEnd; }
        } 

        internal SourceSpan ToDlrSpan() {
            return new SourceSpan(
                new SourceLocation(0, _lineStart, _columnStart),
                new SourceLocation(0, _lineEnd, _columnEnd == -1 ? Int32.MaxValue : _columnEnd)
            );
        }

        internal bool Contains(DebugSourceSpan candidateSpan) {
            if (candidateSpan._sourceFile != _sourceFile)
                return false;

            if (candidateSpan._lineStart < _lineStart || candidateSpan._lineEnd > _lineEnd)
                return false;

            if (candidateSpan._lineStart == _lineStart && candidateSpan._columnStart < _columnStart)
                return false;

            if (candidateSpan._lineEnd == _lineEnd && candidateSpan._columnEnd > _columnEnd)
                return false;

            return true;
        }

        internal bool Intersects(DebugSourceSpan candidateSpan) {
            if (candidateSpan._sourceFile != _sourceFile)
                return false;

            if (candidateSpan._lineEnd < _lineStart || candidateSpan._lineStart > _lineEnd)
                return false;

            if (candidateSpan._lineStart == _lineEnd && candidateSpan._columnStart > _columnEnd)
                return false;

            if (candidateSpan._lineEnd == _lineStart && _columnStart > candidateSpan._columnEnd)
                return false;

            return true;
        }

        internal int GetSequencePointIndex(FunctionInfo funcInfo) {
            DebugSourceSpan[] sequencePoints = funcInfo.SequencePoints;
            for (int i = 0; i < sequencePoints.Length; i++) {
                DebugSourceSpan sequencePoint = sequencePoints[i];

                if (Intersects(sequencePoint)) {
                    return i;
                }
            }

            return Int32.MaxValue;
        }
    }
}
