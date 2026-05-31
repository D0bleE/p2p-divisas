USE CambioDivisasP2P;
GO

-- 1. ELIMINAR TABLAS DEPENDIENTES EN ORDEN CORRECTO (Para evitar conflictos de llaves foráneas)
DROP TABLE IF EXISTS Disputas;
DROP TABLE IF EXISTS Calificaciones;
DROP TABLE IF EXISTS Vouchers;
DROP TABLE IF EXISTS Transacciones;
DROP TABLE IF EXISTS Ofertas;
DROP TABLE IF EXISTS MovimientosFondos; 
DROP TABLE IF EXISTS CuentasBancarias;   
DROP TABLE IF EXISTS Billeteras;
DROP TABLE IF EXISTS Monedas;
GO

-- 2. ASEGURAR QUE LA TABLA DE USUARIOS TENGA LA COLUMNA ROL (Por si acaso)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Usuarios') AND name = 'Rol')
BEGIN
    ALTER TABLE Usuarios ADD Rol VARCHAR(10) NOT NULL DEFAULT 'USU' CHECK (Rol IN ('USU', 'ADM'));
END
GO

-- 3. TABLA DE MONEDAS SOPORTADAS
CREATE TABLE Monedas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CodigoISO VARCHAR(10) NOT NULL UNIQUE, -- 'USD', 'PEN', 'EUR', 'MXN', 'CNY'
    Nombre VARCHAR(50) NOT NULL,          
    Simbolo VARCHAR(5) NOT NULL,           
    RutaBandera VARCHAR(255) NOT NULL,     -- URL o ruta física de la bandera para Vue.js
    Activo BIT DEFAULT 1
);
GO

-- Insertar las 5 monedas exigidas por tu alcance
INSERT INTO Monedas (CodigoISO, Nombre, Simbolo, RutaBandera) VALUES 
('PEN', 'Sol Peruano', 'S/', '/images/flags/peru.png'),
('USD', 'Dólar Americano', '$', '/images/flags/usa.png'),
('EUR', 'Euro', '€', '/images/flags/europa.png'),
('MXN', 'Peso Mexicano', '$', '/images/flags/mexico.png'),
('CNY', 'Yuan Chino', '¥', '/images/flags/china.png');
GO

-- 4. BILLETERAS INTERNAS (El núcleo del sistema de Custodia / Escrow)
CREATE TABLE Billeteras (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId INT NOT NULL,
    MonedaId INT NOT NULL,
    -- Saldo libre que el usuario puede usar o retirar
    SaldoDisponible DECIMAL(18,2) NOT NULL DEFAULT 0.00 CHECK (SaldoDisponible >= 0),
    -- Saldo temporalmente "congelado" mientras su oferta está publicada en la pizarra
    SaldoBloqueado DECIMAL(18,2) NOT NULL DEFAULT 0.00 CHECK (SaldoBloqueado >= 0), 
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    FOREIGN KEY (MonedaId) REFERENCES Monedas(Id),
    CONSTRAINT UQ_Usuario_Moneda UNIQUE (UsuarioId, MonedaId)
);
GO

-- 5. CUENTAS BANCARIAS DEL USUARIO (Para la historia de retiros y transferencias P2P)
CREATE TABLE CuentasBancarias (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId INT NOT NULL,
    MonedaId INT NOT NULL, -- Identifica si la cuenta de banco es en Soles, Dólares, etc.
    Banco VARCHAR(50) NOT NULL,        -- Ej: 'BCP', 'BBVA', 'Interbank'
    NumeroCuenta VARCHAR(50) NOT NULL,
    NumeroCCI VARCHAR(50) NULL,
    TitularNombre VARCHAR(100) NOT NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    FOREIGN KEY (MonedaId) REFERENCES Monedas(Id)
);
GO

-- 6. MOVIMIENTOS DE FONDOS (Para la historia de Recargas, Retiros y Vouchers externos controlados por el ADM)
CREATE TABLE MovimientosFondos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId INT NOT NULL,
    MonedaId INT NOT NULL,
    TipoMovimiento VARCHAR(20) NOT NULL CHECK (TipoMovimiento IN ('RECARGA', 'RETIRO')),
    Monto DECIMAL(18,2) NOT NULL CHECK (Monto > 0),
    RutaVoucher VARCHAR(255) NULL, -- Imagen del depósito simulado que sube el usuario
    Estado VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE' CHECK (Estado IN ('PENDIENTE', 'APROBADO', 'RECHAZADO')),
    FechaSolicitud DATETIME DEFAULT GETDATE(),
    FechaProcesado DATETIME NULL, -- Cuándo el ADM aprobó o rechazó la transacción
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    FOREIGN KEY (MonedaId) REFERENCES Monedas(Id)
);
GO

