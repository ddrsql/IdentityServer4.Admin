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
using System.Linq;
using System.Threading.Tasks;
using IdentityAdmin.Core;
using IdentityAdmin.Core.ApiResource;
using IdentityAdmin.Core.Metadata;
using IdentityAdmin.Extensions;
using System.Configuration;
using IdentityServer4.EntityFramework;
using System.Data.Entity;

namespace IdentityAdmin.Host.InMemoryService
{
    public class InMemoryApiResourceService : IApiResourceService
    {
        private static string connection = ConfigurationManager.ConnectionStrings["IdSvr4ConfigAdmin"].ConnectionString;
        private readonly ICollection<InMemoryApiResource> _apiResources;

        public InMemoryApiResourceService(ICollection<InMemoryApiResource> apiResources)
        {
            this._apiResources = apiResources;
        }

        private ApiResourceMetaData _metadata;

        private ApiResourceMetaData GetMetadata()
        {
            if (_metadata == null)
            {
                var updateApiResource = new List<PropertyMetadata>();
                updateApiResource.AddRange(PropertyMetadata.FromType<IdentityServer4.EntityFramework.Entities.ApiResource>());

                var createApiResource = new List<PropertyMetadata>
                {
                    PropertyMetadata.FromProperty<IdentityServer4.EntityFramework.Entities.ApiResource>(x => x.Name, "ApiResourceName", required: true),
                };

                _metadata = new ApiResourceMetaData
                {
                    SupportsCreate = true,
                    SupportsDelete = true,
                    CreateProperties = createApiResource,
                    UpdateProperties = updateApiResource
                };
            }
            return _metadata;
        }

        public Task<ApiResourceMetaData> GetMetadataAsync()
        {
            return Task.FromResult(GetMetadata());
        }

        public Task<IdentityAdminResult<CreateResult>> CreateAsync(IEnumerable<PropertyValue> properties)
        {
            var ApiResourceNameClaim = properties.Single(x => x.Type == "ApiResourceName");

            var ApiResourceName = ApiResourceNameClaim.Value;

            string[] exclude = { "ApiResourceName" };
            var otherProperties = properties.Where(x => !exclude.Contains(x.Type)).ToArray();

            var metadata = GetMetadata();
            var createProps = metadata.CreateProperties;
            var inMemoryApiResource = new IdentityServer4.EntityFramework.Entities.ApiResource
            {
                //Id = _apiResources.Count + 1,
                Name = ApiResourceName,
                Enabled = true
            };

            foreach (var prop in otherProperties)
            {
                var propertyResult = SetProperty(createProps, inMemoryApiResource, prop.Type, prop.Value);
                if (!propertyResult.IsSuccess)
                {
                    return Task.FromResult(new IdentityAdminResult<CreateResult>(propertyResult.Errors.ToArray()));
                }
            }
            using (var db = new ConfigurationDbContext(connection))
            {
                try
                {
                    db.ApiResources.Add(inMemoryApiResource);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new IdentityAdminResult<CreateResult>(ex.Message));
                }
            }
            return Task.FromResult(new IdentityAdminResult<CreateResult>(new CreateResult { Subject = inMemoryApiResource.Id.ToString() }));
        }

