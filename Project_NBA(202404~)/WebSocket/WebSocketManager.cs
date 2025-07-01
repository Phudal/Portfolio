using Dimps.Application.Socket.Message;
using Dimps.Utility;
using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using Dimps.Utility.Singleton;

using WebSocketSharp;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;




#if UNITY_EDITOR
using UnityEditor;
#endif


public class WebSocketManager : DimpsSingletonMonoBehaviour<WebSocketManager, WebSocketManager>, ISingletonHandler
{
    private WebSocket webSocket = null;

    public List<Chat> guildChatDataList = new List<Chat>();
    
    private string curLoginId = string.Empty;

    private bool isLoginComplete = false;

    private bool isChatReceived = false;

    private IEnumerator pingCO = null;

    private const int HEADER_SIZE = 8;
    private const int PING_INTERVAL = 60;


    private const int MAX_GUILD_CHAT = 100;

    public void Initialize()
    {
        DontDestroyOnLoad(this);
    }

    public void InitializeWebSocketURL(string webSocketUrl)
    {        
        try
        {
            webSocket = new WebSocket(webSocketUrl);
            webSocket.OnMessage += OnMessageRecieve;
            webSocket.OnClose += OnCloseConnect;
            webSocket.OnOpen += OnOpen;

            DebugTool.Log("<color=cyan>[WebSocket]</color> Initialize WebSocket URL : " + webSocketUrl);
        }
        catch (WebSocketException e)
        {
            DebugTool.LogError(e);
        }
    }

    public void InitializeWebSocketLoginId(string loginId)
    {
        curLoginId = loginId;
        DebugTool.Log("<color=cyan>[WebSocket]</color> Initialize WebSocket loginId : " + loginId);
    }

    public async void ConnectServer()
    {
        guildChatDataList = new List<Chat>();
        isLoginComplete = false;
        isChatReceived = false;

        if (pingCO != null)
        {
            StopCoroutine(pingCO);
        }
        pingCO = null;

        await Task.Run(() =>
        {
            try
            {
                if (webSocket == null || !webSocket.IsAlive)
                {
                    webSocket.Connect();
                    

                    if (webSocket.IsAlive)
                    {
                        DebugTool.Log("<color=cyan>[WebSocket]</color> Success Connect to Socket");
                    }
                    else
                    {
                        DebugTool.Log("<color=cyan>[WebSocket]</color> Fail Connect to Socket");
                    }
                }
            }
            catch (Exception e)
            {
                DebugTool.LogError(e.ToString());
            }
        });
        
    }

    private void SendPacket(WebSocket ws, int packetId, IMessage payload)
    {
        try
        {
            var payloadBytes = payload.ToByteArray();

            // 헤더 생성
            byte[] header = new byte[HEADER_SIZE];
            using (var ms = new MemoryStream(header))
            {
                var writer = new BinaryWriter(ms);
                writer.Write(IPAddress.HostToNetworkOrder(packetId));             // Packet ID (2 bytes)
                writer.Write(IPAddress.HostToNetworkOrder(payloadBytes.Length)); // Payload Length (4 bytes)
            }

            // 전체 패킷 생성 (헤더 + 페이로드)
            byte[] packet = new byte[header.Length + payloadBytes.Length];
            Buffer.BlockCopy(header, 0, packet, 0, header.Length);
            Buffer.BlockCopy(payloadBytes, 0, packet, header.Length, payloadBytes.Length);

            // 전송
            ws.Send(packet);

            DebugTool.Log($"<color=cyan>[WebSocket]</color> Sent Packet ID {packetId}, Size: {payloadBytes.Length} bytes");
        }
        catch (Exception ex)
        {
            DebugTool.LogError($"Error sending packet: {ex.Message}");
        }
    }

    public void SendPing()
    {
        try
        {
            var ping = new Dimps.Application.Socket.Message.Ping();
            SendPacket(webSocket, (int)Dimps.Application.Socket.Message.ProtoId.PtIdPing, ping);
        }
        catch (Exception e)
        {
            DebugTool.LogError("Error by ping " + e.ToString());
        }
    }

