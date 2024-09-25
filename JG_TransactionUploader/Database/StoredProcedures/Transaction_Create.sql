CREATE PROCEDURE [dbo].[Transaction_Create]
    @TransactionId NVARCHAR(50),
    @TransactionDate DATETIME,
    @AccountNo NVARCHAR(50),
    @Amount DECIMAL(18, 2),
    @CurrencyCode NVARCHAR(10),
    @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[Transactions] (
        [TransactionId], 
        [TransactionDate], 
        [AccountNo], 
        [Amount], 
        [CurrencyCode], 
        [Status]
    )
    VALUES (
        @TransactionId, 
        @TransactionDate, 
        @AccountNo, 
        @Amount, 
        @CurrencyCode, 
        @Status
    );
END
GO
