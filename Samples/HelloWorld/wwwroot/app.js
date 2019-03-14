
function onMessageFromCSharp(message) {
    document.getElementById('myLabel').innerHTML = message;
}

window.addEventListener('ipc-intialized', function (event) {
    console.log('Listening messages on channel: message-from-csharp');
    window.ipc.on('message-from-csharp', onMessageFromCSharp);
});
