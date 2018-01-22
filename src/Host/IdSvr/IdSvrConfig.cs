﻿/*
 * Copyright 2014 Dominick Baier, Brock Allen, Bert Hoorne
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
using System.Collections.Generic;
using System.Security.Claims;
using Owin;
using Thinktecture.IdentityServer.Core.Configuration;
using Thinktecture.IdentityServer.Core.Logging;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.Core.Services.InMemory;

namespace IdentityAdmin.Host.IdSvr
{
    public class IdSvrConfig
    {
        public static void Configure(IAppBuilder app)
        {
            LogProvider.SetCurrentLogProvider(new DiagnosticsTraceLogProvider());

            var factory = InMemoryFactory.Create(users:GetUsers(), scopes:GetScopes(), clients:GetClients());
            var idsrvOptions = new IdentityServerOptions
            {
                SiteName = "IdentityAdmin",
                SigningCertificate = Cert.Load(),
                Endpoints = new EndpointOptions {
                    EnableCspReportEndpoint = true
                },
                Factory = factory,
                CorsPolicy = CorsPolicy.AllowAll,
            };
            app.UseIdentityServer(idsrvOptions);
        }

        static List<InMemoryUser> GetUsers()
        {
            return new List<InMemoryUser>{
                new InMemoryUser{
                    Subject = Guid.Parse("951a965f-1f84-4360-90e4-3f6deac7b9bc").ToString(),
                    Username = "admin", 
                    Password = "admin",
                    Claims = new Claim[]{
                        new Claim(Constants.ClaimTypes.Name, "Admin"),
                        new Claim(Constants.ClaimTypes.Role, "IdentityAdminAdministrator"),
                    }
                },
                new InMemoryUser{
                    Subject = Guid.Parse("851a965f-1f84-4360-90e4-3f6deac7b9bc").ToString(),
                    Username = "alice", 
                    Password = "alice",
                    Claims = new Claim[]{
                        new Claim(Constants.ClaimTypes.Name, "Alice"),
                        new Claim(Constants.ClaimTypes.Role, "Foo"),
                    }
                }
            };
        }

        static Client[] GetClients()
        {
            return new Client[]{
                new Client{
                    ClientId = "idmAdmgr_client",
                    ClientName = "IdentityAdmin",
                    Enabled = true,
                    Flow = Flows.Implicit,
                    RequireConsent = false,
                    RedirectUris = new List<string>{
                        "https://localhost:44337",
                    },
                    PostLogoutRedirectUris = new List<string>{
                        "https://localhost:44337/idm"
                    },
                    IdentityProviderRestrictions = new List<string>(){Thinktecture.IdentityServer.Core.Constants.PrimaryAuthenticationType}
                },
            };
        }

        static Scope[] GetScopes()
        {
            return new Scope[] {
                StandardScopes.OpenId,
                 new Scope{
                    Name = "idmAdmgr",
                    DisplayName = "IdentityAdmin",
                    Description = "Authorization for IdentityAdmin",
                    Type = ScopeType.Identity,
                    Claims = new List<ScopeClaim>{
                        new ScopeClaim(Constants.ClaimTypes.Name),
                        new ScopeClaim(Constants.ClaimTypes.Role)
                    }
                },
            };
        }
    }
}