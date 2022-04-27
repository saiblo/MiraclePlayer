mergeInto(LibraryManager.library, {
    GetReplayLength: function () {
        return getReplayLength();
    },
    GetReplayData: function (index) {
        return getReplayData(index);
    },
    GetMapByte: function (name, index) {
        return getMapByte(UTF8ToString(name), index);
    },
    GetToken: function () {
        const token = getToken();
        const bufferSize = lengthBytesUTF8(token) + 1;
        const buffer = _malloc(bufferSize);
        stringToUTF8(token, buffer, bufferSize);
        return buffer;
    },
    ConnectSaiblo: function (tokenDecoded, tokenEncoded) {
        const websocket = new WebSocket("wss://" + UTF8ToString(tokenDecoded));
        websocket.onopen = function (event) {
            console.log("judger connected");
            websocket.send(JSON.stringify({
                token: UTF8ToString(tokenEncoded),
                request: "connect",
            }))
        };
        bindWebsocket(websocket, UTF8ToString(tokenEncoded));
    },
    SendWsMessage: function (message) {
        sendWebsocketMessage(UTF8ToString(message));
    },
    GetPlayers: function () {
        const players = getPlayers();
        const playersText = players === undefined ? "" : players[1] + " v.s. " + players[0];
        const bufferSize = lengthBytesUTF8(playersText) + 1;
        const buffer = _malloc(bufferSize);
        stringToUTF8(playersText, buffer, bufferSize);
        return buffer;
    },
    JsAlert: function (message) {
        window.alert(UTF8ToString(message));
    },
});