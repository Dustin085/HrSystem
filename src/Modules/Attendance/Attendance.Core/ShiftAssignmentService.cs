using Attendance.Contracts;
using Microsoft.Extensions.Options;
using Organization.Contracts;

namespace Attendance.Core
{
    public sealed class ShiftAssignmentService
    {
        private readonly InMemoryShiftAssignmentRepository _repository;
        // 跨模組只透過 Organization.Contracts 的介面查詢員工
        private readonly IEmployeeDirectory _employeeDirectory;
        private readonly IOptions<AttendanceOptions> _attendanceOptions;

        public ShiftAssignmentService(
            InMemoryShiftAssignmentRepository repository,
            IEmployeeDirectory employeeDirectory,
            IOptions<AttendanceOptions> attendanceOptions)
        {
            _repository = repository;
            _employeeDirectory = employeeDirectory;
            _attendanceOptions = attendanceOptions;
        }

        public async Task<Result<ShiftAssignmentDto, CreateShiftAssignmentError>> CreateAsync(CreateShiftAssignmentRequest request)
        {
            // 這些欄位已由 [Required] 在進到這裡之前驗證過
            var employeeId = request.EmployeeId!.Value;
            var date = request.Date!.Value;
            var startTime = request.StartTime!.Value;
            var endTime = request.EndTime!.Value;

            if (!TimeRange.TryCreate(startTime, endTime, out TimeRange timeRange))
            {
                return CreateShiftAssignmentError.InvalidTimeRange;
            }

            if (!IsWithinMaxShiftDuration(timeRange))
            {
                return CreateShiftAssignmentError.ExceedMaxShiftDuration;
            }

            var status = await _employeeDirectory.GetEmploymentStatusOnAsync(employeeId, date);
            switch (status)
            {
                case null:
                    return CreateShiftAssignmentError.EmployeeNotFound;
                case not EmploymentStatus.Active:
                    return CreateShiftAssignmentError.EmployeeNotEmployedOnDate;
            }

            var assignment = new ShiftAssignment(employeeId, date, timeRange);

            if (!_repository.TryAddWithoutOverlap(assignment))
            {
                return CreateShiftAssignmentError.Overlapping;
            }

            return assignment.ToDto();
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
        private bool IsWithinMaxShiftDuration(TimeRange timeRange)
        {
            return _attendanceOptions.Value.MaxShiftDuration >= timeRange.Duration;
        }
    }
}
