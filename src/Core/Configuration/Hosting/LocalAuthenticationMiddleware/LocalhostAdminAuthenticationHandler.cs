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

using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityAdmin.Resources;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using System.Configuration;

namespace IdentityAdmin.Configuration.Hosting.LocalAuthenticationMiddleware
{
    public class LocalhostAdminAuthenticationHandler : AuthenticationHandler<LocalhostAdminAuthenticationOptions>
    {
        protected override Task<Microsoft.Owin.Security.AuthenticationTicket> AuthenticateCoreAsync()
        {
            var ctx = this.Context;
            string ips = ConfigurationManager.AppSettings["ipAddr"];
            var ipsArr = ips.Split('|').Concat(new string[] { "127.0.0.1", "::1", ctx.Request.LocalIpAddress });
            //var localAddresses = new string[] { "127.0.0.1", "::1", ctx.Request.LocalIpAddress };
            if (ipsArr.Contains(ctx.Request.RemoteIpAddress))
            {
                var id = new ClaimsIdentity(this.Options.Configuration.HostAuthenticationType, this.Options.Configuration.NameClaimType, this.Options.Configuration.RoleClaimType);
                id.AddClaim(new Claim(this.Options.Configuration.NameClaimType, Messages.LocalUsername));
                id.AddClaim(new Claim(this.Options.Configuration.RoleClaimType, this.Options.Configuration.AdminRoleName));

                var ticket = new AuthenticationTicket(id, new AuthenticationProperties());
                return Task.FromResult(ticket);
            }

            return Task.FromResult<AuthenticationTicket>(null);
        }
    }
}
