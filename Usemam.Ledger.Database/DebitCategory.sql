CREATE TABLE [dbo].[DebitCategory]
(
	[Id] INT NOT NULL constraint [PK_DebitCategory] PRIMARY KEY,
	[Name] nvarchar(500) not null,
	[Description] nvarchar(max)
)
