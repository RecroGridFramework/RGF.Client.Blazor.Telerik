/*!
* recrovit-rgf-blazor-telerik.js v1.0.0
*/

window.Recrovit = window.Recrovit || { RGF: {} };
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
    }
};