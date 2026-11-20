using Attendance.Contracts;
using Organization.Contracts;

namespace Attendance.Core
{
    public sealed class LeaveApplicationService
    {
        private readonly InMemoryLeaveApplicationRepository _repository;
        // 跨模組只透過 Organization.Contracts 的介面查詢員工
        private readonly IEmployeeDirectory _employeeDirectory;

        public LeaveApplicationService(InMemoryLeaveApplicationRepository repository,
            IEmployeeDirectory employeeDirectory)
        {
            _repository = repository;
            _employeeDirectory = employeeDirectory;
        }

        /// <summary>
        /// 建立草稿。重疊檢查不在這裡做：規則只禁止和「待審核、已核准」的申請重疊，所以在送出時檢查。
        /// </summary>
        public async Task<CreateLeaveApplicationResult> CreateAsync(CreateLeaveApplicationRequest request)
        {
            // [Required] 已經驗證過以下屬性的存在
            var employeeId = request.EmployeeId!.Value;
            var type = request.Type!.Value;
            var start = request.Start!.Value;
            var end = request.End!.Value;
            var reason = request.Reason;

            if (!Enum.IsDefined(type))
            {
                return CreateLeaveApplicationResult.Failure(CreateLeaveApplicationError.UnknownLeaveType);
            }
            if (!DateRange.TryCreate(start, end, out DateRange dateRange))
            {
                return CreateLeaveApplicationResult.Failure(CreateLeaveApplicationError.InvalidDateRange);
            }
            if (type is LeaveApplicationType.PersonalLeave
                && dateRange.Duration > LeaveApplicationConstraints.MaxPersonalLeaveDays)
            {
                return CreateLeaveApplicationResult.Failure(CreateLeaveApplicationError.ExceedMaxPersonalLeaveDays);
            }
            if (type is LeaveApplicationType.PersonalLeave
                && string.IsNullOrWhiteSpace(reason))
            {
                return CreateLeaveApplicationResult.Failure(CreateLeaveApplicationError.PersonalLeaveWithoutReason);
            }

            var isEmployed = await _employeeDirectory.IsEmployedThroughoutAsync(employeeId, dateRange.Start, dateRange.End);
            switch (isEmployed)
            {
                case null:
                    return CreateLeaveApplicationResult.Failure(CreateLeaveApplicationError.EmployeeNotFound);
                case false:
                    return CreateLeaveApplicationResult.Failure(CreateLeaveApplicationError.EmployeeNotEmployedOnDateRange);
            }

            var leave = new LeaveApplication(employeeId, type, dateRange, reason);
            _repository.Add(leave);

            return CreateLeaveApplicationResult.Success(leave.ToDto());
        }

        /// <summary>
        /// 送出申請（草稿或已退回 → 待審核）。在職和重疊都在這裡檢查，因為草稿階段日期可能被修改過。
        /// TODO: 加入登入功能後，只有申請人本人可以送出。
        /// </summary>
        public async Task<SubmitLeaveApplicationResult> SubmitAsync(Guid leaveId)
        {
            var leave = _repository.Find(leaveId);
            if (leave is null)
            {
                return SubmitLeaveApplicationResult.Failure(SubmitLeaveApplicationError.LeaveApplicationNotFound);
            }
            // 先檢查狀態：不能送出時，就不需要跨模組查詢員工
            if (!leave.CanSubmit)
            {
                return SubmitLeaveApplicationResult.Failure(SubmitLeaveApplicationError.InvalidStatus);
            }
            var isEmployed = await _employeeDirectory.IsEmployedThroughoutAsync(leave.EmployeeId, leave.DateRange.Start, leave.DateRange.End);
            switch (isEmployed)
            {
                case null:
                    return SubmitLeaveApplicationResult.Failure(SubmitLeaveApplicationError.EmployeeNotFound);
                case false:
                    return SubmitLeaveApplicationResult.Failure(SubmitLeaveApplicationError.EmployeeNotEmployedOnDateRange);
            }

            return _repository.TrySubmitWithoutOverlap(leave) switch
            {
                TrySubmitOutcome.Submitted => SubmitLeaveApplicationResult.Success(leave.ToDto()),
                TrySubmitOutcome.InvalidStatus => SubmitLeaveApplicationResult.Failure(SubmitLeaveApplicationError.InvalidStatus),
                TrySubmitOutcome.Overlapping => SubmitLeaveApplicationResult.Failure(SubmitLeaveApplicationError.Overlapping),
                var outcome => throw new InvalidOperationException($"未處理的送出結果：{outcome}"),
            };
        }
    }
}
