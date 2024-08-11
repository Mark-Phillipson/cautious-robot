window.registerKeyPress = function (dotNetHelper) {
    document.addEventListener('keydown', function (event) {
        dotNetHelper.invokeMethodAsync('OnKeyPress', event.key);
    });
};