        public Task<IdentityAdminResult<QueryResult<ApiResourceSummary>>> QueryAsync(string filter, int start, int count)
        {
            using (var db = new ConfigurationDbContext(connection))
            {
                var query = from apiResource in db.ApiResources orderby apiResource.Name select apiResource;

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    query =
                        from apiResource in query
                        where apiResource.Name.Contains(filter)
                        orderby apiResource.Name
                        select apiResource;
                }

                int total = query.Count();
                var scopes = query.Skip(start).Take(count).ToArray();

                var result = new QueryResult<ApiResourceSummary>
                {
                    Start = start,
                    Count = count,
                    Total = total,
                    Filter = filter,
                    Items = scopes.Select(x =>
                    {
                        var scope = new ApiResourceSummary
                        {
                            Subject = x.Id.ToString(),
                            Name = x.Name,
                            Description = x.Name
                        };

                        return scope;
                    }).ToArray()
                };

                return Task.FromResult(new IdentityAdminResult<QueryResult<ApiResourceSummary>>(result));
            }
        }

        public Task<IdentityAdminResult<ApiResourceDetail>> GetAsync(string subject)
        {
            int parsedId;
            if (int.TryParse(subject, out parsedId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedId);
                    if (inMemoryApiResource == null)
                    {
                        return Task.FromResult(new IdentityAdminResult<ApiResourceDetail>((ApiResourceDetail)null));
                    }

                    var result = new ApiResourceDetail
                    {
                        Subject = subject,
                        Name = inMemoryApiResource.Name,
                        Description = inMemoryApiResource.Description
                    };

                    var metadata = GetMetadata();
                    var props = from prop in metadata.UpdateProperties
                                select new PropertyValue
                                {
                                    Type = prop.Type,
                                    Value = GetProperty(prop, inMemoryApiResource),
                                };

                    result.Properties = props.ToArray();
                    result.ResourceClaims = inMemoryApiResource.Claims.Select(x => new ApiResourceClaimValue
                    {
                        Id = x.Id.ToString(),
                        Type = x.Type
                    });
                    result.ResourceSecrets = inMemoryApiResource.Secrets.Select(x => new ApiResourceSecretValue
                    {
                        Id = x.Id.ToString(),
                        Type = x.Type,
                        Description = x.Description,
                        Value = x.Value,
                        Expiration = x.Expiration
                    });
                    result.ResourceScopes = inMemoryApiResource.Scopes.Select(x => new ApiResourceScopeValue
                    {
                        Id = x.Id.ToString(),
                        Name = x.Name,
                        Description = x.Description,
                        Emphasize = x.Emphasize,
                        Required = x.Required,
                        ShowInDiscoveryDocument = x.ShowInDiscoveryDocument,
                        Claims = x.Claims.Select(y => new ApiResourceScopeClaimValue
                        {
                            Id = y.Id.ToString(),
                            Type = y.Type
                        }).ToList()
                    }).ToList();

                    return Task.FromResult(new IdentityAdminResult<ApiResourceDetail>(result));
                }
            }
            return Task.FromResult(new IdentityAdminResult<ApiResourceDetail>((ApiResourceDetail)null));
        }

        public Task<IdentityAdminResult> DeleteAsync(string subject)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        db.ApiResources.Remove(inMemoryApiResource);
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

        public Task<IdentityAdminResult> SetPropertyAsync(string subject, string type, string value)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var meta = GetMetadata();

                        SetProperty(meta.UpdateProperties, inMemoryApiResource, type, value);
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

        protected string GetProperty(PropertyMetadata propMetadata, IdentityServer4.EntityFramework.Entities.ApiResource apiResource)
        {
            string val;
            if (propMetadata.TryGet(apiResource, out val))
            {
                return val;
            }
            throw new Exception("Invalid property type " + propMetadata.Type);
        }

        protected IdentityAdminResult SetProperty(IEnumerable<PropertyMetadata> propsMeta, IdentityServer4.EntityFramework.Entities.ApiResource apiResource, string type, string value)
        {
            IdentityAdminResult result;
            if (propsMeta.TrySet(apiResource, type, value, out result))
            {
                return result;
            }

            throw new Exception("Invalid property type " + type);
        }

        public Task<IdentityAdminResult> AddClaimAsync(string subject, string type)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingClaims = inMemoryApiResource.Claims;
                        if (existingClaims.All(x => x.Type != type))
                        {
                            inMemoryApiResource.Claims.Add(new IdentityServer4.EntityFramework.Entities.ApiClaims
                            {
                                //Id = inMemoryApiResource.Claims.Count + 1,
                                Type = type
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

        public Task<IdentityAdminResult> RemoveClaimAsync(string subject, string id)
        {
            int parsedSubject;
            int parseClaimId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parseClaimId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var apiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (apiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingClaim = apiResource.Claims.FirstOrDefault(p => p.Id == parseClaimId);
                        if (existingClaim != null)
                        {
                            db.Entry(existingClaim).State = EntityState.Deleted;
                            apiResource.Claims.Remove(existingClaim);
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

        public Task<IdentityAdminResult> AddSecretAsync(string subject, string type, string value, string description, DateTime? expiration)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingSecrets = inMemoryApiResource.Secrets;
                        if (!existingSecrets.Any(x => x.Type == type && x.Value == value))
                        {
                            inMemoryApiResource.Secrets.Add(new IdentityServer4.EntityFramework.Entities.ApiSecret
                            {
                                //Id = inMemoryApiResource.Secrets.Count + 1,
                                Type = type,
                                Value = value,
                                Description = description,
                                Expiration = expiration
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

        public Task<IdentityAdminResult> UpdateSecretAsync(string subject, string secretSubject, string type, string value, string description, DateTime? expiration)
        {
            int parsedSubject, parsedSecretSubject;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(secretSubject, out parsedSecretSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingSecret = inMemoryApiResource.Secrets.FirstOrDefault(p => p.Id == parsedSecretSubject);
                        if (existingSecret != null)
                        {
                            existingSecret.Value = value;
                            existingSecret.Type = type;
                            existingSecret.Description = description;
                            if (expiration.HasValue)
                            {
                                //Save as new DateTimeOffset(expiration.Value)
                                existingSecret.Expiration = expiration.Value;
                            }
                            db.SaveChanges();
                            return Task.FromResult(IdentityAdminResult.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
                return Task.FromResult(new IdentityAdminResult("Not found"));
            }

            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveSecretAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedSecretId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedSecretId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingSecret = inMemoryApiResource.Secrets.FirstOrDefault(p => p.Id == parsedSecretId);
                        if (existingSecret != null)
                        {
                            db.Entry(existingSecret).State = EntityState.Deleted;
                            inMemoryApiResource.Secrets.Remove(existingSecret);
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

        public Task<IdentityAdminResult> AddScopeAsync(string subject, string name)
        {
            int parsedSubject;
            if (int.TryParse(subject, out parsedSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingScopes = inMemoryApiResource.Scopes;
                        if (existingScopes.All(x => x.Name != name))
                        {
                            inMemoryApiResource.Scopes.Add(new IdentityServer4.EntityFramework.Entities.ApiScope
                            {
                                //Id = inMemoryApiResource.Secrets.Count + 1,
                                Name = name,
                                Emphasize = false,
                                Required = false,
                                ShowInDiscoveryDocument = true
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

        public Task<IdentityAdminResult> UpdateScopeAsync(string subject, string scopeSubject, string name, string description, bool emphasize, bool required, bool showInDiscoveryDocument)
        {
            int parsedSubject, parsedScopeSubject;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(scopeSubject, out parsedScopeSubject))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingScope = inMemoryApiResource.Scopes.FirstOrDefault(p => p.Id == parsedScopeSubject);
                        if (existingScope != null)
                        {
                            existingScope.Name = name;
                            existingScope.Description = description;
                            existingScope.Emphasize = emphasize;
                            existingScope.Required = required;
                            existingScope.ShowInDiscoveryDocument = showInDiscoveryDocument;
                            db.SaveChanges();
                            return Task.FromResult(IdentityAdminResult.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        return Task.FromResult(new IdentityAdminResult(ex.Message));
                    }
                }
                return Task.FromResult(new IdentityAdminResult("Not found"));
            }

            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
        }

        public Task<IdentityAdminResult> RemoveScopeAsync(string subject, string id)
        {
            int parsedSubject;
            int parsedScopeId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedScopeId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingScope = inMemoryApiResource.Scopes.FirstOrDefault(p => p.Id == parsedScopeId);
                        if (existingScope != null)
                        {
                            db.Entry(existingScope).State = EntityState.Deleted;
                            inMemoryApiResource.Scopes.Remove(existingScope);
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

        public Task<IdentityAdminResult> AddScopeClaimAsync(string subject, string id, string type)
        {
            int parsedSubject;
            int parsedScopeId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedScopeId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var scope = inMemoryApiResource.Scopes.FirstOrDefault(p => p.Id == parsedScopeId);
                        if (scope == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        if (scope.Claims.Any(p => p.Type == type))
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid scope"));
                        }
                        scope.Claims.Add(new IdentityServer4.EntityFramework.Entities.ApiScopeClaim
                        {
                            //Id = scope.Claims.Count + 1,
                            Type = type
                        });
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

        public Task<IdentityAdminResult> RemoveScopeClaimAsync(string subject, string id, string scopeId)
        {
            int parsedSubject;
            int parsedScopeId;
            int parsedScopeClaimId;
            if (int.TryParse(subject, out parsedSubject) && int.TryParse(id, out parsedScopeId) && int.TryParse(scopeId, out parsedScopeClaimId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    try
                    {
                        var inMemoryApiResource = db.ApiResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryApiResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var scope = inMemoryApiResource.Scopes.FirstOrDefault(p => p.Id == parsedScopeId);
                        if (scope == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var scopeClaim = scope.Claims.FirstOrDefault(p => p.Id == parsedScopeClaimId);
                        if (scopeClaim == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        db.Entry(scopeClaim).State = EntityState.Deleted;
                        scope.Claims.Remove(scopeClaim);
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
    }
}