using System.Net;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Runtime.Versioning;

namespace CIARE.Utils
{
    [SupportedOSPlatform("windows")]
    public class Network
    {
        private string IpAdress { get; set; }
        private int Port { get; set; } = 80;

        /// <summary>
        /// Check network connections.
        /// </summary>
        /// <param name="ipAdress"></param>
        /// <param name="port">Default 80</param>
        public Network(string ipAdress, int port = 80) 
        {
            IpAdress = ipAdress;
            Port = port;
        }
        /// <summary>
        /// Verifies if IP is up or not
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>verifies if IP is up or not</returns>
        public bool PingHost(int timeOut =1)
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(IpAdress,timeOut);
                pingable = reply.Status == IPStatus.Success;
            }
            catch
            {
                // We handle erros in other functions.
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }
            return pingable;
        }

        /// <summary>
        /// Check if website respond 200 code.
        /// </summary>
        /// <returns></returns>
        public bool IsWebResponding()
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = Task.Run(()=> client.GetAsync(IpAdress)).Result;
                HttpContent httpContent = response.Content;
                var  response2=  Task.Run(() => client.PostAsync(IpAdress, httpContent)).Result;
                bool isResponding = response.StatusCode == HttpStatusCode.OK;
                return isResponding;
            }
            catch
            {
               return false;
            }
        }

        /// <summary>
        /// Check socket connections;
        /// </summary>
        /// <returns></returns>
        public bool IsSocketConnected()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                try
                {
                    socket.Connect(IpAdress, Port);
                    return socket.Connected;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Check if api domain is up.
        /// </summary>
        /// <returns></returns>
        public bool IsLiveApiConnected()
        {
            var url = GlobalVariables.apiUrl.Replace("https://", "");
            url = url.Replace("http://", "");
            if(url.Contains("/"))
                url = url.Split("/")[0];
            IpAdress = url;
            return IsSocketConnected();
        }
    }
}
