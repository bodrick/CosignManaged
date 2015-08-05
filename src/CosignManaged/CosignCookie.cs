// CosignCookie.cs
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
    using System.IO;

    /// <summary>
    ///   The cosign cookie class is used to store the data returned from the cosign server, as well as the ability to read the cosign cookie cache file
    /// </summary>
    internal class CosignCookie
    {
        public string ClientIpAddress { get; set; }

        public DateTime TimeStamp { get; set; }

        public string UserId { get; set; }

        public string Realm { get; set; }

        public string Factor { get; set; }

        public int ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CosignCookie" /> class.
        /// </summary>
        public CosignCookie()
        {
            ErrorCode = CosignGlobals.CosignError;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CosignCookie" /> class and attempts to load the file cache for the cookie if its still valid, determined by the timeout
        /// </summary>
        /// <param name = "fileName">Filename of the cookie cache file.</param>
        /// <param name = "timeout">Cache file timeout in seconds</param>
        public CosignCookie(string fileName, double timeout)
        {
            Logger logger = GlobalLogger.Instance.GetLogger("CosignCookie");
            ErrorCode = CosignGlobals.CosignError;
            logger.Trace("Checking cookie already exists - {0}", fileName);
            try
            {
                if (File.Exists(fileName))
                {
                    TimeSpan ts = DateTime.Now - File.GetLastWriteTime(fileName);
                    logger.Trace("Cookie exists, check to see if its expired {0}", ts.TotalSeconds);
                    if (ts.TotalSeconds <= timeout && ts.TotalSeconds > 0)
                    {
                        logger.Trace("Cookie still valid, read in details from file");
                        try
                        {
                            using (var sr = new StreamReader(fileName))
                            {
                                string line;
                                while ((line = sr.ReadLine()) != null)
                                {
                                    switch (line.Substring(0, 1))
                                    {
                                        case "i":
                                            ClientIpAddress = line.Substring(1);
                                            break;

                                        case "p":
                                            UserId = line.Substring(1);
                                            break;

                                        case "r":
                                            Realm = line.Substring(1);
                                            break;

                                        case "f":
                                            Factor = line.Substring(1);
                                            break;
                                    }
                                }
                            }
                            ErrorCode = CosignGlobals.CosignLoggedIn;
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Cookie file could not be read", ex);
                        }
                    }
                    else
                    {
                        ErrorCode = CosignGlobals.CosignCookieInValid;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Cookie file could not be read", ex);
            }
        }

        public bool SaveCookie(string cookieFilePath)
        {
            Logger logger = GlobalLogger.Instance.GetLogger("CosignCookie");
            try
            {
                var file = new StreamWriter(cookieFilePath, false);
                file.WriteLine("i" + ClientIpAddress);
                file.WriteLine("p" + UserId);
                file.WriteLine("r" + Realm);
                file.WriteLine("f" + Factor);
                file.Flush();
                file.Close();
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Unable to create cookie file in cache", ex);
                return false;
            }
        }
    }
}