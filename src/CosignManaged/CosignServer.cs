// CosignServer.cs
//
// Copyright (C) 2011 The Pennsylvania State University
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License as
// published by the Free Software Foundation; either version 2 of
// the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR
// PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston,
// MA 02111-1307 USA

namespace CosignManaged
{
    using NLog;
    using System;
    using System.Net;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    internal class CosignServer
    {
        public readonly IPAddress ServerIpAddress;
        private readonly Logger _logger = GlobalLogger.Instance.GetLogger("CosignConnection");
        private readonly string _serverName;
        private readonly int _serverPort;
        private readonly string _cosignServiceName;
        private readonly X509Certificate2Collection _certificateCollection;
        private SslStream _sslStream;
        private TcpClient _client;

        /// <summary>
        ///   Validates the server certificate.
        ///   The following method is invoked by the RemoteCertificateValidationDelegate.
        /// </summary>
        /// <param name = "sender">The sender.</param>
        /// <param name = "certificate">The certificate.</param>
        /// <param name = "chain">The chain.</param>
        /// <param name = "sslPolicyErrors">The SSL policy errors.</param>
        /// <returns>If true server certificate is valid</returns>
        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            _logger.Fatal("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CosignServer" /> class.
        /// </summary>
        /// <param name = "serverIp">The server ip address.</param>
        /// <param name = "cosignConfig">The cosign config.</param>
        /// <exception cref="Exception">Unable to view the certificate store</exception>
        public CosignServer(IPAddress serverIp, CosignConfiguration cosignConfig)
        {
            ServerIpAddress = serverIp;
            _serverPort = cosignConfig.CosignServerPort;
            _serverName = cosignConfig.CosignServerName;
            _cosignServiceName = cosignConfig.ServiceName;

            // Get server certificate from the store
            try
            {
                var certificateStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                certificateStore.Open(OpenFlags.ReadOnly);
                _certificateCollection = certificateStore.Certificates.Find(X509FindType.FindBySubjectName, cosignConfig.CertCommonName, true);
            }
            catch (Exception ex)
            {
                _logger.Fatal("Unable to view the certificate store", ex);
                throw new Exception("Unable to view the certificate store", ex);
            }
        }

        /// <summary>
        ///   Establishes the connection with a cosign server.
        /// </summary>
        /// <returns>true is connection is established</returns>
        public bool EstablishConnection()
        {
            // Create a TCP/IP client socket.
            // serverIP is the host running the server application.
            var buffer = new byte[256];
            int bytes;
            string receivedData;

            try
            {
                _client = new TcpClient(ServerIpAddress.ToString(), _serverPort) { ReceiveTimeout = 1000 };
            }
            catch (Exception ex)
            {
                _logger.Error("Could not connect to the server", ex);
                return false;
            }

            try
            {
                _logger.Trace("Client connected.");
                NetworkStream stream = _client.GetStream();

                // Read in first message from cosign server
                // Need to verify here if response is valid
                bytes = stream.Read(buffer, 0, buffer.Length);
                receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);

                if (receivedData.Substring(0, 3) != "220")
                {
                    _logger.Error("Server did not return 220, returned : {0}", receivedData);
                    return false;
                }
                if (receivedData.Substring(4, 1) != "2")
                {
                    _logger.Error("Server running wrong version of cosign : {0}", receivedData);
                    return false;
                }
                _logger.Trace("Received: {0}", receivedData);

                // Initiate Secure negotiation
                _logger.Trace("Sending {0}", "STARTTLS 2\r\n");
                byte[] data = Encoding.UTF8.GetBytes("STARTTLS 2\r\n");
                stream.Write(data, 0, data.Length);

                // See if negotiation is started
                buffer = new byte[256];
                bytes = stream.Read(buffer, 0, buffer.Length);
                receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);
                _logger.Trace("Received: {0}", receivedData);
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while communicating with server", ex);
                _client.Close();
                return false;
            }

            // Create an SSL stream that will close the client's stream.
            _sslStream = new SslStream(_client.GetStream(), false, ValidateServerCertificate, null);

            // The server name must match the name on the server certificate.
            try
            {
                // Determine if we should check for cert revocation
                _sslStream.AuthenticateAsClient(_serverName, _certificateCollection, SslProtocols.Default, false);
            }
            catch (AuthenticationException e)
            {
                _logger.Error("Could not Authenticate with server - {0}", e.Message);
                _logger.Trace("Authentication failed - closing the connection.");
                _client.Close();
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while trying to authenticate secure connection with server", ex);
                _client.Close();
                return false;
            }

            // Check to see if negotiation was successful
            try
            {
                buffer = new byte[256];
                bytes = _sslStream.Read(buffer, 0, buffer.Length);
                receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);
                _logger.Trace("Received: {0}", receivedData);
                // -- Changed in Cosign 3.2.0 to code 220, not sure if bug
                if (receivedData.Substring(0, 3) != "220")
                {
                    _logger.Error("Server did not return a success in TLS negotiation - {0}", receivedData);
                    _client.Close();
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception while trying to establish secure connection with server", ex);
                _client.Close();
                return false;
            }
            return true;
        }

