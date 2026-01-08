using Dapper;
using Microsoft.Data.SqlClient;
using RentProject.UIModels;

namespace RentProject.Repository
{
    public class DapperProjectRepository
    {
        private readonly string _connectionString;

        public DapperProjectRepository(string connectionString)
        { 
            _connectionString = connectionString;
        }

        public List<ProjectItem> GetActiveProjects()
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            var selectSql = @"SELECT jm.JobNo, p.ProjectNo, p.ProjectName, p.PE
                              FROM dbo.Projects p
                              LEFT JOIN dbo.JobNoMaster jm ON jm.JobId = p.JobId 
                              WHERE p.IsActive = 1
                              AND (jm.IsActive IS NULL OR jm.IsActive = 1)
                              ORDER BY ProjectNo;";

            return connection.Query<ProjectItem>(selectSql).ToList();
        }

    }
}
