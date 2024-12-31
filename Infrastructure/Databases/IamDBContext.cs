﻿using System.Linq.Expressions;
using DotNetService.Models;
using Microsoft.EntityFrameworkCore;

namespace DotNetService.Infrastructure.Databases
{
    public partial class IamDBContext(DbContextOptions<IamDBContext> options) : DbContext(options)
    {
        public DbSet<Role> Roles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<UserRole> UserRoles { get; set; }

        public DbSet<RolePermission> RolePermissions { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set default max length column for string data type
            modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetProperties())
                .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() == null)
                .ToList()
                .ForEach(p => p.SetMaxLength(255));

            GenerateUuid<Role>(modelBuilder, "Id");
            SoftDelete<Role>(modelBuilder);
            GenerateUuid<User>(modelBuilder, "Id");
            SoftDelete<User>(modelBuilder);
            GenerateUuid<Permission>(modelBuilder, "Id");
            SoftDelete<Permission>(modelBuilder);
            GenerateUuid<UserRole>(modelBuilder, "Id");
            SoftDelete<UserRole>(modelBuilder);
            GenerateUuid<RolePermission>(modelBuilder, "Id");
            SoftDelete<RolePermission>(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Key)
                .IsUnique();

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Key)
                .IsUnique();

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead)
                .HasDefaultValue(false);
        }

        public override int SaveChanges()
        {
            var currentTime = DateTime.Now;

            var entries = ChangeTracker
                .Entries<BaseModel>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var entity = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = currentTime;
                    entity.UpdatedAt = currentTime;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = currentTime;
                    entry.Property(nameof(entity.CreatedAt)).IsModified = false;
                }
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentTime = DateTime.Now;

            var entries = ChangeTracker
                .Entries<BaseModel>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var entity = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = currentTime;
                    entity.UpdatedAt = currentTime;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entity.UpdatedAt = currentTime;
                    entry.Property(nameof(entity.CreatedAt)).IsModified = false;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        /*=================================== Service Support ===========================================*/

        private static void GenerateUuid<T>(ModelBuilder modelBuilder, string column) where T : class
        {
            modelBuilder.Entity<T>()
                .HasIndex(CreateExpression<T>(column));

            modelBuilder.Entity<T>()
                .Property(CreateExpression<T>(column))
                .HasDefaultValueSql("NEWID()");
        }

        private static void SoftDelete<T>(ModelBuilder modelBuilder) where T : class
        {
            modelBuilder.Entity<T>()
                .HasQueryFilter(u => EF.Property<DateTime?>(u, "DeletedAt") == null);
        }

        private static Expression<Func<T, object>> CreateExpression<T>(string uuid) where T : class
        {
            var type = typeof(T);
            var property = type.GetProperty(uuid);
            var parameter = Expression.Parameter(type);
            var access = Expression.Property(parameter, property);
            var convert = Expression.Convert(access, typeof(object));
            var function = Expression.Lambda<Func<T, object>>(convert, parameter);

            return function;
        }
    }
}
