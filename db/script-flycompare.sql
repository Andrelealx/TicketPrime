-- =============================================
-- FLYCOMPARE - Script de Banco de Dados
-- Metabuscador de Passagens Aereas
-- =============================================
-- Este script e IDEMPOTENTE: pode ser executado
-- multiplas vezes sem causar erros.
-- =============================================

IF DB_ID('RedCode') IS NULL
BEGIN
    CREATE DATABASE RedCode;
END
GO

USE RedCode;
GO

-- =============================================
-- TABELAS DO FLYCOMPARE
-- =============================================

-- Companhias Aereas
IF OBJECT_ID('dbo.CompanhiasAereas', 'U') IS NULL
BEGIN
    CREATE TABLE CompanhiasAereas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Codigo VARCHAR(5) NOT NULL UNIQUE,
        Nome VARCHAR(100) NOT NULL,
        SiteBase VARCHAR(500) NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        DataCadastro DATETIME DEFAULT GETDATE()
    );
END
GO

-- Aeroportos
IF OBJECT_ID('dbo.Aeroportos', 'U') IS NULL
BEGIN
    CREATE TABLE Aeroportos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CodigoIATA VARCHAR(3) NOT NULL UNIQUE,
        Nome VARCHAR(200) NOT NULL,
        Cidade VARCHAR(100) NOT NULL,
        Estado VARCHAR(5),
        Pais VARCHAR(50) NOT NULL DEFAULT 'Brasil',
        Latitude DECIMAL(10,7),
        Longitude DECIMAL(10,7)
    );
END
GO

-- Rotas
IF OBJECT_ID('dbo.Rotas', 'U') IS NULL
BEGIN
    CREATE TABLE Rotas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrigemId INT NOT NULL,
        DestinoId INT NOT NULL,
        CONSTRAINT FK_Rotas_Origem FOREIGN KEY (OrigemId) REFERENCES Aeroportos(Id),
        CONSTRAINT FK_Rotas_Destino FOREIGN KEY (DestinoId) REFERENCES Aeroportos(Id),
        CONSTRAINT UQ_Rotas UNIQUE (OrigemId, DestinoId)
    );
END
GO

-- Voos (resultado de scraping)
IF OBJECT_ID('dbo.Voos', 'U') IS NULL
BEGIN
    CREATE TABLE Voos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RotaId INT NOT NULL,
        CompanhiaId INT NOT NULL,
        CodigoVoo VARCHAR(20) NOT NULL,
        DataPartida DATETIME NOT NULL,
        DataChegada DATETIME NOT NULL,
        DuracaoMinutos INT NOT NULL,
        Paradas INT NOT NULL DEFAULT 0,
        AeroportoEscalaId INT NULL,
        Classe VARCHAR(50) DEFAULT 'Econômica',
        CONSTRAINT FK_Voos_Rota FOREIGN KEY (RotaId) REFERENCES Rotas(Id),
        CONSTRAINT FK_Voos_Companhia FOREIGN KEY (CompanhiaId) REFERENCES CompanhiasAereas(Id),
        CONSTRAINT FK_Voos_Escala FOREIGN KEY (AeroportoEscalaId) REFERENCES Aeroportos(Id)
    );
END
GO

-- Precos (historico de precos para cada voo)
IF OBJECT_ID('dbo.Precos', 'U') IS NULL
BEGIN
    CREATE TABLE Precos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        VooId INT NOT NULL,
        Preco DECIMAL(18,2) NOT NULL,
        Taxas DECIMAL(18,2) NOT NULL DEFAULT 0,
        PrecoTotal DECIMAL(18,2) NOT NULL,
        Moeda VARCHAR(3) NOT NULL DEFAULT 'BRL',
        TipoTarifa VARCHAR(50) NOT NULL DEFAULT 'Econômica',
        BagagemIncluida BIT NOT NULL DEFAULT 0,
        FranquiaBagagemKg INT NULL,
        UrlDestino VARCHAR(1000) NOT NULL,
        Fonte VARCHAR(100) NOT NULL,
        DataColeta DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Precos_Voo FOREIGN KEY (VooId) REFERENCES Voos(Id)
    );
END
GO

-- Alertas de Preco
IF OBJECT_ID('dbo.AlertasPreco', 'U') IS NULL
BEGIN
    CREATE TABLE AlertasPreco (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Email VARCHAR(200) NOT NULL,
        RotaId INT NOT NULL,
        PrecoAlvo DECIMAL(18,2) NOT NULL,
        Ativo BIT NOT NULL DEFAULT 1,
        DataCriacao DATETIME DEFAULT GETDATE(),
        CONSTRAINT FK_Alertas_Rota FOREIGN KEY (RotaId) REFERENCES Rotas(Id)
    );
END
GO

