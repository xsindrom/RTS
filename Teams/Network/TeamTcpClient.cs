using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Teams.Network
{
    //public class TeamTcpClient : ITeamClient
    //{
    //    private StreamWriter writer;
    //    private StreamReader reader;
    //    private NetworkStream stream;
    //    private TcpClient tcpClient;

    //    private string address;
    //    private int port;
    //    private string url;
        
    //    public TeamTcpClient(string address, int port)
    //    {
    //        this.address = address;
    //        this.port = port;
    //        this.url = address;
    //    }

    //    public void Connect()
    //    {
    //        tcpClient = new TcpClient();
    //        tcpClient.Connect(url, port);
    //        stream = tcpClient.GetStream();
    //        writer = new StreamWriter(stream);
    //        reader = new StreamReader(stream);

    //        var listenThread = new Thread(() =>
    //        {
    //            while (tcpClient.Connected)
    //            {
    //                var message = ReadMessage();
    //            }
    //        });
    //        listenThread.Start();

    //    }

    //    public string ReadMessage()
    //    {
    //        return stream.DataAvailable ?
    //            reader.ReadToEnd() :
    //            string.Empty;
    //    }

    //    public void Emit(string eventType, params string[] eventParams)
    //    {
    //        StringBuilder builder = new StringBuilder();
    //        builder.Append('[');
    //        builder.Append($"\"{eventType}\"");
    //        for(int i = 0; i < eventParams.Length; i++)
    //        {
    //            builder.Append($",{eventParams[i]}");
    //        }
    //        builder.Append(']');
    //        writer.Write(builder.ToString());
    //        writer.Flush();
    //    }

    //    public void Close()
    //    {
    //        writer.Close();
    //        reader.Close();
    //        stream.Close();
    //        tcpClient.Close();
    //    }
    //}
}