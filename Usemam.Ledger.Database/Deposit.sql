CREATE TABLE [dbo].[Deposit]
(
	[Id] INT NOT NULL constraint [PK_Deposit] PRIMARY KEY,
	[Created] datetime not null,
	[Updated] datetime not null,
	[Name] nvarchar(500) not null,
	[Amount] decimal(18,2) not null,
	[CurrencyCode] varchar(3) not null
)
