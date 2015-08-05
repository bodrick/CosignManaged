// CosignGlobals.cs
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

namespace CosignManagedAdminClient
{
    internal sealed class CosignGlobals
    {
        // Config file constants
        public const int ServiceName = 0;

        public const int CertificateCommonName = 1;
        public const int CosignServerName = 2;
        public const int CosignServerUrl = 3;
        public const int CosignServerPort = 4;
        public const int CosignCookieDbPath = 5;
        public const int CosignProtected = 6;
        public const int CosignErrorUrl = 7;
        public const int CosignUrlValidation = 8;
        public const int CosignSiteEntry = 9;
        public const int CosignCookieTimeout = 10;
        public const int CosignSecureCookies = 11;
        public const int CosignHttpOnlyCookies = 12;
        public const int CosignConnectionRetries = 18;
        public const int CosignKerberosTicketPath = 19;
        public const int CertificateCollection = 23;
    }
}