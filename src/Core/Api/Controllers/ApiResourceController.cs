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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using IdentityAdmin.Core;
using IdentityAdmin.Core.ApiResource;
using IdentityAdmin.Extensions;
using IdentityAdmin.Api.Filters;
using IdentityAdmin.Api.Models.ApiResource;
using IdentityAdmin.Resources;

namespace IdentityAdmin.Api.Controllers
{
    [RoutePrefix(Constants.ApiResourcesRoutePrefix)]
    [NoCache]
    public class ApiResourceController : ApiController
    {
        private readonly IApiResourceService _service;

        public ApiResourceController(IApiResourceService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            _service = service;
        }

        ApiResourceMetaData _metadata;

        async Task<ApiResourceMetaData> GetCoreMetaDataAsync()
        {
            if (_metadata == null)
            {
                _metadata = await _service.GetMetadataAsync();
                if (_metadata == null) throw new InvalidOperationException("IdentityResourceMetaData returned null");
                _metadata.Validate();

                return _metadata;
            }

            return _metadata;
        }

        public IHttpActionResult BadRequest<T>(T data)
        {
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, data));
        }

        public IHttpActionResult NoContent()
        {
            return StatusCode(HttpStatusCode.NoContent);
        }

        public IHttpActionResult MethodNotAllowed()
        {
            return StatusCode(HttpStatusCode.MethodNotAllowed);
        }

        [HttpGet, Route("", Name = Constants.RouteNames.GetApiResources)]
        public async Task<IHttpActionResult> GetApiResourcesAsync(string filter = null, int start = 0, int count = 100)
        {
            var result = await _service.QueryAsync(filter, start, count);
            if (result.IsSuccess)
            {
                var meta = await GetCoreMetaDataAsync();
                var resource = new ApiResourceQueryResultResource(result.Result, Url, meta);
                return Ok(resource);
            }
            return BadRequest(result.ToError());
        }

        [HttpGet, Route("{subject}", Name = Constants.RouteNames.GetApiResource)]
        public async Task<IHttpActionResult> GetApiResourceAsync(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToError());
            }

            var result = await _service.GetAsync(subject);
            if (result.IsSuccess)
            {
                if (result.Result == null)
                {
                    return NotFound();
                }

                var meta = await GetCoreMetaDataAsync();
                return Ok(new ApiResourceDetailResource(result.Result, Url, meta));
            }

            return BadRequest(result.ToError());
        }

        [HttpPost, Route("", Name = Constants.RouteNames.CreateApiResource)]
        public async Task<IHttpActionResult> CreateApiResourceAsync(PropertyValue[] properties)
        {
            var metadata = await GetCoreMetaDataAsync();
            if (!metadata.SupportsCreate)
            {
                return MethodNotAllowed();
            }
            if (properties == null)
            {
                ModelState.AddModelError("", Messages.ApiResourceDataRequired);
            }

            var errors = ValidateCreateProperties(metadata, properties);
            foreach (var error in errors)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid)
            {
                var result = await _service.CreateAsync(properties);
                if (result.IsSuccess)
                {
                    var url = Url.RelativeLink(Constants.RouteNames.GetApiResource, new { subject = result.Result.Subject });
                    var resource = new
                    {
                        Data = new { subject = result.Result.Subject },
                        Links = new { detail = url }
                    };
                    return Created(url, resource);
                }

                ModelState.AddErrors(result);
            }
            return BadRequest("");
        }

        [HttpDelete, Route("{subject}", Name = Constants.RouteNames.DeleteApiResource)]
        public async Task<IHttpActionResult> DeleteApiResourceAsync(string subject)
        {
            var meta = await GetCoreMetaDataAsync();
            if (!meta.SupportsDelete)
            {
                return MethodNotAllowed();
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToError());
            }

            var result = await _service.DeleteAsync(subject);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(result.ToError());
        }

        [HttpPut, Route("{subject}/properties/{type}", Name = Constants.RouteNames.UpdateApiResourceProperty)]
        public async Task<IHttpActionResult> SetPropertyAsync(string subject, string type)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            type = type.FromBase64UrlEncoded();

            string value = await Request.Content.ReadAsStringAsync();
            var meta = await GetCoreMetaDataAsync();
            ValidateUpdateProperty(meta, type, value);

            if (ModelState.IsValid)
            {
                var result = await _service.SetPropertyAsync(subject, type, value);
                if (result.IsSuccess)
                {
                    return NoContent();
                }

                ModelState.AddErrors(result);
            }

            return BadRequest(ModelState.ToError());
        }

        [HttpPost, Route("{subject}/apiresourceclaim", Name = Constants.RouteNames.AddApiResourceClaim)]
        public async Task<IHttpActionResult> AddApiResourceClaimAsync(string subject, ApiResourceClaimValue model)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            if (model == null)
            {
                ModelState.AddModelError("", "Model required");
            }

            if (ModelState.IsValid)
            {
                var result = await _service.AddClaimAsync(subject, model.Type);
                if (result.IsSuccess)
                {
                    return NoContent();
                }

                ModelState.AddErrors(result);
            }

            return BadRequest(ModelState.ToError());
        }

        [HttpDelete, Route("{subject}/identityresourceclaim/{id}", Name = Constants.RouteNames.RemoveApiResourceClaim)]
        public async Task<IHttpActionResult> RemoveApiResourceClaimAsync(string subject, string id)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }
            var result = await _service.RemoveClaimAsync(subject, id);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(result.ToError());
        }

        [HttpPost, Route("{subject}/secret", Name = Constants.RouteNames.AddApiResourceSecret)]
        public async Task<IHttpActionResult> AddApiResourceSecretAsync(string subject, ApiResourceSecretValue model)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            if (model == null)
            {
                ModelState.AddModelError("", Messages.ApiResourceSecretNeeded);
            }

            if (ModelState.IsValid)
            {
                var result = await _service.AddSecretAsync(subject, model.Type, model.Value, model.Description, model.Expiration);
                if (result.IsSuccess)
                {
                    return NoContent();
                }

                ModelState.AddErrors(result);
            }

            return BadRequest(ModelState.ToError());
        }

        [HttpDelete, Route("{subject}/secret/{id}", Name = Constants.RouteNames.RemoveApiResourceSecret)]
        public async Task<IHttpActionResult> RemoveApiResourceSecretAsync(string subject, string id)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var result = await _service.RemoveSecretAsync(subject, id);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(result.ToError());
        }

        [HttpPut, Route("{subject}/secret/{id}", Name = Constants.RouteNames.UpdateApiResourceSecret)]
        public async Task<IHttpActionResult> UpdateApiResourceSecretAsync(string subject, ApiResourceSecretValue model)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            if (model == null)
            {
                ModelState.AddModelError("", Messages.ApiResourceSecretNeeded);
            }

            if (ModelState.IsValid)
            {
                var result = await _service.UpdateSecretAsync(subject, model.Id, model.Type, model.Value, model.Description, model.Expiration);
                if (result.IsSuccess)
                {
                    return NoContent();
                }

                ModelState.AddErrors(result);
            }

            return BadRequest(ModelState.ToError());
        }

        [HttpPost, Route("{subject}/scope", Name = Constants.RouteNames.AddApiResourceScope)]
        public async Task<IHttpActionResult> AddApiResourceScopeAsync(string subject, ApiResourceScopeValue model)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            if (model == null)
            {
                ModelState.AddModelError("", Messages.ApiResourceScopeNeeded);
            }

            if (ModelState.IsValid)
            {
                var result = await _service.AddScopeAsync(subject, model.Name);
                if (result.IsSuccess)
                {
                    return NoContent();
                }

                ModelState.AddErrors(result);
            }

            return BadRequest(ModelState.ToError());
        }        

        [HttpDelete, Route("{subject}/scope/{id}", Name = Constants.RouteNames.RemoveApiResourceScope)]
        public async Task<IHttpActionResult> RemoveApiResourceScopeAsync(string subject, string id)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var result = await _service.RemoveScopeAsync(subject, id);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(result.ToError());
        }

        [HttpPut, Route("{subject}/scope/{id}", Name = Constants.RouteNames.UpdateApiResourceScope)]
        public async Task<IHttpActionResult> UpdateApiResourceScopeAsync(string subject, ApiResourceScopeValue model)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                ModelState["subject.String"].Errors.Clear();
                ModelState.AddModelError("", Messages.SubjectRequired);
            }

            if (model == null)
            {
                ModelState.AddModelError("", Messages.ApiResourceScopeNeeded);
            }

            if (ModelState.IsValid)
            {
                var result = await _service.UpdateScopeAsync(subject, model.Id, model.Name, model.Description, model.Emphasize, model.Required, model.ShowInDiscoveryDocument);
                if (result.IsSuccess)
                {
                    return NoContent();
                }

                ModelState.AddErrors(result);
            }

            return BadRequest(ModelState.ToError());
        }

        [HttpPost, Route("{subject}/scope/{id}/claim", Name = Constants.RouteNames.AddApiResourceScopeClaim)]
        public async Task<IHttpActionResult> AddApiResourceScopeClaimAsync(string subject, string id, ApiResourceScopeClaimValue model)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            if (model == null)
            {
                ModelState.AddModelError("", Messages.ApiResourceScopeClaimNeeded);
            }

            if (ModelState.IsValid)
            {
                var result = await _service.AddScopeClaimAsync(subject, id, model.Type);
                if (result.IsSuccess)
                {
                    return NoContent();
                }

                ModelState.AddErrors(result);
            }

            return BadRequest(ModelState.ToError());
        }

        [HttpDelete, Route("{subject}/scope/{id}/claim/{claimId}", Name = Constants.RouteNames.RemoveApiResourceScopeClaim)]
        public async Task<IHttpActionResult> RemoveApiResourceScopeClaimAsync(string subject, string id, string claimId)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(claimId))
            {
                return NotFound();
            }

            var result = await _service.RemoveScopeClaimAsync(subject, id, claimId);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(result.ToError());
        }

        private IEnumerable<string> ValidateCreateProperties(ApiResourceMetaData apiResourceMetaData, IEnumerable<PropertyValue> properties)
        {
            if (apiResourceMetaData == null) throw new ArgumentNullException(nameof(apiResourceMetaData));
            properties = properties ?? Enumerable.Empty<PropertyValue>();

            var meta = apiResourceMetaData.CreateProperties;
            return meta.Validate(properties);
        }

        private void ValidateUpdateProperty(ApiResourceMetaData apiResourceMetaData, string type, string value)
        {
            if (apiResourceMetaData == null) throw new ArgumentNullException(nameof(apiResourceMetaData));

            if (string.IsNullOrWhiteSpace(type))
            {
                ModelState.AddModelError("", Messages.PropertyTypeRequired);
                return;
            }

            var prop = apiResourceMetaData.UpdateProperties.SingleOrDefault(x => x.Type == type);
            if (prop == null)
            {
                ModelState.AddModelError("", string.Format(Messages.PropertyInvalid, type));
            }
            else
            {
                var error = prop.Validate(value);
                if (error != null)
                {
                    ModelState.AddModelError("", error);
                }
            }
        }
    }
}
