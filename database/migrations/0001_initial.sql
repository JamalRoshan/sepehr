/* Sepehr - 0001_initial (SQL Server 2022)
   اجرا در دیتابیس خالی. پیشنهاد: CREATE DATABASE Sepehr COLLATE Persian_100_CI_AS_SC;
   با sqlcmd حتماً -I و -b. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.SchemaVersions') IS NOT NULL
BEGIN
    PRINT N'0001 already applied; skipping.';
    SET NOEXEC ON;
END
GO
BEGIN TRANSACTION;
GO
CREATE TABLE dbo.SchemaVersions(
    Version int NOT NULL CONSTRAINT PK_SchemaVersions PRIMARY KEY,
    AppliedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());

CREATE TABLE dbo.Users(
    UserId bigint IDENTITY NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    PersonnelCode nvarchar(32) NOT NULL CONSTRAINT UQ_Users_PersonnelCode UNIQUE,
    Username nvarchar(32) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    DisplayName nvarchar(150) NOT NULL,
    FirstName nvarchar(75) NULL,
    LastName nvarchar(75) NULL,
    SearchText nvarchar(400) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    MustChangePassword bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt datetime2(3) NULL,
    DeactivatedAt datetime2(3) NULL);
CREATE INDEX IX_Users_Search ON dbo.Users(SearchText);

CREATE TABLE dbo.UserCredentials(
    CredentialId bigint IDENTITY NOT NULL PRIMARY KEY,
    UserId bigint NOT NULL UNIQUE REFERENCES dbo.Users(UserId),
    PasswordHash varchar(256) NOT NULL,
    PasswordChangedAt datetime2(3) NULL,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());

CREATE TABLE dbo.UserSessions(
    SessionId uniqueidentifier NOT NULL PRIMARY KEY,
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    TokenHash binary(32) NOT NULL UNIQUE,
    CreatedAt datetime2(3) NOT NULL,
    ExpiresAt datetime2(3) NOT NULL,
    LastActivityAt datetime2(3) NOT NULL,
    RevokedAt datetime2(3) NULL,
    DeviceInfo nvarchar(256) NULL,
    SessionStatus tinyint NOT NULL DEFAULT 1,
    CONSTRAINT CK_Sessions_Expiry CHECK (ExpiresAt > CreatedAt));
CREATE INDEX IX_Sessions_User ON dbo.UserSessions(UserId) WHERE RevokedAt IS NULL;

CREATE TABLE dbo.LoginAttempts(
    LoginAttemptId bigint IDENTITY NOT NULL PRIMARY KEY,
    Username nvarchar(32) NOT NULL,
    UserId bigint NULL REFERENCES dbo.Users(UserId),
    AttemptAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    IpAddress varchar(45) NULL,
    Success bit NOT NULL,
    FailureReasonCode varchar(40) NULL);
CREATE INDEX IX_LoginAttempts_User ON dbo.LoginAttempts(Username, AttemptAt DESC);

CREATE TABLE dbo.Roles(
    RoleId int IDENTITY NOT NULL PRIMARY KEY,
    RoleCode nvarchar(50) NOT NULL UNIQUE,
    RoleName nvarchar(100) NOT NULL,
    Scope tinyint NOT NULL CHECK (Scope IN (1,2)),
    Description nvarchar(300) NULL,
    CONSTRAINT UQ_Roles_Scope UNIQUE (RoleId, Scope));

CREATE TABLE dbo.Permissions(
    PermissionId int IDENTITY NOT NULL PRIMARY KEY,
    PermissionCode nvarchar(80) NOT NULL UNIQUE,
    PermissionName nvarchar(120) NOT NULL,
    Description nvarchar(300) NULL);

CREATE TABLE dbo.RolePermissions(
    RoleId int NOT NULL REFERENCES dbo.Roles(RoleId),
    PermissionId int NOT NULL REFERENCES dbo.Permissions(PermissionId),
    PRIMARY KEY (RoleId, PermissionId));

/* ستون محاسباتی + FK مرکب: نقش سراسری و گروهی قابل جابه‌جایی نیستند */
CREATE TABLE dbo.UserRoles(
    UserRoleId bigint IDENTITY NOT NULL PRIMARY KEY,
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    RoleId int NOT NULL,
    RoleScope AS CONVERT(tinyint, 1) PERSISTED NOT NULL,
    AssignedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    AssignedBy bigint NULL REFERENCES dbo.Users(UserId),
    FOREIGN KEY (RoleId, RoleScope) REFERENCES dbo.Roles(RoleId, Scope),
    UNIQUE (UserId, RoleId));

