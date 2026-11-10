using Attendance.Contracts;
using Organization.Contracts;

namespace Attendance.Core
{
    public sealed class ShiftAssignmentService
    {
        private readonly InMemoryShiftAssignmentRepository _repository;
        // 跨模組只透過 Organization.Contracts 的介面查詢員工
        private readonly IEmployeeDirectory _employeeDirectory;

        public ShiftAssignmentService(
            InMemoryShiftAssignmentRepository repository,
            IEmployeeDirectory employeeDirectory)
        {
            _repository = repository;
            _employeeDirectory = employeeDirectory;
        }

        public async Task<CreateShiftAssignmentResult> CreateAsync(CreateShiftAssignmentRequest request)
        {
            // 這些欄位已由 [Required] 在進到這裡之前驗證過
            var employeeId = request.EmployeeId!.Value;
            var date = request.Date!.Value;
            var startTime = request.StartTime!.Value;
            var endTime = request.EndTime!.Value;

            if (!ShiftAssignment.IsValidTimeRange(startTime, endTime))
            {
                return CreateShiftAssignmentResult.Failure(CreateShiftAssignmentError.InvalidTimeRange);
            }

            if (!await _employeeDirectory.ExistsByIdAsync(employeeId))
            {
                return CreateShiftAssignmentResult.Failure(CreateShiftAssignmentError.EmployeeNotFound);
            }

            var assignment = new ShiftAssignment(employeeId, date, startTime, endTime);

            if (!_repository.TryAddWithoutOverlap(assignment))
            {
                return CreateShiftAssignmentResult.Failure(CreateShiftAssignmentError.Overlapping);
            }

            return CreateShiftAssignmentResult.Success(assignment.ToDto());
        }

        public Task<ShiftAssignmentDto?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_repository.Find(id)?.ToDto());
        }

        public Task<IReadOnlyList<ShiftAssignmentDto>> SearchAsync(Guid? employeeId, DateOnly? from, DateOnly? to)
        {
            IReadOnlyList<ShiftAssignmentDto> result = _repository
                .Search(employeeId, from, to)
                .Select(a => a.ToDto())
                .ToList();

            return Task.FromResult(result);
        }
    }
}
