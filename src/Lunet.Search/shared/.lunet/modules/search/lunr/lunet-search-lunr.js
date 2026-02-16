// Case of web-worker, we pre-initialize sql.js
if (typeof importScripts === 'function') {
    // init for worker
}

class LunetSearch {
    constructor() {
	    performance.mark("lunet-search-start");
        this._available = false;
        this._reason = "Search not initialized";
    }

    get available() {
        return this._available;
    }

    get reason()
    {
        return this._reason;
    }

    get perf() {
	    return this._perf;
    }

    _getResponseVersion(response) {
        if (!response || !response.headers) {
            return { etag: null, lastModified: null, contentLength: null };
        }

        const etag = response.headers.get("ETag");
        const lastModifiedHeader = response.headers.get("Last-Modified");
        const contentLengthHeader = response.headers.get("Content-Length");
        const lastModified = lastModifiedHeader ? Date.parse(lastModifiedHeader) : NaN;
        const contentLength = contentLengthHeader ? Number.parseInt(contentLengthHeader, 10) : NaN;

        return {
            etag: etag && etag.length > 0 ? etag : null,
            lastModified: Number.isFinite(lastModified) ? lastModified : null,
            contentLength: Number.isFinite(contentLength) ? contentLength : null
        };
    }

    _isSameResponseVersion(left, right) {
        if (left.etag && right.etag) {
            return left.etag === right.etag;
        }

        if (left.lastModified !== null && right.lastModified !== null) {
            if (left.contentLength !== null && right.contentLength !== null) {
                return left.lastModified === right.lastModified && left.contentLength === right.contentLength;
            }
            return left.lastModified === right.lastModified;
        }

        return false;
    }

    _fetchLatestResponse(url, options) {
        return fetch(url, options).then((response) => response && response.ok ? response : null);
    }

    _initializeDbFromResponse(response, resolve) {
        const thisInstance = this;
        response.json().then((data) => {
            try {
                // fake exec to check if the database is valid
                thisInstance._reason = "Please wait, initializing search engine...";
                thisInstance.docs = data.docs;
                thisInstance.db = lunr.Index.load(data.index);
                thisInstance._available = true;
                thisInstance._reason = "Search database available for queries.";

                performance.mark("lunet-search-end");
                thisInstance._perf = performance.measure("lunet-search-init",
                    "lunet-search-start",
                    "lunet-search-end");

                resolve();
            } catch (err) {
                thisInstance._reason = `Error while loading search database. ${err}`;
            }
        });
    }

    initialize(dbUrl = "/js/lunet-search.json", locateSqliteWasm = (file) => `/js/lunet-${file}`)
    {
        const thisInstance = this;
        if ("caches" in self && "fetch" in self) {
            return new Promise(function(resolve, reject) {
                caches.open("lunet.cache").then((lunetCache) => {
                    thisInstance._reason = "Please wait, initializing search database...";
                    lunetCache.match(dbUrl).then((cachedResponse) => {
                        thisInstance._fetchLatestResponse(dbUrl, { method: "HEAD", cache: "no-store" }).then((latestHeadResponse) => {
                            var requiresFetch = !cachedResponse;

                            if (cachedResponse && latestHeadResponse) {
                                const latestVersion = thisInstance._getResponseVersion(latestHeadResponse);
                                const cachedVersion = thisInstance._getResponseVersion(cachedResponse);
                                requiresFetch = !thisInstance._isSameResponseVersion(latestVersion, cachedVersion);
                            }

                            if (!requiresFetch && cachedResponse && cachedResponse.ok) {
                                thisInstance._initializeDbFromResponse(cachedResponse, resolve);
                                return;
                            }

                            thisInstance._fetchLatestResponse(dbUrl, { cache: "no-store" }).then((latestResponse) => {
                                if (latestResponse) {
                                    lunetCache.put(dbUrl, latestResponse.clone()).finally(() => {
                                        thisInstance._initializeDbFromResponse(latestResponse, resolve);
                                    });
                                    return;
                                }

                                if (cachedResponse && cachedResponse.ok) {
                                    thisInstance._initializeDbFromResponse(cachedResponse, resolve);
                                    return;
                                }

                                thisInstance._reason = "Unable to fetch search database.";
                                resolve();
                            });
                        });
                    });
                }).catch(() => {
                    thisInstance._reason = "Unable to open browser cache for search database.";
                    resolve();
                });
            });
        } else {
            thisInstance._reason = "Browser does not support cache/fetch API required by search.";
            return new Promise(function(resolve, reject) { resolve() });
        }
    }