CREATE TABLE dbo.Groups(
    GroupId bigint IDENTITY NOT NULL PRIMARY KEY,
    ParentGroupId bigint NULL REFERENCES dbo.Groups(GroupId),
    Name nvarchar(150) NOT NULL,
    Description nvarchar(500) NULL,
    SearchText nvarchar(700) NOT NULL,
    HistoryBeforeJoin bit NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Groups_NotSelf CHECK (ParentGroupId IS NULL OR ParentGroupId <> GroupId));

CREATE TABLE dbo.GroupMembers(
    GroupMemberId bigint IDENTITY NOT NULL PRIMARY KEY,
    GroupId bigint NOT NULL REFERENCES dbo.Groups(GroupId),
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    IsActive bit NOT NULL DEFAULT 1,
    JoinedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LeftAt datetime2(3) NULL,
    CONSTRAINT CK_GroupMembers_Left CHECK ((IsActive=1 AND LeftAt IS NULL) OR (IsActive=0 AND LeftAt IS NOT NULL)));
CREATE UNIQUE INDEX UX_GroupMembers_Active ON dbo.GroupMembers(GroupId, UserId) WHERE IsActive=1;
CREATE INDEX IX_GroupMembers_User ON dbo.GroupMembers(UserId, GroupId);

CREATE TABLE dbo.GroupMemberRoles(
    GroupMemberRoleId bigint IDENTITY NOT NULL PRIMARY KEY,
    GroupMemberId bigint NOT NULL REFERENCES dbo.GroupMembers(GroupMemberId),
    RoleId int NOT NULL,
    RoleScope AS CONVERT(tinyint, 2) PERSISTED NOT NULL,
    AssignedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    AssignedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    FOREIGN KEY (RoleId, RoleScope) REFERENCES dbo.Roles(RoleId, Scope),
    UNIQUE (GroupMemberId, RoleId));

/* اعطای صریح مجوز (به‌ویژه برای GroupCoordinator) */
CREATE TABLE dbo.GroupMemberPermissions(
    GroupMemberId bigint NOT NULL REFERENCES dbo.GroupMembers(GroupMemberId),
    PermissionId int NOT NULL REFERENCES dbo.Permissions(PermissionId),
    GrantedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    GrantedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    PRIMARY KEY (GroupMemberId, PermissionId));

CREATE TABLE dbo.RetentionPolicies(
    RetentionPolicyId int IDENTITY NOT NULL PRIMARY KEY,
    AppliesTo tinyint NOT NULL UNIQUE CHECK (AppliesTo IN (1,2,3)), /* 1 پیام، 2 فایل، 3 فضای شخصی */
    RetentionDays int NULL CHECK (RetentionDays IS NULL OR RetentionDays BETWEEN 1 AND 3650),
    UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy bigint NULL REFERENCES dbo.Users(UserId));

CREATE TABLE dbo.Conversations(
    ConversationId bigint IDENTITY NOT NULL PRIMARY KEY,
    ConversationType tinyint NOT NULL CHECK (ConversationType IN (1,2,3)),
    GroupId bigint NULL REFERENCES dbo.Groups(GroupId),
    OwnerUserId bigint NULL REFERENCES dbo.Users(UserId),
    Title nvarchar(150) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Conversations_Group CHECK (
        (ConversationType=2 AND GroupId IS NOT NULL) OR (ConversationType<>2 AND GroupId IS NULL)),
    CONSTRAINT CK_Conversations_Owner CHECK (
        (ConversationType=3 AND OwnerUserId IS NOT NULL) OR (ConversationType<>3 AND OwnerUserId IS NULL)));
