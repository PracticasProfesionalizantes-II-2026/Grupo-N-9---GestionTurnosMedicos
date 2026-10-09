BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    ALTER TABLE [RecetaMedicamentos] DROP CONSTRAINT [FK_RecetaMedicamentos_Medicamentos_IdMedicamento];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    ALTER TABLE [RecetaMedicamentos] ADD [Concentracion] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    ALTER TABLE [RecetaMedicamentos] ADD [FormaFarmaceutica] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    ALTER TABLE [RecetaMedicamentos] ADD [NombreGenerico] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    ALTER TABLE [RecetaMedicamentos] ADD [NombreMedicamento] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    EXEC(N'
                UPDATE rm
                SET rm.NombreMedicamento = m.Nombre,
                    rm.NombreGenerico = m.NombreGenerico,
                    rm.Concentracion = m.Concentracion,
                    rm.FormaFarmaceutica = m.FormaFarmaceutica
                FROM RecetaMedicamentos rm
                INNER JOIN Medicamentos m ON m.Id = rm.IdMedicamento
                WHERE rm.NombreMedicamento IS NULL;')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    ALTER TABLE [RecetaMedicamentos] ADD CONSTRAINT [FK_RecetaMedicamentos_Medicamentos_IdMedicamento] FOREIGN KEY ([IdMedicamento]) REFERENCES [Medicamentos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261009201436_CopiarMedicamentoEnReceta'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261009201436_CopiarMedicamentoEnReceta', N'10.0.7');
END;

COMMIT;
GO

