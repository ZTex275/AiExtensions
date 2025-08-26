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
    //document.addEventListener('keydown', function (event) {
    //    if (event.ctrlKey && event.key === 'c') {
    //        const textToCopy = "Buba";
    //        const textarea = document.createElement('textarea');
    //        textarea.value = textToCopy;
    //        document.body.appendChild(textarea);
    //        textarea.select();
    //        document.execCommand('copy');
    //        document.body.removeChild(textarea);
    //        console.log('Yes (fallback method)');
    //    }
    //});

    //var textarea = document.getElementById("textarea");
    //var copyButton = document.getElementById("copyButton");

    //document.body.addEventListener('click', function (e) {
    //    // Выделяем текст в поле
    //    textarea.select();
    //    // Копируем текст в буфер обмена
    //    document.execCommand('copy');
    //}, false);
};