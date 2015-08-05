// CosignHandler.cs
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
    using System.Text.RegularExpressions;
    using System.Web;

    /// <summary>
    /// </summary>
    public class CosignHandler : IHttpHandler
    {
        private static readonly Logger CurrentLogger = GlobalLogger.Instance.GetLogger("CosignHandler");
        private static readonly CosignConnection Connection = new CosignConnection();

        /// <summary>
        ///   Gets a value indicating whether another request can use the <see cref = "T:System.Web.IHttpHandler" /> instance.
        /// </summary>
        /// <returns>true if the <see cref = "T:System.Web.IHttpHandler" /> instance is reusable; otherwise, false.
        /// </returns>
        public bool IsReusable
        {
            get { return true; }
        }

        /// <summary>
        ///   Enables processing of HTTP Web requests by a custom HttpHandler that implements the <see
        ///    cref = "T:System.Web.IHttpHandler" /> interface.
        /// </summary>
        /// <param name = "context">An <see cref = "T:System.Web.HttpContext" /> object that provides references to the intrinsic server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.</param>
        /// <exception cref="HttpException">Internal Server Error</exception>
        public void ProcessRequest(HttpContext context)
        {
            CosignConfiguration cosignConfig;
            CurrentLogger.Trace("ProcessRequest of handler started");
            try
            {
                CurrentLogger.Trace("Loading configuration");
                cosignConfig = new CosignConfiguration();
            }
            catch (Exception ex)
            {
                CurrentLogger.Fatal("Cosign Configuration Invalid", ex);
                throw new HttpException(500, "Internal Server Error", ex);
            }

            CurrentLogger.Trace("Raw URL received {0}", context.Request.RawUrl);

            if (context.Request.QueryString.Count < 2)
            {
                CurrentLogger.Error("Invalid query string passed to the handler");
                throw new HttpException(403, "Forbidden");
            }

            if (!string.IsNullOrEmpty(context.Request.QueryString[cosignConfig.ServiceName]))
            {
                string serviceCookieValue = context.Server.UrlEncode(context.Request.QueryString[cosignConfig.ServiceName]);
                string redirectUrl = context.Request.RawUrl.Substring(context.Request.RawUrl.IndexOf("&", StringComparison.Ordinal) + 1);
                CurrentLogger.Trace("ServiceCookie Value:{0}", serviceCookieValue);
                CurrentLogger.Trace("RedirectURL Value:{0}", redirectUrl);

                if (serviceCookieValue != null && serviceCookieValue.Length < 120)
                {
                    CurrentLogger.Error("Invalid Cookie Length");
                    throw new HttpException(400, "Bad Request");
                }

                if (!Regex.IsMatch(redirectUrl, cosignConfig.ValidReference))
                {
                    CurrentLogger.Error("Destination of {0} does not match {1}", redirectUrl, cosignConfig.ValidReference);
                    context.Response.Redirect(cosignConfig.CosignErrorUrl, true);
                }

                // Retries to all servers
                int retries = 0;
                bool fatalErrorOrFound = false;
                var cosignCookie = new CosignCookie();
                while (retries < cosignConfig.ServerRetries && fatalErrorOrFound == false)
                {
                    CurrentLogger.Trace("Trying to validate cookie retry - {0}", retries);
                    try
                    {
                        cosignCookie = Connection.ConnectAndValidate(serviceCookieValue, false, cosignConfig);
                    }
                    catch (Exception ex)
                    {
                        CurrentLogger.Fatal("Exception while validating the cookie", ex);
                        throw new HttpException(500, "Internal Server Error", ex);
                    }
                    switch (cosignCookie.ErrorCode)
                    {
                        case CosignGlobals.CosignLoggedOut:
                            CurrentLogger.Trace("User is already logged out, redirecting to login");
                            context.Response.Redirect(cosignConfig.CosignServerUrl + cosignConfig.ServiceName + "&" + redirectUrl, true);
                            break;

                        case CosignGlobals.CosignError:
                        case CosignGlobals.CosignLoggedIn:
                            fatalErrorOrFound = true;
                            break;
                    }
                    retries++;
                }

                if (cosignCookie.ErrorCode == CosignGlobals.CosignLoggedIn)
                {
                    CurrentLogger.Info("Client ip address, Forwarded Address - {0} , {1}", context.Request.UserHostAddress, context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]);
                    ////if (context.Request.UserHostAddress == cosignCookie.ClientIpAddress)
                    ////{
                    CurrentLogger.Trace("Cosign cookie found and validated");
                    var cookie = new HttpCookie(cosignConfig.ServiceName, serviceCookieValue) { Secure = cosignConfig.SecureCookies, HttpOnly = cosignConfig.HttpOnlyCookies };
                    CurrentLogger.Trace("Cookie has been set with the value {0}", serviceCookieValue);
                    context.Response.Cookies.Set(cookie);
                    CurrentLogger.Trace("Redirecting client to {0}", redirectUrl);
                    context.Response.Redirect(redirectUrl);
                    ////}
                    ////else
                    ////{
                    ////logger.Warn("Client ip address is different than the cookie - {0} , {1}, {2}", serviceCookieValue, context.Request.UserHostAddress, context.Request.ServerVariables["REMOTE_ADDR"]);
                    ////context.Response.Redirect(cosignConfig.CosignServerUrl + cosignConfig.ServiceName + "&" + redirectUrl, true);
                    ////}
                }
                else if (cosignCookie.ErrorCode == CosignGlobals.CosignRetry)
                {
                    CurrentLogger.Warn("Cookie was not found in db, try again to authenticate");
                    context.Response.Redirect(
                        cosignConfig.CosignServerUrl + cosignConfig.ServiceName + "&" + redirectUrl, true);
                }
                else
                {
                    CurrentLogger.Fatal("An Error has occurred that prevents cosign doing anything further - {0}", cosignCookie.ErrorMessage);
                    throw new HttpException(503, "Service Unavailable");
                }
            }
            else
            {
                CurrentLogger.Error("Bad Service Name");
            }
            CurrentLogger.Error("Bad Request, fell out the bottom");
            throw new HttpException(400, "Bad Request");
        }
    }
}