CREATE UNIQUE INDEX UX_Conversations_Group ON dbo.Conversations(GroupId) WHERE GroupId IS NOT NULL;
CREATE UNIQUE INDEX UX_Conversations_Personal ON dbo.Conversations(OwnerUserId) WHERE OwnerUserId IS NOT NULL;

CREATE TABLE dbo.PrivateConversationPairs(
    ConversationId bigint NOT NULL PRIMARY KEY REFERENCES dbo.Conversations(ConversationId),
    UserLowId bigint NOT NULL REFERENCES dbo.Users(UserId),
    UserHighId bigint NOT NULL REFERENCES dbo.Users(UserId),
    CHECK (UserLowId < UserHighId),
    UNIQUE (UserLowId, UserHighId));

CREATE TABLE dbo.ConversationMembers(
    ConversationMemberId bigint IDENTITY NOT NULL PRIMARY KEY,
    ConversationId bigint NOT NULL REFERENCES dbo.Conversations(ConversationId),
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    JoinedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LeftAt datetime2(3) NULL,
    IsActive bit NOT NULL DEFAULT 1);
CREATE UNIQUE INDEX UX_ConversationMembers_Active ON dbo.ConversationMembers(ConversationId, UserId) WHERE IsActive=1;
CREATE INDEX IX_ConversationMembers_User ON dbo.ConversationMembers(UserId, ConversationId);

CREATE TABLE dbo.Messages(
    MessageId bigint IDENTITY NOT NULL PRIMARY KEY,
    ConversationId bigint NOT NULL REFERENCES dbo.Conversations(ConversationId),
    SenderUserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    ClientMessageId uniqueidentifier NOT NULL,
    MessageType tinyint NOT NULL CHECK (MessageType BETWEEN 1 AND 7),
    TextContent nvarchar(max) NULL,
    SearchText nvarchar(max) NULL,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    EditedAt datetime2(3) NULL,
    DeletedAt datetime2(3) NULL,
    DeleteScope tinyint NULL CHECK (DeleteScope = 2), /* حذف برای همه؛ حذف برای خود: MessageHidden */
    ExpireAt datetime2(3) NULL,
    ReplyToMessageId bigint NULL,
    MessageStatus tinyint NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Messages_Idempotency UNIQUE (SenderUserId, ClientMessageId),
    CONSTRAINT UQ_Messages_InConversation UNIQUE (ConversationId, MessageId),
    /* پاسخ فقط در همان گفتگو. پاک‌سازی Retention باید ابتدا این ارجاع را NULL کند. */
    FOREIGN KEY (ConversationId, ReplyToMessageId) REFERENCES dbo.Messages(ConversationId, MessageId),
    CONSTRAINT CK_Messages_Text CHECK (MessageType <> 1 OR TextContent IS NOT NULL OR DeletedAt IS NOT NULL));
CREATE INDEX IX_Messages_Timeline ON dbo.Messages(ConversationId, CreatedAt DESC, MessageId DESC);
CREATE INDEX IX_Messages_Expire ON dbo.Messages(ExpireAt) WHERE ExpireAt IS NOT NULL;

CREATE TABLE dbo.MessageHidden(
    MessageId bigint NOT NULL REFERENCES dbo.Messages(MessageId),
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    HiddenAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (UserId, MessageId));

/* تاریخچه ویرایش؛ محتوای خصوصی است و همان ACL و Retention پیام را دارد */
CREATE TABLE dbo.MessageHistory(
    MessageHistoryId bigint IDENTITY NOT NULL PRIMARY KEY,
    MessageId bigint NOT NULL REFERENCES dbo.Messages(MessageId),
    PreviousText nvarchar(max) NULL,
    ChangedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ChangedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    ChangeType tinyint NOT NULL CHECK (ChangeType IN (1,2)));

CREATE TABLE dbo.MessageReceipts(
    MessageReceiptId bigint IDENTITY NOT NULL PRIMARY KEY,
    MessageId bigint NOT NULL REFERENCES dbo.Messages(MessageId),
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    DeliveredAt datetime2(3) NULL,
    ReadAt datetime2(3) NULL,
    UNIQUE (MessageId, UserId));

