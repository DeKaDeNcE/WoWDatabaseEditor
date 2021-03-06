﻿using System;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase.Database
{
    [AutoRegister]
    [SingleInstance]
    public class MySqlExecutor : IMySqlExecutor
    {
        private readonly MySqlSettings settings;

        public MySqlExecutor(IConnectionSettingsProvider connectionSettingsProvider)
        {
            settings = new MySqlSettings(connectionSettingsProvider.GetSettings());
        }

        public async Task ExecuteSql(string query)
        {
            using var writeLock = await MySqlSingleWriteLock.WriteLock();
            
            string connStr = settings.ConnectionStrings.First().ConnectionString;
            MySqlConnection conn = new(connStr);
            MySqlTransaction transaction;
            try
            {
                conn.Open();
                transaction = await conn.BeginTransactionAsync();
            }
            catch (Exception e)
            {
                throw new IMySqlExecutor.CannotConnectToDatabaseException(e);
            }

            try
            {
                MySqlCommand cmd = new(query, conn, transaction);
                await cmd.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(ex);
            }

            await conn.CloseAsync();
        }
    }
}