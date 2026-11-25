namespace Sepehr.Contracts
{
    // مقادیر عددی با قیود CHECK در database/migrations یکسان هستند.
    public enum ConversationType : byte { Private = 1, Group = 2, Personal = 3 }

    public enum MessageType : byte
    {
        Text = 1,
        Voice = 2,
        File = 3,
        Sticker = 4,
        Gif = 5,
        Announcement = 6,
        System = 7
    }

    public enum RoleScope : byte { Global = 1, Group = 2 }
}