CREATE TABLE dbo.UserConversationStates(
    UserConversationStateId bigint IDENTITY NOT NULL PRIMARY KEY,
    ConversationId bigint NOT NULL REFERENCES dbo.Conversations(ConversationId),
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    LastDeliveredMessageId bigint NULL,
    LastDeliveredAt datetime2(3) NULL,
    LastReadMessageId bigint NULL,
    LastReadAt datetime2(3) NULL,
    IsMuted bit NOT NULL DEFAULT 0,
    IsPinned bit NOT NULL DEFAULT 0,
    IsArchived bit NOT NULL DEFAULT 0,
    UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UNIQUE (UserId, ConversationId),
    FOREIGN KEY (ConversationId, LastDeliveredMessageId) REFERENCES dbo.Messages(ConversationId, MessageId),
    FOREIGN KEY (ConversationId, LastReadMessageId) REFERENCES dbo.Messages(ConversationId, MessageId));

CREATE TABLE dbo.FileStorageLocations(
    StorageLocationId int IDENTITY NOT NULL PRIMARY KEY,
    LocationName nvarchar(100) NOT NULL UNIQUE,
    RootPath nvarchar(400) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME());

CREATE TABLE dbo.Files(
    FileId uniqueidentifier NOT NULL PRIMARY KEY,
    OriginalFileName nvarchar(255) NOT NULL,
    StorageName varchar(64) NOT NULL UNIQUE,
    StoragePathReference nvarchar(400) NOT NULL,
    Extension nvarchar(20) NOT NULL,
    MimeType varchar(150) NOT NULL,
    DetectedContentType varchar(150) NULL,
    FileSize bigint NOT NULL CHECK (FileSize >= 0),
    Sha256 binary(32) NOT NULL,
    UploadedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    UploadIdempotencyKey uniqueidentifier NOT NULL,
    StorageLocationId int NOT NULL REFERENCES dbo.FileStorageLocations(StorageLocationId),
    FileStatus tinyint NOT NULL DEFAULT 1 CHECK (FileStatus IN (1,2,3,4)), /* 1 قرنطینه 2 پاک 3 ردشده 4 حذف */
    ScanDetail nvarchar(300) NULL,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpireAt datetime2(3) NULL,
    DeletedAt datetime2(3) NULL,
    UNIQUE (UploadedBy, UploadIdempotencyKey));
CREATE INDEX IX_Files_Expire ON dbo.Files(ExpireAt) WHERE ExpireAt IS NOT NULL AND DeletedAt IS NULL;

CREATE TABLE dbo.MessageAttachments(
    MessageAttachmentId bigint IDENTITY NOT NULL PRIMARY KEY,
    MessageId bigint NOT NULL REFERENCES dbo.Messages(MessageId),
    FileId uniqueidentifier NOT NULL REFERENCES dbo.Files(FileId),
    AttachmentType tinyint NOT NULL,
    DurationSeconds int NULL CHECK (DurationSeconds IS NULL OR DurationSeconds > 0),
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UNIQUE (MessageId, FileId));
CREATE INDEX IX_Attachments_File ON dbo.MessageAttachments(FileId);

CREATE TABLE dbo.FileAccessLogs(
    FileAccessLogId bigint IDENTITY NOT NULL PRIMARY KEY,
    FileId uniqueidentifier NOT NULL, /* بدون FK تا لاگ پس از پاک‌سازی بماند */
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    AccessType tinyint NOT NULL,
    AccessAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    Result tinyint NOT NULL);

CREATE TABLE dbo.ExecutableSendGrants(
    ExecutableSendGrantId bigint IDENTITY NOT NULL PRIMARY KEY,
    UserId bigint NULL REFERENCES dbo.Users(UserId),
    GroupId bigint NULL REFERENCES dbo.Groups(GroupId),
    GrantedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    GrantedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    RevokedAt datetime2(3) NULL,
    CHECK ((UserId IS NULL AND GroupId IS NOT NULL) OR (UserId IS NOT NULL AND GroupId IS NULL)));

