using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatServer.ChatServer;
using Shifr;

namespace ChatServer
{
    public class ServerObject
    {
        static TcpListener tcpListener; // сервер для прослушивания
        List<ClientObject> clients = new List<ClientObject>(); // все подключения
        public CryptoInitializers Keys;
        public string JsonKeys;

        public ServerObject()
        {
            Keys = DiffieHellman.GetOpenParametersAll();
            var stringModel = DiffieHellman.Converter(Keys);
            JsonKeys = JsonSerializer.Serialize(stringModel);
        }
        protected internal void AddConnection(ClientObject clientObject)
        {
            clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id)
        {
            // получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                clients.Remove(client);
        }
        // прослушивание входящих подключений
        protected internal void Listen()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 8888);
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                    Task.Delay(3);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        // трансляция сообщения подключенным клиентам
        protected internal void BroadcastMessage(string message, string id)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i].Id != id) // если id клиента не равно id отправляющего
                {
                    clients[i].Stream.Write(data, 0, data.Length); //передача данных
                }
            }
        }
        protected internal void BroadcastMessageToUser(string message, string id, string yOpen)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            var index = clients.FindIndex(x => x.Equals(yOpen));
            clients[index].Stream.Write(data, 0, data.Length);
        }
        // отключение всех клиентов
        protected internal void Disconnect()
        {
            tcpListener.Stop(); //остановка сервера

            for (int i = 0; i < clients.Count; i++)
            {
                clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }

        protected internal void SendKey(string id)
        {
            //var json = JsonSerializer.Serialize(Keys);
            var msg = JsonKeys + "XXXKEYXXX";
            foreach (var item in clients.Where(x=> x.Id != id).ToList())
            {
                msg += "," + item.YString;
            }
            byte[] data = Encoding.Unicode.GetBytes(msg);
            var index = clients.FindIndex(x => x.Id == id);
            clients[index].Stream.Write(data, 0, data.Length);
        }
    }
}
