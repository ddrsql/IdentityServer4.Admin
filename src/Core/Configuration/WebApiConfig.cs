﻿/*
 * Copyright 2014 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using IdentityAdmin.Configuration.Hosting;

namespace IdentityAdmin.Configuration
{
    public class WebApiConfig
    {
        public static HttpConfiguration Configure(IdentityAdminOptions options)
        {
            if (options == null) throw new ArgumentNullException("idAdminConfig");

            var config = new HttpConfiguration();
            config.MessageHandlers.Insert(0, new KatanaDependencyResolver());

            config.MapHttpAttributeRoutes();
            if (!options.DisableUserInterface)
            {
                config.Routes.MapHttpRoute(Constants.RouteNames.Home,
                    "",
                    new { controller = "AdminPage", action = "Index" });
                config.Routes.MapHttpRoute(Constants.RouteNames.Logout,
                    "logout",
                    new { controller = "AdminPage", action = "Logout" });
            }

            config.SuppressDefaultHostAuthentication();
            if (!options.DisableSecurity)
            {
                config.Filters.Add(new HostAuthenticationAttribute(options.AdminSecurityConfiguration.BearerAuthenticationType));
                config.Filters.Add(new AuthorizeAttribute() { Roles = options.AdminSecurityConfiguration.AdminRoleName });
            }


            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.Remove(config.Formatters.FormUrlEncodedFormatter);
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();

            config.Services.Add(typeof(IExceptionLogger), new LogProviderExceptionLogger());

            return config;
        }
    }
}
