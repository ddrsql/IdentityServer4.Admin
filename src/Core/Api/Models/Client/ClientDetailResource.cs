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
using System.Web.Http.Routing;
using IdentityAdmin.Core.Client;
using IdentityAdmin.Extensions;

namespace IdentityAdmin.Api.Models.Client
{
    public class ClientDetailResource
    {
        public ClientDetailResource(ClientDetail client, UrlHelper url, ClientMetaData idmAdminMeta)
        {
            if (client == null) throw new ArgumentNullException("client");
            if (url == null) throw new ArgumentNullException("url");
            if (idmAdminMeta == null) throw new ArgumentNullException("idmAdminMeta");

            Data = new ClientDetailDataResource(client, url, idmAdminMeta);

            var links = new Dictionary<string, string>();
            if (idmAdminMeta.SupportsDelete)
            {
                links["Delete"] = url.RelativeLink(Constants.RouteNames.DeleteClient, new {subject = client.Subject});
            }
            Links = links;
        }

        public ClientDetailDataResource Data { get; set; }
        public object Links { get; set; }
    }
}