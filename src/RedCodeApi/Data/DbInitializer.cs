using Dapper;
using Microsoft.Data.Sqlite;

namespace RedCodeApi.Data;

/// <summary>
/// Inicializa o banco de dados SQLite com as tabelas e seed data do FlyCompare.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        using var db = new SqliteConnection(connectionString);
        db.Open();

        CreateTables(db);
        SeedCompanhias(db);
        SeedAeroportos(db);
        SeedRotas(db);
    }

    private static void CreateTables(SqliteConnection db)
    {
        db.Execute(@"
            CREATE TABLE IF NOT EXISTS CompanhiasAereas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Codigo VARCHAR(5) NOT NULL UNIQUE,
                Nome VARCHAR(100) NOT NULL,
                SiteBase VARCHAR(500) NOT NULL,
                Ativo INTEGER NOT NULL DEFAULT 1,
                DataCadastro TEXT DEFAULT (datetime('now'))
            );

            CREATE TABLE IF NOT EXISTS Aeroportos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CodigoIATA VARCHAR(3) NOT NULL UNIQUE,
                Nome VARCHAR(200) NOT NULL,
                Cidade VARCHAR(100) NOT NULL,
                Estado VARCHAR(5),
                Pais VARCHAR(50) NOT NULL DEFAULT 'Brasil',
                Latitude REAL,
                Longitude REAL
            );

            CREATE TABLE IF NOT EXISTS Rotas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                OrigemId INTEGER NOT NULL,
                DestinoId INTEGER NOT NULL,
                FOREIGN KEY (OrigemId) REFERENCES Aeroportos(Id),
                FOREIGN KEY (DestinoId) REFERENCES Aeroportos(Id),
                UNIQUE (OrigemId, DestinoId)
            );

            CREATE TABLE IF NOT EXISTS Voos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RotaId INTEGER NOT NULL,
                CompanhiaId INTEGER NOT NULL,
                CodigoVoo VARCHAR(20) NOT NULL,
                DataPartida TEXT NOT NULL,
                DataChegada TEXT NOT NULL,
                DuracaoMinutos INTEGER NOT NULL,
                Paradas INTEGER NOT NULL DEFAULT 0,
                AeroportoEscalaId INTEGER NULL,
                Classe VARCHAR(50) DEFAULT 'Econômica',
                FOREIGN KEY (RotaId) REFERENCES Rotas(Id),
                FOREIGN KEY (CompanhiaId) REFERENCES CompanhiasAereas(Id),
                FOREIGN KEY (AeroportoEscalaId) REFERENCES Aeroportos(Id)
            );

            CREATE TABLE IF NOT EXISTS Precos (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VooId INTEGER NOT NULL,
                Preco REAL NOT NULL,
                Taxas REAL NOT NULL DEFAULT 0,
                PrecoTotal REAL NOT NULL,
                Moeda VARCHAR(3) NOT NULL DEFAULT 'BRL',
                TipoTarifa VARCHAR(50) NOT NULL DEFAULT 'Econômica',
                BagagemIncluida INTEGER NOT NULL DEFAULT 0,
                FranquiaBagagemKg INTEGER NULL,
                UrlDestino VARCHAR(1000) NOT NULL,
                Fonte VARCHAR(100) NOT NULL,
                DataColeta TEXT NOT NULL DEFAULT (datetime('now')),
                FOREIGN KEY (VooId) REFERENCES Voos(Id)
            );

            CREATE TABLE IF NOT EXISTS AlertasPreco (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email VARCHAR(200) NOT NULL,
                RotaId INTEGER NOT NULL,
                PrecoAlvo REAL NOT NULL,
                Ativo INTEGER NOT NULL DEFAULT 1,
                DataCriacao TEXT DEFAULT (datetime('now')),
                FOREIGN KEY (RotaId) REFERENCES Rotas(Id)
            );
        ");
    }

    private static void SeedCompanhias(SqliteConnection db)
    {
        var qtd = db.ExecuteScalar<int>("SELECT COUNT(*) FROM CompanhiasAereas");
        if (qtd == 0)
        {
            db.Execute(@"
                INSERT INTO CompanhiasAereas (Codigo, Nome, SiteBase) VALUES
                ('LATAM', 'LATAM Airlines Brasil', 'https://www.latam.com'),
                ('GOL', 'GOL Linhas Aereas', 'https://www.voegol.com.br'),
                ('AZUL', 'Azul Linhas Aereas', 'https://www.voeazul.com.br');
            ");
        }
    }

    private static void SeedAeroportos(SqliteConnection db)
    {
        var qtd = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Aeroportos");
        if (qtd == 0)
        {
            db.Execute(@"
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
            ");
        }
    }

    private static void SeedRotas(SqliteConnection db)
    {
        var qtd = db.ExecuteScalar<int>("SELECT COUNT(*) FROM Rotas");
        if (qtd == 0)
        {
            db.Execute(@"
                INSERT INTO Rotas (OrigemId, DestinoId)
                SELECT a1.Id, a2.Id FROM Aeroportos a1, Aeroportos a2
                WHERE (a1.CodigoIATA = 'GRU' AND a2.CodigoIATA = 'REC')
                   OR (a1.CodigoIATA = 'REC' AND a2.CodigoIATA = 'GRU')
                   OR (a1.CodigoIATA = 'GRU' AND a2.CodigoIATA = 'GIG')
                   OR (a1.CodigoIATA = 'GIG' AND a2.CodigoIATA = 'GRU')
                   OR (a1.CodigoIATA = 'CGH' AND a2.CodigoIATA = 'SDU')
                   OR (a1.CodigoIATA = 'SDU' AND a2.CodigoIATA = 'CGH')
                   OR (a1.CodigoIATA = 'GRU' AND a2.CodigoIATA = 'BSB')
                   OR (a1.CodigoIATA = 'BSB' AND a2.CodigoIATA = 'GRU')
                   OR (a1.CodigoIATA = 'GRU' AND a2.CodigoIATA = 'SSA')
                   OR (a1.CodigoIATA = 'SSA' AND a2.CodigoIATA = 'GRU')
                   OR (a1.CodigoIATA = 'CGH' AND a2.CodigoIATA = 'POA')
                   OR (a1.CodigoIATA = 'POA' AND a2.CodigoIATA = 'CGH')
                   OR (a1.CodigoIATA = 'GRU' AND a2.CodigoIATA = 'CNF')
                   OR (a1.CodigoIATA = 'CNF' AND a2.CodigoIATA = 'GRU')
                   OR (a1.CodigoIATA = 'GRU' AND a2.CodigoIATA = 'FOR')
                   OR (a1.CodigoIATA = 'FOR' AND a2.CodigoIATA = 'GRU')
                   OR (a1.CodigoIATA = 'CGH' AND a2.CodigoIATA = 'CWB')
                   OR (a1.CodigoIATA = 'CWB' AND a2.CodigoIATA = 'CGH')
                   OR (a1.CodigoIATA = 'GRU' AND a2.CodigoIATA = 'VIX')
                   OR (a1.CodigoIATA = 'VIX' AND a2.CodigoIATA = 'GRU')
                   OR (a1.CodigoIATA = 'CGH' AND a2.CodigoIATA = 'FLN')
                   OR (a1.CodigoIATA = 'FLN' AND a2.CodigoIATA = 'CGH');
            ");
        }
    }
}
