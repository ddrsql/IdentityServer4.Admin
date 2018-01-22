// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

#pragma warning disable 1591

using System.Collections.Generic;

namespace IdentityServer4.EntityFramework.Entities
{
    public class ApiResource
    {
        public int Id { get; set; }
        public bool Enabled { get; set; } = true;
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public virtual List<ApiSecret> Secrets { get; set; }
        public virtual List<ApiScope> Scopes { get; set; }
            
        //public virtual List<ApiResourceClaim> UserClaims { get; set; }

        public virtual List<ApiClaims> Claims { get; set; }
    }

    public class ApiClaims
    {
        public int ApiResourceId { get; set; }

        public int Id { get; set; }
        public string Type { get; set; }
    }
}
