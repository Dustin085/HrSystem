using Attendance.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Attendance.Core
{
    [ApiController]
    [Route("api/shift-assignments")]
    public sealed class ShiftAssignmentsController : ControllerBase
    {
        private readonly ShiftAssignmentService _shiftAssignmentService;
        private readonly IOptions<AttendanceOptions> _attendanceOptions;

        public ShiftAssignmentsController(ShiftAssignmentService shiftAssignmentService,
            IOptions<AttendanceOptions> attendanceOptions)
        {
            _shiftAssignmentService = shiftAssignmentService;
            _attendanceOptions = attendanceOptions;
        }

        [HttpPost]
        [ProducesResponseType<ShiftAssignmentDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ShiftAssignmentDto>> CreateShiftAssignment(CreateShiftAssignmentRequest request)
        {
            var result = await _shiftAssignmentService.CreateAsync(request);

            return result.Error switch
            {
                null => CreatedAtAction(nameof(GetShiftAssignment), new { id = result.Value!.Id }, result.Value),

                CreateShiftAssignmentError.InvalidTimeRange =>
                    FieldError(nameof(request.EndTime), "下班時間必須晚於上班時間。"),

                CreateShiftAssignmentError.EmployeeNotFound =>
                    FieldError(nameof(request.EmployeeId), "找不到此員工。"),

                CreateShiftAssignmentError.EmployeeNotEmployedOnDate =>
                    FieldError(nameof(request.Date), "排班日當天此員工不在職（尚未到職或已離職）。"),

                CreateShiftAssignmentError.ExceedMaxShiftDuration =>
                    FieldError(nameof(request.EndTime),
                    "超過最長單次排班時間: " + _attendanceOptions.Value.MaxShiftDuration.TotalHours + " 小時"),

                CreateShiftAssignmentError.Overlapping =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "排班時段重疊",
                        detail: "此員工當天已有與這個時段重疊的班。"),

                _ => throw new InvalidOperationException($"未處理的錯誤：{result.Error}"),
            };
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<ShiftAssignmentDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ShiftAssignmentDto>> GetShiftAssignment([FromRoute] Guid id)
        {
            var assignment = await _shiftAssignmentService.GetByIdAsync(id);
            return assignment is null ? NotFound() : assignment;
        }

        /// <summary>查詢排班，所有條件都可省略；日期區間包含頭尾。</summary>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<ShiftAssignmentDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<ShiftAssignmentDto>>> ListShiftAssignments(
            [FromQuery] Guid? employeeId,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to)
        {
            if (from is not null && to is not null && from > to)
            {
                return FieldError(nameof(to), "結束日期不可早於開始日期。");
            }

            var assignments = await _shiftAssignmentService.SearchAsync(employeeId, from, to);
            return Ok(assignments);
        }

        /// <summary>回傳和 [Required] 驗證失敗相同格式的 400，前端可以用同一套方式顯示欄位錯誤。</summary>
        private ActionResult FieldError(string field, string message)
        {
            ModelState.AddModelError(field, message);
            return ValidationProblem(ModelState);
        }
    }
}
