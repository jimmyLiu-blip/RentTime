using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RentProject.Api.Contracts;
using RentProject.Domain;
using RentProject.Service;

// 什麼時候用 IActionResult：當你的 API 只需要回傳狀態（不一定有資料）或回傳型別可能很多種
// 什麼時候用 ActionResult<T>：當你想表達「成功時會回傳一個 T，但失敗時也可能回 404/400 等等」
// [FromQuery]：指的是 URL 後面 ?key=value&... 那段
// [FromBody] ：Body 指的是 HTTP request 的內容本體（通常是 JSON）。

namespace RentProject.Api.Controllers
{
    [Route("api/renttimes")]
    [ApiController]
    public class RentTimesController : ControllerBase
    {
        private readonly RentTimeService _rentTimeService;

        public RentTimesController(RentTimeService rentTimeService)
        { 
            _rentTimeService = rentTimeService;
        }

        [HttpGet("project-view")]
        public ActionResult<List<RentTime>> GetProjectViewList()
        { 
            var list = _rentTimeService.GetProjectViewList();
            return Ok(list);
        }

        // 讀單
        [HttpGet("{id:int}")]
        public ActionResult<RentTime> GetRentTimeById(int id)
        {
            var rentTime = _rentTimeService.GetRentTimeById(id);
            return Ok(rentTime);
        }

        // 新增
        [HttpPost]
        public ActionResult<CreateRentTimeResult> CreatRentTime([FromBody] RentTime model, [FromQuery] long? bookingBatchId = null)
        {
            var result = _rentTimeService.CreateRentTime(model, bookingBatchId);
            return Ok(result);
        }

        // 更新：user 用 query 最直覺（也可以改 body，但你現在 UI 比較好接 query）
        [HttpPut("{id:int}")]
        public IActionResult UpdateRentTimeById(int id, [FromBody] RentTime model, [FromQuery] string user)
        { 
            model.RentTimeId = id;
            _rentTimeService.UpdateRentTimeById(model, user);
            return NoContent();
        }

        // 開始 / 完成 / 回草稿（都吃 body 的 UserRequest）
        [HttpPost("{id:int}/start")]
        public IActionResult StartRentTime(int id, [FromBody] UserRequest req)
        {
            _rentTimeService.StartRentTimeById(id, req.User);
            return NoContent();
        }

        [HttpPost("{id:int}/finish")]
        public IActionResult Finish(int id, [FromBody] UserRequest req)
        {
            _rentTimeService.FinishRentTimeById(id, req.User);
            return NoContent();
        }

        [HttpPost("{id:int}/restore")]
        public IActionResult Restore(int id, [FromBody] UserRequest req)
        {
            _rentTimeService.RestoreToDraftById(id, req.User);
            return NoContent();
        }

        // 刪除：DELETE /api/renttimes/{id}?user=Jimmy
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id, [FromQuery] string user)
        {
            _rentTimeService.DeletedRentTime(id, user, DateTime.Now);
            return NoContent();
        }

        // 送出給助理：POST /api/renttimes/{id}/submit
        [HttpPost("{id:int}/submit")]
        public IActionResult SubmitToAssistant(int id, [FromBody] UserRequest req)
        {
            _rentTimeService.SubmitToAssistantById(id, req.User);
            return NoContent();
        }

        // 拖拉改期：POST /api/renttimes/{id}/change-period
        [HttpPost("{id:int}/change-period")]
        public ActionResult<bool> ChangedPeriod(int id, [FromBody] ChangePeriodRequest req)
        {
            var ok = _rentTimeService.ChangeDraftPeriodWithSplit(
                id, req.NewStart, req.NewEnd, req.User, DateTime.Now);

            return Ok(ok);
        }

        // 複製
        [HttpPost("{id:int}/copy")]
        public ActionResult<CreateRentTimeResult> Copy(int id, [FromBody] CopyRequest req)
        {
            var result = _rentTimeService.CopyRentTime(id, req.IsHandOver, req.User);
            return Ok(result);
        }

        // 回傳一個新 BookingBatchId
        [HttpPost("booking-batch")]
        public ActionResult<long> CreateBookingBatch()
        {
            // 這會呼叫你原本 Dapper 的 CreateBookingBatch()
            var batchId = _rentTimeService.CreateBookingBatch();
            return Ok(batchId);
        }

    }
}