    public void SendLogin()
    {
        try
        {
            var login = new Dimps.Application.Socket.Message.LoginReq();
            login.UserId = curLoginId;
            SendPacket(webSocket, (int)Dimps.Application.Socket.Message.ProtoId.PtIdLoginReq, login);
        }
        catch (Exception ex)
        {
            DebugTool.LogError($"Error sending packet: {ex.Message}");
        }
    }

    public void SendChatMessage(string message)
    {
        try
        {
            var chat = new Dimps.Application.Socket.Message.GuildChatReq();
            chat.UserId = curLoginId;
            chat.ChatType = (int)Dimps.Application.Socket.Message.ChatType.NormalChat;
            chat.Msg = message;
            SendPacket(webSocket, (int)Dimps.Application.Socket.Message.ProtoId.PtIdGuildChatReq, chat);
        }
        catch (Exception ex)
        {
            DebugTool.LogError($"Error sending packet: {ex.Message}");
        }
    }

    public void SendChatMessageList(long lastIndex)
    {
        try
        {
            var chatList = new Dimps.Application.Socket.Message.GuildChatListReq();
            chatList.UserId = curLoginId;
            chatList.LastIdx = lastIndex;
            SendPacket(webSocket, (int)Dimps.Application.Socket.Message.ProtoId.PtIdGuildChatListReq, chatList);
        }
        catch (Exception ex)
        {
            DebugTool.LogError($"Error sending packet: {ex.Message}");
        }
    }

    public void DisconnectServer()
    {
        if (pingCO != null)
        {
            StopCoroutine(pingCO);
            pingCO = null;
        }

        isLoginComplete = false;
        isChatReceived = false;
        guildChatDataList.Clear();

        try
        {
            if (webSocket == null)
                return;

            if (webSocket.IsAlive)
            {
                webSocket.Close();
                DebugTool.Log("<color=cyan>[WebSocket]</color> Disconnect to Socket");
            }
        }

        catch (Exception e)
        {
            DebugTool.LogError(e.ToString());
        }
    }

    private void OnMessageRecieve(object sender, MessageEventArgs e)
    {
        int packetId;
        int payloadLength;

        // 헤더 파싱
        using (var ms = new MemoryStream(e.RawData))
        {
            var reader = new BinaryReader(ms);
            packetId = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            payloadLength = IPAddress.NetworkToHostOrder(reader.ReadInt32());
        }

        // Body 파싱
        if (e.RawData != null && e.RawData.Length >= HEADER_SIZE)
        {
            using var stream = new MemoryStream(e.RawData);
            using var reader = new BinaryReader(stream);

            byte[] payload = new byte[payloadLength];
            Array.Copy(e.RawData, HEADER_SIZE, payload, 0, payloadLength);

            switch (packetId)
            {
                case (int)Dimps.Application.Socket.Message.ProtoId.PtIdCommonRes:
                    {
                        var msg = Dimps.Application.Socket.Message.CommonRes.Parser.ParseFrom(payload);
                        if (msg.TargetProtoId == (short)Dimps.Application.Socket.Message.ProtoId.PtIdPing)
                        {
                            DebugTool.Log("<color=cyan>[WebSocket]</color> Response Ping");
                        }
                        else if (msg.TargetProtoId == (short)Dimps.Application.Socket.Message.ProtoId.PtIdLoginReq)
                        {
                            if (msg.Status == (short)StatusCode.PtSuccess)
                            {
                                isLoginComplete = true;

                                AwaitAndStartPingCO().Forget();

                                DebugTool.Log("<color=cyan>[WebSocket]</color> Socket Login Success");

                                SendChatListAsync().Forget();
                            }
                            else
                            {
                                DebugTool.Log($"<color=cyan>[WebSocket]</color> Socket Login Fail : {(StatusCode)msg.Status}");
                            }
                        }
                        else
                        {
                            DebugTool.Log($"<color=cyan>[WebSocket]</color> Recive proto id : {msg.TargetProtoId}, Status : {msg.Status}");
                        }

                    }
                    break;
                case (int)Dimps.Application.Socket.Message.ProtoId.PtIdGuildChatMsg:
                    {
                        var msg = Dimps.Application.Socket.Message.GuildChatMsg.Parser.ParseFrom(payload);                        

#if UNITY_EDITOR
                        StringBuilder sb = new StringBuilder();
                        foreach (var chat in msg.ChatList)
                        {
                            sb.AppendLine($"<color=cyan>[WebSocket]</color> Recive chat message index : {chat.Idx}, Sender : {chat.SenderUserId}, " +
                                $"Type : {chat.ChatType}, message : {chat.Msg}, Time : {chat.RegisterTime}");
                        }

                        DebugTool.Log(sb.ToString());
#endif

                        guildChatDataList.AddRange(msg.ChatList);

                        if (guildChatDataList.Count > MAX_GUILD_CHAT)
                        {
                            guildChatDataList.RemoveRange(0, guildChatDataList.Count - MAX_GUILD_CHAT);
                        }                        

                        Broadcaster<List<Chat>>.Broadcast(EBroadcastKey.SOCKET_OnChatMessageRecv, msg.ChatList.ToList());

                        isChatReceived = true;
                    }
                    break;
                default:
                    {
                        DebugTool.Log("<color=cyan>[WebSocket]</color> UNKNOWN packetId : " + packetId);
                    }
                    break;
            }

        }
    }