        /// <summary>
        ///   Closes the connection with the cosign server.
        /// </summary>
        public void CloseConnection()
        {
            try
            {
                _sslStream.Close();
                _client.Close();
            }
            catch (Exception ex)
            {
                _logger.Warn("Could not close connections to cosign server", ex);
            }
            _logger.Trace("Connections closed.");
        }

        /// <summary>
        ///   Checks the service cookie and returns a CosignCookie object.
        /// </summary>
        /// <param name = "serviceCookieValue">The service cookie value.</param>
        /// <returns>Returns the CosignCookie.</returns>
        public CosignCookie CheckCookie(string serviceCookieValue)
        {
            char[] charsToTrim = { '\r', '\n' };
            var returnValue = new CosignCookie();
            try
            {
                // Check to see if connection is valid here
                // Send check to see if cookie is valid
                _logger.Trace("Sending {0}", "CHECK " + _cosignServiceName + "=" + serviceCookieValue + "\r\n");
                byte[] data = Encoding.UTF8.GetBytes("CHECK " + _cosignServiceName + "=" + serviceCookieValue + "\r\n");
                _sslStream.Write(data, 0, data.Length);

                var buffer = new byte[256];
                int bytes = _sslStream.Read(buffer, 0, buffer.Length);
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);
                _logger.Trace("Received: {0}", receivedData);

                returnValue.ErrorMessage = receivedData;
                switch (receivedData.Substring(0, 1))
                {
                    case "2":
                        // Success
                        string[] returnData = receivedData.Split(' ');
                        returnValue.ClientIpAddress = returnData[1];
                        returnValue.UserId = returnData[2];
                        returnValue.Realm = returnData[3].Trim(charsToTrim);
                        returnValue.Factor = returnData[3].Trim(charsToTrim);
                        returnValue.ErrorCode = CosignGlobals.CosignLoggedIn;
                        break;

                    case "4":
                        // Logged out
                        returnValue.ErrorCode = CosignGlobals.CosignLoggedOut;
                        break;

                    case "5":
                        // Try a different server
                        returnValue.ErrorCode = CosignGlobals.CosignRetry;
                        break;

                    default:
                        returnValue.ErrorCode = CosignGlobals.CosignError;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Exception checking cookie", ex);
            }
            return returnValue;
        }

        /// <summary>
        ///   Checks to see if the connection is still alive
        /// </summary>
        /// <returns>Returns true if the connection is still active</returns>
        public bool CheckAlive()
        {
            try
            {
                // Check to see if connection is valid here
                // Send check to see if cookie is valid
                _logger.Trace("Sending {0}", "NOOP\r\n");
                byte[] data = Encoding.UTF8.GetBytes("NOOP \r\n");
                _sslStream.Write(data, 0, data.Length);

                var buffer = new byte[256];
                int bytes = _sslStream.Read(buffer, 0, buffer.Length);
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);
                _logger.Trace("Received: {0}", receivedData);
                switch (receivedData.Substring(0, 1))
                {
                    case "2":
                        // Success
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Warn("Server connection not alive", ex);
            }
            return false;
        }

        ////Clean up this code and do something with it useful
        ////public Byte[] RetrieveKerberosTicket(String serviceCookieValue, String cosignServiceName, String cosignKerbDbPath)
        ////{
        ////    var returnData = new byte[0];
        ////    Byte[] data = Encoding.UTF8.GetBytes("RETR " + cosignServiceName + "=" + serviceCookieValue + " tgt\r\n");
        ////    _sslStream.Write(data, 0, data.Length);
        ////    var buffer = new byte[1024];
        ////    Int32 bytes = _sslStream.Read(buffer, 0, buffer.Length);
        ////    String receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);
        ////    if (receivedData.Substring(0, 3) == "240")
        ////    {
        ////        logger.Trace("Kerberos ticket available for retrieval");
        ////        bytes = _sslStream.Read(buffer, 0, buffer.Length);
        ////        receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);
        ////        logger.Trace("Kerberos ticket size is {0} bytes", int.Parse(receivedData));
        ////        buffer = new byte[int.Parse(receivedData)];
        ////        _sslStream.Read(buffer, 0, buffer.Length);
        ////        using (var stream = new FileStream(cosignKerbDbPath + "\\" + serviceCookieValue, FileMode.Create))
        ////        {
        ////            using (var writer = new BinaryWriter(stream))
        ////            {
        ////                writer.Write(buffer);
        ////                writer.Close();
        ////            }
        ////        }
        ////        returnData = new byte[int.Parse(receivedData)];
        ////        buffer.CopyTo(returnData, 0);
        ////        bytes = _sslStream.Read(buffer, 0, buffer.Length);
        ////        receivedData = Encoding.UTF8.GetString(buffer, 0, bytes);
        ////    }
        ////    logger.Trace("Received: {0}", receivedData);
        ////    return returnData;
        ////}
    }
}