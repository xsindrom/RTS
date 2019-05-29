using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SimpleJSON;
namespace Teams.Network
{
    public class TeamWebSocketPackets
    {
        public const int CONNECT = 0;
        public const int DISCONNECT = 1;
        public const int EVENT = 2;
        public const int ACK = 3;
        public const int ERROR = 4;
        public const int BINARY_EVENT = 5;
        public const int BINARY_ACK = 6;
    }
    [Serializable]
    public struct TeamWebSocketPacket
    {
        public const string MESSAGE_PATTERN = "{0}{1}[\"{2}\", \"{3}\"]";
        [SerializeField]
        private int packet;
        [SerializeField]
        private int protocolVersion;
        [SerializeField]
        private string eventType;
        [SerializeField]
        private string[] eventParams;

        public int Packet
        {
            get { return packet; }
            set { packet = value; }
        }

        public int ProtocolVersion
        {
            get { return protocolVersion; }
            set { protocolVersion = value; }
        }

        public string EventType
        {
            get { return eventType; }
            set { eventType = value; }
        }

        public string[] EventParams
        {
            get { return eventParams; }
            set { eventParams = value; }
        }

        public static TeamWebSocketPacket Parse(string inputString)
        {
            if (inputString.Length > 2 && inputString[2] != '[')
                return default(TeamWebSocketPacket);

            var endMessageIndex = inputString.LastIndexOf(']');

            var codeStr = inputString.Substring(0, 2);
            var message = inputString.Substring(2, endMessageIndex - 1);

            int.TryParse(codeStr,out int code);

            int protocolVersion = code / 10;
            int packet = code % 10;

            JSONArray jsonArray = (JSONArray)JSONNode.Parse(message);
            var newPacket = new TeamWebSocketPacket()
            {
                ProtocolVersion = protocolVersion,
                Packet = packet,
                EventType = jsonArray.Count > 0 ? jsonArray[0].Value : string.Empty
            };
            newPacket.EventParams = new string[jsonArray.Count - 1];
            for (int i = 1; i < jsonArray.Count; i++)
            {
                newPacket.EventParams[i-1] = jsonArray[i].Value;
            }
            return newPacket;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(protocolVersion);
            builder.Append(packet);
            builder.Append('[');
            builder.Append($"\"{eventType}\"");
            if (eventParams != null)
            {
                for (int i = 0; i < eventParams.Length; i++)
                {
                    builder.Append($",\"{eventParams[i]}\"");
                }
            }
            builder.Append(']');
            return builder.ToString();
        }
    }

    public class TeamWebSocketClient : ITeamClient, IDisposable
    {
        private const string URL_PATTERN = "ws://{0}:{1}/socket.io/?EIO=3&transport=websocket";
        private const string NORMAL_CLOSURE_DESCRIPTION_STATUS = "Normal Closure";
        private const int PROTOCOL_VERSION = 4;

        private ClientWebSocket socket;
        private string address;
        private int port;
        private string url;
        private bool isReconnect;

        public event Action OnReconnect;
        public event Action<TeamWebSocketPacket> OnPacketReceived;
        public event Action<TeamWebSocketPacket> OnPacketSent;

        private ArraySegment<byte> receiveBuffer;
        private ArraySegment<byte> sendBuffer;
        

        public TeamWebSocketClient(string address, int port)
        {
            this.address = address;
            this.port = port;
            this.url = string.Format(URL_PATTERN, address, port);
        }

        public Task<bool> WaitForConnect()
        {
            var task = new Task<bool>(() =>
            {
                while (socket == null || socket.State != WebSocketState.Open) {  }
                return true;
            });
            task.Start();
            return task;
        }
        

        public async void Connect()
        {
            try
            {
                socket = new ClientWebSocket();
                await socket.ConnectAsync(new Uri(url), CancellationToken.None);
                if (isReconnect)
                {
                    OnReconnect?.Invoke();
                    isReconnect = false;
                }

                receiveBuffer = new ArraySegment<byte>(new byte[1024]);
                sendBuffer = new ArraySegment<byte>(new byte[1024]);

                while (!socket.CloseStatus.HasValue)
                {
                    await socket.ReceiveAsync(receiveBuffer, CancellationToken.None);
                    var message = ReadMessage();

                    var packet = TeamWebSocketPacket.Parse(message);

                    OnPacketReceived?.Invoke(packet);

                    Array.Clear(receiveBuffer.Array, 0, receiveBuffer.Array.Length);
                }

                if (socket.CloseStatusDescription != NORMAL_CLOSURE_DESCRIPTION_STATUS)
                {
                    isReconnect = true;
                    Connect();
                }
            }
            catch (WebSocketException e)
            {
                isReconnect = true;
                Connect();
            }
        }

        public string ReadMessage()
        {
            return receiveBuffer.Array == null ? string.Empty: Encoding.UTF8.GetString(receiveBuffer.Array);
        }

        public async void Emit(string eventType, params string[] eventParams)
        {
            try
            {
                var packet = new TeamWebSocketPacket()
                {
                    ProtocolVersion = PROTOCOL_VERSION,
                    Packet = TeamWebSocketPackets.EVENT,
                    EventType = eventType,
                    EventParams = eventParams
                };

                sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(packet.ToString()));
                await socket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                Array.Clear(sendBuffer.Array, 0, sendBuffer.Array.Length);
            }
            catch (WebSocketException e)
            {

            }
        }

        public async void Close()
        {
            if (socket == null || socket.State != WebSocketState.Open)
                return;

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, NORMAL_CLOSURE_DESCRIPTION_STATUS, CancellationToken.None);
            Dispose();
        }

        public void Dispose()
        {
            OnReconnect = null;
            OnPacketReceived = null;
            OnPacketSent = null;
        }
    }
}