using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organization.Contracts;

namespace Organization.Core
{
    [ApiController]
    [Route("api/employees")]
    public sealed class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
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
    }
}
