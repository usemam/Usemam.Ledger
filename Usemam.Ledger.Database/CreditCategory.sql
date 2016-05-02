CREATE TABLE [dbo].[CreditCategory]
(
	[Id] INT NOT NULL constraint [PK_CreditCategory] PRIMARY KEY,
	[Name] nvarchar(500) not null,
	[Description] nvarchar(max)
)
