-- Approved demo request email template. Run once against the application database.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.EmailTemplates WHERE TemplateName = N'DemoRequest')
    UPDATE dbo.EmailTemplates
    SET Subject = N'New demo request from {{Name}} — {{SchoolName}}',
        Body = N'A new demo request was submitted. Name: {{Name}}. School: {{SchoolName}}. Email: {{Email}}. Phone: {{Phone}}. Message: {{Message}}.',
        IsActive = 1
    WHERE TemplateName = N'DemoRequest';
ELSE
    INSERT INTO dbo.EmailTemplates (TemplateName, Subject, Body, IsActive)
    VALUES (
        N'DemoRequest',
        N'New demo request from {{Name}} — {{SchoolName}}',
        N'A new demo request was submitted. Name: {{Name}}. School: {{SchoolName}}. Email: {{Email}}. Phone: {{Phone}}. Message: {{Message}}.',
        1
    );

COMMIT TRANSACTION;
