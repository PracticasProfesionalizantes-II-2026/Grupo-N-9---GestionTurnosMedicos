BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004164901_AgregarUsuarioFoto'
)
BEGIN
    CREATE TABLE [UsuarioFotos] (
        [IdUsuario] int NOT NULL,
        [Contenido] varbinary(max) NOT NULL,
        [TipoContenido] nvarchar(50) NOT NULL,
        [ActualizadaEn] datetime2 NOT NULL,
        CONSTRAINT [PK_UsuarioFotos] PRIMARY KEY ([IdUsuario]),
        CONSTRAINT [FK_UsuarioFotos_Usuarios_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004164901_AgregarUsuarioFoto'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004164901_AgregarUsuarioFoto', N'10.0.7');
END;

COMMIT;
GO

