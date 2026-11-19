namespace Attendance.Core
{
    internal enum LeaveApplicationStatus
    {
        Draft = 1,
        Submitted = 2, // 待審核
        Rejected = 3,
        Approved = 4,
        Withdrawn = 9, // 已撤回
    }
}
