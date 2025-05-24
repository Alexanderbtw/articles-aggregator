CREATE TABLE IF NOT EXISTS articles
(
    id      UUID PRIMARY KEY,
    title   TEXT NOT NULL,
    content TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_articles_title_trgm
    ON articles
    USING gin (title gin_trgm_ops);
