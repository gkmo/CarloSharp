class Ipc {

    constructor() {
        this.callbacks = new Map();
    }

    send(channel, message) {
        __sendIpcMessageAsync(channel, message);
    }

    sendSync(channel, message) {
        return __sendIpcMessageSync(channel, message);
    }

    on(channel, callback) {
        var listeners = this.callbacks.get(channel);

        if (listeners !== undefined) {
            listeners.push(callback);
        } else {
            this.callbacks.set(channel, new Array(callback));
        }
        
        console.trace('listening on ' + channel);
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

    __receiveIpcMessage(channel, message) {
        console.trace('received: ' + message + ' on ' + channel);

        var listeners = this.callbacks.get(channel);

        if (listeners !== undefined) {
            for (var i = 0; i < listeners.length; i++) {
                var callback = listeners[i];
                if (callback !== undefined) {
                    callback(message);
                }
            }
        } else {
            console.trace('Nobody listenning on ' + channel);
        }
    }
}

window.ipc = new Ipc();

window.dispatchEvent(new Event('ipc-intialized'));