using System;

namespace Sepehr.Domain
{
    public static class MessagePolicy
    {
        public static readonly TimeSpan DefaultOwnerWindow = TimeSpan.FromMinutes(5);

        /// <summary>
        /// فراخواننده باید پیش از این، دسترسی خواندن گفتگو را با ConversationAccess تأیید کرده باشد.
        /// </summary>
        public static bool CanEdit(
            long actorId, long senderId, DateTime createdAtUtc, DateTime nowUtc, TimeSpan window) =>
            actorId == senderId && IsInsideWindow(createdAtUtc, nowUtc, window);

        /// <param name="hasGroupDeletePermission">
        /// Message.DeleteGroup در همان گروه؛ برای گفتگوی خصوصی و شخصی همیشه false.
        /// </param>
        public static bool CanDeleteForEveryone(
            long actorId, long senderId, DateTime createdAtUtc, DateTime nowUtc, TimeSpan window,
            bool hasGroupDeletePermission) =>
            hasGroupDeletePermission || CanEdit(actorId, senderId, createdAtUtc, nowUtc, window);

        private static bool IsInsideWindow(DateTime createdAtUtc, DateTime nowUtc, TimeSpan window) =>
            nowUtc - createdAtUtc <= window;
    }
}
