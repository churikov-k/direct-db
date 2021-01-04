using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SomeNamespace.Models;

namespace SomeNamespace.Services
{
    public interface IDbUtilsService
    {
        Task<Result<bool>> ExecuteNonQuery(string connString, string query, SqlParameter parameter = null, bool useTransaction = true);
        Task<Result<string>> ExecuteScalar(string connString, string query);
        Task<List<T>> GetListFromDb<T>(string connString, string query, Func<SqlDataReader, T> mapper);
    }
    
    public class DbUtilsService : IDbUtilsService
    {
        private readonly ILogger<DbUtilsService> _logger;

        public DbUtilsService(ILogger<DbUtilsService> logger)
        {
            _logger = logger;
        }
        
        public async Task<Result<bool>> ExecuteNonQuery(string connString, string query, SqlParameter parameter = null, bool useTransaction = true)
        {
            var retVal = new Result<bool>();
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            SqlTransaction transaction = null;
            
            if(useTransaction) transaction = conn.BeginTransaction();

            try
            {
                // to run scripts with GO
                string[] splitter = {"\r\nGO\r\n"};
                var cmdTexts = query.Split(splitter,
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (var cmdText in cmdTexts)
                {
                    var cmd = new SqlCommand(cmdText, conn) {Transaction = transaction};
                    if (parameter != null && cmdText.Contains(parameter.ParameterName))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.Add(parameter);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }

                // Attempt to commit the transaction.
                if (useTransaction) transaction.Commit();
            }
            catch (SqlException e)
            {
                _logger.LogError(
                    $"Failed to ExecuteNonQuery(). Connection string: {connString}. Query: {query}. Exception: {e.Message}");
                retVal.IsOk = false;
                retVal.ErrorMessage = e.Message;
                if (e.Message.ToLower().StartsWith("the delete statement conflicted with the reference constraint"))
                {
                    retVal.ErrorMessage = "The selected object is in use.";
                }
                // Attempt to roll back the transaction.
                try
                {
                    if(useTransaction) transaction.Rollback();
                }
                catch (Exception e2)
                {
                    // This catch block will handle any errors that may have occurred
                    // on the server that would cause the rollback to fail, such as
                    // a closed connection.
                    _logger.LogError(
                        $"Rollback Exception Type: {e2.GetType()}. Connection string: {connString}. Query: {query}. Exception: {e2.Message}");
                    retVal.IsOk = false;
                    retVal.ErrorMessage = e2.Message;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"Failed to ExecuteNonQuery(). Connection string: {connString}. Query: {query}. Exception: {e.Message}");
                retVal.IsOk = false;
                retVal.ErrorMessage = e.Message;
                // Attempt to roll back the transaction.
                try
                {
                    if(useTransaction) transaction.Rollback();
                }
                catch (Exception e2)
                {
                    // This catch block will handle any errors that may have occurred
                    // on the server that would cause the rollback to fail, such as
                    // a closed connection.
                    _logger.LogError(
                        $"Rollback Exception Type: {e2.GetType()}. Connection string: {connString}. Query: {query}. Exception: {e2.Message}");
                    retVal.IsOk = false;
                    retVal.ErrorMessage = e2.Message;
                }
            }

            return retVal;
        }

        public async Task<Result<string>> ExecuteScalar(string connString, string query)
        {
            var retVal = new Result<string>();
            try
            {
                await using var conn = new SqlConnection(connString);
                await conn.OpenAsync();
                var cmd = new SqlCommand(query, conn);
                var reader = await cmd.ExecuteScalarAsync();
                if (reader != null)
                {
                    retVal.Data = reader.ToString();
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to ExecuteScalar(). Connection string: {connString}. Exception: {e.Message}");
                retVal.IsOk = false;
                retVal.ErrorMessage = e.Message;
            }

            return retVal;
        }
        
        public async Task<List<T>> GetListFromDb<T>(string connString, string query, Func<SqlDataReader, T> mapper)
        {
            var retVal = new List<T>();
            try
            {
                await using var conn = new SqlConnection(connString);
                await conn.OpenAsync();
                
                var cmd = new SqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (reader.Read())
                {
                    retVal.Add(mapper(reader));
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to GetDbReader. Connection string: {connString}. Query: {query}. Exception: {e.Message}");
                throw new Exception(e.Message, e);
            }

            return retVal;
        }
    }
}