CREATE OR REPLACE FUNCTION idx_SMChannelStreamLink_indexes() RETURNS VOID AS $$
BEGIN
    CREATE UNIQUE INDEX IF NOT EXISTS idx_smchannelstreamlinks_smchannelid_smstreamid ON "SMChannelStreamLink" ("SMChannelId", "SMStreamId");
    CREATE INDEX IF NOT EXISTS idx_smchannelstreamlinks_smchannelid_rank ON "SMChannelStreamLink" ("SMChannelId", "Rank");
    CREATE INDEX IF NOT EXISTS idx_smchannelstreamlink_smchannelid ON "SMChannelStreamLink" ("SMChannelId");
END;
$$ LANGUAGE plpgsql;
