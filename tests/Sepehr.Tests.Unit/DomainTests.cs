using System;
using System.Collections.Generic;
using Sepehr.Domain;
using Xunit;

namespace Sepehr.Tests.Unit
{
    public class DomainTests
    {
        private static readonly DateTime Now = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

        private static GroupMembership Membership(
            long groupId = 10, bool active = true, bool history = false, string role = Roles.Member) =>
            new(groupId, 1, Now, active, history, Roles.DefaultPermissions(role));

        [Fact]
        public void Private_and_personal_are_limited_to_participants()
        {
            Assert.True(ConversationAccess.CanReadPrivate(1, 1, 2));
            Assert.False(ConversationAccess.CanReadPrivate(99, 1, 2));
            Assert.True(ConversationAccess.CanReadPersonal(1, 1));
            Assert.False(ConversationAccess.CanReadPersonal(99, 1));
        }

        [Fact]
        public void Group_read_requires_active_membership_of_that_group()
        {
            Assert.True(ConversationAccess.CanReadGroupMessage(1, 10, Now, Membership()));
            Assert.False(ConversationAccess.CanReadGroupMessage(1, 10, Now, null));
            Assert.False(ConversationAccess.CanReadGroupMessage(1, 10, Now, Membership(active: false)));
            // عضویت در والد (10) دسترسی زیرگروه (11) نمی‌دهد
            Assert.False(ConversationAccess.CanReadGroupMessage(1, 11, Now, Membership(10)));
            // عضویت کاربر دیگر قابل استفاده نیست
            Assert.False(ConversationAccess.CanReadGroupMessage(2, 10, Now, Membership()));
        }

        [Fact]
        public void History_before_join_is_hidden_by_default()
        {
            var old = Now.AddMinutes(-1);
            Assert.False(ConversationAccess.CanReadGroupMessage(1, 10, old, Membership()));
            Assert.True(ConversationAccess.CanReadGroupMessage(1, 10, old, Membership(history: true)));
        }

        [Fact]
        public void Global_admin_defaults_contain_no_content_permission()
        {
            var admin = Roles.DefaultPermissions(Roles.GlobalAdmin);
            Assert.DoesNotContain(Permissions.MessageRead, admin);
            Assert.DoesNotContain(Permissions.FileDownload, admin);
            Assert.DoesNotContain(Permissions.FileExecutableSend, admin);
            Assert.DoesNotContain(Permissions.AuditView, admin);
        }

        [Fact]
        public void Coordinator_has_no_implicit_management_permission()
        {
            var coordinator = Roles.DefaultPermissions(Roles.GroupCoordinator);
            Assert.DoesNotContain(Permissions.GroupMemberManage, coordinator);
            Assert.DoesNotContain(Permissions.AnnouncementCreate, coordinator);
            Assert.Empty(Roles.DefaultPermissions("Unknown"));
        }

        [Fact]
        public void Private_pair_is_ordered_and_rejects_self()
        {
            Assert.Equal((3L, 9L), PrivatePair.Create(9, 3));
            Assert.Throws<ArgumentException>(() => PrivatePair.Create(5, 5));
        }

        [Fact]
        public void Owner_window_is_five_minutes()
        {
            var window = MessagePolicy.DefaultOwnerWindow;
            Assert.True(MessagePolicy.CanEdit(1, 1, Now.AddMinutes(-5), Now, window));
            Assert.False(MessagePolicy.CanEdit(1, 1, Now.AddMinutes(-5).AddSeconds(-1), Now, window));
            Assert.False(MessagePolicy.CanEdit(2, 1, Now, Now, window));
            Assert.True(MessagePolicy.CanDeleteForEveryone(2, 1, Now.AddDays(-1), Now, window, true));
            Assert.False(MessagePolicy.CanDeleteForEveryone(2, 1, Now, Now, window, false));
        }

        [Fact]
        public void Hierarchy_rejects_cycles_and_depth_over_three()
        {
            var tree = new Dictionary<long, long?> { [1] = null, [2] = 1, [3] = 2, [4] = null, [5] = 4 };
            Assert.Equal(HierarchyResult.SelfParent, GroupHierarchy.ValidateParent(tree, 1, 1));
            Assert.Equal(HierarchyResult.Cycle, GroupHierarchy.ValidateParent(tree, 1, 3));
            Assert.Equal(HierarchyResult.TooDeep, GroupHierarchy.ValidateParent(tree, null, 3));
            Assert.Equal(HierarchyResult.Valid, GroupHierarchy.ValidateParent(tree, null, 2));
            // جابه‌جایی زیردرخت دو سطحی زیر گره سطح دو
            Assert.Equal(HierarchyResult.TooDeep, GroupHierarchy.ValidateParent(tree, 4, 2));
            Assert.Equal(HierarchyResult.Valid, GroupHierarchy.ValidateParent(tree, 4, 1));
            Assert.Equal(HierarchyResult.UnknownParent, GroupHierarchy.ValidateParent(tree, 4, 77));
        }

        [Theory]
        [InlineData("1234567", PasswordError.TooShort)]
        [InlineData("10012345", PasswordError.SameAsPersonnelCode)]
        [InlineData("oldpassword", PasswordError.SameAsCurrent)]
        [InlineData("abcdefgh", PasswordError.None)]
        public void New_password_rules(string candidate, PasswordError expected) =>
            Assert.Equal(expected, PasswordPolicy.ValidateNew(candidate, "oldpassword", "10012345"));

        [Fact]
        public void Five_failures_lock_for_fifteen_minutes()
        {
            var failures = new List<DateTime>();
            for (var i = 4; i >= 1; i--) failures.Add(Now.AddMinutes(-i));
            Assert.Null(LockoutPolicy.LockedUntil(failures, Now, LockoutOptions.Default));

            failures.Add(Now);
            Assert.Equal(Now.AddMinutes(15), LockoutPolicy.LockedUntil(failures, Now, LockoutOptions.Default));
            Assert.Null(LockoutPolicy.LockedUntil(failures, Now.AddMinutes(15), LockoutOptions.Default));
        }

        [Fact]
        public void Session_has_idle_absolute_and_revocation_limits()
        {
            var options = SessionOptions.Default;
            Assert.True(SessionPolicy.IsActive(Now, Now, null, Now.AddMinutes(29), options));
            Assert.False(SessionPolicy.IsActive(Now, Now, null, Now.AddMinutes(30), options));
            Assert.False(SessionPolicy.IsActive(Now, Now.AddHours(12), null, Now.AddHours(12), options));
            Assert.False(SessionPolicy.IsActive(Now, Now, Now, Now, options));
        }
    }
}
