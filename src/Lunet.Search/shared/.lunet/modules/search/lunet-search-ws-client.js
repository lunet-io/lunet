class LunetSearch {
    constructor() {
        this._reason = "Search not initialized";
        this._ws = new Worker("/js/lunet-search.js");
        this._commandId = 0;
    }

    get available() {
        return true;
    }

    get reason()
    {
        return this._reason;
    }

    query(text) {

        this._commandId++;
        this._reason = "Searching...";
        var thisInstance = this;

        return new Promise(function (resolve, failure) {
            var channel = new MessageChannel();
            channel.port1.onmessage = e => {
                if ("results" in e.data) {
                    resolve(e.data.results);
                } else {
                    failure(e.data.reason);
                }
            };

            thisInstance._ws.postMessage({ command: "query", args: text }, [channel.port2]);
        });
    }
}

const DefaultLunetSearch = new LunetSearch();
