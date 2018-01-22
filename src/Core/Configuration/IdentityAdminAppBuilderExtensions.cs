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

using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Logging;
using Microsoft.Owin.StaticFiles;
using System;
using IdentityAdmin.Configuration;
using IdentityAdmin.Configuration.Hosting;
using IdentityAdmin.Logging;

namespace Owin
{
    public static class IdentityAdminAppBuilderExtensions
    {
        private readonly static ILog Logger = LogProvider.GetCurrentClassLogger();

        public static void UseIdentityAdmin(this IAppBuilder app, IdentityAdminOptions options)
        {
            if (app == null) throw new ArgumentNullException("app");
            if (options == null) throw new ArgumentNullException("config");

            app.SetLoggerFactory(new LibLogLoggerFactory());
            
            Logger.Info("Starting IdentityAdmin configuration");

            options.Validate();

            app.Use(async (ctx, next) =>
            {
                if (!ctx.Request.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && 
                    options.AdminSecurityConfiguration.RequireSsl)
                {
                    ctx.Response.Write("HTTPS required");
                }
                else
                {
                    await next();
                }
            });

            var container = AutofacConfig.Configure(options);
            app.Use<AutofacContainerMiddleware>(container);

            if (!options.DisableSecurity)
            {
                options.AdminSecurityConfiguration.Configure(app);
            }

            if (!options.DisableUserInterface)
            {
                app.UseFileServer(new FileServerOptions
                {
                    RequestPath = new PathString("/assets"),
                    FileSystem = new EmbeddedResourceFileSystem(typeof(IdentityAdminAppBuilderExtensions).Assembly, "IdentityAdmin.Assets")
                });
                app.UseFileServer(new FileServerOptions
                {
                    RequestPath = new PathString("/assets/libs/fonts"),
                    FileSystem = new EmbeddedResourceFileSystem(typeof(IdentityAdminAppBuilderExtensions).Assembly, "IdentityAdmin.Assets.Content.fonts")
                });
                app.UseStageMarker(PipelineStage.MapHandler);
            }

            SignatureConversions.AddConversions(app);
            app.UseWebApi(WebApiConfig.Configure(options));
            app.UseStageMarker(PipelineStage.MapHandler);

            // clears out the OWIN logger factory so we don't recieve other hosting related logs
            app.Properties["server.LoggerFactory"] = null;
        }
    }
}
