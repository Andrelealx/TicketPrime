CREATE DATABASE TicketPrime;
GO
USE TicketPrime;
GO

CREATE TABLE Usuarios (
    Cpf VARCHAR(11) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL
);

CREATE TABLE Eventos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    CapacidadeTotal INT NOT NULL,
    DataEvento DATETIME NOT NULL,
    PrecoPadrao DECIMAL(18,2) NOT NULL
);

CREATE TABLE Cupons (
    Codigo VARCHAR(20) PRIMARY KEY,
    PorcentagemDesconto DECIMAL(5,2) NOT NULL,
    ValorMinimoRegra DECIMAL(18,2) NOT NULL
);

CREATE TABLE Reservas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CpfUsuario VARCHAR(11) NOT NULL,
    EventoId INT NOT NULL,
    CupomUtilizado VARCHAR(20) NULL,
    ValorFinalPago DECIMAL(18,2) NOT NULL,
    DataReserva DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT FK_Reservas_Usuarios FOREIGN KEY (CpfUsuario) REFERENCES Usuarios(Cpf),
    CONSTRAINT FK_Reservas_Eventos FOREIGN KEY (EventoId) REFERENCES Eventos(Id),
    CONSTRAINT FK_Reservas_Cupons FOREIGN KEY (CupomUtilizado) REFERENCES Cupons(Codigo)
);