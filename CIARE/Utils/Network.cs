﻿using System.Net;
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
        private string IpAddress { get; set; }
        private int Port { get; set; } = 80;

        /// <summary>
        /// Check network connections.
        /// </summary>
        /// <param name="ipAdress"></param>
        /// <param name="port">Default 80</param>
        public Network(string ipAdress, int port = 80) 
        {
            IpAddress = ipAdress;
            Port = port;
        }

        /// <summary>
        /// Verifies if IP is up or not
        /// </summary>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public bool PingHost(int timeOut = 100)
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(IpAddress,timeOut);
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
                HttpResponseMessage response = Task.Run(()=> client.GetAsync(IpAddress)).Result;
                HttpContent httpContent = response.Content;
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
                    socket.Connect(IpAddress, Port);
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
            var url = IpAddress.EndsWith("/live") ? GlobalVariables.apiUrl.Replace("/live", "/ping"):"";
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = Task.Run(() => client.GetAsync(url)).Result;
                HttpContent httpContent = response.Content;
                bool isResponding = response.StatusCode == HttpStatusCode.OK;
                return isResponding;
            }
            catch
            {
                return false;
            }
        }
    }
}
