using Attendance.Contracts;

namespace Attendance.Core
{
    internal sealed class LeaveApplication
    {
        public Guid Id { get; private set; } = Guid.CreateVersion7();
        public Guid EmployeeId { get; private set; }
        public LeaveApplicationType Type { get; private set; }
        public DateRange DateRange { get; private set; }
        public string? Reason { get; private set; } // 事假必須寫理由，其他假別可選
        public LeaveApplicationStatus Status { get; private set; } = LeaveApplicationStatus.Draft;
        public Guid? ReviewerId { get; private set; }
        public DateTimeOffset? ReviewedAt { get; private set; }
        public string? RejectReason { get; private set; }

        public int Duration => DateRange.Duration;

        public bool CanEdit => Status is LeaveApplicationStatus.Draft
            or LeaveApplicationStatus.Rejected;
        public bool CanSubmit => Status is LeaveApplicationStatus.Draft
            or LeaveApplicationStatus.Rejected;
        public bool CanWithdraw => Status is LeaveApplicationStatus.Draft
            or LeaveApplicationStatus.Submitted;
        public bool CanReject => Status is LeaveApplicationStatus.Submitted;
        public bool CanApprove => Status is LeaveApplicationStatus.Submitted;

        /// <summary>
        /// 給 EF Core 從資料庫還原物件用。資料庫中的資料在寫入時已經驗證過，所以這裡不重複驗證；
        /// 程式碼建立申請時請使用另一個建構子。
        /// </summary>
        private LeaveApplication()
        {
        }

        public LeaveApplication(Guid employeeId,
            LeaveApplicationType type,
            DateRange dateRange,
            string? reason)
        {
            if (employeeId == Guid.Empty)
            {
                throw new ArgumentException("必須指定員工。", nameof(employeeId));
            }
            EnsureValid(type, dateRange, reason);

            EmployeeId = employeeId;
            Type = type;
            DateRange = dateRange;
            Reason = reason;
        }

        public void Submit()
        {
            if (!CanSubmit)
            {
                throw new InvalidOperationException("只有草稿或已退回的請假申請可以送出。");
            }
            ReviewerId = null;
            ReviewedAt = null;
            RejectReason = null;
            Status = LeaveApplicationStatus.Submitted;
        }

        public void Update(LeaveApplicationType type,
            DateRange dateRange,
            string? reason)
        {
            if (!CanEdit)
            {
                throw new InvalidOperationException("只有草稿或已退回的請假申請可以修改。");
            }

            EnsureValid(type, dateRange, reason);

            Type = type;
            DateRange = dateRange;
            Reason = reason;
        }

        public void Reject(Guid reviewerId, string rejectReason, DateTimeOffset reviewedAt)
        {
            if (!CanReject)
            {
                throw new InvalidOperationException("只有待審核的請假申請可以退回。");
            }
            EnsureValidReviewer(reviewerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(rejectReason);
            if (rejectReason.Length > LeaveApplicationConstraints.MaxRejectReasonLength)
            {
                throw new ArgumentException($"退回理由最多 {LeaveApplicationConstraints.MaxRejectReasonLength} 個字。", nameof(rejectReason));
            }

            ReviewerId = reviewerId;
            RejectReason = rejectReason;
            Status = LeaveApplicationStatus.Rejected;
            ReviewedAt = reviewedAt;
        }

        public void Approve(Guid reviewerId, DateTimeOffset reviewedAt)
        {
            if (!CanApprove)
            {
                throw new InvalidOperationException("只有待審核的請假申請可以核准。");
            }
            EnsureValidReviewer(reviewerId);

            ReviewerId = reviewerId;
            Status = LeaveApplicationStatus.Approved;
            ReviewedAt = reviewedAt;
        }

        public void Withdraw()
        {
            if (!CanWithdraw)
            {
                throw new InvalidOperationException("只有草稿或待審核的請假申請可以撤回。");
            }
            Status = LeaveApplicationStatus.Withdrawn;
        }

        public bool OverlapsWith(LeaveApplication other)
        {
            return EmployeeId == other.EmployeeId
                && DateRange.Overlaps(other.DateRange);
        }

        private void EnsureValidReviewer(Guid reviewerId)
        {
            if (reviewerId == Guid.Empty)
            {
                throw new ArgumentException("必須指定審核人。", nameof(reviewerId));
            }
            if (reviewerId == EmployeeId)
            {
                throw new ArgumentException("審核人不能是申請人本人。", nameof(reviewerId));
            }
        }

        private static void EnsureValidReason(LeaveApplicationType type, string? reason)
        {
            if (type == LeaveApplicationType.PersonalLeave && string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("事假必須填寫請假理由。", nameof(reason));
            }
            if (reason?.Length > LeaveApplicationConstraints.MaxReasonLength)
            {
                throw new ArgumentException($"請假理由最多 {LeaveApplicationConstraints.MaxReasonLength} 個字。", nameof(reason));
            }
        }

        private static void EnsureValidDateRange(LeaveApplicationType type, DateRange dateRange)
        {
            if (dateRange == default)
            {
                throw new ArgumentException("必須指定有效的請假日期區間。", nameof(dateRange));
            }
            if (type == LeaveApplicationType.PersonalLeave && dateRange.Duration > LeaveApplicationConstraints.MaxPersonalLeaveDays)
            {
                throw new ArgumentException($"事假最多 {LeaveApplicationConstraints.MaxPersonalLeaveDays} 天。", nameof(dateRange));
            }
        }

        private static void EnsureValid(LeaveApplicationType type,
            DateRange dateRange,
            string? reason)
        {
            if (!Enum.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }
            EnsureValidReason(type, reason);
            EnsureValidDateRange(type, dateRange);
        }
    }
}
