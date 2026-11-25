using System;
using System.Collections.Generic;

namespace Sepehr.Domain
{
    public static class Permissions
    {
        public const string UserView = "User.View";
        public const string UserCreate = "User.Create";
        public const string UserUpdate = "User.Update";
        public const string UserDisable = "User.Disable";
        public const string UserImport = "User.Import";
        public const string GroupView = "Group.View";
        public const string GroupCreate = "Group.Create";
        public const string GroupUpdate = "Group.Update";
        public const string GroupDelete = "Group.Delete";
        public const string GroupMemberManage = "Group.MemberManage";
        public const string GroupRoleManage = "Group.RoleManage";
        public const string MessageSend = "Message.Send";
        public const string MessageRead = "Message.Read";
        public const string MessageEditOwn = "Message.EditOwn";
        public const string MessageDeleteOwn = "Message.DeleteOwn";
        public const string MessageDeleteGroup = "Message.DeleteGroup";
        public const string AnnouncementCreate = "Announcement.Create";
        public const string AnnouncementSchedule = "Announcement.Schedule";
        public const string FileUpload = "File.Upload";
        public const string FileDownload = "File.Download";
        public const string FileExecutableSend = "File.ExecutableSend";
        public const string SettingsManage = "Settings.Manage";
        public const string AuditView = "Audit.View";
        public const string ReportsView = "Reports.View";

        public static IReadOnlyList<string> All { get; } = new[]
        {
            UserView, UserCreate, UserUpdate, UserDisable, UserImport,
            GroupView, GroupCreate, GroupUpdate, GroupDelete, GroupMemberManage, GroupRoleManage,
            MessageSend, MessageRead, MessageEditOwn, MessageDeleteOwn, MessageDeleteGroup,
            AnnouncementCreate, AnnouncementSchedule,
            FileUpload, FileDownload, FileExecutableSend,
            SettingsManage, AuditView, ReportsView
        };
    }

    public static class Roles
    {
        public const string GlobalAdmin = "GlobalAdmin";
        public const string GroupAdmin = "GroupAdmin";
        public const string GroupCoordinator = "GroupCoordinator";
        public const string Member = "Member";

        private static readonly string[] MemberDefaults =
        {
            Permissions.GroupView, Permissions.MessageSend, Permissions.MessageRead,
            Permissions.MessageEditOwn, Permissions.MessageDeleteOwn,
            Permissions.FileUpload, Permissions.FileDownload
        };

        /// <summary>پیش‌فرض‌ها. هر چه فهرست نشده باشد ممنوع است (deny by default).</summary>
        public static IReadOnlyCollection<string> DefaultPermissions(string roleCode)
        {
            switch (roleCode)
            {
                case GlobalAdmin:
                    return new[]
                    {
                        Permissions.UserView, Permissions.UserCreate, Permissions.UserUpdate,
                        Permissions.UserDisable, Permissions.UserImport,
                        Permissions.GroupView, Permissions.GroupCreate, Permissions.GroupUpdate,
                        Permissions.GroupDelete, Permissions.GroupMemberManage, Permissions.GroupRoleManage,
                        Permissions.AnnouncementCreate, Permissions.AnnouncementSchedule,
                        Permissions.SettingsManage, Permissions.ReportsView
                    };
                case GroupAdmin:
                    var admin = new List<string>(MemberDefaults)
                    {
                        Permissions.GroupUpdate, Permissions.GroupMemberManage, Permissions.GroupRoleManage,
                        Permissions.MessageDeleteGroup,
                        Permissions.AnnouncementCreate, Permissions.AnnouncementSchedule
                    };
                    return admin;
                case GroupCoordinator:
                    // هیچ مجوز ضمنی فراتر از عضو؛ اختیارات بیشتر فقط با اعطای صریح.
                    return MemberDefaults;
                case Member:
                    return MemberDefaults;
                default:
                    return Array.Empty<string>();
            }
        }
    }
}