    _extractWordsAround(content, offset, wordCount, positions) {
        let beforeCount = wordCount / 2;
        let afterCount = beforeCount;
        let isInWord = false;
        let startOffset = offset - 1;
        let endOffset = offset;
        if (startOffset > 0) {
            for (; startOffset >= 0 && beforeCount > 0; startOffset--) {
                const c = content[startOffset];
                if (c.match(/\w/)) {
                    isInWord = true;
                } else {
                    if (isInWord) {
                        beforeCount--;
                        if (beforeCount === 0) {
                            break;
                        }
                        isInWord = false;
                    }
                }
            }
            startOffset++;
        }
        startOffset = startOffset < 0 ? 0 : startOffset;

        afterCount += beforeCount;
        for (; endOffset < content.length && afterCount > 0; endOffset++) {
            const c = content[endOffset];
            if (c.match(/\w/)) {
                isInWord = true;
            } else {
                if (isInWord) {
                    afterCount--;
                    if (afterCount === 0) {
                        break;
                    }
                    isInWord = false;
                }
            }
        }
        endOffset--;

        // Extract words and highlight keywords
        let extract = "";
        let previousOffset = startOffset;
        for (let i = 0; i < positions.length; i++) {
            const pos = positions[i];
            const wordOffset = pos[0];
            const wordSize = pos[1];
            if (wordOffset < endOffset) {
                extract += content.substr(previousOffset, wordOffset - previousOffset) +
                    "<b>" +
                    content.substr(wordOffset, wordSize) +
                    "</b>";
                previousOffset = wordOffset + wordSize;
            } else {
                break;
            }
        }
        extract += content.substr(previousOffset, endOffset - previousOffset + 1);
        return extract;
    }

    query(text) {
        var thisInstance = this;
        if (!this.available) {
            return new Promise(function (resolve, failure) {
                failure(thisInstance.reason);
            });
        }
        else if (!text) {
            return new Promise(function (resolve, failure) {
                resolve([]);
            });
        }

        var escapeText = text.replace("'", " ");
        // Make sure that any " is closed by "
        if (((escapeText.match(/"/g) || []).length % 2) !== 0) {
            escapeText = escapeText + "\"";
        }
        var results = [];

        try {
            const rows = this.db.search(escapeText);
            if (rows.length > 0) {
                for (let i = 0; i < rows.length; i++) {
                    const row = rows[i];
                    const url = row.ref;
                    const doc = this.docs[row.ref];
                    const title = doc.title;
                    
                    // Extract words around the match
                    let offsetInBody = 0;
                    var firstMatch = row.matchData.metadata[Object.keys(row.matchData.metadata)[0]];
                    var positions = [];
                    if ("body" in firstMatch) {
                        positions = firstMatch.body.position;
                        if (positions.length > 0) {
                            offsetInBody = positions[0][0];
                        }
                    }
                    const snippet = this._extractWordsAround(doc.body, offsetInBody, 20, positions);

                    results.push({ url: url, title: title, snippet: snippet });
                }
            }
            return new Promise(function (resolve, failure) {
                resolve(results);
            });
        } catch (err) {
            return new Promise(function (resolve, failure) {
                failure(err);
            });
        }
    }
}

const DefaultLunetSearch = new LunetSearch();
const DefaultLunetSearchPromise = DefaultLunetSearch.initialize();

// Case of web-worker
if (typeof importScripts === 'function') {
    onmessage = function (e) {
        if (e.ports.length > 0 && e.data.command === "query") {
            DefaultLunetSearch.query(e.data.args).catch(err => {
                e.ports[0].postMessage({ reason: err });
            }).then(results => {
                e.ports[0].postMessage({results: results });
            });
        }
    };
}
