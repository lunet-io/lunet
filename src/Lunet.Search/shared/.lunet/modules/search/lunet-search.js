class LunetSearch {
    constructor() {
        this._available = false;
        this._reason = "Search not initialized";

        // configurable, should be properties 
        this.url_weight = 3.0;
        this.title_weight = 2.0;
        this.content_weight = 1.0;
        this.snipped_words = 20;
    }

    get available() {
        return this._available;
    }

    get reason()
    {
        return this._reason;
    }

    initialize(dbUrl = "/js/lunet-search.db", locateSqliteWasm = (file) => `/js/lunet-${file}`)
    {
        if ("caches" in window) {
            caches.open("lunet.cache").then((lunetCache) => {
                const sqlLoader = window.lunetInitSqlJs({locateFile: function(file, prefix){ return locateSqliteWasm(file);} });
                lunetCache.add(dbUrl);
                this._reason = "Please wait, initializing search database.";
                lunetCache.match(dbUrl).then((cachedResponse) => {
                    if (cachedResponse) {
                        cachedResponse.arrayBuffer().then((buffer) => {
                            const u8Buffer = new Uint8Array(buffer);
                            this._reason = "Please wait, initializing search engine.";
                            sqlLoader.then((sql) => {
                                this.db = new sql.Database(u8Buffer);
                                try {
                                    // fake exec to check if the database is valid
                                    this.db.exec("pragma schema_version;");
                                    this._available = true;
                                    this._reason = "Search database available for queries.";
                                    if ("onAvailable" in this)
                                    {
                                        this.onAvailable();
                                    }
                                } catch (err) {
                                    this._reason = `Error while loading search database. ${err}`;
                                }
                            });
                        });
                    } else {
                        this._reason = "Error, search database not found";
                    }
                });
            });
        }
        else {
            this._reason = "Browser does not support cache required by search.";
        }
    }

    query(text) {
        if (!this.available) return [];

        const escapeText = text.replace("'", " ");
        const sqlQuery = `SELECT pages.url, pages.title, snippet(pages, 2, '<b>', '</b>', '', ${this.snipped_words}) FROM pages WHERE pages MATCH '${escapeText}' ORDER BY bm25(pages, ${this.url_weight}, ${this.title_weight}, ${this.content_weight});`;
        const results = [];
        const dbResults = this.db.exec(sqlQuery);
        if (dbResults.length > 0) {
            const rows = dbResults[0].values;
            for(let i = 0; i < rows.length; i++) {
                const row = rows[i];
                const url = row[0];
                const title = row[1];
                const snippet = row[2];

                results.push({url: url, title: title, snippet: snippet });
            }
        }
        return results;
    }
}

const DefaultLunetSearch = new LunetSearch();
DefaultLunetSearch.initialize();
