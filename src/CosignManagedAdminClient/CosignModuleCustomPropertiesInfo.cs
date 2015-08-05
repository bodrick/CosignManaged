// CosignModuleCustomPropertiesInfo.cs
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

using System.Globalization;

namespace CosignManagedAdminClient
{
    using Microsoft.Web.Management.Client;
    using Microsoft.Web.Management.Client.Win32;
    using Microsoft.Web.Management.Server;
    using System;
    using System.ComponentModel;
    using System.Drawing.Design;
    using System.IO;
    using System.Reflection;

    // Throw in more checks for values
    internal class CosignModuleCustomPropertiesInfo : PropertyGridObject
    {
        internal PropertyBag Bag { get; set; }

        internal bool IsLocalConnection { get; set; }

        public enum ProtectedStatus
        {
            Off,
            On,
            AllowPublicAccess
        }

        public CosignModuleCustomPropertiesInfo(ModulePropertiesPage page, PropertyBag bag, ConfigurationPathType scope, bool isLocalConnection)
            : base(page)
        {
            Initialize(bag, scope, isLocalConnection);
        }

        #region Cosign Server Settings

        [Category("\t\tCosign Server Settings")]
        [DisplayName("Connection Retries")]
        [Description("How many times the module should try and contact the cosign servers")]
        [DefaultValue(2)]
        [ReadOnly(false)]
        public int CosignConnectionRetries
        {
            get
            {
                object o = Bag[CosignGlobals.CosignConnectionRetries];
                if (o == null)
                {
                    return 2;
                }
                return (int)o;
            }
            set
            {
                int result;
                if (!int.TryParse(value.ToString(CultureInfo.InvariantCulture), out result))
                {
                    throw new ArgumentException("Cosign connection retries must be a number");
                }
                if (result < 1)
                {
                    throw new ArgumentException("Cosign connection retries must be greater than zero");
                }
                Bag[CosignGlobals.CosignConnectionRetries] = result;
            }
        }

        [Category("\t\tCosign Server Settings")]
        [DisplayName("\tValidation Error URL")]
        [Description("URL to redirect to if URL being returned from cosign login server does not meet validation")]
        [ReadOnly(false)]
        public string CosignErrorUrl
        {
            get
            {
                object o = Bag[CosignGlobals.CosignErrorUrl];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Validation error url cannot be blank");
                }
                Bag[CosignGlobals.CosignErrorUrl] = value;
            }
        }

