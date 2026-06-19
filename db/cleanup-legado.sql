-- =============================================
-- CLEANUP LEGADO - RedCode
-- =============================================
-- ATENCAO: Este script remove as tabelas legadas do RedCode.
-- Execute APENAS apos confirmar que todos os dados do FlyCompare
-- estao funcionando corretamente e que a migracao foi validada.
-- =============================================
-- USE RedCode;
-- =============================================

IF OBJECT_ID('dbo.Reservas', 'U') IS NOT NULL DROP TABLE dbo.Reservas;
IF OBJECT_ID('dbo.Cupons', 'U') IS NOT NULL DROP TABLE dbo.Cupons;
IF OBJECT_ID('dbo.Eventos', 'U') IS NOT NULL DROP TABLE dbo.Eventos;
IF OBJECT_ID('dbo.Usuarios', 'U') IS NOT NULL DROP TABLE dbo.Usuarios;
GO

PRINT 'Tabelas legadas do RedCode removidas com sucesso.';
GO
