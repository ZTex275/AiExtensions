function scrollToEnd() {
    start = Date.now(); // ��������� ����� ������

    interval = setInterval(function () {
        // ������� ������� ������ � ������ ��������?
        if (Date.now() - start >= 5000) {
            clearInterval(interval); // ��������� �������� ����� 3 �������
        }
        else {
            window.scrollBy(0, 20);
        }
    }, 200);
}

function ctrlC() {
    // ���������� ���������� �������
    //window.onkeydown = function () {
    //    alert("onkeydown");
    //    var e = window.event;
    //    if (e.ctrlKey && e.keyCode === 67) {
    //        // �������� HTML
    //        var html = document.documentElement.outerHTML;

    //        // ���������� clipboardData
    //        if (window.clipboardData) {
    //            window.clipboardData.setData('Text', html);
    //            alert('HTML ����������!');
    //        }

    //        e.returnValue = false;
    //        return false;
    //    }
    //    return true;
    //};

    // ��� Internet Explorer ���������� attachEvent ������ addEventListener
    //if (document.attachEvent) {
    //    document.attachEvent('onkeydown', function (event) {
    //        alert("onkeydown");
    //        event = event || window.event;

    //        // ��������� Ctrl+C
    //        if (event.ctrlKey && event.keyCode === 67) {
    //            alert("event.ctrlKey && event.keyCode");
    //            var htmlContent = document.documentElement.outerHTML;
    //            // ����������� ����� IE-����������� �����
    //            copyToClipboardIE(htmlContent);
    //            alert(htmlContent);

    //            // ���������� �����������
    //            showNotification('HTML ����������!');

    //            // ������������� ����������� ���������
    //            event.returnValue = false;
    //            return false;
    //        }
    //    });
    //}

    //document.body.addEventListener('keydown', function (event) {
    //    if ((event.ctrlKey || event.metaKey) && event.key === 'c') {
    //        // �������� ���� �������� ��� ���������� �������
    //        elementToCopy = document.getElementById('content') || document.documentElement;
    //        htmlContent = elementToCopy.outerHTML;

    //        copyToClipboard(htmlContent);
    //        event.preventDefault();
    //    }
    //});
};