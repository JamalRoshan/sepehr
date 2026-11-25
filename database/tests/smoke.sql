/* آزمون قیود پس از Migration؛ در دیتابیس آزمایشی اجرا شود. همه تغییرات Rollback می‌شوند.
   هر خطای مورد انتظار در TRY جدا گرفته می‌شود؛ XACT_ABORT خاموش است. */
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT OFF;
SET NOCOUNT ON;

DECLARE @u1 bigint, @u2 bigint, @c bigint, @ok bit;

BEGIN TRAN;
INSERT dbo.Users(PersonnelCode,Username,DisplayName,SearchText) VALUES (N'T1',N'T1',N'یک',N'یک');
SET @u1 = SCOPE_IDENTITY();
INSERT dbo.Users(PersonnelCode,Username,DisplayName,SearchText) VALUES (N'T2',N'T2',N'دو',N'دو');
SET @u2 = SCOPE_IDENTITY();

INSERT dbo.Conversations(ConversationType,CreatedBy) VALUES (1,@u1);
SET @c = SCOPE_IDENTITY();
INSERT dbo.PrivateConversationPairs VALUES (@c,@u1,@u2);

/* 1) زوج نامرتب رد شود */
SET @ok = 0;
BEGIN TRY
    INSERT dbo.Conversations(ConversationType,CreatedBy) VALUES (1,@u1);
    INSERT dbo.PrivateConversationPairs VALUES (SCOPE_IDENTITY(),@u2,@u1);
END TRY BEGIN CATCH SET @ok = 1; END CATCH;
IF @ok = 0 THROW 52001, 'unordered pair accepted', 1;

/* 2) پیام تکراری (Idempotency) رد شود */
DECLARE @cm uniqueidentifier = NEWID();
INSERT dbo.Messages(ConversationId,SenderUserId,ClientMessageId,MessageType,TextContent) VALUES (@c,@u1,@cm,1,N'a');
SET @ok = 0;
BEGIN TRY
    INSERT dbo.Messages(ConversationId,SenderUserId,ClientMessageId,MessageType,TextContent) VALUES (@c,@u1,@cm,1,N'a');
END TRY BEGIN CATCH SET @ok = 1; END CATCH;
IF @ok = 0 THROW 52002, 'duplicate message accepted', 1;

/* 3) گفتگوی گروهی بدون GroupId رد شود */
SET @ok = 0;
BEGIN TRY INSERT dbo.Conversations(ConversationType,CreatedBy) VALUES (2,@u1); END TRY
BEGIN CATCH SET @ok = 1; END CATCH;
IF @ok = 0 THROW 52003, 'group conversation without group accepted', 1;

/* 4) نقش گروهی به‌عنوان نقش سراسری رد شود */
SET @ok = 0;
BEGIN TRY
    INSERT dbo.UserRoles(UserId,RoleId) SELECT @u1, RoleId FROM dbo.Roles WHERE RoleCode=N'GroupAdmin';
END TRY BEGIN CATCH SET @ok = 1; END CATCH;
IF @ok = 0 THROW 52004, 'group role accepted as global role', 1;

/* 5) Sync داخل تراکنش، افزایشی */
EXEC dbo.usp_AppendChange 1, N'1', @c, NULL;
EXEC dbo.usp_AppendChange 1, N'2', @c, NULL;
IF (SELECT COUNT(*) FROM dbo.ChangeLog WHERE ConversationId=@c) <> 2 THROW 52005, 'sync failed', 1;

IF @@TRANCOUNT > 0 ROLLBACK;

/* 6) چرخه گروه رد شود (خطای Trigger تراکنش را باطل می‌کند، پس تراکنش جدا) */
SET @ok = 0;
BEGIN TRY
    BEGIN TRAN;
    INSERT dbo.Users(PersonnelCode,Username,DisplayName,SearchText) VALUES (N'T3',N'T3',N'سه',N'سه');
    SET @u1 = SCOPE_IDENTITY();
    INSERT dbo.Groups(Name,SearchText,CreatedBy) VALUES (N'a',N'a',@u1);
    DECLARE @g1 bigint = SCOPE_IDENTITY();
    INSERT dbo.Groups(ParentGroupId,Name,SearchText,CreatedBy) VALUES (@g1,N'b',N'b',@u1);
    DECLARE @g2 bigint = SCOPE_IDENTITY();
    UPDATE dbo.Groups SET ParentGroupId=@g2 WHERE GroupId=@g1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 51002 SET @ok = 1; ELSE PRINT ERROR_MESSAGE();
END CATCH;
IF @@TRANCOUNT > 0 ROLLBACK;
IF @ok = 0 THROW 52006, 'group cycle accepted', 1;

PRINT 'SMOKE OK';
