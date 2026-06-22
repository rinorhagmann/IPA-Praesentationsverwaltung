-- ===========================================================================
-- 02_seed_admin.sql
-- Inserts a default administrator account so the system is usable after a
-- manual schema creation. Run AFTER 01_create_schema.sql.
--
-- Login:    admin@wgbs.ch
-- Password: Admin123!
--
-- SECURITY: change this password immediately after the first login. The hash
-- below is a PBKDF2-HMAC-SHA256 hash (100'000 iterations) in the application's
-- "{iterations}.{base64Salt}.{base64Subkey}" format.
--
-- NOTE: When the application starts it also seeds this account automatically
-- (see DbInitializer). This script only inserts the admin when it is missing,
-- so it is safe to run alongside the application.
-- ===========================================================================

USE [IPADatabase];
GO

IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Email] = N'admin@wgbs.ch')
BEGIN
    INSERT INTO [Users]
        ([Email], [FirstName], [LastName], [PasswordHash], [Role], [IsActive], [CreatedAt], [CanManageSystem])
    VALUES
        (N'admin@wgbs.ch',
         N'System',
         N'Administrator',
         N'100000.aHX4nP1A+xwu5fONJtWpCA==.chV0fOh1im3CQ91oZr0PU4i2RDAbh09sRftDBBOyGzw=',
         1,            -- UserRole.Admin
         1,            -- IsActive
         SYSUTCDATETIME(),
         1);           -- CanManageSystem
END;
GO
