ALTER TABLE articles
    ADD COLUMN source_url TEXT;

UPDATE articles SET source_url = '' WHERE source_url IS NULL;