CREATE TABLE dbo.StickerCatalog(
    StickerId int IDENTITY NOT NULL PRIMARY KEY,
    Kind tinyint NOT NULL CHECK (Kind IN (4,5)),
    Title nvarchar(100) NOT NULL,
    FileId uniqueidentifier NOT NULL REFERENCES dbo.Files(FileId),
    IsActive bit NOT NULL DEFAULT 1);

CREATE TABLE dbo.PersonalSpaceItems(
    PersonalSpaceItemId bigint IDENTITY NOT NULL PRIMARY KEY,
    OwnerUserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    MessageId bigint NULL REFERENCES dbo.Messages(MessageId),
    FileId uniqueidentifier NULL REFERENCES dbo.Files(FileId),
    Title nvarchar(200) NULL,
    TextContent nvarchar(max) NULL,
    SearchText nvarchar(max) NULL,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    DeletedAt datetime2(3) NULL);
CREATE INDEX IX_PersonalSpace_Owner ON dbo.PersonalSpaceItems(OwnerUserId, CreatedAt DESC);

CREATE TABLE dbo.Notifications(
    NotificationId bigint IDENTITY NOT NULL PRIMARY KEY,
    NotificationType tinyint NOT NULL,
    Title nvarchar(200) NOT NULL,
    Body nvarchar(4000) NOT NULL,
    TargetType tinyint NOT NULL CHECK (TargetType IN (1,2,3)),
    TargetId bigint NULL,
    CreatedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ScheduledAt datetime2(3) NULL,
    SentAt datetime2(3) NULL,
    Status tinyint NOT NULL DEFAULT 1,
    ExpireAt datetime2(3) NULL,
    ClaimedUntil datetime2(3) NULL); /* جلوگیری از ارسال هم‌زمان توسط دو Worker */
CREATE INDEX IX_Notifications_Due ON dbo.Notifications(Status, ScheduledAt) WHERE SentAt IS NULL;

CREATE TABLE dbo.NotificationRecipients(
    NotificationRecipientId bigint IDENTITY NOT NULL PRIMARY KEY,
    NotificationId bigint NOT NULL REFERENCES dbo.Notifications(NotificationId),
    UserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    DeliveredAt datetime2(3) NULL,
    ReadAt datetime2(3) NULL,
    Status tinyint NOT NULL DEFAULT 1,
    UNIQUE (NotificationId, UserId)); /* ارسال تکراری ممکن نیست */

CREATE TABLE dbo.UserContacts(
    UserContactId bigint IDENTITY NOT NULL PRIMARY KEY,
    OwnerUserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    ContactUserId bigint NOT NULL REFERENCES dbo.Users(UserId),
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UNIQUE (OwnerUserId, ContactUserId),
    CHECK (OwnerUserId <> ContactUserId));

CREATE TABLE dbo.SystemSettings(
    SettingId int IDENTITY NOT NULL PRIMARY KEY,
    SettingKey nvarchar(100) NOT NULL UNIQUE,
    SettingValue nvarchar(1000) NOT NULL,
    UpdatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedBy bigint NULL REFERENCES dbo.Users(UserId));

CREATE TABLE dbo.UserPreferences(
    UserPreferenceId bigint IDENTITY NOT NULL PRIMARY KEY,
    UserId bigint NOT NULL UNIQUE REFERENCES dbo.Users(UserId),
    Theme varchar(10) NOT NULL DEFAULT 'light',
    Language varchar(10) NOT NULL DEFAULT 'fa-IR',
    NotificationSettings nvarchar(2000) NULL,
    OtherPreferences nvarchar(2000) NULL);

CREATE TABLE dbo.AuditLogs(
    AuditLogId bigint IDENTITY NOT NULL PRIMARY KEY,
    ActorUserId bigint NULL REFERENCES dbo.Users(UserId),
    Action varchar(80) NOT NULL,
    EntityType varchar(80) NOT NULL,
    EntityId nvarchar(64) NULL,
    [Timestamp] datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CorrelationId uniqueidentifier NOT NULL,
    Result tinyint NOT NULL,
    Metadata nvarchar(2000) NULL); /* هرگز متن پیام، رمز یا توکن */
