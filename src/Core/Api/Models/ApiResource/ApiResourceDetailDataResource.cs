﻿
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
using System.Web.Http.Routing;
using IdentityAdmin.Core.ApiResource;
using IdentityAdmin.Extensions;

namespace IdentityAdmin.Api.Models.ApiResource
{
    public class ApiResourceDetailDataResource : Dictionary<string, object>
    {
        public ApiResourceDetailDataResource(ApiResourceDetail apiResource, UrlHelper url, ApiResourceMetaData metaData)
        {
            if (apiResource == null) throw new ArgumentNullException(nameof(apiResource));
            if (url == null) throw new ArgumentNullException(nameof(url));
            if (metaData == null) throw new ArgumentNullException(nameof(metaData));

            this["Name"] = apiResource.Name;
            this["Description"] = apiResource.Description;
            this["Subject"] = apiResource.Subject;

            if (apiResource.Properties != null)
            {
                var props = (from p in apiResource.Properties
                            let m = (from m in metaData.UpdateProperties where m.Type == p.Type select m).SingleOrDefault()
                            where m != null
                            select new
                            {
                                Data = m.Convert(p.Value),
                                Meta = m,
                                Links = new
                                {
                                    update = url.RelativeLink(Constants.RouteNames.UpdateApiResourceProperty,
                                        new
                                        {
                                            subject = apiResource.Subject,
                                            type = p.Type.ToBase64UrlEncoded()
                                        }
                                    )
                                }
                            }).ToList();

                if (props.Any())
                {
                    this["Properties"] = props.ToArray();
                }
            }

            this["Claims"] = new
            {
                Data = GetClaims(apiResource, url).ToArray(),
                Links = new
                {
                    create = url.RelativeLink(Constants.RouteNames.AddApiResourceClaim, new { subject = apiResource.Subject })
                }
            };

            this["Secrets"] = new
            {
                Data = GetSecrets(apiResource, url).ToArray(),
                Links = new
                {
                    create = url.RelativeLink(Constants.RouteNames.AddApiResourceSecret, new { subject = apiResource.Subject })
                }
            };

            this["Scopes"] = new
            {
                Data = GetScopes(apiResource, url).ToArray(),
                Links = new
                {
                    create = url.RelativeLink(Constants.RouteNames.AddApiResourceScope, new { subject = apiResource.Subject })                    
                }
            };
        }

        private IEnumerable<object> GetClaims(ApiResourceDetail apiResource, UrlHelper url)
        {
            if (apiResource.ResourceClaims != null)
            {
                return from c in apiResource.ResourceClaims.ToArray()
                    select new
                    {
                        Data = c,
                        Links = new
                        {
                            delete = url.RelativeLink(Constants.RouteNames.RemoveApiResourceClaim, new
                            {
                                subject = apiResource.Subject,
                                id = c.Id
                            })
                        }
                    };
            }
            return new object[0];
        }

        private IEnumerable<object> GetSecrets(ApiResourceDetail apiResource, UrlHelper url)
        {
            if (apiResource.ResourceSecrets != null)
            {
                return from c in apiResource.ResourceSecrets
                    select new
                    {
                        Data = c,
                        Links = new
                        {
                            update = url.RelativeLink(Constants.RouteNames.UpdateApiResourceSecret, new
                            {
                               subject = apiResource.Subject,
                               id = c.Id 
                            }),
                            delete = url.RelativeLink(Constants.RouteNames.RemoveApiResourceSecret, new
                            {
                                subject = apiResource.Subject,
                                id = c.Id
                            })
                        }
                    };
            }
            return new object[0];            
        }

        private IEnumerable<object> GetScopes(ApiResourceDetail apiResource, UrlHelper url)
        {
            if (apiResource.ResourceScopes != null)
            {
                return from c in apiResource.ResourceScopes
                        select new
                        {
                            Data = c,
                            Claims = GetScopeClaims(apiResource, c, url),
                            Links = new
                            {
                                update = url.RelativeLink(Constants.RouteNames.UpdateApiResourceScope, new
                                {
                                    subject = apiResource.Subject,
                                    id = c.Id
                                }),
                                delete = url.RelativeLink(Constants.RouteNames.RemoveApiResourceScope, new
                                {
                                    subject = apiResource.Subject,
                                    id = c.Id
                                }),
                                addClaim = url.RelativeLink(Constants.RouteNames.AddApiResourceScopeClaim, new
                                {
                                    subject = apiResource.Subject,
                                    id = c.Id
                                })
                            }
                        };
            }
            return new object[0];
        }

        private IEnumerable<object> GetScopeClaims(ApiResourceDetail apiResource, ApiResourceScopeValue apiResourceScope, UrlHelper url)
        {
            if (apiResourceScope.Claims != null)
            {
                return from c in apiResourceScope.Claims
                    select new
                    {
                        Data = c,
                        Links = new
                        {
                            delete = url.RelativeLink(Constants.RouteNames.RemoveApiResourceScopeClaim, new
                            {
                                subject = apiResource.Subject,
                                id = apiResourceScope.Id,
                                claimId = c.Id
                            })
                        }
                    };
            }
            return new object[0];
        }
    }
}
