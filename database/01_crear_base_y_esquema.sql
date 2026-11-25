/* =====================================================================
   AA01 - Autenticación segura | Esquema ASP.NET Core Identity (EF Core 8)
   Equivalente al esquema que genera EF Core para ApplicationDbContext.
   Ejecutar en SSMS / sqlcmd contra SQL Server o LocalDB.
   NO contiene usuarios ni contraseñas: el administrador inicial y los
   usuarios de prueba los crea la aplicación (IdentitySeeder) al arrancar,
   porque el hash PBKDF2 debe generarlo el PasswordHasher de Identity.
   ===================================================================== */
IF DB_ID(N'AutoTallerAuth') IS NULL
    CREATE DATABASE [AutoTallerAuth];
GO
USE [AutoTallerAuth];
GO

IF OBJECT_ID(N'dbo.AspNetRoles') IS NULL
CREATE TABLE dbo.AspNetRoles (
    Id               NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
    Name             NVARCHAR(256) NULL,
    NormalizedName   NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);
GO
IF OBJECT_ID(N'dbo.AspNetUsers') IS NULL
CREATE TABLE dbo.AspNetUsers (
    Id                   NVARCHAR(450)  NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
    NombreCompleto       NVARCHAR(100)  NOT NULL,
    IsActive             BIT            NOT NULL CONSTRAINT DF_AspNetUsers_IsActive DEFAULT (1),
    CreatedAtUtc         DATETIME2(7)   NOT NULL,
    UserName             NVARCHAR(256)  NULL,
    NormalizedUserName   NVARCHAR(256)  NULL,
    Email                NVARCHAR(256)  NULL,
    NormalizedEmail      NVARCHAR(256)  NULL,
    EmailConfirmed       BIT            NOT NULL,
    PasswordHash         NVARCHAR(MAX)  NULL,   -- hash PBKDF2 con sal (PasswordHasher de Identity), nunca texto plano
    SecurityStamp        NVARCHAR(MAX)  NULL,
    ConcurrencyStamp     NVARCHAR(MAX)  NULL,
    PhoneNumber          NVARCHAR(MAX)  NULL,
    PhoneNumberConfirmed BIT            NOT NULL,
    TwoFactorEnabled     BIT            NOT NULL,
    LockoutEnd           DATETIMEOFFSET(7) NULL,
    LockoutEnabled       BIT            NOT NULL,
    AccessFailedCount    INT            NOT NULL
);
GO
IF OBJECT_ID(N'dbo.AspNetRoleClaims') IS NULL
CREATE TABLE dbo.AspNetRoleClaims (
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
    RoleId     NVARCHAR(450) NOT NULL,
    ClaimType  NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
);
GO
IF OBJECT_ID(N'dbo.AspNetUserClaims') IS NULL
CREATE TABLE dbo.AspNetUserClaims (
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
    UserId     NVARCHAR(450) NOT NULL,
    ClaimType  NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
GO
IF OBJECT_ID(N'dbo.AspNetUserLogins') IS NULL
CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider       NVARCHAR(128) NOT NULL,
    ProviderKey         NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId              NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
GO
IF OBJECT_ID(N'dbo.AspNetUserRoles') IS NULL
CREATE TABLE dbo.AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
GO
IF OBJECT_ID(N'dbo.AspNetUserTokens') IS NULL
CREATE TABLE dbo.AspNetUserTokens (
    UserId        NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name          NVARCHAR(128) NOT NULL,
    Value         NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- Índices (los mismos que crea EF Core)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex')
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex')
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex')
    CREATE INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId')
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims(RoleId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId')
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId')
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId')
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles(RoleId);
GO

-- Roles base (la aplicación también los crea si faltan)
INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
SELECT CONVERT(NVARCHAR(450), NEWID()), v.Name, UPPER(v.Name), CONVERT(NVARCHAR(36), NEWID())
FROM (VALUES (N'Cliente'), (N'Administrador')) AS v(Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles r WHERE r.NormalizedName = UPPER(v.Name));
GO

/* ---------------------------------------------------------------------
   Registro de la migración de EF Core (OPCIONAL pero recomendado)
   El repositorio incluye la migración 20261125152835_InitialCreate y la
   aplicación llama a Database.Migrate() al arrancar. Si el esquema se creó
   con ESTE script, hay que registrar la migración como ya aplicada; de lo
   contrario EF intentaría crear de nuevo las tablas y fallaría.
   Si usted creó la base con migraciones (dotnet ef database update o
   arrancando la app), NO ejecute este bloque.
   --------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NULL
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId    NVARCHAR(150) NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
        ProductVersion NVARCHAR(32)  NOT NULL
    );
GO
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20261125152835_InitialCreate')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20261125152835_InitialCreate', N'8.0.11');
GO
