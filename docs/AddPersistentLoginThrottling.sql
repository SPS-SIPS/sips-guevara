START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migrationid" = '20261116072112_AddPersistentLoginThrottling') THEN
    DELETE FROM tokens WHERE tokentype = 2;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migrationid" = '20261116072112_AddPersistentLoginThrottling') THEN
    ALTER TABLE tokens DROP COLUMN value;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migrationid" = '20261116072112_AddPersistentLoginThrottling') THEN
    ALTER TABLE users ADD loginattemptcount integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migrationid" = '20261116072112_AddPersistentLoginThrottling') THEN
    ALTER TABLE users ADD loginwindowstart timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migrationid" = '20261116072112_AddPersistentLoginThrottling') THEN
    INSERT INTO "__EFMigrationsHistory" (migrationid, productversion)
    VALUES ('20261116072112_AddPersistentLoginThrottling', '8.0.34');
    END IF;
END $EF$;
COMMIT;
