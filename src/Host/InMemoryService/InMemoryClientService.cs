/*
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
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using IdentityAdmin.Core;
using IdentityAdmin.Core.Client;
using IdentityAdmin.Core.Metadata;
using IdentityAdmin.Extensions;
using Thinktecture.IdentityServer.Core.Models;
using IdentityServer4.EntityFramework;
using System.Data.Entity;
using System.Configuration;

namespace IdentityAdmin.Host.InMemoryService
{
    internal class InMemoryClientService : IClientService
    {
        private static string connection = ConfigurationManager.ConnectionStrings["IdSvr4ConfigAdmin"].ConnectionString;
        private ICollection<InMemoryClient> _clients;        
        public static MapperConfiguration Config;
        public static IMapper Mapper;
        public InMemoryClientService(ICollection<InMemoryClient> clients)
        {
            this._clients = clients;
            Config = new MapperConfiguration(cfg => {
                cfg.CreateMap<InMemoryClientClaim, ClientClaimValue>();//
                cfg.CreateMap<ClientClaimValue, InMemoryClientClaim>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientClaim, ClientClaimValue>();
                cfg.CreateMap<ClientClaimValue, IdentityServer4.EntityFramework.Entities.ClientClaim>();

                cfg.CreateMap<InMemoryClientSecret, ClientSecretValue>();//
                cfg.CreateMap<ClientSecretValue, InMemoryClientSecret>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientSecret, ClientSecretValue>();
                cfg.CreateMap<ClientSecretValue, IdentityServer4.EntityFramework.Entities.ClientSecret>();

                cfg.CreateMap<InMemoryClientIdPRestriction, ClientIdPRestrictionValue>();//
                cfg.CreateMap<ClientIdPRestrictionValue, InMemoryClientIdPRestriction>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientIdPRestriction, ClientIdPRestrictionValue>();
                cfg.CreateMap<ClientIdPRestrictionValue, IdentityServer4.EntityFramework.Entities.ClientIdPRestriction>();

                cfg.CreateMap<InMemoryClientPostLogoutRedirectUri, ClientPostLogoutRedirectUriValue>();//
                cfg.CreateMap<ClientPostLogoutRedirectUriValue, InMemoryClientPostLogoutRedirectUri>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientPostLogoutRedirectUri, ClientPostLogoutRedirectUriValue>();
                cfg.CreateMap<ClientPostLogoutRedirectUriValue, IdentityServer4.EntityFramework.Entities.ClientPostLogoutRedirectUri>();

                cfg.CreateMap<InMemoryClientRedirectUri, ClientRedirectUriValue>();//
                cfg.CreateMap<ClientRedirectUriValue, InMemoryClientRedirectUri>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientRedirectUri, ClientRedirectUriValue>();
                cfg.CreateMap<ClientRedirectUriValue, IdentityServer4.EntityFramework.Entities.ClientRedirectUri>();

                cfg.CreateMap<InMemoryClientCorsOrigin, ClientCorsOriginValue>();//
                cfg.CreateMap<ClientCorsOriginValue, InMemoryClientCorsOrigin>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientCorsOrigin, ClientCorsOriginValue>();
                cfg.CreateMap<ClientCorsOriginValue, IdentityServer4.EntityFramework.Entities.ClientCorsOrigin>();

                cfg.CreateMap<InMemoryClientCustomGrantType, ClientCustomGrantTypeValue>();//
                cfg.CreateMap<ClientCustomGrantTypeValue, InMemoryClientCustomGrantType>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientGrantType, ClientCustomGrantTypeValue>();
                cfg.CreateMap<ClientCustomGrantTypeValue, IdentityServer4.EntityFramework.Entities.ClientGrantType>();

                cfg.CreateMap<InMemoryClientScope, ClientScopeValue>();//
                cfg.CreateMap<ClientScopeValue, InMemoryClientScope>();//
                cfg.CreateMap<IdentityServer4.EntityFramework.Entities.ClientScope, ClientScopeValue>();
                cfg.CreateMap<ClientScopeValue, IdentityServer4.EntityFramework.Entities.ClientScope>();
            });
            Mapper = Config.CreateMapper();
        }


        private ClientMetaData _metadata;

        private ClientMetaData GetMetadata()
        {
            if (_metadata == null)
            {
                var updateClient = new List<PropertyMetadata>();
                updateClient.AddRange(PropertyMetadata.FromType<IdentityServer4.EntityFramework.Entities.Client>());

                var createClient = new List<PropertyMetadata>
                {
                    PropertyMetadata.FromProperty<IdentityServer4.EntityFramework.Entities.Client>(x => x.ClientName, "ClientName", required: true),
                    PropertyMetadata.FromProperty<IdentityServer4.EntityFramework.Entities.Client>(x => x.ClientId, "ClientId", required: true),
                };

                _metadata = new ClientMetaData
                {
                    SupportsCreate = true,
                    SupportsDelete = true,
                    CreateProperties = createClient,
                    UpdateProperties = updateClient
                };
            }
            return _metadata;
        }

        #region Clients

        public Task<IdentityAdminResult<ClientDetail>> GetClientAsync(string subject)
        {
            using (var db = new ConfigurationDbContext(connection))
            {
                int parsedId;
                if (int.TryParse(subject, out parsedId))
                {
                    var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedId);
                    if (inMemoryClient == null)
                    {
                        return Task.FromResult(new IdentityAdminResult<ClientDetail>((ClientDetail)null));
                    }

                    var result = new ClientDetail
                    {
                        Subject = subject,
                        ClientId = inMemoryClient.ClientId,
                        ClientName = inMemoryClient.ClientName,
                    };
                    result.AllowedCorsOrigins = new List<ClientCorsOriginValue>();
                    Mapper.Map(inMemoryClient.AllowedCorsOrigins.ToList(), result.AllowedCorsOrigins);
                    result.AllowedCustomGrantTypes = new List<ClientCustomGrantTypeValue>();
                    Mapper.Map(inMemoryClient.AllowedGrantTypes.ToList(), result.AllowedCustomGrantTypes);
                    result.AllowedScopes = new List<ClientScopeValue>();
                    Mapper.Map(inMemoryClient.AllowedScopes.ToList(), result.AllowedScopes);
                    result.Claims = new List<ClientClaimValue>();
                    Mapper.Map(inMemoryClient.Claims.ToList(), result.Claims);
                    result.ClientSecrets = new List<ClientSecretValue>();
                    Mapper.Map(inMemoryClient.ClientSecrets.ToList(), result.ClientSecrets);
                    result.IdentityProviderRestrictions = new List<ClientIdPRestrictionValue>();
                    Mapper.Map(inMemoryClient.IdentityProviderRestrictions.ToList(), result.IdentityProviderRestrictions);
                    result.PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUriValue>();
                    Mapper.Map(inMemoryClient.PostLogoutRedirectUris.ToList(), result.PostLogoutRedirectUris);
                    result.RedirectUris = new List<ClientRedirectUriValue>();
                    Mapper.Map(inMemoryClient.RedirectUris.ToList(), result.RedirectUris);

                    var metadata = GetMetadata();
                    var props = from prop in metadata.UpdateProperties
                                select new PropertyValue
                                {
                                    Type = prop.Type,
                                    Value = GetClientProperty(prop, inMemoryClient),
                                };

                    result.Properties = props.ToArray();
                    return Task.FromResult(new IdentityAdminResult<ClientDetail>(result));
                }
                return Task.FromResult(new IdentityAdminResult<ClientDetail>((ClientDetail)null));
            }
        }

        public Task<IdentityAdminResult> DeleteClientAsync(string subject)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var client = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (client == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        db.Clients.Remove(client);
                        db.SaveChanges();
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }

            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult<QueryResult<ClientSummary>>> QueryClientsAsync(string filter, int start, int count)
        {
            using (var db = new ConfigurationDbContext(connection))
            {
                var query =
                from client in db.Clients
                orderby client.ClientName
                select client;

                if (!String.IsNullOrWhiteSpace(filter))
                {
                    query =
                        from client in query
                        where client.ClientName.Contains(filter)
                        orderby client.ClientName
                        select client;
                }

                int total = query.Count();
                var clients = query.Skip(start).Take(count).ToArray();

                var result = new QueryResult<ClientSummary>();
                result.Start = start;
                result.Count = count;
                result.Total = total;
                result.Filter = filter;
                result.Items = clients.Select(x =>
                {
                    var client = new ClientSummary
                    {
                        Subject = x.Id.ToString(),
                        ClientName = x.ClientName,
                        ClientId = x.ClientId
                    };

                    return client;
                }).ToArray();

                return Task.FromResult(new IdentityAdminResult<QueryResult<ClientSummary>>(result));
            }
        }

        public Task<IdentityAdminResult<CreateResult>> CreateClientAsync(IEnumerable<PropertyValue> properties)
        {
            var clientNameClaim = properties.Single(x => x.Type == "ClientName");
            var clientIdClaim = properties.Single(x => x.Type == "ClientId");

            var clientId = clientNameClaim.Value;
            var clientName = clientIdClaim.Value;

            string[] exclude = new string[] {"ClientName", "ClientId"};
            var otherProperties = properties.Where(x => !exclude.Contains(x.Type)).ToArray();

            var metadata = GetMetadata();
            var createProps = metadata.CreateProperties;
            var client  = new Client();
            var inMemoryClient = new IdentityServer4.EntityFramework.Entities.Client
            {
                ClientId = clientId,
                ClientName = clientName,
                //Id = _clients.Count + 1,
                AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
                AccessTokenLifetime = client.AccessTokenLifetime,
                IdentityTokenLifetime = client.IdentityTokenLifetime,
                SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
                Enabled =  true,
                EnableLocalLogin =  true,
            };

            foreach (var prop in otherProperties)
            {
                var propertyResult = SetClientProperty(createProps, inMemoryClient, prop.Type, prop.Value);
                if (!propertyResult.IsSuccess)
                {
                    return Task.FromResult(new IdentityAdminResult<CreateResult>(propertyResult.Errors.ToArray()));
                }
            }

            using (var db = new ConfigurationDbContext(connection))
            {
                try
                {
                    db.Clients.Add(inMemoryClient);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new IdentityAdminResult<CreateResult>(ex.Message));
                }
            }
            return
                Task.FromResult(
                    new IdentityAdminResult<CreateResult>(new CreateResult {Subject = inMemoryClient.Id.ToString()}));
        }

        public Task<IdentityAdminResult> SetClientPropertyAsync(string subject, string type, string value)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var meta = GetMetadata();

                        SetClientProperty(meta.UpdateProperties, inMemoryClient, type, value);

                        db.SaveChanges();
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        #region Client claim

        public Task<IdentityAdminResult> AddClientClaimAsync(string subject, string type, string value)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingClaims = inMemoryClient.Claims;
                        if (!existingClaims.Any(x => x.Type == type && x.Value == value))
                        {
                            inMemoryClient.Claims.Add(new IdentityServer4.EntityFramework.Entities.ClientClaim()
                            {
                                Type = type,
                                Value = value
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveClientClaimAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedClientId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedClientId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var client = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (client == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingClaim = client.Claims.FirstOrDefault(p => p.Id == parsedClientId);
                        if (existingClaim != null)
                        {
                            db.Entry(existingClaim).State = EntityState.Deleted;
                            client.Claims.Remove(existingClaim);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        #endregion

        #region Client Secret

        public Task<IdentityAdminResult> AddClientSecretAsync(string subject, string type, string value)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingSecrets = inMemoryClient.ClientSecrets;
                        if (!existingSecrets.Any(x => x.Type == type && x.Value == value))
                        {
                            inMemoryClient.ClientSecrets.Add(new IdentityServer4.EntityFramework.Entities.ClientSecret
                            {
                                Type = type,
                                Value = value
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveClientSecretAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedObjectId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedObjectId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingClientSecret = inMemoryClient.ClientSecrets.FirstOrDefault(p => p.Id == parsedObjectId);
                        if (existingClientSecret != null)
                        {
                            db.Entry(existingClientSecret).State = EntityState.Deleted;
                            inMemoryClient.ClientSecrets.Remove(existingClientSecret);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject or secretId"));
        }

        #endregion

        #region ClientIdPRestriction

        public Task<IdentityAdminResult> AddClientIdPRestrictionAsync(string subject, string provider)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingIdentityProviderRestrictions = inMemoryClient.IdentityProviderRestrictions;
                        if (existingIdentityProviderRestrictions.All(x => x.Provider != provider))
                        {
                            inMemoryClient.IdentityProviderRestrictions.Add(new IdentityServer4.EntityFramework.Entities.ClientIdPRestriction
                            {
                                Provider = provider,
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveClientIdPRestrictionAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedObjectId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedObjectId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingIdentityProviderRestrictions =
                            inMemoryClient.IdentityProviderRestrictions.FirstOrDefault(p => p.Id == parsedObjectId);
                        if (existingIdentityProviderRestrictions != null)
                        {
                            db.Entry(existingIdentityProviderRestrictions).State = EntityState.Deleted;
                            inMemoryClient.IdentityProviderRestrictions.Remove(existingIdentityProviderRestrictions);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject or secretId"));
        }

        #endregion

        #region PostLogoutRedirectUri

        public Task<IdentityAdminResult> AddPostLogoutRedirectUriAsync(string subject, string uri)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var client = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (client == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingPostLogoutRedirectUris = client.PostLogoutRedirectUris;
                        if (existingPostLogoutRedirectUris.All(x => x.PostLogoutRedirectUri != uri))
                        {
                            client.PostLogoutRedirectUris.Add(new IdentityServer4.EntityFramework.Entities.ClientPostLogoutRedirectUri
                            {
                                PostLogoutRedirectUri = uri,
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemovePostLogoutRedirectUriAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedObjectId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedObjectId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }

                        var existingPostLogoutRedirectUris =
                            inMemoryClient.PostLogoutRedirectUris.FirstOrDefault(p => p.Id == parsedObjectId);
                        if (existingPostLogoutRedirectUris != null)
                        {
                            db.Entry(existingPostLogoutRedirectUris).State = EntityState.Deleted;
                            inMemoryClient.PostLogoutRedirectUris.Remove(existingPostLogoutRedirectUris);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject or secretId"));
        }

        #endregion

        #region ClientRedirectUri

        public Task<IdentityAdminResult> AddClientRedirectUriAsync(string subject, string uri)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var client = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (client == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingRedirectUris = client.RedirectUris;
                        if (existingRedirectUris.All(x => x.RedirectUri != uri))
                        {
                            client.RedirectUris.Add(new IdentityServer4.EntityFramework.Entities.ClientRedirectUri
                            {
                                RedirectUri = uri,
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveClientRedirectUriAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedObjectId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedObjectId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingRedirectUris = inMemoryClient.RedirectUris.FirstOrDefault(p => p.Id == parsedObjectId);
                        if (existingRedirectUris != null)
                        {
                            db.Entry(existingRedirectUris).State = EntityState.Deleted;
                            inMemoryClient.RedirectUris.Remove(existingRedirectUris);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject or secretId"));
        }

        #endregion

        #region ClientCorsOrigin

        public Task<IdentityAdminResult> AddClientCorsOriginAsync(string subject, string origin)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingCorsOrigins = inMemoryClient.AllowedCorsOrigins;
                        if (existingCorsOrigins.All(x => x.Origin != origin))
                        {
                            inMemoryClient.AllowedCorsOrigins.Add(new IdentityServer4.EntityFramework.Entities.ClientCorsOrigin
                            {
                                Origin = origin,
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveClientCorsOriginAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedObjectId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedObjectId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingCorsOrigins = inMemoryClient.AllowedCorsOrigins.FirstOrDefault(p => p.Id == parsedObjectId);
                        if (existingCorsOrigins != null)
                        {
                            db.Entry(existingCorsOrigins).State = EntityState.Deleted;
                            inMemoryClient.AllowedCorsOrigins.Remove(existingCorsOrigins);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject or secretId"));
        }

        #endregion

        #region ClientCustomGrantType

        public Task<IdentityAdminResult> AddClientCustomGrantTypeAsync(string subject, string grantType)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingGrantTypes = inMemoryClient.AllowedGrantTypes;
                        if (existingGrantTypes.All(x => x.GrantType != grantType))
                        {
                            inMemoryClient.AllowedGrantTypes.Add(new IdentityServer4.EntityFramework.Entities.ClientGrantType
                            {
                                GrantType = grantType,
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveClientCustomGrantTypeAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedObjectId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedObjectId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingGrantTypes =
                            inMemoryClient.AllowedGrantTypes.FirstOrDefault(p => p.Id == parsedObjectId);
                        if (existingGrantTypes != null)
                        {
                            db.Entry(existingGrantTypes).State = EntityState.Deleted;
                            inMemoryClient.AllowedGrantTypes.Remove(existingGrantTypes);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject or secretId"));
        }

        #endregion

        #region ClientScope

        public Task<IdentityAdminResult> AddClientScopeAsync(string subject, string scope)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingScopes = inMemoryClient.AllowedScopes;
                        if (existingScopes.All(x => x.Scope != scope))
                        {
                            inMemoryClient.AllowedScopes.Add(new IdentityServer4.EntityFramework.Entities.ClientScope
                            {
                                Scope = scope,
                            });
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveClientScopeAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedObjectId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedObjectId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryClient = db.Clients.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryClient == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingScopes = inMemoryClient.AllowedScopes.FirstOrDefault(p => p.Id == parsedObjectId);
                        if (existingScopes != null)
                        {
                            db.Entry(existingScopes).State = EntityState.Deleted;
                            inMemoryClient.AllowedScopes.Remove(existingScopes);
                            db.SaveChanges();
                        }
                        return Task.FromResult(IdentityAdminResult.Success);
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
            }
            return Task.FromResult(new IdentityAdminResult("Invalid subject or secretId"));
        }

        #endregion

        #endregion

        public Task<ClientMetaData> GetMetadataAsync()
        {
            return Task.FromResult(GetMetadata());
        }

        #region helperMethods

        protected IdentityAdminResult SetClientProperty(IEnumerable<PropertyMetadata> propsMeta, IdentityServer4.EntityFramework.Entities.Client client,
            string type, string value)
        {
            IdentityAdminResult result;
            if (propsMeta.TrySet(client, type, value, out result))
            {
                return result;
            }

            throw new Exception("Invalid property type " + type);
        }

        protected string GetClientProperty(PropertyMetadata propMetadata, IdentityServer4.EntityFramework.Entities.Client client)
        {
            string val;
            if (propMetadata.TryGet(client, out val))
            {
                return val;
            }
            throw new Exception("Invalid property type " + propMetadata.Type);
        }

        private IEnumerable<string> ValidateRoleProperties(IEnumerable<PropertyValue> properties)
        {
            return properties.Select(x => ValidateRoleProperty(x.Type, x.Value)).Aggregate((x, y) => x.Concat(y));
        }

        private IEnumerable<string> ValidateRoleProperty(string type, string value)
        {
            return Enumerable.Empty<string>();
        }

        #endregion
    }
}