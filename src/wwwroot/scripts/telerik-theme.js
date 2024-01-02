export function setTheme(theme) {
    var body = document.getElementsByTagName('body')[0];
    body.style.display = 'none';
    document.getElementById('telerik-theme').href = theme;
    setTimeout(function () {
        body.style.display = '';
    }, 500);
}
