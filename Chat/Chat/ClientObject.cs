using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Shifr;

namespace ChatServer
{
    namespace ChatServer
    {
        public class ClientObject
        {
            protected internal string Id { get; private set; }
            protected internal NetworkStream Stream { get; private set; }
            string userName;
            TcpClient client;
            ServerObject server; // объект сервера
            private BigInteger Y;
            protected internal string YString;

            public ClientObject(TcpClient tcpClient, ServerObject serverObject)
            {
                Id = Guid.NewGuid().ToString();
                client = tcpClient;
                server = serverObject;
                serverObject.AddConnection(this);
            }

            public void Process()
            {
                try
                {
                    Stream = client.GetStream();
                    server.SendKey(this.Id);
                    // получаем имя пользователя
                    string message = GetNameAndKey();
                    server.BroadcastMessage("XXXNEWUSER" + YString, this.Id);
                    userName = message;

                    message = userName + " вошел в чат";
                    // посылаем сообщение о входе в чат всем подключенным пользователям
                    server.BroadcastMessage(message, this.Id);
                    Console.WriteLine(message);
                    // в бесконечном цикле получаем сообщения от клиента
                    while (true)
                    {
                        try
                        {
                            message = GetMessage();
                            Console.WriteLine(message);
                            var model = JsonSerializer.Deserialize<List<string>>(message);
                            foreach (var str in model)
                            {
                                var list = str.Split("XXXKEYXXX");
                                
                                server.BroadcastMessageToUser(YString + "XXXKEYXXX" + list[1], this.Id, list[0]);
                            }
                        }
                        catch
                        {
                            message = String.Format("{0}: покинул чат", userName);
                            Console.WriteLine(message);
                            server.BroadcastMessage(message, this.Id);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    // в случае выхода из цикла закрываем ресурсы
                    server.RemoveConnection(this.Id);
                    Close();
                }
            }

            private string GetNameAndKey()
            {
                byte[] data = new byte[64]; // буфер для получаемых данных
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = Stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                } while (Stream.DataAvailable);

                var str = builder.ToString();
                var strMas = str.Split(",");
                var name = strMas[0];
                YString = strMas[1];

                return name;
            }

            // чтение входящего сообщения и преобразование в строку
            private string GetMessage()
            {
                byte[] data = new byte[64]; // буфер для получаемых данных
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = Stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                } while (Stream.DataAvailable);

                return builder.ToString();
            }

            // закрытие подключения
            protected internal void Close()
            {
                if (Stream != null)
                    Stream.Close();
                if (client != null)
                    client.Close();
            }
        }
    }
}
