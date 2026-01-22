using Microsoft.AspNetCore.Mvc;
using RentProject.Api.Contracts;
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

        // GET /api/jobno/JOB123 (抓外部API + 存DB + 回傳）
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

        // GET /api/jobno/active?top=8 （下拉清單）
        [HttpGet("active")]
        public async Task<ActionResult<List<string>>> GetActiveJobNo([FromQuery] int top = 8, CancellationToken ct = default)
        {
            var list = await _jobNoService.GetActiveJobNosAsync(top, ct);

             return Ok(list);
        }

        [HttpPost("id")]
        public ActionResult<int> GetOrCreateJobId([FromBody] JobNoIdRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.JobNo))
                return BadRequest("jobNo不可為空");

            var id = _jobNoService.GetOrCreateJobId(req.JobNo);
            return Ok(id);
        }
    }
}
