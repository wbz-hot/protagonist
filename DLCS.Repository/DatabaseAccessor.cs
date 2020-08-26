using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using DLCS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DLCS.Repository
{
    /// <summary>
    /// Helper base class for dealing with connections and executing db queries
    /// </summary>
    public class DatabaseAccessor
    {
        // TODO - could add Transaction handling here if need be
        private readonly IConfiguration configuration;
        private readonly ILogger<DatabaseAccessor> logger;
        private readonly IMapper mapper;

        public DatabaseAccessor(
            IConfiguration configuration,
            ILogger<DatabaseAccessor> logger,
            IMapper mapper)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.mapper = mapper;
        }

        /// <summary>
        /// Gets an open database connection
        /// </summary>
        public Task<NpgsqlConnection> GetOpenDbConnection()
            => DatabaseConnectionManager.GetOpenNpgSqlConnection(configuration);

        /// <summary>
        /// Maps model to entity type and execute provided SQL, using entity as params.
        /// </summary>
        public async Task<bool> MapAndExecute<TModel, TEntity>(TModel model, string sql,
            IDbTransaction? transaction = null)
            where TEntity : class, IEntity
        {
            try
            {
                var entity = mapper.Map<TEntity>(model);
                return await Execute(entity, sql, transaction);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating item of type {type}.", typeof(TEntity).Name);
                return false;
            }
        }

        /// <summary>
        /// Execute provided SQL using entity as params.
        /// </summary>
        public async Task<bool> Execute<T>(T? entity, string sql, IDbTransaction? transaction = null)
            where T : class, IEntity
        {
            if (entity == null) return true;
            try
            {
                entity.PrepareForDatabase();

                if (transaction != null)
                {
                    await transaction.Connection.ExecuteAsync(sql, entity, transaction);
                }
                else
                {
                    await using var connection = await GetOpenDbConnection();
                    await connection.ExecuteAsync(sql, entity);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inserting item of type {type}.", typeof(T).Name);
                return false;
            }
        }

        /// <summary>
        /// Use specified SQL to make SELECT request to db, mapping returned type.
        /// </summary>
        public async Task<TModel> SelectAndMap<TEntity, TModel>(string sql,
            object? param = null,
            IDbTransaction? transaction = null)
            where TEntity : class, IEntity
        {
            try
            {
                TEntity entity;
                if (transaction != null)
                {
                    entity = await transaction.Connection.QuerySingleOrDefaultAsync<TEntity>(sql, param, transaction);
                }
                else
                {
                    await using var connection = await GetOpenDbConnection();
                    entity = await connection.QuerySingleOrDefaultAsync<TEntity>(sql, param);
                }
                
                var model = mapper.Map<TModel>(entity);
                return model;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting item of type {type}.", typeof(TEntity).Name);
                throw;
            }
        }

        /// <summary>
        /// Use specified SQL to make SELECT request to db, mapping returned type to list.
        /// </summary>
        public async Task<List<TModel>> SelectAndMapList<TEntity, TModel>(string sql,
            object? param = null,
            IDbTransaction? transaction = null)
            where TEntity : class, IEntity
        {
            try
            {
                IEnumerable<TEntity> entities;
                if (transaction != null)
                {
                    entities = await transaction.Connection.QueryAsync<TEntity>(sql, param, transaction);
                }
                else
                {
                    await using var connection = await GetOpenDbConnection();
                    entities = await connection.QueryAsync<TEntity>(sql, param);
                }
                
                var model = mapper.Map<List<TModel>>(entities);
                return model;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting list of items of type {type}.", typeof(TEntity).Name);
                throw;
            }
        }
    }
}