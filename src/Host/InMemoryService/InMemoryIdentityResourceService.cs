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
using IdentityAdmin.Core.IdentityResource;
using IdentityAdmin.Core.Metadata;
using IdentityAdmin.Extensions;
using System.Configuration;
using IdentityServer4.EntityFramework;
using System.Data.Entity;

namespace IdentityAdmin.Host.InMemoryService
{
    public class InMemoryIdentityResourceService : IIdentityResourceService
    {
        private static string connection = ConfigurationManager.ConnectionStrings["IdSvr4ConfigAdmin"].ConnectionString;
        private readonly ICollection<InMemoryIdentityResource> _identityResources;
        public static MapperConfiguration Config;

        public InMemoryIdentityResourceService(ICollection<InMemoryIdentityResource> identityResources)
        {
            this._identityResources = identityResources;
        }

        private IdentityResourceMetaData _metadata;

        private IdentityResourceMetaData GetMetadata()
        {
            if (_metadata == null)
            {
                var updateIdentityResource = new List<PropertyMetadata>();
                updateIdentityResource.AddRange(PropertyMetadata.FromType<IdentityServer4.EntityFramework.Entities.IdentityResource>());

                var createIdentityResource = new List<PropertyMetadata>
                {
                    PropertyMetadata.FromProperty<IdentityServer4.EntityFramework.Entities.IdentityResource>(x => x.Name, "IdentityResourceName", required: true),
                };

                _metadata = new IdentityResourceMetaData
                {
                    SupportsCreate = true,
                    SupportsDelete = true,
                    CreateProperties = createIdentityResource,
                    UpdateProperties = updateIdentityResource
                };
            }
            return _metadata;
        }

        public Task<IdentityResourceMetaData> GetMetadataAsync()
        {
            return Task.FromResult(GetMetadata());
        }

        public Task<IdentityAdminResult<CreateResult>> CreateAsync(IEnumerable<PropertyValue> properties)
        {
            var IdentityResourceNameClaim = properties.Single(x => x.Type == "IdentityResourceName");

            var IdentityResourceName = IdentityResourceNameClaim.Value;
            

            string[] exclude = { "IdentityResourceName" };
            var otherProperties = properties.Where(x => !exclude.Contains(x.Type)).ToArray();

            var metadata = GetMetadata();
            var createProps = metadata.CreateProperties;
            var inMemoryIdentityResource = new IdentityServer4.EntityFramework.Entities.IdentityResource
            {
                //Id = _identityResources.Count + 1,
                Name = IdentityResourceName,
                Enabled = true,
                Required = false,
                ShowInDiscoveryDocument = true
            };
            
            foreach (var prop in otherProperties)
            {
                var propertyResult = SetProperty(createProps, inMemoryIdentityResource, prop.Type, prop.Value);
                if (!propertyResult.IsSuccess)
                {
                    return Task.FromResult(new IdentityAdminResult<CreateResult>(propertyResult.Errors.ToArray()));
                }
            }
            using (var db = new ConfigurationDbContext(connection))
            {
                try
                {
                    db.IdentityResources.Add(inMemoryIdentityResource);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new IdentityAdminResult<CreateResult>(ex.Message));
                }
            }
            return Task.FromResult(new IdentityAdminResult<CreateResult>(new CreateResult { Subject = inMemoryIdentityResource.Id.ToString() }));
        }

        public Task<IdentityAdminResult<QueryResult<IdentityResourceSummary>>> QueryAsync(string filter, int start, int count)
        {
            using (var db = new ConfigurationDbContext(connection))
            {
                var query = from identityResource in db.IdentityResources orderby identityResource.Name select identityResource;

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    query =
                        from identityResource in query
                        where identityResource.Name.Contains(filter)
                        orderby identityResource.Name
                        select identityResource;
                }

                int total = query.Count();
                var scopes = query.Skip(start).Take(count).ToArray();

                var result = new QueryResult<IdentityResourceSummary>
                {
                    Start = start,
                    Count = count,
                    Total = total,
                    Filter = filter,
                    Items = scopes.Select(x =>
                    {
                        var scope = new IdentityResourceSummary
                        {
                            Subject = x.Id.ToString(),
                            Name = x.Name,
                            Description = x.Name
                        };

                        return scope;
                    }).ToArray()
                };

                return Task.FromResult(new IdentityAdminResult<QueryResult<IdentityResourceSummary>>(result));
            }
        }

        public Task<IdentityAdminResult<IdentityResourceDetail>> GetAsync(string subject)
        {
            int parsedId;
            if (int.TryParse(subject, out parsedId))
            {
                using (var db = new ConfigurationDbContext(connection))
                {
                    var inMemoryApiResource = db.IdentityResources.FirstOrDefault(p => p.Id == parsedId);
                    if (inMemoryApiResource == null)
                    {
                        return Task.FromResult(new IdentityAdminResult<IdentityResourceDetail>((IdentityResourceDetail)null));
                    }

                    var result = new IdentityResourceDetail
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
                    result.IdentityResourceClaims = inMemoryApiResource.UserClaims.Select(x => new IdentityResourceClaimValue
                    {
                        Id = x.Id.ToString(),
                        Type = x.Type
                    });

                    return Task.FromResult(new IdentityAdminResult<IdentityResourceDetail>(result));
                }
            }
            return Task.FromResult(new IdentityAdminResult<IdentityResourceDetail>((IdentityResourceDetail)null));
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
                        var inMemoryIdentityResource = db.IdentityResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryIdentityResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        db.IdentityResources.Remove(inMemoryIdentityResource);
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
                        var inMemoryApiResource = db.IdentityResources.FirstOrDefault(p => p.Id == parsedSubject);
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

        protected string GetProperty(PropertyMetadata propMetadata, IdentityServer4.EntityFramework.Entities.IdentityResource identityResource)
        {
            string val;
            if (propMetadata.TryGet(identityResource, out val))
            {
                return val;
            }
            throw new Exception("Invalid property type " + propMetadata.Type);
        }

        protected IdentityAdminResult SetProperty(IEnumerable<PropertyMetadata> propsMeta, IdentityServer4.EntityFramework.Entities.IdentityResource identityResource, string type, string value)
        {
            IdentityAdminResult result;
            if (propsMeta.TrySet(identityResource, type, value, out result))
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
                        var inMemoryIdentityResource = db.IdentityResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (inMemoryIdentityResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingClaims = inMemoryIdentityResource.UserClaims;
                        if (existingClaims.All(x => x.Type != type))
                        {
                            inMemoryIdentityResource.UserClaims.Add(new IdentityServer4.EntityFramework.Entities.IdentityClaim
                            {
                                //Id = inMemoryIdentityResource.UserClaims.Count + 1,
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
                        var identityResource = db.IdentityResources.FirstOrDefault(p => p.Id == parsedSubject);
                        if (identityResource == null)
                        {
                            return Task.FromResult(new IdentityAdminResult("Invalid subject"));
                        }
                        var existingClaim = identityResource.UserClaims.FirstOrDefault(p => p.Id == parseClaimId);
                        if (existingClaim != null)
                        {
                            db.Entry(existingClaim).State = EntityState.Deleted;
                            identityResource.UserClaims.Remove(existingClaim);
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
    }
}