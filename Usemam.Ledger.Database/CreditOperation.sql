CREATE TABLE [dbo].[CreditOperation]
(
	[Id] INT NOT NULL constraint [PK_CreditOperation] PRIMARY KEY,
	[Created] datetime not null,
	[Updated] datetime not null,
	[Amount] decimal(18,2) not null,
	[CurrencyCode] varchar(3) not null,
	[DepositId] int not null,
	[CategoryId] int not null,
	[Note] nvarchar(max), 
    CONSTRAINT [FK_CreditOperation_Deposit] FOREIGN KEY ([DepositId]) REFERENCES [Deposit]([Id]), 
    CONSTRAINT [FK_CreditOperation_CreditCategory] FOREIGN KEY ([CategoryId]) REFERENCES [CreditCategory]([Id])
)
