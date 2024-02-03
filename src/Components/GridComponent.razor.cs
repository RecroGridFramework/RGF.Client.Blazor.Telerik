using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Recrovit.RecroGridFramework.Abstraction.Contracts.Services;
using Recrovit.RecroGridFramework.Abstraction.Infrastructure.Events;
using Recrovit.RecroGridFramework.Abstraction.Models;
using Recrovit.RecroGridFramework.Client.Blazor.Components;
using Recrovit.RecroGridFramework.Client.Blazor.Events;
using Recrovit.RecroGridFramework.Client.Handlers;
using Telerik.Blazor.Components;
using Telerik.DataSource;

namespace Recrovit.RecroGridFramework.Client.Blazor.TelerikUI.Components;

public partial class GridComponent : ComponentBase, IDisposable
{
    [Inject]
    private ILogger<GridComponent> _logger { get; set; } = default!;

    [Inject]
    private IJSRuntime _jsRuntime { get; set; } = default!;

    private RgfGridComponent _rgfGridRef { get; set; } = default!;

    private TelerikGrid<RgfDynamicDictionary> _gridRef { get; set; } = default!;

    private RgfEntity EntityDesc => Manager.EntityDesc;

    private bool AutoFitFlag { get; set; }

    private DotNetObjectReference<GridComponent>? _selfRef;

    private bool _initialized { get; set; }

    private GridColumnState[] _columnStates { get; set; } = new GridColumnState[0];

    public IRgListHandler ListHandler => Manager.ListHandler;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        GridParameters.EventDispatcher.Subscribe(RgfGridEventKind.CreateAttributes, OnCreateAttributes);
        GridParameters.EventDispatcher.Subscribe(RgfGridEventKind.ColumnSettingsChanged, (arg) => Recreate());
        _initialized = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
        }
        if (_initialized && _selfRef == null)
        {
            _selfRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync(RGFClientBlazorTelerikConfiguration.JsBlazorTelerikUiNamespace + ".Grid.initializeTable", _selfRef, Manager.EntityDomId);
            await AutoFitColumnsAsync();
        }
    }

    private void Recreate()
    {
        _initialized = false;
        Dispose();
        StateHasChanged();
        _ = Task.Run(() =>
        {
            _initialized = true;
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        if (_selfRef != null)
        {
            _selfRef.Dispose();
            _selfRef = null;
        }
    }

    private async Task AutoFitColumnsAsync()
    {
        //https://docs.telerik.com/blazor-ui/knowledge-base/grid-autofit-columns
        AutoFitFlag = true;
        await Task.Delay(200);
        var cols = EntityDesc.SortedVisibleColumns.Where(e => e.ColWidth == 0).Select(e => e.ClientName).ToArray();
        await _gridRef.AutoFitColumnsAsync(cols);
        _columnStates = _gridRef.GetState().ColumnStates.ToArray();
        AutoFitFlag = false;
    }

    protected void OnStateInit(GridStateEventArgs<RgfDynamicDictionary> args)
    {
        foreach (var item in Manager.EntityDesc.SortColumns)
        {
            args.GridState.SortDescriptors.Add(new SortDescriptor(item.Alias, item.Sort > 0 ? ListSortDirection.Ascending : ListSortDirection.Descending));
        }
    }

    private async Task OnStateChanged(GridStateEventArgs<RgfDynamicDictionary> args)
    {
        var columns = Manager.EntityDesc.SortedVisibleColumns.Select(e => new GridColumnSettings(e)).ToArray();
        bool recreate = false;
        foreach (var item in args.GridState.ColumnStates)
        {
            //https://www.telerik.com/forums/blazor-grid-column-resize-event
            if (!AutoFitFlag)
            {
                var col = _columnStates.SingleOrDefault(e => e.Field == item.Field);
                if (col != null)
                {
                    var prop = columns.SingleOrDefault(e => e.Property.Alias == col.Field);
                    if (prop != null)
                    {
                        if (col.Width != item.Width)
                        {
                            prop.ColWidth = Convert.ToInt32(item.Width.Replace("px", ""));
                            recreate = true;
                        }
                        if (col.Index != item.Index)
                        {
                            prop.ColPos = item.Index + 1;
                            recreate = true;
                        }
                    }
                }
            }
        }

        if (recreate)
        {
            await ListHandler.SetVisibleColumnsAsync(columns);
            Recreate();
        }
        else
        {
            await Manager.ListHandler.SetSortAsync(args.GridState.SortDescriptors
               .Select((desc, i) => new { Alias = desc.Member, Sort = desc.SortDirection == ListSortDirection.Ascending ? i + 1 : -(i + 1) })
               .ToDictionary(e => e.Alias, e => e.Sort));
        }
    }

    protected virtual Task OnCreateAttributes(IRgfEventArgs<RgfGridEventArgs> arg)
    {
        var rowData = arg.Args.RowData ?? throw new ArgumentException();
        foreach (var prop in EntityDesc.SortedVisibleColumns)
        {
            var attr = rowData["__attributes"] as RgfDynamicDictionary;
            if (attr != null)
            {
                string? propAttr = null;
                if (prop.FormType == PropertyFormType.CheckBox)
                {
                    propAttr = " rg-center";
                }
                else if (prop.ListType == PropertyListType.Numeric)
                {
                    propAttr = " rg-numeric";
                }
                if (propAttr != null)
                {
                    attr[$"class-{prop.Alias}"] += propAttr;
                }
            }
        }
        return Task.CompletedTask;
    }

    private void OnRowRender(GridRowRenderEventArgs args)
    {
        var rowData = (RgfDynamicDictionary)args.Item;
        var attributes = (RgfDynamicDictionary)rowData["__attributes"];
        var attr = attributes.GetItemData("class").StringValue;
        if (attr != null)
        {
            args.Class = attr;
        }
        //Not supported
        //attr = attributes.GetItemData("style").StringValue;
        //if (attr != null)
        //{
        //    args.?.Attributes.Add("style", attr);
        //}
    }

    private void OnCellRender(RgfProperty prop, GridCellRenderEventArgs args)
    {
        var rowData = (RgfDynamicDictionary)args.Item;
        if (prop.ColPos > 0)
        {
            var attributes = (RgfDynamicDictionary)rowData["__attributes"];
            var attr = attributes.GetItemData($"class-{prop.Alias}").StringValue;
            if (attr != null)
            {
                args.Class = attr;
            }
            //Not supported
            //attr = attributes.GetItemData($"style-{prop.Alias}").StringValue;
            //if (attr != null)
            //{
            //    args.?.Attributes.Add("style", attr);
            //}
        }
    }

    protected virtual async Task OnRowClick(GridRowClickEventArgs args)
    {
        var rowData = (RgfDynamicDictionary)args.Item;
        if (_rgfGridRef.SelectedItems.SingleOrDefault() == rowData)
        {
            await _rgfGridRef.RowDeselectHandlerAsync(rowData);
        }
        else
        {
            await _rgfGridRef.RowSelectHandlerAsync(rowData);
        }
    }

    protected virtual Task OnRowDoubleClick(GridRowClickEventArgs args) => _rgfGridRef.OnRecordDoubleClickAsync((RgfDynamicDictionary)args.Item);
}
