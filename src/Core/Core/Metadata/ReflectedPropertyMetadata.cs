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

using System;
using System.Reflection;
using IdentityAdmin.Resources;

namespace IdentityAdmin.Core.Metadata
{
    public class ReflectedPropertyMetadata : ExecutablePropertyMetadata
    {
        PropertyInfo property;

        public ReflectedPropertyMetadata(PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");

            this.property = property;
        }

        public override string Get(object instance)
        {
            if (instance == null) throw new ArgumentNullException("instance");

            if (this.DataType == PropertyDataType.Password) return null;
            if (this.DataType == PropertyDataType.Enumerable) return null;

            var value = property.GetValue(instance);
            if (value != null)
            {
                return value.ToString();
            }

            return null;
        }

        public override IdentityAdminResult Set(object instance, string value)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            
            if (String.IsNullOrWhiteSpace(value))
            {
                property.SetValue(instance, null);
            }
            else
            {
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                object convertedValue = null;
                try
                {
                    if (type.IsEnum) 
                    { 
                         convertedValue= Enum.Parse(type, value);
                    }else
                    {
                        convertedValue = Convert.ChangeType(value, type);
                    }
              
                }
                catch
                {
                    return new IdentityAdminResult(Messages.ConversionFailed);
                }
                
                property.SetValue(instance, convertedValue);
            }

            return IdentityAdminResult.Success;
        }
    }
}
