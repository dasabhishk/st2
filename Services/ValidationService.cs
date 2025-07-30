using CMMT.dao;
using CMMT.Helpers;
using CMMT.Models;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.Json;

namespace CMMT.Services
{
    internal class ValidationService
    {
        int returnStatusCode = -1;
        private string pathForStoredProcedure;

        public async Task<int> RunValidation(string csvType, DBLayer db, IProgress<(int percent, string message)>? progress = null)
        {
            LoggingService.LogInfo("Validate button clicked");
            progress?.Report((100, "Validation in progress..."));
            try
            {
                LoggingService.LogInfo($"Validation service invoked", false);
                pathForStoredProcedure = ConfigFileHelper.GetConfigFilePath("Configuration", "SQLProcedure.json");

                /* checking if there are any rows present in series and study table
                 *  if no rows present, then unnecessary overhead of executing procedure
                 * will be prevented */

                var countRowsStudyTable = Convert.ToInt32(
                    db.ExecuteScalar_Query(
                       @"SELECT COUNT(*)
                       FROM [cmmt].[cmmt_PatientStudyMetaData]
                       WHERE Status IS NULL
                       OR Status = ''"
                    ));

                var countRowsSeriesTable = Convert.ToInt32(
                    db.ExecuteScalar_Query(
                       @"SELECT COUNT(*)
                       FROM [cmmt].[cmmt_PatientStudySeriesData]
                       WHERE Status IS NULL
                       OR Status = ''"
                    ));

                if (countRowsStudyTable == 0 && countRowsSeriesTable == 0)
                {
                    LoggingService.LogInfo($"No records for validation", true);
                    return returnStatusCode = -1;
                }

                // Load JSON config of all .sql scripts

                if (!File.Exists(pathForStoredProcedure))
                {
                    LoggingService.LogError($"Procedure JSON not found at '{pathForStoredProcedure}'", null);
                    return returnStatusCode = -1;
                }
                var sqlScripts = await File.ReadAllTextAsync(pathForStoredProcedure); //Reading all the scripts
                var SQLConfig = JsonSerializer.Deserialize<SqlProcedureConfig>(sqlScripts)
                    ?? throw new InvalidOperationException("Failed to parse SQLProcedure.json");
                string? masterProcedureName = "cmmt." + Path.GetFileNameWithoutExtension(SQLConfig?.MasterProcedure);

                var allFiles = SQLConfig.Procedures.Keys;
                var procedureWithoutExtension = allFiles // all files without extension
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToList();

                //checking if procedures already exists, if not, then deploy the procedures

                int existingCount = CheckAndDeployProcedures(db, procedureWithoutExtension);

                if (existingCount == -1)
                {
                    LoggingService.LogError("Error checking existing procedures, cannot proceed with validation.", null);
                    return returnStatusCode = -1;
                }

                if (existingCount < procedureWithoutExtension.Count)
                {
                    LoggingService.LogInfo($"Some procedures are missing, deploying all procedures first", false);
                    await DatabaseService.ProcedureCreationFromJson(db, pathForStoredProcedure, SQLConfig);
                }

                //adding input and output parameters when calling stored procedure
                var parms = new DBParameters();
                parms.Add("@ReturnStatus", 0, ParameterDirection.Output, SqlDbType.Int);
                parms.Add("@CsvType", csvType, ParameterDirection.Input, SqlDbType.NVarChar);

                object returnStatusObj;
                db.Execute_SP(
                    masterProcedureName,
                    parms,
                    out returnStatusObj
                );

                // getting the value back from stored procedure
                returnStatusCode = Convert.ToInt32(returnStatusObj);
                LoggingService.LogInfo($"Stored procedure completed with status = {returnStatusCode}");
            }

            catch (SqlException sqlEx)
            {
                LoggingService.LogError($"SQL error executing validation procedure: {sqlEx.Message}", sqlEx);
                returnStatusCode = -3;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Unexpected error executing validation procedure: {ex.Message}", ex);
                returnStatusCode = -3;
            }
            finally
            {
                //Disconnect from db
                db.Disconnect();
            }
            return returnStatusCode;
        }

        internal static int CheckAndDeployProcedures(DBLayer db, List<string> procedureWithoutExtension)
        {
            var existingCount = -1;
            object result = null;
            try
            {
                // Check how many procedures already exist
                var procedureNames = string.Join("','", procedureWithoutExtension);
                result = db.ExecuteScalar_Query(
                    $"SELECT COUNT(*) FROM sys.procedures " +
                    $"WHERE SCHEMA_NAME(schema_id) = 'cmmt' " +
                    $"AND name IN ('{procedureNames}')"
                );
                if(result != null && result != DBNull.Value)
                {
                    existingCount = Convert.ToInt32(result);
                }
                else
                {
                    LoggingService.LogError("Unexpected NULL/DBNULL from COUNT(*) query", null);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Unexpected error when executing query for checking procedures: {ex.Message}", ex);
                return existingCount = -1;
            }

            return existingCount;
        }
    }
}
