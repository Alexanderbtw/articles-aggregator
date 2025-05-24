ALTER TABLE articles
    ADD CONSTRAINT uk_articles_source_url
        UNIQUE (source_url);
