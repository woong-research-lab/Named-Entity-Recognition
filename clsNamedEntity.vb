Imports System
Imports System.IO
Imports System.IO.Stream
Imports System.Xml

''' <summary>
''' 내용어 추출과 관련된 핵심 엔진 모듈 연계
''' </summary>

Public Class ClsNamedEntity

    Structure Pattern
        Dim source As String
        Dim target As String
    End Structure

    Public patterns(0) As Pattern

    ''' <summary>
    ''' 내용어 엔진이 처음 만들어질 때 내용어 사전을 불러들임
    ''' </summary>
    ''' <param name="srcFileName">mecab에서 처리가 완료된 내용어 사전 목록. 각 내용어는 EOS 스트림으로 분리되어 있어야 함.</param>
    Public Sub New(srcFileName As String)
        Dim sr As System.IO.StreamReader
        Dim tarr(), tarr2(), lexArr(1) As String
        Dim tstr As String
        sr = New System.IO.StreamReader(srcFileName)
        Do Until sr.EndOfStream
            tstr = sr.ReadLine
            If tstr <> "EOS" Then
                tarr = tstr.Split(vbTab)
                tarr2 = tarr(1).Split(",")
                patterns(patterns.Length - 1).source &= tarr(0) & vbTab & tarr2(0) & vbTab & "\n" & vbTab
                patterns(patterns.Length - 1).target &= tarr(0) & vbTab & "NERTAG" & vbTab & "\n" & vbTab
            Else
                ReDim Preserve patterns(patterns.Length + 1)
            End If
        Loop
    End Sub

    ''' <summary>
    ''' 어노테이션 된 패턴을 찾아준다.
    ''' 들어오는 데이터에서 엔터키 부분을 모두 나열형 문자로 분리해준뒤, 패턴 검색이 가능하도록 구성.
    ''' 그 이후 기존의 패턴을 1:1 텍스트 패턴 방식으로 찾아내고 변환한 뒤, 패턴으로 된 내용은 NERTAG로 처리
    ''' </summary>
    ''' <param name="text">어노데이션 되기 이전의 형태소-품사-형태소-품사 형태의 데이터</param>
    ''' <returns>어노테이션 된 패턴을 반납한다.</returns>
    Public Function annotatePattern(text As String) As String
        Dim tempPattern As Pattern
        text = text.Replace(vbLf, vbTab & "\n" & vbTab)
        For Each tempPattern In patterns
            If Not tempPattern.source Is Nothing Then
                text = text.Replace(tempPattern.source, tempPattern.target)
            End If
        Next
        Return text.Replace(vbTab & "\n" & vbTab, vbLf)
    End Function

    ''' <summary>
    ''' 패턴 추출용 사전을 읽어들이고 클래스 내에 셋업함
    ''' (미래 품사+seed word 기반의 NER 엔진을 구성하는 경우)
    ''' </summary>
    ''' <param name="srcFileName">패턴 파일을 읽어들임(품사와 패턴 중심으로 이동)</param>
    Public Sub SetPattern(srcFileName As String)
        Dim fr As System.IO.StreamReader
        fr = New System.IO.StreamReader(srcFileName)
    End Sub

End Class
