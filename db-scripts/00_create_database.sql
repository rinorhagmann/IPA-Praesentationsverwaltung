-- ===========================================================================
-- 00_create_database.sql
-- Creates the application database on a Microsoft SQL Server instance.
-- Run this first (e.g. via SSMS or sqlcmd) before applying the schema script.
-- The Docker setup and the application create/migrate the database
-- automatically; this script is provided for manual provisioning.
-- ===========================================================================

IF DB_ID(N'IPADatabase') IS NULL
BEGIN
    CREATE DATABASE [IPADatabase];
END;
GO

USE [IPADatabase];
GO