    private void OnOpen(object sender, EventArgs e)
    {
        DebugTool.Log("<color=cyan>[WebSocket]</color> Success to socket connection");

        SendLogin();
    }

    private void OnCloseConnect(object sender, CloseEventArgs e)
    {
        DebugTool.Log($"<color=cyan>[WebSocket]</color> OnCloseConnect Code - {e.Code}, Reason - {e.Reason}");

        // 서버에서 강제로 끊을 경우
        if (e.Code == (short)Dimps.Application.Socket.Message.StatusCode.CloseSocketAuthSessionError)
        {
            // 끊겼음
        }

        DisconnectServer();
    }

    WaitForSeconds ping_interval = new WaitForSeconds(PING_INTERVAL);

    // Recv를 처리하는 스레드가 메인 스레드가 아닐 때
    // 메인 스레드에서 코루틴을 처리하기 위해 사용
    private async UniTask AwaitAndStartPingCO()
    {
        await UniTask.Yield();

        if (pingCO != null)
        {
            StopCoroutine(pingCO);
            pingCO = null;
        }

        pingCO = IntervalPingCO();
        StartCoroutine(pingCO);
    }

    private async UniTask SendChatListAsync()
    {
        await UniTask.Yield();

        long lastIndex = 0;
        if (guildChatDataList.Count > 0)
        {
            lastIndex = guildChatDataList[guildChatDataList.Count - 1].Idx;
        }

        SendChatMessageList(lastIndex);
    }

    private IEnumerator IntervalPingCO()
    {
        while (true)
        {
            if (webSocket.IsAlive == false || isLoginComplete == false)
                break;

            SendPing();

            yield return ping_interval;
        }
    }

    public bool IsChatEnable()
    {
        if (webSocket == null)
            return false;

        if (webSocket.IsAlive == false)
            return false;

        if (isLoginComplete == false)
            return false;

        if (isChatReceived == false)
            return false;

        return true;
    }

    public List<Chat> GetGuildChatDataList()
    {
        return guildChatDataList;
    }   

    public void Destroy()
    {
        DisconnectServer();
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(WebSocketManager))]
public class WebSocketManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Connect"))
        {
            WebSocketManager.Instance.ConnectServer();
        }

        if (GUILayout.Button("Send Login"))
        {
            WebSocketManager.Instance.SendLogin();
        }

        if (GUILayout.Button("Disconnect Server"))
        {
            WebSocketManager.Instance.DisconnectServer();
        }
    }
}
#endif