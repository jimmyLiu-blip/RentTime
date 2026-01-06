using RentProject.Repository;
using RentProject.UIModels;

namespace RentProject.Service
{
    public class ProjectService
    {
        private readonly DapperProjectRepository _projectRepo;

        public ProjectService(DapperProjectRepository projectRepo)
        {
            _projectRepo = projectRepo;
        }

        public List<ProjectItem> GetActiveProjects()
        { 
            return _projectRepo.GetActiveProjects();
        }
    }
}
