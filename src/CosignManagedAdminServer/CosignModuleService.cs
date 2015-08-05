// CosignModuleService.cs
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

namespace CosignManagedAdminServer
{
    using Microsoft.Web.Administration;
    using Microsoft.Web.Management.Server;
    using System;
    using System.Security.Cryptography.X509Certificates;

    internal class CosignModuleService : ModuleService
    {
        /// <summary>
        /// Gets the app settings from the correct configuration file
        /// </summary>
        /// <returns>The app settings</returns>
        [ModuleServiceMethod(PassThrough = true)]
        public PropertyBag GetAppSettings()
        {
            var bag = new PropertyBag();
            ConfigurationSection section = ManagementUnit.Configuration.GetSection("system.webServer/cosign");
            bag[CosignGlobals.ServiceName] = section.GetChildElement("service").GetAttributeValue("name");
            bag[CosignGlobals.CertificateCommonName] = section.GetChildElement("crypto").GetAttributeValue("certificateCommonName");
            bag[CosignGlobals.CosignServerName] = section.GetChildElement("webloginServer").GetAttributeValue("name");
            bag[CosignGlobals.CosignServerUrl] = section.GetChildElement("webloginServer").GetAttributeValue("loginUrl");
            bag[CosignGlobals.CosignServerPort] = section.GetChildElement("webloginServer").GetAttributeValue("port");
            bag[CosignGlobals.CosignCookieDbPath] = section.GetChildElement("cookieDb").GetAttributeValue("directory");
            bag[CosignGlobals.CosignProtected] = section.GetChildElement("protected").GetAttributeValue("status");
            bag[CosignGlobals.CosignErrorUrl] = section.GetChildElement("validation").GetAttributeValue("errorRedirectUrl");
            bag[CosignGlobals.CosignUrlValidation] = section.GetChildElement("validation").GetAttributeValue("validReference");
            bag[CosignGlobals.CosignSiteEntry] = section.GetChildElement("siteEntry").GetAttributeValue("url");
            bag[CosignGlobals.CosignCookieTimeout] = section.GetChildElement("cookieDb").GetAttributeValue("expireTime");
            bag[CosignGlobals.CosignSecureCookies] = section.GetChildElement("cookies").GetAttributeValue("secure");
            bag[CosignGlobals.CosignHttpOnlyCookies] = section.GetChildElement("cookies").GetAttributeValue("httpOnly");
            bag[CosignGlobals.CosignKerberosTicketPath] = section.GetChildElement("kerberosTickets").GetAttributeValue("directory");
            bag[CosignGlobals.CosignConnectionRetries] = section.GetChildElement("webloginServer").GetAttributeValue("connectionRetries");

            var certificateStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            certificateStore.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificateCollection =
                certificateStore.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, true);
            var certs = new string[certificateCollection.Count];
            int count = 0;
            foreach (X509Certificate2 x509 in certificateCollection)
            {
                certs[count] = x509.GetNameInfo(X509NameType.DnsName, false);
                count++;
            }
            bag[CosignGlobals.CertificateCollection] = certs;
            return bag;
        }

        [ModuleServiceMethod(PassThrough = true)]
        public void UpdateAppSettings(PropertyBag updatedSettings)
        {
            if (updatedSettings == null)
            {
                throw new ArgumentNullException("updatedSettings");
            }
            ConfigurationSection section = ManagementUnit.Configuration.GetSection("system.webServer/cosign");

            foreach (int key in updatedSettings.ModifiedKeys)
            {
                switch (key)
                {
                    case CosignGlobals.ServiceName:
                        section.GetChildElement("service").SetAttributeValue("name", updatedSettings[CosignGlobals.ServiceName]);
                        break;

                    case CosignGlobals.CertificateCommonName:
                        section.GetChildElement("crypto").SetAttributeValue("certificateCommonName", updatedSettings[CosignGlobals.CertificateCommonName]);
                        break;

                    case CosignGlobals.CosignServerName:
                        section.GetChildElement("webloginServer").SetAttributeValue("name", updatedSettings[CosignGlobals.CosignServerName]);
                        break;

                    case CosignGlobals.CosignServerUrl:
                        section.GetChildElement("webloginServer").SetAttributeValue("loginUrl", updatedSettings[CosignGlobals.CosignServerUrl]);
                        break;

                    case CosignGlobals.CosignServerPort:
                        section.GetChildElement("webloginServer").SetAttributeValue("port", updatedSettings[CosignGlobals.CosignServerPort]);
                        break;

                    case CosignGlobals.CosignCookieDbPath:
                        section.GetChildElement("cookieDb").SetAttributeValue("directory", updatedSettings[CosignGlobals.CosignCookieDbPath]);

                        // Add code to set the permissions
                        break;

                    case CosignGlobals.CosignProtected:
                        section.GetChildElement("protected").SetAttributeValue("status", updatedSettings[CosignGlobals.CosignProtected]);
                        break;

                    case CosignGlobals.CosignErrorUrl:
                        section.GetChildElement("validation").SetAttributeValue("errorRedirectUrl", updatedSettings[CosignGlobals.CosignErrorUrl]);
                        break;

                    case CosignGlobals.CosignUrlValidation:
                        section.GetChildElement("validation").SetAttributeValue("validReference", updatedSettings[CosignGlobals.CosignUrlValidation]);
                        break;

                    case CosignGlobals.CosignSiteEntry:
                        section.GetChildElement("siteEntry").SetAttributeValue("url", updatedSettings[CosignGlobals.CosignSiteEntry]);
                        break;

                    case CosignGlobals.CosignCookieTimeout:
                        section.GetChildElement("cookieDb").SetAttributeValue("expireTime", updatedSettings[CosignGlobals.CosignCookieTimeout]);
                        break;

                    case CosignGlobals.CosignSecureCookies:
                        section.GetChildElement("cookies").SetAttributeValue("secure", updatedSettings[CosignGlobals.CosignSecureCookies]);
                        break;

                    case CosignGlobals.CosignHttpOnlyCookies:
                        section.GetChildElement("cookies").SetAttributeValue("httpOnly", updatedSettings[CosignGlobals.CosignHttpOnlyCookies]);
                        break;

                    case CosignGlobals.CosignKerberosTicketPath:
                        section.GetChildElement("kerberosTickets").SetAttributeValue("directory", updatedSettings[CosignGlobals.CosignKerberosTicketPath]);

                        // Add code to set the permissions
                        break;

                    case CosignGlobals.CosignConnectionRetries:
                        section.GetChildElement("webloginServer").SetAttributeValue("connectionRetries", updatedSettings[CosignGlobals.CosignConnectionRetries]);
                        break;
                }
            }
            ManagementUnit.Update();
        }
    }
}