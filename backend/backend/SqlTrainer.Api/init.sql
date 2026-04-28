CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Topics` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Slug` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `DescriptionMarkdown` LONGTEXT CHARACTER SET utf8mb4 NOT NULL,
    `IsPublished` tinyint(1) NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `PublishedAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_Topics` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Users` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `Email` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Role` int NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_Users` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Tasks` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `TopicId` bigint NULL,
    `Category` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Title` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DescriptionMarkdown` LONGTEXT CHARACTER SET utf8mb4 NOT NULL,
    `StarterSql` LONGTEXT CHARACTER SET utf8mb4 NOT NULL,
    `SeedSql` LONGTEXT CHARACTER SET utf8mb4 NOT NULL,
    `ExpectedSql` LONGTEXT CHARACTER SET utf8mb4 NOT NULL,
    `SqlMode` int NOT NULL,
    `IsPublished` tinyint(1) NOT NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `PublishedAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_Tasks` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Tasks_Topics_TopicId` FOREIGN KEY (`TopicId`) REFERENCES `Topics` (`Id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `Submissions` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `UserId` bigint NOT NULL,
    `TaskItemId` bigint NOT NULL,
    `StudentSql` LONGTEXT CHARACTER SET utf8mb4 NOT NULL,
    `Status` int NOT NULL,
    `IsCorrect` tinyint(1) NULL,
    `RunnerMessage` longtext CHARACTER SET utf8mb4 NOT NULL,
    `StudentResultJson` LONGTEXT CHARACTER SET utf8mb4 NULL,
    `ExpectedResultJson` LONGTEXT CHARACTER SET utf8mb4 NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `SubmittedAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_Submissions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Submissions_Tasks_TaskItemId` FOREIGN KEY (`TaskItemId`) REFERENCES `Tasks` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Submissions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Reviews` (
    `Id` bigint NOT NULL AUTO_INCREMENT,
    `SubmissionId` bigint NOT NULL,
    `Score` int NOT NULL,
    `Comment` LONGTEXT CHARACTER SET utf8mb4 NOT NULL,
    `ReviewedByUserId` bigint NOT NULL,
    `ReviewedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_Reviews` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Reviews_Submissions_SubmissionId` FOREIGN KEY (`SubmissionId`) REFERENCES `Submissions` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Reviews_SubmissionId` ON `Reviews` (`SubmissionId`);

CREATE INDEX `IX_Submissions_TaskItemId` ON `Submissions` (`TaskItemId`);

CREATE INDEX `IX_Submissions_UserId` ON `Submissions` (`UserId`);

CREATE INDEX `IX_Tasks_TopicId` ON `Tasks` (`TopicId`);

CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260428105638_InitialCreate', '8.0.1');

COMMIT;

