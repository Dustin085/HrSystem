using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organization.Contracts;

namespace Organization.Core
{
    [ApiController]
    [Route("api/employees")]
    public sealed class EmployeesController : ControllerBase
    {
        private readonly EmployeeService _employeeService;

        public EmployeesController(EmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpPost]
        [ProducesResponseType<EmployeeDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee(CreateEmployeeRequest request)
        {
            var employee = await _employeeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeDto>> GetEmployee([FromRoute] Guid id)
        {
            var employee = await _employeeService.GetByIdAsync(id);
            return employee is null ? NotFound() : employee;
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyList<EmployeeDto>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> ListEmployees()
        {
            return Ok(await _employeeService.ListAsync());
        }

        [HttpPut("{id:guid}/name")]
        [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeDto>> RenameEmployee(
            [FromRoute] Guid id,
            [FromBody] RenameEmployeeRequest request)
        {
            var employee = await _employeeService.RenameAsync(id, request.Name);
            return employee is null ? NotFound() : employee;
        }

        [HttpPost("{id:guid}/termination")]
        [ProducesResponseType<EmployeeDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EmployeeDto>> TerminateEmployee(
            [FromRoute] Guid id,
            [FromBody] TerminateEmployeeRequest request)
        {
            // TerminationDate 已由 [Required] 在進到這裡之前驗證過
            var result = await _employeeService.TerminateAsync(id, request.TerminationDate!.Value);

            return result.Error switch
            {
                null => Ok(result.Value),

                // 和 GetEmployee 一致：404 不帶內容
                TerminateEmployeeError.EmployeeNotFound => NotFound(),

                // 用 Problem() 回傳 ProblemDetails（JSON），和 swagger 宣告的格式一致，
                // 前端的 toErrorMessages 才能讀到 detail。Conflict("字串") 會回傳 text/plain。
                TerminateEmployeeError.AlreadyTerminated =>
                    Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "員工已離職",
                        detail: "此員工已經離職，不能再次離職。"),

                TerminateEmployeeError.TerminationDateBeforeHireDate =>
                    FieldError(nameof(request.TerminationDate), "離職日不可以早於到職日。"),

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
