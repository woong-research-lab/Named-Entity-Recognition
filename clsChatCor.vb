Imports System
Imports System.IO
Imports System.IO.Stream
Imports System.Xml
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class clsChatCor

    ''' <summary>
    ''' 챗봇 데이터 파싱 구조체
    ''' </summary>
    Structure Dic
        Public dicname As String
        Public lists() As String
    End Structure

    ''' <summary>
    ''' 도로공사의 챗봇 데이터에서 내용 추출. 다이얼로그 플로우의 데이터에 삽입
    ''' </summary>
    Public Sub ProcDFChat()
        Dim sr As System.IO.StreamReader
        Dim sw As System.IO.StreamWriter
        Dim tstr, tarr() As String
        Dim filePath, filePathArr() As String
        Dim fileInformation As System.IO.FileInfo
        Dim intentName As String
        Dim tempDic, dicList() As Dic
        Dim arr As JArray
        Dim text As JObject = New JObject()
        Dim cntr As Integer = 0

        ' 일단 반복관용어구를 읽어 사전으로 저장
        sr = New StreamReader("J: \exChatbot_1007\dic\lists.txt", System.Text.Encoding.UTF8)
        Do Until sr.EndOfStream
            ReDim Preserve dicList(cntr)
            tstr = sr.ReadLine
            tarr = tstr.Split(":")
            tempDic.dicname = tarr(0).Trim
            tempDic.lists = tarr(1).Split(",")
            dicList(cntr) = tempDic
            cntr += 1
        Loop
        sr.Close()

        '패턴 파일 목록을 읽고 패턴 내의 각 문장을 traversal
        filePathArr = Directory.GetFiles("J:\exChatbot_1007\pattern")
        For Each filePath In filePathArr
            '기존 다이얼로그 플로우 자료 읽기
            fileInformation = New FileInfo(filePath)
            intentName = fileInformation.Name.Split("_")(1).Replace(".txt", "")
            tstr = New StreamReader("J:\exChatbot_1007\intents\" & intentName & "_usersays_ko.json").ReadToEnd
            arr = JArray.Parse(tstr)

            '패턴 파일 내에서 각 패턴 읽기
            sr = New StreamReader(filePath)
            Do Until sr.EndOfStream
                tstr = sr.ReadLine
                ProcSentence(tstr, dicList, arr)
            Loop
            sr.Close()

            ' 파일로 출력
            sw = New StreamWriter("j:\izawa\" & intentName & "_usersays_ko.json")
            sw.Write(arr.ToString())
            sw.Close()
        Next

    End Sub

    ''' <summary>
    ''' 패턴 문장을 하나 받으면 이를 토크나이징 하고 JSON array로 만들어서 반환함. 단 용언 사전별로 문장을 반복 생성.
    ''' </summary>
    ''' <param name="sentence">입력되는 문장</param>
    ''' <param name="pattern">반복되는 용언 패턴</param>
    ''' <param name="intentDoc">실제 적용을 할 문서 이름</param>
    Public Sub ProcSentence(sentence As String, pattern() As Dic, ByRef intentDoc As JArray)
        Dim stack As String = ""
        Dim stack2 As String
        Dim cntr, acntr, tcntr As Integer
        Dim arr(0), entity() As String
        Dim doc As New JArray
        Dim tDoc As Dic
        Dim tPhrase As String                '반복관용어구 생성시 들어가는 반복 문장
        Dim hasPattern As Boolean
        Dim tToken As String                    '임시 토큰
        Dim obj As JObject
        acntr = 0

        '문장을 토크나이징 함
        Do Until cntr >= sentence.Length
            Select Case sentence(cntr)
                'entity 인식 태그 확인 (시스템에는 <> 형식으로 마킹)
                Case "<"
                    ' 만약 이전에 스택이 차 있는 곳이 있다면 채우고 지나감
                    If stack <> "" Then

                        arr(acntr) = stack
                        stack = ""
                        acntr += 1
                        ReDim Preserve arr(acntr)
                    End If

                    ' 특수 토큰 처리
                    tcntr = cntr
                    stack2 = ""
                    Do Until sentence(tcntr) = ">" Or tcntr >= sentence.Length
                        stack2 &= sentence(tcntr)
                        tcntr += 1
                    Loop
                    arr(acntr) = stack2.Replace("<", "@")
                    cntr = tcntr
                    acntr += 1
                    ReDim Preserve arr(acntr)
                '반복관용여구 인식 태그 확인(시스템에는 #; 형식으로 마킹)
                Case "#"
                    ' 만약 이전에 스택이 차 있는 곳이 있다면 채우고 지나감
                    If stack <> "" Then
                        arr(acntr) = stack
                        stack = ""
                        acntr += 1
                        ReDim Preserve arr(acntr)
                    End If

                    tcntr = cntr
                    stack2 = ""
                    Do Until sentence(tcntr) = ";" Or tcntr >= sentence.Length
                        stack2 &= sentence(tcntr)
                        tcntr += 1
                    Loop
                    arr(acntr) = stack2
                    cntr = tcntr
                    acntr += 1
                    ReDim Preserve arr(acntr)
                Case Else
                    stack &= sentence(cntr)
            End Select
            cntr += 1
        Loop

        '만약 최종 스택이 다른 문자열로 있다면 문자열을 넣어주고, 비어있다면 스택의 길이를 줄일 것
        If stack <> "" Then
            arr(acntr) = stack
        Else
            ReDim Preserve arr(acntr - 1)
        End If

        '패턴별로 루틴을 돌고 해당 문장의 sequence에 해당 패턴이 있을 경우 JArray에 문장을 추가
        '하나의 패턴 문장에는 하나의 반복관용어구만 허용(일단 버전 1)

        For Each tDoc In pattern        '패턴 인식 루틴 구동
            hasPattern = False
            For Each tstr In arr        '입력받은 문장의 토큰에서 패턴 인식이 있는 지확인
                If "#" & tDoc.dicname = tstr Then
                    hasPattern = True
                End If
            Next

            ' 만약 패턴이 있으면 반복해서 운영
            If hasPattern = True Then
                For Each tPhrase In tDoc.lists
                    Dim jSenDoc = New JArray
                    For Each tToken In arr
                        Select Case tToken(0)
                            Case "@"
                                entity = tToken.Replace("@", "").Split(":")
                                obj = New JObject
                                If entity.Length > 1 Then
                                    obj.Add(New JProperty("text", entity(1)))
                                    obj.Add(New JProperty("alias", entity(0)))
                                    obj.Add(New JProperty("meta", "@" & entity(0)))
                                Else
                                    obj.Add(New JProperty("text", tToken.Replace("@", "")))
                                    obj.Add(New JProperty("alias", tToken.Replace("@", "")))
                                    obj.Add(New JProperty("meta", tToken))
                                End If
                                obj.Add(New JProperty("userdefined", False))
                            Case "#"
                                obj = New JObject
                                obj.Add(New JProperty("text", tPhrase))
                                obj.Add(New JProperty("userdefined", False))
                            Case Else
                                obj = New JObject
                                obj.Add(New JProperty("text", tToken))
                                obj.Add(New JProperty("userdefined", False))
                        End Select
                        jSenDoc.Add(obj)
                    Next
                    Dim jMotherDoc As New JObject
                    jMotherDoc.Add(New JProperty("data", jSenDoc))
                    jMotherDoc.Add(New JProperty("isTemplate", False))
                    jMotherDoc.Add(New JProperty("count", 0))
                    jMotherDoc.Add(New JProperty("updated", 0))
                    '문장을 새로운 속성값으로 하여 Object 형태로 추가

                    intentDoc.Children.Last.AddAfterSelf(jMotherDoc)
                Next
            End If
        Next
    End Sub
End Class
