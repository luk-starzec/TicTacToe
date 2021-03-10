window.clipboardCopy = {
    copyText: function (text) {
        return navigator.clipboard.writeText(text)
            .then(() => true)
            .catch((error) => { console.error(error); return false; });
    },
};