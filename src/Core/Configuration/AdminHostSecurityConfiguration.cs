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
using System.Security.Cryptography.X509Certificates;
using IdentityAdmin.Configuration.Hosting;
using Microsoft.Owin;
using Microsoft.Owin.Security.DataHandler;
using Owin;

namespace IdentityAdmin.Configuration
{
    public class AdminHostSecurityConfiguration : AdminSecurityConfiguration
    {
        public X509Certificate2 TokenDataProtectorCertificate { get; set;  }
        public string HostAuthenticationType { get; set; }
        public string AdditionalSignOutType { get; set; }
        public TimeSpan TokenExpiration { get; set; }

        public AdminHostSecurityConfiguration()
        {
            TokenExpiration = Constants.DefaultTokenExpiration;
        }

        internal override void Validate()
        {
            base.Validate();

            if (String.IsNullOrWhiteSpace(HostAuthenticationType)) throw new Exception("HostAuthenticationType is required.");
        }

        public override void Configure(IAppBuilder app)
        {
            app.UseOAuthAuthorizationServer(this);
        }

        internal override void SignOut(IOwinContext context)
        {
            context.Authentication.SignOut(this.HostAuthenticationType);
            if (!String.IsNullOrWhiteSpace(AdditionalSignOutType))
            {
                context.Authentication.SignOut(AdditionalSignOutType);
            }
        }
    }
}
