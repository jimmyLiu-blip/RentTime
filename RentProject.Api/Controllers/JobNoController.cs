using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentProject.Service;

namespace RentProject.Api.Controllers
{
    [Route("api/jobno")]
    [ApiController]
    public class JobNoController : ControllerBase
    {
        private readonly JobNoService _jobNoService;

        public JobNoController(JobNoService jobNoService)
        { 
            _jobNoService = jobNoService;
        }

        // GET /api/jobno/JOB123
        [HttpGet("{jobNo}")]
        public async Task<IActionResult> GetByJobNo(string jobNo, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(jobNo))
                return BadRequest("jobNo 不可為空");

            // 外部 API 當真相：抓回來後 Upsert 到 DB，再回傳 DB 版本
            var data = await _jobNoService.GetJobNoMasterFromApiAndSaveAsync(jobNo, ct);

            if (data == null)
                return NotFound();

            return Ok(data);
        }
    }
}
