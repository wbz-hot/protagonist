﻿using DLCS.Model.Assets;
using DLCS.Repository.Entities;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace DLCS.Repository
{
    public partial class DlcsContext : DbContext
    {
        public DlcsContext()
        {
        }

        public DlcsContext(DbContextOptions<DlcsContext> options)
            : base(options)
        {
        }

        public virtual DbSet<ActivityGroup> ActivityGroups { get; set; }
        public virtual DbSet<AuthService> AuthServices { get; set; }
        public virtual DbSet<AuthToken> AuthTokens { get; set; }
        public virtual DbSet<Batch> Batches { get; set; }
        public virtual DbSet<CustomHeader> CustomHeaders { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<CustomerImageServer> CustomerImageServers { get; set; }
        public virtual DbSet<CustomerOriginStrategy> CustomerOriginStrategies { get; set; }
        public virtual DbSet<CustomerStorage> CustomerStorages { get; set; }
        public virtual DbSet<EntityCounter> EntityCounters { get; set; }
        public virtual DbSet<Asset> Images { get; set; }
        public virtual DbSet<ImageLocation> ImageLocations { get; set; }
        public virtual DbSet<ImageOptimisationPolicy> ImageOptimisationPolicies { get; set; }
        public virtual DbSet<ImageServer> ImageServers { get; set; }
        public virtual DbSet<ImageStorage> ImageStorages { get; set; }
        public virtual DbSet<InfoJsonTemplate> InfoJsonTemplates { get; set; }
        public virtual DbSet<MetricThreshold> MetricThresholds { get; set; }
        public virtual DbSet<NamedQuery> NamedQueries { get; set; }
        public virtual DbSet<OriginStrategy> OriginStrategies { get; set; }
        public virtual DbSet<Queue> Queues { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<RoleProvider> RoleProviders { get; set; }
        public virtual DbSet<SessionUser> SessionUsers { get; set; }
        public virtual DbSet<Space> Spaces { get; set; }
        public virtual DbSet<StoragePolicy> StoragePolicies { get; set; }
        public virtual DbSet<ThumbnailPolicy> ThumbnailPolicies { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
             * NOTE - the following was auto-generated by running the following command
             * dotnet ef dbcontext scaffold 
             */ 
            modelBuilder.HasPostgresExtension("tablefunc")
                .HasAnnotation("Relational:Collation", "en_US.UTF-8");

            modelBuilder.Entity<ActivityGroup>(entity =>
            {
                entity.HasKey(e => e.Group);

                entity.Property(e => e.Group).HasMaxLength(100);

                entity.Property(e => e.Inhabitant).HasMaxLength(500);

                entity.Property(e => e.Since).HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<AuthService>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Customer });

                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.CallToAction).HasMaxLength(1000);

                entity.Property(e => e.ChildAuthService).HasMaxLength(500);

                entity.Property(e => e.Description).HasMaxLength(4000);

                entity.Property(e => e.Label).HasMaxLength(1000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(250);

                entity.Property(e => e.PageDescription).HasMaxLength(4000);

                entity.Property(e => e.PageLabel).HasMaxLength(1000);

                entity.Property(e => e.Profile)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.RoleProvider).HasMaxLength(500);

                entity.Property(e => e.Ttl).HasColumnName("TTL");
            });

            modelBuilder.Entity<AuthToken>(entity =>
            {
                entity.HasIndex(e => e.BearerToken, "IX_AuthTokens_BearerToken");

                entity.HasIndex(e => e.CookieId, "IX_AuthTokens_CookieId");

                entity.Property(e => e.Id).HasMaxLength(100);

                entity.Property(e => e.BearerToken)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CookieId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Created).HasColumnType("timestamp with time zone");

                entity.Property(e => e.Expires).HasColumnType("timestamp with time zone");

                entity.Property(e => e.LastChecked).HasColumnType("timestamp with time zone");

                entity.Property(e => e.SessionUserId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Ttl).HasColumnName("TTL");
            });

            modelBuilder.Entity<Batch>(entity =>
            {
                entity.HasIndex(e => new { e.Customer, e.Superseded, e.Submitted }, "IX_BatchTest");

                entity.Property(e => e.Id).HasDefaultValueSql("nextval('batch_id_sequence'::regclass)");

                entity.Property(e => e.Finished).HasColumnType("timestamp with time zone");

                entity.Property(e => e.Submitted).HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<CustomHeader>(entity =>
            {
                entity.HasIndex(e => new { e.Customer, e.Space }, "IX_CustomHeaders_ByCustomerSpace");

                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Role)
                    .HasMaxLength(500)
                    .HasDefaultValueSql("NULL::character varying");

                entity.Property(e => e.Value)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Created).HasColumnType("timestamp with time zone");

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Keys)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<CustomerImageServer>(entity =>
            {
                entity.HasKey(e => e.Customer);

                entity.Property(e => e.Customer).ValueGeneratedNever();

                entity.Property(e => e.ImageServer)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<CustomerOriginStrategy>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Credentials)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Regex)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Strategy)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<CustomerStorage>(entity =>
            {
                entity.HasKey(e => new { e.Customer, e.Space });

                entity.ToTable("CustomerStorage");

                entity.Property(e => e.LastCalculated).HasColumnType("timestamp with time zone");

                entity.Property(e => e.StoragePolicy).HasMaxLength(500);
            });

            modelBuilder.Entity<EntityCounter>(entity =>
            {
                entity.HasKey(e => new { e.Type, e.Scope, e.Customer });

                entity.Property(e => e.Type).HasMaxLength(100);

                entity.Property(e => e.Scope).HasMaxLength(100);
            });

            modelBuilder.Entity<Asset>().ToTable("Images");
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasIndex(e => e.Batch, "IX_ImagesByBatch");

                entity.HasIndex(e => new { e.Id, e.Customer, e.Space }, "IX_ImagesByCustomerSpace");

                entity.HasIndex(e => new { e.Id, e.Customer, e.Error, e.Batch }, "IX_ImagesByErrors")
                    .HasFilter("((\"Error\" IS NOT NULL) AND ((\"Error\")::text <> ''::text))");

                entity.HasIndex(e => e.Reference1, "IX_ImagesByReference1");

                entity.HasIndex(e => e.Reference2, "IX_ImagesByReference2");

                entity.HasIndex(e => e.Reference3, "IX_ImagesByReference3");

                entity.HasIndex(e => new { e.Id, e.Customer, e.Space, e.Batch }, "IX_ImagesBySpace_NotWellcome")
                    .HasFilter("(\"Customer\" <> 2)");

                entity.HasIndex(e => new { e.Customer, e.Space }, "IX_ImagesBySpace_NotWellcomeSpace1")
                    .HasFilter("((\"Customer\" <> 2) OR ((\"Customer\" = 2) AND (\"Space\" <> 1)))");

                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Created).HasColumnType("timestamp with time zone");

                entity.Property(e => e.Duration).HasDefaultValueSql("0");

                entity.Property(e => e.Error)
                    .HasMaxLength(1000)
                    .HasDefaultValueSql("NULL::character varying");

                entity.Property(e => e.Family)
                    .IsRequired()
                    .HasColumnType("char")
                    .HasDefaultValueSql("'I'::\"char\"");

                entity.Property(e => e.Finished).HasColumnType("timestamp with time zone");

                entity.Property(e => e.ImageOptimisationPolicy)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("'fast-lossy'::character varying");

                entity.Property(e => e.MediaType)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasDefaultValueSql("'image/jp2'::character varying");

                entity.Property(e => e.Origin)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.PreservedUri)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Reference1)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Reference2)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Reference3)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Roles)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Tags)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.ThumbnailPolicy)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("'original'::character varying");
            });

            modelBuilder.Entity<ImageLocation>(entity =>
            {
                entity.ToTable("ImageLocation");

                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Nas)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.S3)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<ImageOptimisationPolicy>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.TechnicalDetails)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<ImageServer>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.InfoJsonTemplate).HasMaxLength(500);
            });

            modelBuilder.Entity<ImageStorage>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Customer, e.Space });

                entity.ToTable("ImageStorage");

                entity.HasIndex(e => new { e.Customer, e.Space, e.Id }, "IX_ImageStorageByCustomerSpace");

                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.LastChecked).HasColumnType("timestamp with time zone");
            });

            modelBuilder.Entity<InfoJsonTemplate>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Template)
                    .IsRequired()
                    .HasMaxLength(4000);
            });

            modelBuilder.Entity<MetricThreshold>(entity =>
            {
                entity.HasKey(e => new { e.Name, e.Metric });

                entity.Property(e => e.Name).HasMaxLength(500);

                entity.Property(e => e.Metric).HasMaxLength(500);
            });

            modelBuilder.Entity<NamedQuery>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Template)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<OriginStrategy>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);
            });

            modelBuilder.Entity<Queue>(entity =>
            {
                entity.HasKey(e => e.Customer)
                    .HasName("Queues_pkey");

                entity.Property(e => e.Customer).ValueGeneratedNever();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValueSql("'default'::character varying");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Customer });

                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Aliases).HasMaxLength(1000);

                entity.Property(e => e.AuthService)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);
            });

            modelBuilder.Entity<RoleProvider>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.AuthService)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Configuration).HasMaxLength(4000);

                entity.Property(e => e.Credentials).HasMaxLength(4000);
            });

            modelBuilder.Entity<SessionUser>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(100);

                entity.Property(e => e.Created).HasColumnType("timestamp with time zone");

                entity.Property(e => e.Roles).HasMaxLength(4000);
            });

            modelBuilder.Entity<Space>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Customer })
                    .HasName("Spaces_pkey");

                entity.Property(e => e.Created).HasColumnType("timestamp with time zone");

                entity.Property(e => e.ImageBucket)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Roles)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Tags)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<StoragePolicy>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);
            });

            modelBuilder.Entity<ThumbnailPolicy>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Sizes)
                    .IsRequired()
                    .HasMaxLength(1000);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(500);

                entity.Property(e => e.Created).HasColumnType("timestamp with time zone");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.EncryptedPassword)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Roles)
                    .IsRequired()
                    .HasMaxLength(1000);
            });
            
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
