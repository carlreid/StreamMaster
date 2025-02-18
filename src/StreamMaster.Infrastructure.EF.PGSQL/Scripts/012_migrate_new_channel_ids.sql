BEGIN;

-- Function to generate MD5 hash
CREATE OR REPLACE FUNCTION generate_md5(key TEXT, M3UFileId INT)
RETURNS TEXT AS $$
DECLARE
    hash TEXT;
BEGIN
    SELECT md5(concat(key, '_', M3UFileId)) INTO hash;
    RETURN hash;
END;
$$ LANGUAGE plpgsql;

-- Function to generate M3UKey value
CREATE OR REPLACE FUNCTION generate_m3u_key_value(M3UKey INT, M3UFileId INT, Url TEXT, 
                                                  CUID TEXT, ChannelId TEXT, EPGID TEXT, 
                                                  TVGName TEXT, Name TEXT)
RETURNS TEXT AS $$
DECLARE
    key TEXT;
BEGIN
    CASE M3UKey
        WHEN 0 THEN key := Url;
        WHEN 1 THEN key := CUID;
        WHEN 2 THEN key := ChannelId;
        WHEN 3 THEN key := EPGID;
        WHEN 4 THEN key := COALESCE(TVGName, Name);
        WHEN 5 THEN 
            IF TVGName IS NOT NULL AND EPGID IS NOT NULL THEN
                key := TVGName || '_' || EPGID;
            END IF;
        WHEN 6 THEN key := Name;
        WHEN 7 THEN 
            IF Name IS NOT NULL AND EPGID IS NOT NULL THEN
                key := Name || '_' || EPGID;
            END IF;
        ELSE
            RAISE EXCEPTION 'Invalid M3UKey value: %', M3UKey;
    END CASE;
    
    IF key IS NOT NULL THEN
        RETURN generate_md5(key, M3UFileId);
    ELSE
        RETURN NULL;
    END IF;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE
    duplicate_count INTEGER;
BEGIN
    -- Only proceed if migration hasn't been done
    IF NOT EXISTS (SELECT 1 FROM "SystemKeyValues" WHERE "Key" = 'didIDMigration') THEN
        -- Create temporary tables for streams and m3ufiles data
        CREATE TEMP TABLE temp_SMStreams AS
        SELECT "Id", "Url", "CUID", "ChannelId", "EPGID", "TVGName", "Name", "M3UFileId"
        FROM "SMStreams";

        CREATE TEMP TABLE temp_M3UFiles AS
        SELECT "Id", COALESCE("M3UKey", 0) AS "M3UKey"
        FROM "M3UFiles";

        -- Create a temporary table for batch processing
        CREATE TEMP TABLE temp_batch_update (old_id TEXT, new_id TEXT, m3ufileid INT);

        -- Insert new IDs into the batch update table
        INSERT INTO temp_batch_update (old_id, new_id, m3ufileid)
        SELECT s."Id", generate_m3u_key_value(f."M3UKey", s."M3UFileId", s."Url", s."CUID", 
                                              s."ChannelId", s."EPGID", s."TVGName", s."Name"), s."M3UFileId"
        FROM temp_SMStreams s
        LEFT JOIN temp_M3UFiles f ON s."M3UFileId" = f."Id"
        WHERE s."M3UFileId" IS NOT NULL AND s."M3UFileId" >= 0;

        -- Update SMStreams with new IDs
        INSERT INTO "SMStreams" ("Id", "ClientUserAgent", "FilePosition", "IsHidden", 
                                 "IsUserCreated", "M3UFileId", "ChannelNumber", 
                                 "M3UFileName", "Group", "EPGID", "Logo", "Name", 
                                 "Url", "StationId", "IsSystem", "CUID", "SMStreamType", 
                                 "NeedsDelete", "ChannelName", "ChannelId", 
                                 "CommandProfileName", "TVGName", "ExtInf")
        SELECT t.new_id, s."ClientUserAgent", s."FilePosition", s."IsHidden", 
               s."IsUserCreated", t.m3ufileid, s."ChannelNumber", s."M3UFileName", 
               s."Group", s."EPGID", s."Logo", s."Name", s."Url", s."StationId", 
               s."IsSystem", s."CUID", s."SMStreamType", s."NeedsDelete", s."ChannelName", 
               s."ChannelId", s."CommandProfileName", s."TVGName", s."ExtInf"
        FROM temp_batch_update t
        INNER JOIN "SMStreams" s ON t.old_id = s."Id";

        -- Update SMChannelStreamLinks with new IDs
        INSERT INTO "SMChannelStreamLinks" ("SMStreamId", "SMChannelId", "Rank")
        SELECT t.new_id, l."SMChannelId", l."Rank"
        FROM temp_batch_update t
        INNER JOIN "SMChannelStreamLinks" l ON t.old_id = l."SMStreamId";

        -- Delete old SMChannelStreamLinks
        DELETE FROM "SMChannelStreamLinks"
        WHERE "SMStreamId" IN (SELECT old_id FROM temp_batch_update);

        -- Delete old SMStreams
        DELETE FROM "SMStreams"
        WHERE "Id" IN (SELECT old_id FROM temp_batch_update);

        -- Drop temporary tables
        DROP TABLE temp_batch_update;
        DROP TABLE temp_SMStreams;
        DROP TABLE temp_M3UFiles;

        -- Add the didIDMigration entry to SystemKeyValues
        INSERT INTO "SystemKeyValues" ("Key", "Value") VALUES ('didIDMigration', 'true');

        RAISE NOTICE 'Migration completed successfully.';
    ELSE
        -- Check for duplicate didIDMigration entries
        SELECT COUNT(*) INTO duplicate_count
        FROM "SystemKeyValues"
        WHERE "Key" = 'didIDMigration';

        IF duplicate_count > 1 THEN
            -- Keep the first entry and delete the rest
            WITH ordered_keys AS (
                SELECT ctid
                FROM "SystemKeyValues"
                WHERE "Key" = 'didIDMigration'
                ORDER BY ctid
                LIMIT 1
            )
            DELETE FROM "SystemKeyValues"
            WHERE "Key" = 'didIDMigration'
            AND ctid NOT IN (SELECT ctid FROM ordered_keys);

            RAISE NOTICE 'Cleaned up % duplicate didIDMigration entries.', duplicate_count - 1;
        END IF;

        RAISE NOTICE 'Migration has already been performed. No action needed.';
    END IF;
END $$;

COMMIT;
