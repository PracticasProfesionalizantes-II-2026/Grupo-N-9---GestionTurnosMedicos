BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007174135_AgregarMovimientos'
)
BEGIN
    CREATE TABLE [Movimientos] (
        [Id] int NOT NULL IDENTITY,
        [FechaUtc] datetime2 NOT NULL,
        [IdUsuario] int NOT NULL,
        [RolUsuario] nvarchar(20) NOT NULL,
        [Accion] nvarchar(40) NOT NULL,
        [Entidad] nvarchar(20) NOT NULL,
        [IdEntidad] int NOT NULL,
        [IdDoctor] int NULL,
        [Resumen] nvarchar(300) NOT NULL,
        CONSTRAINT [PK_Movimientos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Movimientos_Doctores_IdDoctor] FOREIGN KEY ([IdDoctor]) REFERENCES [Doctores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Movimientos_Usuarios_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007174135_AgregarMovimientos'
)
BEGIN
    CREATE INDEX [IX_Movimientos_Accion_FechaUtc] ON [Movimientos] ([Accion], [FechaUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007174135_AgregarMovimientos'
)
BEGIN
    CREATE INDEX [IX_Movimientos_FechaUtc] ON [Movimientos] ([FechaUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007174135_AgregarMovimientos'
)
BEGIN
    CREATE INDEX [IX_Movimientos_IdDoctor_FechaUtc] ON [Movimientos] ([IdDoctor], [FechaUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007174135_AgregarMovimientos'
)
BEGIN
    CREATE INDEX [IX_Movimientos_IdUsuario] ON [Movimientos] ([IdUsuario]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007174135_AgregarMovimientos'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007174135_AgregarMovimientos', N'10.0.7');
END;

COMMIT;
GO

