// CosignModule.cs
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

using System.Security.Claims;
using System.Threading;

namespace CosignManaged
{
    using NLog;
    using System;
    using System.Net;
    using System.Web;

    public class CosignModule : IHttpModule
    {
        private static Logger logger = GlobalLogger.Instance.GetLogger("CosignModule");
        private static CosignConfiguration _cosignConfig;
        private static CosignConnection _cosignConnection;

        /// <summary>
        ///   Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name = "context">An <see cref = "T:System.Web.HttpApplication" /> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application</param>
        public void Init(HttpApplication context)
        {
            logger.Trace("Cosign Module loaded");
            context.AuthenticateRequest += ContextAuthenticateRequest;
            _cosignConnection = new CosignConnection();
        }

        /// <summary>
        ///   Handler for the authenticate request
        /// </summary>
        /// <param name = "sender">The sender.</param>
        /// <param name = "e">The <see cref = "System.EventArgs" /> instance containing the event data.</param>
        public void ContextAuthenticateRequest(object sender, EventArgs e)
        {
            logger.Trace("AuthenticateRequest started");
            var application = (HttpApplication)sender;
            HttpContext context = application.Context;

            try
            {
                logger.Trace("Loading configuration");
                _cosignConfig = new CosignConfiguration();
            }
            catch (Exception ex)
            {
                logger.Fatal("Cosign Configuration Invalid", ex);
                throw new HttpException(500, "Internal Server Error", ex);
            }

            var redirectURL = CalculateSiteEntryURL(context);
            logger.Trace("Getting service cookie from request");
            HttpCookie serviceCookie = context.Request.Cookies[_cosignConfig.ServiceName];
            if (_cosignConfig.Protected != 0 && serviceCookie != null)
            {
                logger.Trace("Site is set as protected/allowPublic and there is a service cookie");
                string cookieFilePath = _cosignConfig.CookieDb + "\\" + serviceCookie.Value;
                logger.Trace("Check cached cookie");
                var cosignCookie = new CosignCookie(cookieFilePath, _cosignConfig.CookieTimeOut);
                if (cosignCookie.ErrorCode != CosignGlobals.CosignLoggedIn)
                {
                    logger.Trace("Cached cookie not found or not valid");

                    int retries = 0;
                    bool fatalErrorOrFound = false;
                    while (retries < _cosignConfig.ServerRetries && fatalErrorOrFound == false)
                    {
                        logger.Trace("Trying to validate cookie retry - {0}", retries);
                        try
                        {
                            cosignCookie = _cosignConnection.ConnectAndValidate(serviceCookie.Value, true, _cosignConfig);
                        }
                        catch (Exception ex)
                        {
                            logger.Fatal("Exception while validating the cookie", ex);
                            cosignCookie.ErrorCode = CosignGlobals.CosignError;
                        }

                        switch (cosignCookie.ErrorCode)
                        {
                            case CosignGlobals.CosignLoggedOut:
                                logger.Trace("User is already logged out, redirecting to login");
                                if (CheckCosignServer(_cosignConfig.CosignServerUrl))
                                {
                                    context.Response.Redirect(_cosignConfig.CosignServerUrl + _cosignConfig.ServiceName + "&" + redirectURL, true);
                                }
                                else
                                {
                                    fatalErrorOrFound = true;
                                }
                                break;

                            case CosignGlobals.CosignError:
                            case CosignGlobals.CosignLoggedIn:
                                fatalErrorOrFound = true;
                                break;
                        }
                        retries++;
                    }
                }

                if (cosignCookie.ErrorCode == CosignGlobals.CosignLoggedIn)
                {
                    ////Need to figure out how to deal with proxies ip addresses

                    ////logger.Info("Client ip address, Forwarded Address - {0} , {1}", context.Request.UserHostAddress, context.Request.ServerVariables["X_FORWARDED_FOR"]);
                    ////if (context.Request.UserHostAddress == cosignCookie.ClientIpAddress)
                    ////{
                    //var principalIdentity = new ClaimsIdentity("cosign", ClaimTypes.Name, ClaimTypes.Role);
                    //principalIdentity.AddClaim(new Claim(ClaimTypes.Name, cosignCookie.UserId));
                    //principalIdentity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, "cosign"));
                    //var principal = new ClaimsPrincipal(principalIdentity);
                    //Thread.CurrentPrincipal = principal;
                    //context.User = principal;
                    //foreach (var claim in principal.Claims)
                    //{
                    //    logger.Trace("Claim Type : {0}, Value : {1}", claim.Type, claim.Value);
                    //}
                    application.Request.ServerVariables.Set("HTTP_COSIGN_FACTOR", cosignCookie.Factor);
                    application.Request.ServerVariables.Set("HTTP_REMOTE_USER", cosignCookie.UserId);
                    application.Request.ServerVariables.Set("HTTP_REMOTE_REALM", cosignCookie.Realm);
                    application.Request.ServerVariables.Set("HTTP_COSIGN_SERVICE", _cosignConfig.ServiceName);
                    logger.Trace("Cosign cookie found and validated");
                    ////}
                    ////else
                    ////{
                    ////logger.Warn("Client ip address is different than the cookie - {0} , {1}, {2}", serviceCookie.Value, context.Request.UserHostAddress, context.Request.ServerVariables["REMOTE_ADDR"]);

                    ////What do we do here, seems silly to do a server unavailable, redirect to cosign server and try again
                    ////if (cosignConfig.Protected == 1)
                    ////context.Response.Redirect(cosignConfig.CosignServerUrl + cosignConfig.ServiceName + "&" + context.Request.Url.AbsoluteUri,true);
                    ////}
                }
                else if (_cosignConfig.Protected == 1)
                {
                    // Retry means that cosign must be working otherwise would not get a retry
                    if (cosignCookie.ErrorCode == CosignGlobals.CosignRetry)
                    {
                        context.Response.Redirect(_cosignConfig.CosignServerUrl + _cosignConfig.ServiceName + "&" + redirectURL, true);
                    }
                    logger.Fatal("An Error has occurred that prevents cosign doing anything further - {0}", cosignCookie.ErrorMessage);
                    ServiceUnavailable();
                }
            }
            else if (_cosignConfig.Protected == 1 && serviceCookie == null)
            {
                if (CheckCosignServer(_cosignConfig.CosignServerUrl))
                {
                    logger.Trace("Service Cookie not found, redirecting to webaccess server - {0}, {1}, {2}", _cosignConfig.CosignServerUrl, _cosignConfig.ServiceName, redirectURL);
                    context.Response.Redirect(_cosignConfig.CosignServerUrl + _cosignConfig.ServiceName + "&" + redirectURL, true);
                }
                else
                {
                    logger.Fatal("An Error has occurred that prevents cosign doing anything further");
                    ServiceUnavailable();
                }
            }
        }