-- 7. TABLA DE OFERTAS PUBLICADAS EN LA PIZARRA
CREATE TABLE Ofertas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId INT NOT NULL,
    MonedaOrigenId INT NOT NULL,   -- Moneda que el ofertante entrega (Se congelará de su SaldoDisponible)
    MonedaDestinoId INT NOT NULL,  -- Moneda que el ofertante espera recibir en su cuenta bancaria
    MontoOrigen DECIMAL(18,2) NOT NULL CHECK (MontoOrigen > 0),
    TasaCambio DECIMAL(18,4) NOT NULL CHECK (TasaCambio > 0),
    Estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVA' CHECK (Estado IN ('ACTIVA', 'EN_PROCESO', 'COMPLETADA', 'CANCELADA')),
    FechaPublicacion DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
    FOREIGN KEY (MonedaOrigenId) REFERENCES Monedas(Id),
    FOREIGN KEY (MonedaDestinoId) REFERENCES Monedas(Id),
    CONSTRAINT CK_Monedas_Distintas CHECK (MonedaOrigenId <> MonedaDestinoId)
);
GO

-- 8. TRANSACCIONES P2P (Contrato temporal de intercambio entre dos usuarios)
CREATE TABLE Transacciones (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OfertaId INT NOT NULL,
    UsuarioContraparteId INT NOT NULL, -- El usuario que acepta la oferta de la pizarra
    
    -- Valores fijos e históricos de la operación
    MonedaOrigenId INT NOT NULL,       
    MontoOrigen DECIMAL(18,2) NOT NULL,
    MonedaDestinoId INT NOT NULL,      
    MontoDestino DECIMAL(18,2) NOT NULL, -- Calculado: MontoOrigen * TasaCambioPactada
    TasaCambioPactada DECIMAL(18,4) NOT NULL,
    
    -- Estados estrictos de flujo P2P para notificaciones bancarias externas
    Estado VARCHAR(30) NOT NULL DEFAULT 'PENDIENTE_PAGO' 
        CHECK (Estado IN ('PENDIENTE_PAGO', 'PAGO_REPORTADO', 'COMPLETADA', 'DISPUTA', 'CANCELADA')),
    
    FechaInicio DATETIME DEFAULT GETDATE(),
    FechaActualizacion DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (OfertaId) REFERENCES Ofertas(Id),
    FOREIGN KEY (UsuarioContraparteId) REFERENCES Usuarios(Id),
    FOREIGN KEY (MonedaOrigenId) REFERENCES Monedas(Id),
    FOREIGN KEY (MonedaDestinoId) REFERENCES Monedas(Id)
);
GO

-- 9. VOUCHERS DE TRANSACCIÓN P2P (Comprobante de transferencia bancaria que ve la contraparte)
CREATE TABLE Vouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TransaccionId INT NOT NULL UNIQUE,
    RutaImagen VARCHAR(255) NOT NULL, 
    FechaSubida DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (TransaccionId) REFERENCES Transacciones(Id)
);
GO

-- 10. CALIFICACIONES (Sistema de reputación de 1 a 5 estrellas entre usuarios)
CREATE TABLE Calificaciones (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TransaccionId INT NOT NULL,
    UsuarioEvaluadorId INT NOT NULL,
    UsuarioEvaluadoId INT NOT NULL,
    Puntuacion INT NOT NULL CHECK (Puntuacion BETWEEN 1 AND 5), 
    Comentario VARCHAR(255) NULL,
    Fecha DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (TransaccionId) REFERENCES Transacciones(Id),
    FOREIGN KEY (UsuarioEvaluadorId) REFERENCES Usuarios(Id),
    FOREIGN KEY (UsuarioEvaluadoId) REFERENCES Usuarios(Id)
);
GO

-- 11. DISPUTAS (Canal seguro administrado exclusivamente por el ADM)
CREATE TABLE Disputas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TransaccionId INT NOT NULL,
    UsuarioDemandanteId INT NOT NULL,
    Motivo VARCHAR(255) NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'ABIERTA' CHECK (Estado IN ('ABIERTA', 'EN_REVISION', 'RESUELTA')),
    Resolucion VARCHAR(MAX) NULL, -- Veredicto final escrito por el Admin
    FechaApertura DATETIME DEFAULT GETDATE(),
    FechaResolucion DATETIME NULL,
    FOREIGN KEY (TransaccionId) REFERENCES Transacciones(Id),
    FOREIGN KEY (UsuarioDemandanteId) REFERENCES Usuarios(Id)
);
GO