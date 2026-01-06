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

            var selectSql = @"SELECT ProjectNo, ProjectName, JobPM
                              FROM dbo.Projects
                              WHERE IsActive = 1
                              ORDER BY ProjectNo;";

            return connection.Query<ProjectItem>(selectSql).ToList();
        }
    }
}
