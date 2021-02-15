CREATE TABLE IF NOT EXISTS `Instructions` (
    `ID` INTEGER NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `Description` VARCHAR(1028),
    `Active` BOOLEAN,
    `Format` VARCHAR (16),
    `Assets` TEXT,
    `Strategy` VARCHAR(256),
    `Shares` INTEGER
    );