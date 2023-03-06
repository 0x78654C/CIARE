using System.Net.NetworkInformation;

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
    }
}
