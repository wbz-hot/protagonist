﻿using System;
using System.Threading.Tasks;
using DLCS.Model.Assets;
using DLCS.Repository.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Test.Helpers.Integration
{
    public static class DatabaseTestDataPopulation
    {
        public static ValueTask<EntityEntry<Asset>> AddTestAsset(this DbSet<Asset> assets,
            string id,
            AssetFamily family = AssetFamily.Image,
            int customer = 99,
            int space = 1,
            string origin = "",
            string roles = "",
            string mediaType = "image/jpeg",
            int maxUnauthorised = 0,
            int width = 8000,
            int height = 8000)
            => assets.AddAsync(new Asset
            {
                Created = DateTime.Now, Customer = customer, Space = space, Id = id, Origin = origin,
                Width = width, Height = height, Roles = roles, Family = family, MediaType = mediaType,
                ThumbnailPolicy = "default", MaxUnauthorised = maxUnauthorised
            });

        public static ValueTask<EntityEntry<AuthToken>> AddTestToken(this DbSet<AuthToken> authTokens,
            int customer = 99, int ttl = 100, DateTime? expires = null, string? sessionUserId = null,
            DateTime? lastChecked = null)
            => authTokens.AddAsync(
                new AuthToken
                {
                    Id = Guid.NewGuid().ToString(),
                    Created = DateTime.Now,
                    LastChecked = lastChecked ?? DateTime.Now,
                    CookieId = Guid.NewGuid().ToString(),
                    SessionUserId = sessionUserId ?? Guid.NewGuid().ToString(),
                    BearerToken = Guid.NewGuid().ToString().Replace("-", string.Empty),
                    Customer = customer,
                    Expires = expires ?? DateTime.Now.AddSeconds(ttl),
                    Ttl = ttl
                });
    }
}