-- Cache de Busca (tabela auxiliar para fallback se Redis nao disponivel)
IF OBJECT_ID('dbo.CacheBusca', 'U') IS NULL
BEGIN
    CREATE TABLE CacheBusca (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ChaveCache VARCHAR(500) NOT NULL UNIQUE,
        ResultadoJson NVARCHAR(MAX) NOT NULL,
        DataExpiracao DATETIME NOT NULL,
        DataCriacao DATETIME DEFAULT GETDATE()
    );
END
GO

-- =============================================
-- SEED DATA - DADOS DE REFERENCIA
-- =============================================

-- Companhias Aereas Brasileiras
IF NOT EXISTS (SELECT 1 FROM CompanhiasAereas WHERE Codigo = 'LATAM')
BEGIN
    INSERT INTO CompanhiasAereas (Codigo, Nome, SiteBase) VALUES
    ('LATAM', 'LATAM Airlines Brasil', 'https://www.latam.com'),
    ('GOL', 'GOL Linhas Aereas', 'https://www.voegol.com.br'),
    ('AZUL', 'Azul Linhas Aereas', 'https://www.voeazul.com.br');
END
GO

-- Aeroportos Brasileiros (principais)
IF NOT EXISTS (SELECT 1 FROM Aeroportos WHERE CodigoIATA = 'GRU')
BEGIN
    INSERT INTO Aeroportos (CodigoIATA, Nome, Cidade, Estado, Pais) VALUES
    ('GRU', 'Aeroporto Internacional de Sao Paulo', 'Sao Paulo', 'SP', 'Brasil'),
    ('CGH', 'Aeroporto de Congonhas', 'Sao Paulo', 'SP', 'Brasil'),
    ('GIG', 'Aeroporto Internacional do Rio de Janeiro', 'Rio de Janeiro', 'RJ', 'Brasil'),
    ('SDU', 'Aeroporto Santos Dumont', 'Rio de Janeiro', 'RJ', 'Brasil'),
    ('BSB', 'Aeroporto Internacional de Brasilia', 'Brasilia', 'DF', 'Brasil'),
    ('REC', 'Aeroporto Internacional do Recife', 'Recife', 'PE', 'Brasil'),
    ('SSA', 'Aeroporto Internacional de Salvador', 'Salvador', 'BA', 'Brasil'),
    ('CNF', 'Aeroporto Internacional de Belo Horizonte', 'Belo Horizonte', 'MG', 'Brasil'),
    ('POA', 'Aeroporto Internacional de Porto Alegre', 'Porto Alegre', 'RS', 'Brasil'),
    ('CWB', 'Aeroporto Internacional de Curitiba', 'Curitiba', 'PR', 'Brasil'),
    ('FOR', 'Aeroporto Internacional de Fortaleza', 'Fortaleza', 'CE', 'Brasil'),
    ('MAO', 'Aeroporto Internacional de Manaus', 'Manaus', 'AM', 'Brasil'),
    ('VIX', 'Aeroporto de Vitoria', 'Vitoria', 'ES', 'Brasil'),
    ('FLN', 'Aeroporto Internacional de Florianopolis', 'Florianopolis', 'SC', 'Brasil'),
    ('BEL', 'Aeroporto Internacional de Belem', 'Belem', 'PA', 'Brasil');
END
GO

-- Rotas Populares
IF NOT EXISTS (SELECT 1 FROM Rotas WHERE OrigemId = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'GRU') AND DestinoId = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'REC'))
BEGIN
    DECLARE @GRU INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'GRU');
    DECLARE @CGH INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'CGH');
    DECLARE @GIG INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'GIG');
    DECLARE @SDU INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'SDU');
    DECLARE @BSB INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'BSB');
    DECLARE @REC INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'REC');
    DECLARE @SSA INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'SSA');
    DECLARE @CNF INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'CNF');
    DECLARE @POA INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'POA');
    DECLARE @FOR INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'FOR');
    DECLARE @CWB INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'CWB');
    DECLARE @VIX INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'VIX');
    DECLARE @FLN INT = (SELECT Id FROM Aeroportos WHERE CodigoIATA = 'FLN');

    INSERT INTO Rotas (OrigemId, DestinoId) VALUES
    (@GRU, @REC), (@REC, @GRU),
    (@GRU, @GIG), (@GIG, @GRU),
    (@CGH, @SDU), (@SDU, @CGH),
    (@GRU, @BSB), (@BSB, @GRU),
    (@GRU, @SSA), (@SSA, @GRU),
    (@CGH, @POA), (@POA, @CGH),
    (@GRU, @CNF), (@CNF, @GRU),
    (@GRU, @FOR), (@FOR, @GRU),
    (@CGH, @CWB), (@CWB, @CGH),
    (@GRU, @VIX), (@VIX, @GRU),
    (@CGH, @FLN), (@FLN, @CGH);
END
GO

-- =============================================
-- FIM DO SCRIPT FLYCOMPARE
-- =============================================
GO
