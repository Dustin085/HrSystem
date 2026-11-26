using Attendance.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Core
{
    [ApiController]
    [Route("api/leave-applications")]
    public sealed class LeaveApplicationsController : ControllerBase
    {
        private readonly LeaveApplicationService _leaveApplicationService;

        public LeaveApplicationsController(LeaveApplicationService leaveApplicationService)
        {
            _leaveApplicationService = leaveApplicationService;
        }

        /// <summary>建立請假申請的草稿。</summary>
        [HttpPost]
        [ProducesResponseType<LeaveApplicationDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LeaveApplicationDto>> CreateLeaveApplication(CreateLeaveApplicationRequest request)
        {
            var result = await _leaveApplicationService.CreateAsync(request);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetLeaveApplication), new { id = result.Value.Id }, result.Value);
            }

            // 建立時使用者可以修改表單，所以錯誤都放在需要修改的欄位上
            return result.Error.Value switch
            {
                CreateLeaveApplicationError.UnknownLeaveType =>
                    FieldError(nameof(request.Type), "無效的假別。"),

                CreateLeaveApplicationError.InvalidDateRange =>
                    FieldError(nameof(request.End), "結束日不能早於開始日。"),

                CreateLeaveApplicationError.ExceedMaxPersonalLeaveDays =>
                    FieldError(nameof(request.End), $"事假最多 {LeaveApplicationConstraints.MaxPersonalLeaveDays} 天。"),

                CreateLeaveApplicationError.PersonalLeaveWithoutReason =>
                    FieldError(nameof(request.Reason), "事假必須填寫請假理由。"),

                CreateLeaveApplicationError.EmployeeNotFound =>
                    FieldError(nameof(request.EmployeeId), "找不到此員工。"),

                CreateLeaveApplicationError.EmployeeNotEmployedOnDateRange =>
                    FieldError(nameof(request.End), "請假期間內有此員工不在職的日子（尚未到職或已離職）。"),

                _ => throw new InvalidOperationException($"未處理的錯誤：{result.Error}"),
            };
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<LeaveApplicationDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LeaveApplicationDto>> GetLeaveApplication([FromRoute] Guid id)
        {
            var leave = await _leaveApplicationService.GetByIdAsync(id);
            return leave is null ? NotFound() : leave;
        }

        /// <summary>送出請假申請（草稿或已退回 → 待審核）。</summary>
        [HttpPost("{id:guid}/submit")]
        [ProducesResponseType<LeaveApplicationDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<LeaveApplicationDto>> SubmitLeaveApplication([FromRoute] Guid id)
        {
            var result = await _leaveApplicationService.SubmitAsync(id);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            // 送出沒有表單欄位，所以用 Problem() 說明原因
            return result.Error.Value switch
            {
                // 和 GetLeaveApplication 一致：404 不帶內容
                SubmitLeaveApplicationError.LeaveApplicationNotFound => NotFound(),

                SubmitLeaveApplicationError.InvalidStatus =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "請假申請無法送出",
                        detail: "只有草稿或已退回的請假申請可以送出。"),

                SubmitLeaveApplicationError.Overlapping =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "請假期間重疊",
                        detail: "此員工在這段期間已有待審核或已核准的請假申請。"),

                SubmitLeaveApplicationError.EmployeeNotEmployedOnDateRange =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "員工不在職",
                        detail: "請假期間內有此員工不在職的日子（尚未到職或已離職）。"),

                // 不用 404：網址指的是請假申請，而申請本身存在，只是對應的員工找不到（資料不一致）
                SubmitLeaveApplicationError.EmployeeNotFound =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "找不到申請人",
                        detail: "找不到此請假申請的申請人。"),

                _ => throw new InvalidOperationException($"未處理的錯誤：{result.Error}"),
            };
        }

        /// <summary>回傳和 [Required] 驗證失敗相同格式的 400，前端可以用同一套方式顯示欄位錯誤。</summary>
        private ActionResult FieldError(string field, string message)
        {
            ModelState.AddModelError(field, message);
            return ValidationProblem(ModelState);
        }
    }
}
