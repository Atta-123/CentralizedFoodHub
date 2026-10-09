IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE TABLE [Restaurants] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Cuisine] nvarchar(80) NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [CommissionPercent] decimal(5,2) NOT NULL,
        [IsOpen] bit NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [DeliveryRadiusKm] float NOT NULL,
        CONSTRAINT [PK_Restaurants] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Restaurants_Commission] CHECK ([CommissionPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Restaurants_Location] CHECK (([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)),
        CONSTRAINT [CK_Restaurants_Radius] CHECK ([DeliveryRadiusKm] BETWEEN 0.1 AND 50),
        CONSTRAINT [FK_Restaurants_Users_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] uniqueidentifier NOT NULL,
        [RestaurantId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [PriceCents] bigint NOT NULL,
        [IsAvailable] bit NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MenuItems_Price] CHECK ([PriceCents] > 0),
        CONSTRAINT [FK_MenuItems_Restaurants_RestaurantId] FOREIGN KEY ([RestaurantId]) REFERENCES [Restaurants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [RestaurantId] uniqueidentifier NOT NULL,
        [RestaurantName] nvarchar(100) NOT NULL,
        [CustomerName] nvarchar(80) NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [Phone] nvarchar(30) NOT NULL,
        [SubtotalCents] bigint NOT NULL,
        [CommissionCents] bigint NOT NULL,
        [RestaurantPayableCents] bigint NOT NULL,
        [CommissionPercent] decimal(5,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IdempotencyKey] nvarchar(80) NOT NULL,
        [DeliveryLatitude] float NULL,
        [DeliveryLongitude] float NULL,
        [RequestHash] nvarchar(64) NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Orders_Commission] CHECK ([CommissionPercent] BETWEEN 0 AND 100),
        CONSTRAINT [CK_Orders_Totals] CHECK ([SubtotalCents] >= 0 AND [CommissionCents] >= 0 AND [RestaurantPayableCents] >= 0 AND [SubtotalCents] = [CommissionCents] + [RestaurantPayableCents]),
        CONSTRAINT [FK_Orders_Restaurants_RestaurantId] FOREIGN KEY ([RestaurantId]) REFERENCES [Restaurants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderHistories] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [ActorId] uniqueidentifier NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_OrderHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderHistories_Users_ActorId] FOREIGN KEY ([ActorId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [MenuItemId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [UnitPriceCents] bigint NOT NULL,
        [Quantity] int NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderItems_QuantityPrice] CHECK ([Quantity] BETWEEN 1 AND 20 AND [UnitPriceCents] > 0),
        CONSTRAINT [FK_OrderItems_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MenuItems_RestaurantId_IsAvailable] ON [MenuItems] ([RestaurantId], [IsAvailable]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderHistories_ActorId] ON [OrderHistories] ([ActorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderHistories_OrderId_ChangedAt] ON [OrderHistories] ([OrderId], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_MenuItemId] ON [OrderItems] ([MenuItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId_CreatedAt] ON [Orders] ([CustomerId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_CustomerId_IdempotencyKey] ON [Orders] ([CustomerId], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_RestaurantId_Status_CreatedAt] ON [Orders] ([RestaurantId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Restaurants_Latitude_Longitude] ON [Restaurants] ([Latitude], [Longitude]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Restaurants_OwnerId] ON [Restaurants] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008065408_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008065408_InitialCreate', N'10.0.0');
END;

COMMIT;
GO

