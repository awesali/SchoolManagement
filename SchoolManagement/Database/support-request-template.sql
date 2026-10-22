-- Approved support request template. Safe to run more than once.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.EmailTemplates WHERE TemplateName = N'SupportRequest')
    UPDATE dbo.EmailTemplates
    SET Subject = N'Support request: {{Subject}} — {{SchoolName}}',
        Body = N'A support request was submitted. Name: {{Name}}. Role: {{Role}}. School: {{SchoolName}}. Email: {{Email}}. Subject: {{Subject}}. Problem: {{Problem}}.',
        IsActive = 1
    WHERE TemplateName = N'SupportRequest';
ELSE
    INSERT INTO dbo.EmailTemplates (TemplateName, Subject, Body, IsActive)
    VALUES (N'SupportRequest',
            N'Support request: {{Subject}} — {{SchoolName}}',
            N'A support request was submitted. Name: {{Name}}. Role: {{Role}}. School: {{SchoolName}}. Email: {{Email}}. Subject: {{Subject}}. Problem: {{Problem}}.',
            1);

COMMIT TRANSACTION;
