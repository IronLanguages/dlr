' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the Apache 2.0 License.
' See the LICENSE file in the project root for more information.

Namespace Merlin.Testing.BaseClass
  Public Interface IVbIndexer10
    Default Property IntProperty(ByVal index As Integer) As Integer
  End Interface

  Public Interface IVbIndexer11
    Default Property StrProperty(ByVal index1 As Integer, ByVal index2 As Integer) As String
  End Interface

  Public Interface IVbIndexer20
    Property DoubleProperty(ByVal index As Integer) As Double
  End Interface

  Public Class CVbIndexer30
    Public Overridable Property StrProperty(ByVal index1 As Integer, ByVal index2 As Integer) As String
      Get
        Return "abc"
      End Get
      Set(ByVal value As String)

      End Set
    End Property
  End Class

  Public Class VbCallback
    Public Shared Sub Act(ByVal arg As IVbIndexer10)
      arg.IntProperty(10) = arg.IntProperty(100) + 1000
    End Sub

    Public Shared Sub Act(ByVal arg As IVbIndexer20)
      arg.DoubleProperty(1) = arg.DoubleProperty(2) + 0.003
    End Sub
  End Class
End Namespace
