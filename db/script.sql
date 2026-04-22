IF DB_ID('TicketPrime') IS NULL
BEGIN
    CREATE DATABASE TicketPrime;
END
GO

USE TicketPrime;
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE Usuarios (
        Cpf VARCHAR(11) PRIMARY KEY,
        Nome VARCHAR(100) NOT NULL,
        Email VARCHAR(100) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Eventos', 'U') IS NULL
BEGIN
    CREATE TABLE Eventos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nome VARCHAR(100) NOT NULL,
        CapacidadeTotal INT NOT NULL,
        DataEvento DATETIME NOT NULL,
        PrecoPadrao DECIMAL(18,2) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Cupons', 'U') IS NULL
BEGIN
    CREATE TABLE Cupons (
        Codigo VARCHAR(20) PRIMARY KEY,
        PorcentagemDesconto DECIMAL(5,2) NOT NULL,
        ValorMinimoRegra DECIMAL(18,2) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Reservas', 'U') IS NULL
BEGIN
    CREATE TABLE Reservas (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioCpf VARCHAR(11) NOT NULL,
        EventoId INT NOT NULL,
        CupomUtilizado VARCHAR(20) NULL,
        ValorFinalPago DECIMAL(18,2) NOT NULL,
        DataReserva DATETIME DEFAULT GETDATE(),

        CONSTRAINT FK_Reservas_Usuarios FOREIGN KEY (UsuarioCpf) REFERENCES Usuarios(Cpf),
        CONSTRAINT FK_Reservas_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
        CONSTRAINT FK_Reservas_Cupons FOREIGN KEY (CupomUtilizado) REFERENCES Cupons(Codigo)
    );
END
GO
