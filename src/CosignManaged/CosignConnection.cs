// CosignConnection.cs
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
    using System.Collections.Generic;
    using System.Net;

    /// <summary>   Cosign connection.  </summary>
    /// <remarks>   Chris Motch, 9/23/2011. </remarks>
    internal class CosignConnection
    {
        private List<CosignServer> _serverList;

        /// <summary>   Connects to the cosign server and validates the serviceCookie value. </summary>
        /// <remarks>   Chris Motch, 9/23/2011. </remarks>
        /// <exception cref="Exception">    Thrown when an exception error condition occurs. </exception>
        /// <param name="serviceCookieValue">   The service cookie value. </param>
        /// <param name="writeCookie">          if set to <c>true</c> write the cookie file to the file
        ///                                     cache. </param>
        /// <param name="cosignConfig">         The cosign config. </param>
        /// <returns>A valid cosign cookie</returns>
        public CosignCookie ConnectAndValidate(string serviceCookieValue, bool writeCookie, CosignConfiguration cosignConfig)
        {
            Logger logger = GlobalLogger.Instance.GetLogger("CosignConnection");
            CosignCookie cosignCookie = null;

            if (_serverList != null)
            {
                foreach (CosignServer server in _serverList)
                {
                    logger.Trace("Checking to see if server connection is alive for server : {0}", server.ServerIpAddress);
                    if (server.CheckAlive())
                    {
                        cosignCookie = server.CheckCookie(serviceCookieValue);
                        if (cosignCookie.ErrorCode == CosignGlobals.CosignError)
                        {
                            server.CloseConnection();
                        }
                        else if (cosignCookie.ErrorCode == CosignGlobals.CosignLoggedIn)
                        {
                            break;
                        }
                    }
                }
            }
            if (cosignCookie == null)
            {
                // Either there were no existing connections, or all the existing connections failed
                _serverList = new List<CosignServer>();
                logger.Trace("Getting ip address for cosign server {0}", cosignConfig.CosignServerName);
                IPAddress[] addresslist = Dns.GetHostAddresses(cosignConfig.CosignServerName);
                logger.Trace("Number of ip address from DNS host name : {0}", addresslist.Length);
                foreach (IPAddress theaddress in addresslist)
                {
                    logger.Trace("Initiating secure connection with cosign server - {0}", theaddress);
                    CosignServer cosignServer;
                    try
                    {
                        cosignServer = new CosignServer(theaddress, cosignConfig);
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal("Unable to to create cosign server object", ex);
                        throw new Exception("Unable to create cosign server object", ex);
                    }

                    if (cosignServer.EstablishConnection())
                    {
                        logger.Trace("Secure connection established with cosign server - {0}", theaddress);
                        logger.Trace("Checking cosign cookie {0},{1}", serviceCookieValue, cosignConfig.ServiceName);
                        _serverList.Add(cosignServer);
                        cosignCookie = cosignServer.CheckCookie(serviceCookieValue);
                        if (cosignCookie.ErrorCode == CosignGlobals.CosignLoggedIn)
                        {
                            break;
                        }
                        logger.Warn("Response from Cosign server indicated a problem - {0}", cosignCookie.ErrorMessage);
                    }
                    else
                    {
                        logger.Error("Could not initiate secure connection with cosign server - {0}", theaddress);
                    }
                }
            }

            if (cosignCookie != null && cosignCookie.ErrorCode == CosignGlobals.CosignLoggedIn)
            {
                logger.Trace("Cookie found, writing to cache - {0},{1}", writeCookie, cosignCookie.ErrorMessage);
                if (writeCookie)
                {
                    cosignCookie.SaveCookie(cosignConfig.CookieDb + "\\" + serviceCookieValue);
                    //// Do something with the kerb ticket, stick it in the return cookie or something
                    //// Byte[] tgtTicket=cosignSsl.RetrieveKerberosTicket(serviceCookieValue, cosignConfig.ServiceName,cosignConfig.KerbDBPath);
                }
            }

            // We have our cookie and its valid, lets get out of here
            return cosignCookie;
        }
    }
}