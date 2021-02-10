CREATE TABLE IF NOT EXISTS `Assets` (
    `ID` VARCHAR(64) PRIMARY KEY,
    `Symbol` VARCHAR(10) NOT NULL,
    `Class` VARCHAR (16),
    `Exchange` VARCHAR (16),
    `Status` VARCHAR (16),
    `Tradeable` BOOLEAN,
    `Marginable` BOOLEAN,
    `Shortable` BOOLEAN,
    `EasyToBorrow` BOOLEAN
    );