CREATE INDEX IX_AuditLogs_Time ON dbo.AuditLogs([Timestamp] DESC);

CREATE TABLE dbo.ImportJobs(
    ImportJobId bigint IDENTITY NOT NULL PRIMARY KEY,
    UploadedBy bigint NOT NULL REFERENCES dbo.Users(UserId),
    FileName nvarchar(255) NOT NULL,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt datetime2(3) NULL,
    Status tinyint NOT NULL,
    TotalRows int NOT NULL DEFAULT 0,
    SuccessRows int NOT NULL DEFAULT 0,
    ErrorRows int NOT NULL DEFAULT 0);

CREATE TABLE dbo.ImportJobErrors(
    ImportJobErrorId bigint IDENTITY NOT NULL PRIMARY KEY,
    ImportJobId bigint NOT NULL REFERENCES dbo.ImportJobs(ImportJobId),
    RowNumber int NOT NULL,
    FieldName nvarchar(100) NULL,
    ErrorCode varchar(60) NOT NULL,
    ErrorMessage nvarchar(500) NOT NULL);

/* Sync: شماره Cursor داخل همان تراکنش از یک ردیف قفل‌شونده گرفته می‌شود.
   قفل ردیف تا Commit می‌ماند، پس ترتیب Seq همان ترتیب Commit است. */
CREATE TABLE dbo.SyncState(
    Id tinyint NOT NULL PRIMARY KEY CHECK (Id=1),
    LastSeq bigint NOT NULL);

CREATE TABLE dbo.ChangeLog(
    Seq bigint NOT NULL PRIMARY KEY,
    ChangeType tinyint NOT NULL, /* پیام، ویرایش، حذف، رسید، عضویت، مجوز، اعلان */
    EntityId nvarchar(64) NOT NULL,
    ConversationId bigint NULL,
    TargetUserId bigint NULL,
    CreatedAt datetime2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    DispatchedAt datetime2(3) NULL,
    CHECK (ConversationId IS NOT NULL OR TargetUserId IS NOT NULL));
CREATE INDEX IX_ChangeLog_Conversation ON dbo.ChangeLog(ConversationId, Seq);
CREATE INDEX IX_ChangeLog_User ON dbo.ChangeLog(TargetUserId, Seq);
CREATE INDEX IX_ChangeLog_Pending ON dbo.ChangeLog(Seq) WHERE DispatchedAt IS NULL;
GO
/* باید آخرین نوشتن تراکنش باشد تا مدت نگه‌داشتن قفل کوتاه بماند */
CREATE PROCEDURE dbo.usp_AppendChange
    @ChangeType tinyint,
    @EntityId nvarchar(64),
    @ConversationId bigint = NULL,
    @TargetUserId bigint = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT = 0 THROW 51000, 'usp_AppendChange requires an open transaction.', 1;
    DECLARE @Seq bigint;
    UPDATE dbo.SyncState SET @Seq = LastSeq = LastSeq + 1 WHERE Id = 1;
    INSERT dbo.ChangeLog(Seq, ChangeType, EntityId, ConversationId, TargetUserId)
    VALUES (@Seq, @ChangeType, @EntityId, @ConversationId, @TargetUserId);
