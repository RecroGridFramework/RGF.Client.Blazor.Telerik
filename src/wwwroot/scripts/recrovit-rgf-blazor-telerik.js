/*!
* recrovit-rgf-blazor-telerik.js v1.0.1
*/

window.Recrovit = window.Recrovit || {};
window.Recrovit.RGF = window.Recrovit.RGF || {};
window.Recrovit.RGF.Blazor = window.Recrovit.RGF.Blazor || {};
var Blazor = window.Recrovit.RGF.Blazor;

Blazor.TelerikUI = {
    Grid: {
        initializeTable: function (dotNetObj, entityDomId) {
            var entity = document.querySelector(`#${entityDomId}`);
            var table = entity.querySelector('table.k-grid-table.k-table');
            table.classList.add('recro-grid');
            table = entity.querySelector('table.k-grid-header-table.k-table');
            table.classList.add('recro-grid');
        }
    },
    Dialog: {
        saveDialogPos: function (name, width, height, top, left) {
            const key = `RGF.DialogPos.${name}`;
            if (width == undefined || height == undefined || top == undefined || left == undefined) {
                localStorage.removeItem(key);
            }
            else {
                const dialogPos = [
                    parseInt(width),
                    parseInt(height),
                    parseInt(top),
                    parseInt(left)
                ];
                localStorage.setItem(key, JSON.stringify(dialogPos));
            }
        },
        loadDialogPos: function (name, dialogId) {
            const data = localStorage.getItem(`RGF.DialogPos.${name}`);
            if (data != undefined) {
                const dialogPos = JSON.parse(data);
                return dialogPos;
            }
        }
    }
};