IF COL_LENGTH('FeePayments', 'AcknowledgementId') IS NULL
BEGIN
    ALTER TABLE FeePayments ADD AcknowledgementId nvarchar(100) NULL;
END;
