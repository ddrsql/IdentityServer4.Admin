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

namespace IdentityAdmin.Host.InMemoryService
{
    public class ApiResourceSeeder
    {
        public static ICollection<InMemoryApiResource> Get(int random = 0)
        {
            var apiResources = new HashSet<InMemoryApiResource>
            {
                new InMemoryApiResource
                {
                    Id = 1,
                    Name = "Admin",
                    Description = "They run the show",
                    Claims = new List<InMemoryApiResourceClaim>
                    {
                        new InMemoryApiResourceClaim
                        {
                            Id = 1,
                            Type = "openid"
                        }
                    }
                },
                new InMemoryApiResource
                {
                    Id = 2,
                    Name = "Manager",
                    Description = "They pay the bills",
                    Claims = new List<InMemoryApiResourceClaim>
                    {
                        new InMemoryApiResourceClaim
                        {
                            Id = 1,
                            Type = "email"
                        }
                    }
                }
            };

            for (var i = 0; i < random; i++)
            {
                apiResources.Add(new InMemoryApiResource
                {
                    Id = apiResources.Count + 1,
                    Name = GenName().ToLower(),
                    Description = GenName().ToLower()
                });                
            }
            return apiResources;
        }

        private static string GenName()
        {
            var firstChar = (char)((rnd.Next(26)) + 65);
            var username = firstChar.ToString();
            for (var j = 0; j < 6; j++)
            {
                username += Char.ToLower((char)(rnd.Next(26) + 65));
            }
            return username;
        }

        static Random rnd = new Random();
    }
}