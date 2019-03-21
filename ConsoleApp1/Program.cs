using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Server();
            Console.ReadLine();
        }

        private static async Task<string> teste(string url)
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);

            HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
            htmlDocument.LoadHtml(html);

            var divs = htmlDocument.DocumentNode.Descendants().Where(node => node.GetAttributeValue("class", "").Equals("infobox"));
            Teste Cadeira = new Teste();
            foreach (var item in divs)
            {

                Cadeira.Nome = item.Descendants("caption").FirstOrDefault().InnerText;

                foreach (var item2 in item.Descendants("td"))
                {
                    if (item2.Descendants("a").FirstOrDefault() != null)
                    {
                        Cadeira.Professor = item2.Descendants("a").FirstOrDefault().InnerText;
                        break;
                    }

                }

                if (Cadeira.Nome != "" && Cadeira.Professor != "")
                {
                    break;
                }
            }

            return Cadeira.Nome + " - " + Cadeira.Professor;
        }

        const int PORT_NO = 5000;
        const string SERVER_IP = "127.0.0.1";

        static async void Server()
        {
            //---listen at the specified IP and port no.---
            IPAddress localAdd = IPAddress.Parse(SERVER_IP);
            TcpListener listener = new TcpListener(localAdd, PORT_NO);
            Console.WriteLine("Listening...");
            listener.Start();

            //---incoming client connected---
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                //---get the incoming data through a network stream---
                NetworkStream nwStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                //---read incoming stream---
                int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                //---convert the data received into a string---
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                string response = await teste(dataReceived);

                //---write back the text to the client---
                Console.WriteLine("Sending back : " + response);
                nwStream.Write(Encoding.UTF8.GetBytes(response), 0, Encoding.UTF8.GetBytes(response).Length);
                client.Close();
            }
        }
    }
}
