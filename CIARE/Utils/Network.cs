using System.Net;
using System;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;

namespace CIARE.Utils
{
    public class Network
    {
        private string IpAdress { get; set; }

        public Network(string ipAdress) 
        {
            IpAdress = ipAdress;    
        }
        /// <summary>
        /// Verifies if IP is up or not
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>verifies if IP is up or not</returns>
        public bool PingHost()
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(IpAdress);
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