        [Category("\t\tCosign Server Settings")]
        [DisplayName("\t\tServer Address")]
        [Description("The IP or DNS name of the cosign login server")]
        [ReadOnly(false)]
        public string CosignServerName
        {
            get
            {
                object o = Bag[CosignGlobals.CosignServerName];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Login server Address cannot be blank");
                }
                Bag[CosignGlobals.CosignServerName] = value;
            }
        }

        [Category("\t\tCosign Server Settings")]
        [DisplayName("\tServer login Url")]
        [Description("The url to login to the cosign server")]
        [ReadOnly(false)]
        public string CosignServerUrl
        {
            get
            {
                object o = Bag[CosignGlobals.CosignServerUrl];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("The cosign login url cannot be blank");
                }
                Bag[CosignGlobals.CosignServerUrl] = value;
            }
        }

        [Category("\t\tCosign Server Settings")]
        [DefaultValue(6663)]
        [DisplayName("\t\tServer Port")]
        [Description("The port of the cosign login server")]
        [ReadOnly(false)]
        public int CosignServerPort
        {
            get
            {
                object o = Bag[CosignGlobals.CosignServerPort];
                if (o == null)
                {
                    return 6663;
                }
                return (int)o;
            }
            set
            {
                int result;
                if (!int.TryParse(value.ToString(CultureInfo.InvariantCulture), out result))
                {
                    throw new ArgumentException("Login server port must be a number");
                }
                if (result < 1)
                {
                    throw new ArgumentException("Login server port must be greater than zero");
                }
                Bag[CosignGlobals.CosignServerPort] = result;
            }
        }

        #endregion Cosign Server Settings

        #region Local Server Settings

        [Category("\t\tLocal Server Settings")]
        [DisplayName("Cookie Timeout")]
        [Description("Cookie timeout in seconds")]
        [DefaultValue(120)]
        [ReadOnly(false)]
        public int CosignCookieTimeout
        {
            get
            {
                object o = Bag[CosignGlobals.CosignCookieTimeout];
                if (o == null)
                {
                    return 120;
                }
                return (int)o;
            }
            set
            {
                int result;
                if (!int.TryParse(value.ToString(CultureInfo.InvariantCulture), out result))
                {
                    throw new ArgumentException("Cosign cookie timeout must be a number");
                }
                if (result < 1)
                {
                    throw new ArgumentException("Cosign cookie timeout must be greater than zero");
                }
                Bag[CosignGlobals.CosignCookieTimeout] = result;
            }
        }

        [Category("\t\tLocal Server Settings")]
        [DefaultValue(true)]
        [DisplayName("Secure Cookie")]
        [Description("Determines whether the browser cookie is set to secure https only")]
        [ReadOnly(false)]
        public bool CosignSecureCookies
        {
            get
            {
                object o = Bag[CosignGlobals.CosignSecureCookies];
                if (o == null)
                {
                    return true;
                }
                return (bool)o;
            }
            set
            {
                Bag[CosignGlobals.CosignSecureCookies] = value;
            }
        }

        [Category("\t\tLocal Server Settings")]
        [DisplayName("HttpOnly Cookie")]
        [Description("Determines whether the browser cookie is set to be accessible via HTTP only")]
        [DefaultValue(true)]
        [ReadOnly(false)]
        public bool CosignHttpOnlyCookies
        {
            get
            {
                object o = Bag[CosignGlobals.CosignHttpOnlyCookies];
                if (o == null)
                {
                    return true;
                }
                return (bool)o;
            }
            set
            {
                Bag[CosignGlobals.CosignHttpOnlyCookies] = value;
            }
        }

        [Category("\t\tLocal Server Settings")]
        [DisplayName("Kerberos Ticket Path")]
        [Description("Path to temporarily store kerberos tickets received from the cosign server")]
        [ReadOnly(false)]
        [Browsable(false)]
        public string CosignKerberosTicketPath
        {
            get
            {
                object o = Bag[CosignGlobals.CosignKerberosTicketPath];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                Bag[CosignGlobals.CosignKerberosTicketPath] = value;
            }
        }

        [Category("\t\tLocal Server Settings")]
        [DisplayName("URL Validator RegEx")]
        [Description("Regular expression to match against URLs being returned from the cosign login server")]
        [ReadOnly(false)]
        public string CosignUrlValidation
        {
            get
            {
                object o = Bag[CosignGlobals.CosignUrlValidation];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("URL validation RegEx cannot be blank");
                }
                Bag[CosignGlobals.CosignUrlValidation] = value;
            }
        }

        [Category("\t\tLocal Server Settings")]
        [DisplayName("\tService Name")]
        [Description("The name of the cosign servivce assigned to the server, usually in the form of cosign-%server name%")]
        [ReadOnly(false)]
        public string ServiceName
        {
            get
            {
                object o = Bag[CosignGlobals.ServiceName];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Service name cannot be blank");
                }
                Bag[CosignGlobals.ServiceName] = value;
            }
        }

        // Could we get all the certificate names from the server
        [Category("\t\tLocal Server Settings")]
        [DisplayName("\tCertificate Common Name")]
        [Description("The name of the ssl certificate used by the IIS server")]
        [ReadOnly(false)]
        [TypeConverter(typeof(CertificateConverter))]
        public string CertificateCommonName
        {
            get
            {
                object o = Bag[CosignGlobals.CertificateCommonName];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Certificate common name cannot be blank");
                }
                Bag[CosignGlobals.CertificateCommonName] = value;
            }
        }

        [EditorAttribute(typeof(CustomFolderNameEditor), typeof(UITypeEditor))]
        [Category("\t\tLocal Server Settings")]
        [DisplayName("Cookie DB Path")]
        [Description("The local path of the directory where the cookies retrieved from the cosign server can be stored")]
        [ReadOnly(false)]
        public string CosignCookieDbPath
        {
            get
            {
                object o = Bag[CosignGlobals.CosignCookieDbPath];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                if (!Directory.Exists(value))
                {
                    throw new ArgumentException("Directory does not exist");
                }
                Bag[CosignGlobals.CosignCookieDbPath] = value;
            }
        }

        #endregion Local Server Settings

        #region Application Settings

        [Category("Application Settings")]
        [DisplayName("Protected")]
        [Description("Status to determine whether the application/site is protected by cosign")]
        [DefaultValue(ProtectedStatus.Off)]
        [ReadOnly(false)]
        public ProtectedStatus CosignProtected
        {
            get
            {
                object o = Bag[CosignGlobals.CosignProtected];
                if (o == null)
                {
                    return ProtectedStatus.Off;
                }
                return (ProtectedStatus)o;
            }
            set
            {
                Bag[CosignGlobals.CosignProtected] = (int)value;
            }
        }

        [Category("Application Settings")]
        [DisplayName("Site Entry URL")]
        [Description("If the application needs to return to a specific URL from login, this field will determine that redirect")]
        [ReadOnly(false)]
        public string CosignSiteEntry
        {
            get
            {
                object o = Bag[CosignGlobals.CosignSiteEntry];
                if (o == null)
                {
                    return string.Empty;
                }
                return (string)o;
            }
            set
            {
                Bag[CosignGlobals.CosignSiteEntry] = value;
            }
        }

        #endregion Application Settings

        internal void Initialize(PropertyBag bag, ConfigurationPathType scope, bool isLocalConnection)
        {
            Bag = bag;
            IsLocalConnection = isLocalConnection;

            if (scope != ConfigurationPathType.Server)
            {
                SetReadOnly("CosignHttpOnlyCookies", true);
                SetReadOnly("CosignSecureCookies", true);
                SetReadOnly("CosignCookieTimeout", true);
                SetReadOnly("CosignUrlValidation", true);
                SetReadOnly("CosignErrorUrl", true);
                SetReadOnly("CosignCookieDbPath", true);
                SetReadOnly("CosignServerPort", true);
                SetReadOnly("CosignServerName", true);
                SetReadOnly("CosignServerUrl", true);
                SetReadOnly("CosignConnectionRetries", true);
                SetReadOnly("CosignKerberosTicketPath", true);
            }
            else
            {
                SetReadOnly("CosignHttpOnlyCookies", false);
                SetReadOnly("CosignSecureCookies", false);
                SetReadOnly("CosignCookieTimeout", false);
                SetReadOnly("CosignUrlValidation", false);
                SetReadOnly("CosignErrorUrl", false);
                SetReadOnly("CosignCookieDbPath", false);
                SetReadOnly("CosignServerPort", false);
                SetReadOnly("CosignServerName", false);
                SetReadOnly("CosignServerUrl", false);
                SetReadOnly("CosignConnectionRetries", false);
                SetReadOnly("CosignKerberosTicketPath", false);
            }

            if (scope != ConfigurationPathType.Server && scope != ConfigurationPathType.Site)
            {
                SetReadOnly("ServiceName", true);
                SetReadOnly("CertificateCommonName", true);
            }
            else
            {
                SetReadOnly("ServiceName", false);
                SetReadOnly("CertificateCommonName", false);
            }
        }

        internal void SetReadOnly(string propertyName, bool readOnly)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())[propertyName];
            var attribute = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
            FieldInfo fieldToChange = attribute.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldToChange != null)
            {
                fieldToChange.SetValue(attribute, readOnly);
            }
        }
    }
}