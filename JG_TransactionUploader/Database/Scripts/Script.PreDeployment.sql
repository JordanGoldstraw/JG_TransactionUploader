﻿IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'JG_FileUploader')
BEGIN
    CREATE DATABASE JG_FileUploader;
END
GO

USE JG_FileUploader;
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Transactions')
BEGIN
    CREATE TABLE Transactions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TransactionId NVARCHAR(50) NOT NULL,
        TransactionDate DATETIME NOT NULL,
        AccountNo NVARCHAR(50) NOT NULL,
        Amount DECIMAL(18, 2) NOT NULL,
        CurrencyCode NVARCHAR(3) NOT NULL,
        Status NVARCHAR(20) NOT NULL
    );
END
GO
