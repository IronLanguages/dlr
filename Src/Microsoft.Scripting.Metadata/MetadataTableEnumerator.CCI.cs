// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.Scripting.Metadata {
    internal enum EnumerationIndirection {
        None = 0,
        Method = 1,
        Field = 2,
        Property = 3,
        Event = 4,
        Param = 5,
    }

    public sealed class MetadataTableEnumerator {
        private readonly int m_startRid;
        private readonly int m_endRid;
        private readonly MetadataTokenType m_type;
        private readonly EnumerationIndirection m_indirection;
        private MetadataTables m_tables;
        private int m_currentRid;
        private MetadataToken m_currentToken;

        internal MetadataTableEnumerator(MetadataRecord parent, MetadataTokenType type) {
            Debug.Assert(parent.IsValid);

            m_type = type;
            m_tables = parent.m_tables;

            m_indirection = parent.m_tables.m_import.GetEnumeratorRange(
                type,
                parent.Token,
                out int start,
                out int count);
            
            m_startRid = start;
            m_endRid = start + count;
            m_currentRid = start - 1;
        }

        public int Count {
            get { return m_endRid - m_startRid; }
        }

        public void Reset() {
            m_currentRid = m_startRid;
            m_currentToken = default(MetadataToken);
        }

        public bool MoveNext() {
            int nextRid = m_currentRid + 1;

            if (nextRid >= m_endRid) {
                if (m_tables == null) {
                    throw new ObjectDisposedException("MetadataTableEnumerator");
                }
                return false;
            }

            m_currentRid = nextRid;

            switch (m_indirection) {
                case EnumerationIndirection.Method:
                    m_currentToken = m_tables.m_import.MethodPtrTable.GetMethodFor(m_currentRid);
                    break;

                case EnumerationIndirection.Field:
                    m_currentToken = m_tables.m_import.FieldPtrTable.GetFieldFor(m_currentRid);
                    break;

                case EnumerationIndirection.Property:
                    m_currentToken = m_tables.m_import.PropertyPtrTable.GetPropertyFor(m_currentRid);
                    break;

                case EnumerationIndirection.Event:
                    m_currentToken = m_tables.m_import.EventPtrTable.GetEventFor(m_currentRid);
                    break;

                case EnumerationIndirection.Param:
                    m_currentToken = m_tables.m_import.ParamPtrTable.GetParamFor(m_currentRid);
                    break;

                default:
                    m_currentToken = new MetadataToken(m_type, (uint)m_currentRid);
                    break;
            }

            return true;
        }

        public MetadataRecord Current {
            get { 
                return new MetadataRecord(m_currentToken, m_tables); 
            }
        }
    }

}
