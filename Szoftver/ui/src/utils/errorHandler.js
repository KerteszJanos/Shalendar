export const setErrorMessage = (() => {
    let currentErrorId = 0;

    return (errorMessageRef, message, timeout = 5000) => {
        const errorId = ++currentErrorId;
        errorMessageRef.value = message;

        setTimeout(() => {
            if (errorId === currentErrorId) {
                errorMessageRef.value = "";
            }
        }, timeout);
    };
})();
