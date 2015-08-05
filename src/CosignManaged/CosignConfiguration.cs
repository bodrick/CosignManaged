// CosignConfiguration.cs
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
    using Microsoft.Web.Administration;
    using System;

    internal class CosignConfiguration
    {
        public string ServiceName { get; private set; }

        public string CertCommonName { get; private set; }

        public string CosignServerName { get; private set; }

        public string CosignServerUrl { get; private set; }

        public int CosignServerPort { get; private set; }

        public string CookieDb { get; private set; }

        public int CookieTimeOut { get; private set; }

        public int Protected { get; private set; }

        public string CosignErrorUrl { get; private set; }

        public string SiteEntryUrl { get; private set; }

        public string ValidReference { get; private set; }

        public bool SecureCookies { get; private set; }

        public bool HttpOnlyCookies { get; private set; }

        public string KerbDbPath { get; private set; }

        public int ServerRetries { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosignConfiguration"/> class.
        ///   Load the cosign configuration from the configuration files using the proper hierarchy.
        /// </summary>
        /// <exception cref="Exception">Configuration is invalid</exception>
        public CosignConfiguration()
        {
            try
            {
                ConfigurationSection config = WebConfigurationManager.GetSection("system.webServer/cosign");
                ServiceName = (string)config.GetChildElement("service").GetAttributeValue("name");
                if (string.IsNullOrEmpty(ServiceName))
                {
                    throw new Exception("Config file missing service name");
                }
                CertCommonName = (string)config.GetChildElement("crypto").GetAttributeValue("certificateCommonName");
                if (string.IsNullOrEmpty(CertCommonName))
                {
                    throw new Exception("Config file missing cert common name");
                }
                CosignServerName = (string)config.GetChildElement("webloginServer").GetAttributeValue("name");
                if (string.IsNullOrEmpty(CosignServerName))
                {
                    throw new Exception("Config file missing cosign server name");
                }
                CosignServerUrl = (string)config.GetChildElement("webloginServer").GetAttributeValue("loginUrl");
                if (string.IsNullOrEmpty(CosignServerUrl))
                {
                    throw new Exception("Config file missing cosign server url");
                }
                CosignServerPort = (int)config.GetChildElement("webloginServer").GetAttributeValue("port");
                if (CosignServerPort == 0)
                {
                    throw new Exception("Config file missing cosign server port");
                }
                CookieDb = (string)config.GetChildElement("cookieDb").GetAttributeValue("directory");
                if (string.IsNullOrEmpty(CookieDb))
                {
                    throw new Exception("Config file missing Cookie db directory");
                }
                Protected = (int)config.GetChildElement("protected").GetAttributeValue("status");
                CosignErrorUrl = (string)config.GetChildElement("validation").GetAttributeValue("errorRedirectUrl");
                if (string.IsNullOrEmpty(CosignErrorUrl))
                {
                    throw new Exception("Config file missing cosign error url");
                }
                SiteEntryUrl = (string)config.GetChildElement("siteEntry").GetAttributeValue("url");
                ValidReference = (string)config.GetChildElement("validation").GetAttributeValue("validReference");
                if (string.IsNullOrEmpty(ValidReference))
                {
                    throw new Exception("Config file missing validation reference");
                }
                CookieTimeOut = (int)config.GetChildElement("cookieDb").GetAttributeValue("expireTime");
                if (CookieTimeOut == 0)
                {
                    throw new Exception("Config file missing cookie timeout");
                }
                SecureCookies = (bool)config.GetChildElement("cookies").GetAttributeValue("secure");
                HttpOnlyCookies = (bool)config.GetChildElement("cookies").GetAttributeValue("httpOnly");
                KerbDbPath = (string)config.GetChildElement("kerberosTickets").GetAttributeValue("directory");
                ServerRetries = (int)config.GetChildElement("webloginServer").GetAttributeValue("connectionRetries");
                if (ServerRetries == 0)
                {
                    throw new Exception("Config file missing server retries");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Configuration is invalid", ex);
            }
        }
    }
}