END
GO
/* دفاع دوم در برابر چرخه و عمق بیش از 3؛ applock جابه‌جایی‌های هم‌زمان را سریال می‌کند */
CREATE TRIGGER dbo.TR_Groups_Hierarchy ON dbo.Groups AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(ParentGroupId) RETURN;
    DECLARE @lock int;
    EXEC @lock = sp_getapplock @Resource = N'Sepehr.GroupHierarchy', @LockMode = 'Exclusive', @LockTimeout = 10000;
    IF @lock < 0 THROW 51001, 'Could not lock group hierarchy.', 1;

    ;WITH Walk AS (
        SELECT g.GroupId AS StartId, g.ParentGroupId, 1 AS Depth
        FROM dbo.Groups g WITH (READCOMMITTEDLOCK)
        UNION ALL
        SELECT w.StartId, p.ParentGroupId, w.Depth + 1
        FROM Walk w
        JOIN dbo.Groups p WITH (READCOMMITTEDLOCK) ON p.GroupId = w.ParentGroupId
        WHERE w.Depth < 4
    )
    SELECT TOP (1) StartId INTO #Violation FROM Walk WHERE Depth > 3;

    IF EXISTS (SELECT 1 FROM #Violation)
        THROW 51002, 'Group hierarchy has a cycle or exceeds depth 3.', 1;
END
GO
INSERT dbo.SyncState(Id, LastSeq) VALUES (1, 0);

INSERT dbo.Roles(RoleCode, RoleName, Scope) VALUES
 (N'GlobalAdmin', N'مدیرکل', 1),
 (N'GroupAdmin', N'مدیر گروه', 2),
 (N'GroupCoordinator', N'هماهنگ‌کننده گروه', 2),
 (N'Member', N'کاربر عادی', 2);

-- BEGIN PERMISSIONS
INSERT dbo.Permissions(PermissionCode, PermissionName)
SELECT v.Code, v.Code FROM (VALUES
 (N'User.View'),(N'User.Create'),(N'User.Update'),(N'User.Disable'),(N'User.Import'),
 (N'Group.View'),(N'Group.Create'),(N'Group.Update'),(N'Group.Delete'),(N'Group.MemberManage'),(N'Group.RoleManage'),
 (N'Message.Send'),(N'Message.Read'),(N'Message.EditOwn'),(N'Message.DeleteOwn'),(N'Message.DeleteGroup'),
 (N'Announcement.Create'),(N'Announcement.Schedule'),
 (N'File.Upload'),(N'File.Download'),(N'File.ExecutableSend'),
 (N'Settings.Manage'),(N'Audit.View'),(N'Reports.View')
) AS v(Code);
-- END PERMISSIONS

/* همان پیش‌فرض‌های Sepehr.Domain.Roles */
INSERT dbo.RolePermissions(RoleId, PermissionId)
SELECT r.RoleId, p.PermissionId FROM dbo.Roles r CROSS JOIN dbo.Permissions p
WHERE (r.RoleCode = N'GlobalAdmin' AND p.PermissionCode IN (
        N'User.View',N'User.Create',N'User.Update',N'User.Disable',N'User.Import',
        N'Group.View',N'Group.Create',N'Group.Update',N'Group.Delete',N'Group.MemberManage',N'Group.RoleManage',
        N'Announcement.Create',N'Announcement.Schedule',N'Settings.Manage',N'Reports.View'))
   OR (r.Scope = 2 AND p.PermissionCode IN (
        N'Group.View',N'Message.Send',N'Message.Read',N'Message.EditOwn',N'Message.DeleteOwn',
        N'File.Upload',N'File.Download'))
   OR (r.RoleCode = N'GroupAdmin' AND p.PermissionCode IN (
        N'Group.Update',N'Group.MemberManage',N'Group.RoleManage',N'Message.DeleteGroup',
        N'Announcement.Create',N'Announcement.Schedule'));

INSERT dbo.RetentionPolicies(AppliesTo, RetentionDays) VALUES (1, 30), (2, 30), (3, NULL);

INSERT dbo.SystemSettings(SettingKey, SettingValue) VALUES
 (N'File.MaxBytes', N'26214400'),
 (N'File.AllowExecutable', N'false'),
 (N'Voice.MaxSeconds', N'180'),
 (N'Session.IdleMinutes', N'30'),
 (N'Session.AbsoluteHours', N'12'),
 (N'Login.MaxFailures', N'5'),
 (N'Login.WindowMinutes', N'15'),
 (N'Login.LockMinutes', N'15'),
 (N'Message.OwnerEditMinutes', N'5'),
 (N'Import.MaxRows', N'5000');

INSERT dbo.SchemaVersions(Version) VALUES (1);
COMMIT TRANSACTION;
GO
SET NOEXEC OFF;
GO
