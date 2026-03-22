-- ============================================
-- Smart Attendance System - Database Setup
-- ASP.NET Core + MySQL
-- ============================================

CREATE DATABASE IF NOT EXISTS `smart_attendance`
CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;

USE `smart_attendance`;

-- ============================================
-- Tabel Users
-- ============================================
DROP TABLE IF EXISTS `leave_requests`;
DROP TABLE IF EXISTS `attendances`;
DROP TABLE IF EXISTS `app_settings`;
DROP TABLE IF EXISTS `users`;

CREATE TABLE `users` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(100) NOT NULL,
    `email` VARCHAR(100) NOT NULL,
    `password` VARCHAR(255) NOT NULL,
    `role` VARCHAR(20) NOT NULL DEFAULT 'user',
    `position` VARCHAR(100) NULL,
    `department` VARCHAR(100) NULL,
    `phone` VARCHAR(20) NULL,
    `is_active` TINYINT(1) NOT NULL DEFAULT 1,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE INDEX `IX_users_email` (`email`)
) ENGINE=InnoDB;

-- ============================================
-- Tabel Attendances
-- ============================================
CREATE TABLE `attendances` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `user_id` INT NOT NULL,
    `date` DATE NOT NULL,
    `check_in` TIME(6) NULL,
    `check_out` TIME(6) NULL,
    `status` VARCHAR(20) NOT NULL DEFAULT 'hadir',
    `location_in` VARCHAR(255) NULL,
    `latitude_in` DOUBLE NULL,
    `longitude_in` DOUBLE NULL,
    `location_out` VARCHAR(255) NULL,
    `latitude_out` DOUBLE NULL,
    `longitude_out` DOUBLE NULL,
    `photo_in` VARCHAR(500) NULL,
    `photo_out` VARCHAR(500) NULL,
    `notes` VARCHAR(500) NULL,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    UNIQUE INDEX `IX_attendances_user_date` (`user_id`, `date`),
    CONSTRAINT `FK_attendances_users` FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

-- ============================================
-- Tabel Leave Requests
-- ============================================
CREATE TABLE `leave_requests` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `user_id` INT NOT NULL,
    `type` VARCHAR(20) NOT NULL DEFAULT 'izin',
    `start_date` DATE NOT NULL,
    `end_date` DATE NOT NULL,
    `reason` VARCHAR(500) NOT NULL,
    `attachment` VARCHAR(500) NULL,
    `status` VARCHAR(20) NOT NULL DEFAULT 'pending',
    `approved_by` INT NULL,
    `approved_at` DATETIME(6) NULL,
    `admin_notes` VARCHAR(500) NULL,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `updated_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`id`),
    CONSTRAINT `FK_leave_users` FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE,
    CONSTRAINT `FK_leave_approver` FOREIGN KEY (`approved_by`) REFERENCES `users`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB;

-- ============================================
-- Tabel App Settings
-- ============================================
CREATE TABLE `app_settings` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `key` VARCHAR(50) NOT NULL,
    `value` VARCHAR(255) NOT NULL,
    `description` VARCHAR(255) NULL,
    PRIMARY KEY (`id`),
    UNIQUE INDEX `IX_app_settings_key` (`key`)
) ENGINE=InnoDB;

-- ============================================
-- EF Migrations History
-- ============================================
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` VARCHAR(150) NOT NULL,
    `ProductVersion` VARCHAR(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB;

INSERT INTO `__EFMigrationsHistory` VALUES ('20260322_InitialCreate', '9.0.0');

-- ============================================
-- Default Settings
-- ============================================
INSERT INTO `app_settings` (`key`, `value`, `description`) VALUES
('jam_masuk', '08:00', 'Jam masuk kerja'),
('jam_pulang', '17:00', 'Jam pulang kerja'),
('office_latitude', '-6.200000', 'Latitude kantor'),
('office_longitude', '106.816666', 'Longitude kantor'),
('office_radius', '100', 'Radius absensi dalam meter'),
('hari_kerja', 'senin,selasa,rabu,kamis,jumat', 'Hari kerja');

-- ============================================
-- Sample Users (password: password123)
-- ============================================
INSERT INTO `users` (`name`, `email`, `password`, `role`, `position`, `department`, `phone`) VALUES
('Admin HR', 'admin@company.com', '$2a$11$8KxX7klWQFhTkHKr5cMJCOQYJXD1ntK7GJkFvk3z3Y5IgXrxNBFKG', 'admin', 'HR Manager', 'Human Resources', '081234567890'),
('Budi Santoso', 'budi@company.com', '$2a$11$8KxX7klWQFhTkHKr5cMJCOQYJXD1ntK7GJkFvk3z3Y5IgXrxNBFKG', 'user', 'Software Engineer', 'IT', '081234567891'),
('Siti Nurhaliza', 'siti@company.com', '$2a$11$8KxX7klWQFhTkHKr5cMJCOQYJXD1ntK7GJkFvk3z3Y5IgXrxNBFKG', 'user', 'UI/UX Designer', 'IT', '081234567892'),
('Ahmad Fauzi', 'ahmad@company.com', '$2a$11$8KxX7klWQFhTkHKr5cMJCOQYJXD1ntK7GJkFvk3z3Y5IgXrxNBFKG', 'user', 'Marketing Staff', 'Marketing', '081234567893'),
('Dewi Lestari', 'dewi@company.com', '$2a$11$8KxX7klWQFhTkHKr5cMJCOQYJXD1ntK7GJkFvk3z3Y5IgXrxNBFKG', 'user', 'Finance Staff', 'Finance', '081234567894');

-- ============================================
-- Selesai!
-- Login: admin@company.com / password123
--        budi@company.com / password123
-- ============================================
