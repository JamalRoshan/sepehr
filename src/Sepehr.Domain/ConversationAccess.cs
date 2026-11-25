using System;
using System.Collections.Generic;
using System.Linq;

namespace Sepehr.Domain
{
    /// <summary>عضویت فعال کاربر در همان گروه (نه گروه والد).</summary>
    public sealed record GroupMembership(
        long GroupId,
        long UserId,
        DateTime JoinedAtUtc,
        bool IsActive,
        bool HistoryBeforeJoin,
        IReadOnlyCollection<string> EffectivePermissions);

    /// <summary>
    /// حریم خصوصی: عمداً هیچ ورودی «نقش سراسری» وجود ندارد؛ بنابراین هیچ نقش مدیریتی
    /// نمی‌تواند دسترسی خواندن ایجاد کند.
    /// </summary>
    public static class ConversationAccess
    {
        public static bool CanReadPrivate(long userId, long userLowId, long userHighId) =>
            userId == userLowId || userId == userHighId;

        public static bool CanReadPersonal(long userId, long ownerUserId) => userId == ownerUserId;

        public static bool CanReadGroupMessage(
            long userId, long conversationGroupId, DateTime messageCreatedAtUtc, GroupMembership? membership)
        {
            if (!HasGroupPermission(userId, conversationGroupId, membership, Permissions.MessageRead))
                return false;

            return membership!.HistoryBeforeJoin || messageCreatedAtUtc >= membership.JoinedAtUtc;
        }

        public static bool HasGroupPermission(
            long userId, long groupId, GroupMembership? membership, string permission) =>
            membership != null
            && membership.IsActive
            && membership.UserId == userId
            && membership.GroupId == groupId
            && membership.EffectivePermissions.Contains(permission, StringComparer.Ordinal);
    }

    public static class PrivatePair
    {
        public static (long Low, long High) Create(long first, long second)
        {
            // گفتگو با خود همان فضای شخصی است، نه گفتگوی خصوصی.
            if (first == second)
                throw new ArgumentException("Self conversation must use the Personal type.", nameof(second));

            return first < second ? (first, second) : (second, first);
        }
    }
}
