
function onMessageFromCSharp(message) {
    console.log('Received message from C#:' + message);

    document.getElementById('myLabel').innerHTML = message;
}

if (window.ipc === undefined) {
    window.addEventListener('ipc-intialized', function (event) {
        console.log('Listening messages on channel: message-from-csharp');
        window.ipc.on('message-from-csharp', onMessageFromCSharp);
    });
} else {
    console.log('Listening messages on channel: message-from-csharp');
    window.ipc.on('message-from-csharp', onMessageFromCSharp);
}
