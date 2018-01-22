using IdentityServer4.EntityFramework.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer4.EntityFramework
{
    public class ConfigurationDbContext : DbContext
    {
        public string Schema { get; protected set; }

        /// <summary>
        /// Gets or sets the clients.
        /// </summary>
        /// <value>
        /// The clients.
        /// </value>
        public DbSet<Client> Clients { get; set; }

        /// <summary>
        /// Gets or sets the identity resources.
        /// </summary>
        /// <value>
        /// The identity resources.
        /// </value>
        public DbSet<IdentityResource> IdentityResources { get; set; }
        /// <summary>
        /// Gets or sets the API resources.
        /// </summary>
        /// <value>
        /// The API resources.
        /// </value>
        public DbSet<ApiResource> ApiResources { get; set; }

        public ConfigurationDbContext(string connectionString):base(connectionString)
        {
            //this.Configuration.LazyLoadingEnabled = false;
        }

        public ConfigurationDbContext(string connectionString, string schema)
        {
            this.Schema = schema;
        }

        //protected override void ConfigureChildCollections()
        //{
        //    DbModelBuilderExtensions.RegisterClientChildTablesForDelete<Client>((DbContext)this);
        //}

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //DbModelBuilderExtensions.ConfigureClients(modelBuilder, this.Schema);
        }
    }
}
