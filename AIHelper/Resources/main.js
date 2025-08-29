function scrollToEnd() {
    start = Date.now(); // запомнить время начала

    interval = setInterval(function () {
        // сколько времени прошло с начала анимации?
        if (Date.now() - start >= 5000) {
            clearInterval(interval); // закончить анимацию через 3 секунды
        }
        else {
            window.scrollBy(0, 20);
        }
    }, 200);
}

function ctrlC() {
    // Глобальный обработчик событий
    //window.onkeydown = function () {
    //    alert("onkeydown");
    //    var e = window.event;
    //    if (e.ctrlKey && e.keyCode === 67) {
    //        // Копируем HTML
    //        var html = document.documentElement.outerHTML;

    //        // Используем clipboardData
    //        if (window.clipboardData) {
    //            window.clipboardData.setData('Text', html);
    //            alert('HTML скопирован!');
    //        }

    //        e.returnValue = false;
    //        return false;
    //    }
    //    return true;
    //};

    // Для Internet Explorer используем attachEvent вместо addEventListener
    //if (document.attachEvent) {
    //    document.attachEvent('onkeydown', function (event) {
    //        alert("onkeydown");
    //        event = event || window.event;

    //        // Проверяем Ctrl+C
    //        if (event.ctrlKey && event.keyCode === 67) {
    //            alert("event.ctrlKey && event.keyCode");
    //            var htmlContent = document.documentElement.outerHTML;
    //            // Копирование через IE-совместимый метод
    //            copyToClipboardIE(htmlContent);
    //            alert(htmlContent);

    //            // Показываем уведомление
    //            showNotification('HTML скопирован!');

    //            // Предотвращаем стандартное поведение
    //            event.returnValue = false;
    //            return false;
    //        }
    //    });
    //}

    //document.body.addEventListener('keydown', function (event) {
    //    if ((event.ctrlKey || event.metaKey) && event.key === 'c') {
    //        // Копируем весь документ или конкретный элемент
    //        elementToCopy = document.getElementById('content') || document.documentElement;
    //        htmlContent = elementToCopy.outerHTML;

    //        copyToClipboard(htmlContent);
    //        event.preventDefault();
    //    }
    //});
};