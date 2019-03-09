class Ipc {

    constructor(windowId) {
        this.windowId = windowId;
        this.callbacks = new Map();
    }

    send(channel, message) {
        __sendIpcMessageAsync(this.windowId, channel, message);
    }

    sendSync(channel, message) {
        return __sendIpcMessageSync(this.windowId, channel, message);
    }

    on(channel, callback) {
        var listeners = this.callbacks.get(channel);

        if (listeners !== undefined) {
            listeners.push(callback);
        } else {
            this.callbacks.set(channel, new Array(callback));
        }
        
        console.log('listening on ' + channel);
    }

    off(channel, callback) {
        var listeners = this.callbacks.get(channel);

        if (listeners !== undefined) {
            for (i = 0; i < listeners.lenngth; i++) {
                if (listeners[i] === callback) {
                    listeners.splice(i, 1);
                }
            }
        }
    }

    __receiveIpcMessage(windowId, channel, message) {
        console.log('received: ' + message + ' on ' + channel);

        var listeners = this.callbacks.get(channel);

        if (listeners !== undefined) {
            for (i = 0; i < listeners.lenngth; i++) {
                var callback = listeners[i];
                if (callback !== undefined) {
                    callback[i](windowId, message);
                }
            }
        } else {
            console.log('Nobody listenning on ' + channel);
        }
    }
}