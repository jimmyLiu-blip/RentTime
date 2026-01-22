using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentProject.Repository;

namespace RentProject.Api.Controllers
{
    [Route("api/health")]
    [ApiController]
    public sealed class HealthController : ControllerBase
    {
        private readonly DapperHealthRepository _healthRepository;

        public HealthController(DapperHealthRepository healthRepository)
        {
            _healthRepository = healthRepository;
        }

        [HttpGet]
        public IActionResult PingApi() => Ok("OK：WebAPI is running");

        [HttpGet("db")]
        public IActionResult PingDb()
        { 
            var (ok, msg) = _healthRepository.TestDbConnection();

            // DB OK -> 200
            if (ok) return Ok(msg);

            // DB 不 OK -> 503（代表服務目前不可用/依賴失敗）
            return StatusCode(StatusCodes.Status503ServiceUnavailable, msg);
        }
    }
}