        private string CalculateSiteEntryURL(HttpContext context)
        {
            var returnValue = context.Request.Url.AbsoluteUri;
            if (!string.IsNullOrEmpty(_cosignConfig.SiteEntryUrl))
            {
                //Parse Site Entry URL parameters
                Uri currentUrl = new Uri(context.Request.Url.AbsoluteUri);
                var parameters = HttpUtility.ParseQueryString(currentUrl.Query);
                var newUrl = _cosignConfig.SiteEntryUrl;
                var newQuery = "";
                foreach (var param in parameters.AllKeys)
                {
                    newQuery += (newQuery.IndexOf('?') == -1 ? "?" : "&") + param + "=" + parameters[param];
                }
                returnValue = newUrl.Replace("{query}", newQuery);
                logger.Trace("RequestURL - {0}, Site Entry URL - Orig {1}, New {2}", context.Request.Url.AbsoluteUri, _cosignConfig.SiteEntryUrl, newUrl);
            }
            return returnValue;
        }

        /// <summary>
        ///   Returns a 503 error that the service is unavailable, should only get here if nothing else can be done
        /// </summary>
        private void ServiceUnavailable()
        {
            HttpContext context = HttpContext.Current;
            context.Response.StatusCode = 503;
            context.Response.StatusDescription = "Service Unavailable";
            context.Response.End();
        }

        /// <summary>
        ///   Disposes of the resources (other than memory) used by the module that implements <see cref = "T:System.Web.IHttpModule" />.
        /// </summary>
        public void Dispose()
        {
            logger.Trace("Module destroyed");
        }

        /// <summary>
        ///   Checks the cosign server login page to see if its available.
        /// </summary>
        /// <param name = "url">URL to check</param>
        /// <returns>True if url is valid and can be reached</returns>
        public static bool CheckCosignServer(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.Warn("Cosign Server is not responding");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Warn("Cosign Server is not responding", ex);
                return false;
            }
        }
    }
}