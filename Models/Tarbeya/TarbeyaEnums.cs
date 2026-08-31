namespace SchoolSystemAPI.Models;

public enum TarbeyaMeetingType
{
    PrepMeeting = 1,    // اجتماع تحضيري
    Liturgy = 2,        // قداس الخدمة
    SundaySchool = 3    // فصل مدارس الأحد
}

public enum TarbeyaServiceTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3
}
