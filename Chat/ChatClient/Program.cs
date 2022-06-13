using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Shifr;

namespace ChatClient
{
    class Program
    {
        static string userName;
        private const string host = "127.0.0.1";
        private const int port = 8888;
        static TcpClient client;
        static NetworkStream stream;
        private static BigInteger X;
        private static BigInteger Y;
        private static string YString;
        private static CryptoInitializers Keys;
        private static List<Tuple<BigInteger, BigInteger, string>> UserList = new List<Tuple<BigInteger, BigInteger, string>>(); //Y, Z, ZString 

        static void Main(string[] args)
        {
            Console.Write("Введите свое имя: ");
            userName = Console.ReadLine();
            client = new TcpClient();
            try
            {
                client.Connect(host, port); //подключение клиента
                stream = client.GetStream(); // получаем поток

                GetKeys();

                string message = userName + "," + YString;
                byte[] data = Encoding.Unicode.GetBytes(message);
                stream.Write(data, 0, data.Length);


                // запускаем новый поток для получения данных
                Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
                receiveThread.Start(); //старт потока
                Console.WriteLine("Добро пожаловать, {0}", userName);
                SendMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }

        private static void GetKeys()
        {
            try
            {
                byte[] data = new byte[64]; // буфер для получаемых данных
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                do
                {
                    bytes = stream.Read(data, 0, data.Length);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                } while (stream.DataAvailable);

                string message = builder.ToString();
                var list = message.Split("XXXKEYXXX");
                var keys = JsonSerializer.Deserialize<CryptoInitializersString>(list[0]);
                Keys = DiffieHellman.Converter(keys);
                X = DiffieHellman.GetCloseKeyX(Keys);
                Y = DiffieHellman.GetOpenKeyY(Keys, X);
                YString = Y.ToString();
                var listw = list[1].Split(",");
                for (int i = 1; i < listw.Length; i++)
                {
                    var y = new BigInteger(Encoding.ASCII.GetBytes(listw[i]));
                    var z = DiffieHellman.GetZ(y, X, Keys.P);
                    UserList.Add(new Tuple<BigInteger, BigInteger, string>(y, z, z.ToString()));
                }
                Console.WriteLine("Параметры qpg получены"); //вывод сообщения
                return;
            }
            catch
            {
                Console.WriteLine("Подключение прервано! Попробуйте еще раз."); //соединение было прервано
                Console.ReadLine();
                Disconnect();
            }
        }

        // отправка сообщений
        static void SendMessage()
        {
            Console.WriteLine("Введите сообщение: ");

            while (true)
            {
                string message = Console.ReadLine();
                var intList = Encrypt(message);
                byte[] data = Encoding.Unicode.GetBytes(intList);
                stream.Write(data, 0, data.Length);
            }
        }
        static string Encrypt(string message)
        {
            message = String.Format("{0}: {1}", userName, message);
            var msgList = new List<string>();
            foreach (var user in UserList)
            {
                var msg = user.Item3 + "XXXKEYXXX";
                for (int i = 0; i < message.Length; i++)
                {
                    var name = message[i];
                    var e = DiffieHellman.Encript((int)name, user.Item2, Keys.P);
                    msg += e.ToString() + ",";
                }

                msg = msg.Substring(0, msg.Length - 1);
                msgList.Add(msg);
            }

            return JsonSerializer.Serialize(msgList);
        }
        // получение сообщений
        static void ReceiveMessage()
        {
            while (true)
            {
                try
                {
                    byte[] data = new byte[64]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);

                    string messageShift = builder.ToString();
                    if (messageShift.Contains("XXXNEWUSER"))
                    {
                        try
                        {
                            var str = messageShift.Replace("XXXNEWUSER", "");
                            var y = new BigInteger(Encoding.ASCII.GetBytes(str));
                            var z = DiffieHellman.GetZ(y, X, Keys.P);
                            UserList.Add(new Tuple<BigInteger, BigInteger, string>(y, z, z.ToString()));
                            return;
                        }
                        catch
                        {
                            Console.WriteLine("Новый пользователь не добавлен!");
                            Console.ReadLine();
                        }
                    }
                    var message = messageShift.Contains("XXXKEYXXX") ? DecoderMessage(messageShift) : messageShift; 
                    Console.WriteLine(message);//вывод сообщения
                }
                catch
                {
                    Console.WriteLine("Подключение прервано!"); //соединение было прервано
                    Console.ReadLine();
                    Disconnect();
                }

            }
        }

        private static string DecoderMessage(string messageShift)
        {
            try
            {
                var strM = messageShift.Split("XXXKEYXXX");
                var y = new BigInteger(Encoding.ASCII.GetBytes(strM[0]));
                var msgS = JsonSerializer.Deserialize<List<string>>(strM[1]);
                var message = string.Empty;
                var z = UserList.FirstOrDefault(x => x.Item1.CompareTo(y) == 0)?.Item2;
                for (int i = 0; i < msgS.Count; i++)
                {
                    var name = msgS[i];
                    var e = DiffieHellman.Decript(new BigInteger(Encoding.ASCII.GetBytes(name)), z.GetValueOrDefault(), Keys.P);
                    message += char.ConvertFromUtf32(int.Parse(e.ToString()));
                }

                return message;
            }
            catch
            {
                Console.WriteLine("Не удалось декодировать прервано!"); //соединение было прервано
                Console.ReadLine();
                return "";
            }
        }

        static void Disconnect()
        {
            if (stream != null)
                stream.Close();//отключение потока
            if (client != null)
                client.Close();//отключение клиента
            Environment.Exit(0); //завершение процесса
        }
    }
}
