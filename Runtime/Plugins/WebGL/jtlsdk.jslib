var JTLSDKLibrary = {
    JTLSDK_Select: function (platformPointer) {
        var bridges = Module.JTLSDKBridges || {};
        var bridge = bridges[UTF8ToString(platformPointer)];
        Module.JTLSDK = typeof bridge !== 'undefined' ? bridge : null;
        return Module.JTLSDK !== null ? 1 : 0;
    },

    JTLSDK_IsAvailable: function () {
        return typeof Module.JTLSDK !== 'undefined' && Module.JTLSDK !== null ? 1 : 0;
    },

    JTLSDK_Register: function (callbackPointer) {
        if (typeof Module.JTLSDK === 'undefined' || Module.JTLSDK === null) {
            return;
        }

        Module.JTLSDK.register(callbackPointer);
    },

    JTLSDK_Call: function (modulePointer, actionPointer, payloadPointer, requestId) {
        if (typeof Module.JTLSDK === 'undefined' || Module.JTLSDK === null) {
            return;
        }

        Module.JTLSDK.call(UTF8ToString(modulePointer), UTF8ToString(actionPointer), UTF8ToString(payloadPointer), requestId);
    },

    JTLSDK_Query: function (modulePointer, actionPointer, payloadPointer) {
        var result = '{"code":1,"message":"bridge unavailable"}';

        if (typeof Module.JTLSDK !== 'undefined' && Module.JTLSDK !== null) {
            result = Module.JTLSDK.query(UTF8ToString(modulePointer), UTF8ToString(actionPointer), UTF8ToString(payloadPointer));
        }

        var size = lengthBytesUTF8(result) + 1;
        var buffer = _malloc(size);
        stringToUTF8(result, buffer, size);
        return buffer;
    },

    JTLSDK_WriteBackup: function (keyPointer, valuePointer) {
        try {
            window.localStorage.setItem(UTF8ToString(keyPointer), UTF8ToString(valuePointer));
        } catch (error) {
            console.warn('[JTL SDK] The save backup was not written.', error);
        }
    },

    JTLSDK_ReadBackup: function (keyPointer) {
        var value = '';

        try {
            value = window.localStorage.getItem(UTF8ToString(keyPointer)) || '';
        } catch (error) {
            value = '';
        }

        var size = lengthBytesUTF8(value) + 1;
        var buffer = _malloc(size);
        stringToUTF8(value, buffer, size);
        return buffer;
    },

    JTLSDK_ClearBackup: function (keyPointer) {
        try {
            window.localStorage.removeItem(UTF8ToString(keyPointer));
        } catch (error) {
        }
    }
};

mergeInto(LibraryManager.library, JTLSDKLibrary);
