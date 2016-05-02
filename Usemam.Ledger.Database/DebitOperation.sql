CREATE TABLE [dbo].[DebitOperation]
(
	[Id] INT identity(1,1) NOT NULL constraint [PK_DebitOperation] PRIMARY KEY,
	[Created] datetime not null,
	[Updated] datetime not null,
	[Amount] decimal(18,2) not null,
	[CurrencyCode] varchar(3) not null,
	[DepositId] int not null,
	[CategoryId] int not null,
	[Note] nvarchar(max), 
    CONSTRAINT [FK_DebitOperation_Deposit] FOREIGN KEY ([DepositId]) REFERENCES [Deposit]([Id]), 
    CONSTRAINT [FK_DebitOperation_DebitCategory] FOREIGN KEY ([CategoryId]) REFERENCES [DebitCategory]([Id])
)
