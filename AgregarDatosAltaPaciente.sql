BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008172540_AgregarDatosAltaPaciente'
)
BEGIN
    ALTER TABLE [Pacientes] ADD [CodigoPostal] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008172540_AgregarDatosAltaPaciente'
)
BEGIN
    ALTER TABLE [Pacientes] ADD [ContactoEmergenciaNombre] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008172540_AgregarDatosAltaPaciente'
)
BEGIN
    ALTER TABLE [Pacientes] ADD [ContactoEmergenciaTelefono] nvarchar(30) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008172540_AgregarDatosAltaPaciente'
)
BEGIN
    ALTER TABLE [Pacientes] ADD [Localidad] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008172540_AgregarDatosAltaPaciente'
)
BEGIN
    ALTER TABLE [Pacientes] ADD [Provincia] nvarchar(80) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008172540_AgregarDatosAltaPaciente'
)
BEGIN
    ALTER TABLE [Pacientes] ADD [TipoDocumento] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008172540_AgregarDatosAltaPaciente'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008172540_AgregarDatosAltaPaciente', N'10.0.7');
END;

COMMIT;
GO

