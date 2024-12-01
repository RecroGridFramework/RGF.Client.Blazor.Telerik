using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Recrovit.RecroGridFramework.Abstraction.Contracts.Services;
using Recrovit.RecroGridFramework.Abstraction.Models;
using Recrovit.RecroGridFramework.Client.Blazor.Components;
using Recrovit.RecroGridFramework.Client.Events;
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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        GridParameters.EnableMultiRowSelection = false;
        GridParameters.EventDispatcher.Subscribe(RgfListEventKind.CreateRowData, OnCreateAttributes);
        GridParameters.EventDispatcher.Subscribe(RgfListEventKind.ColumnSettingsChanged, OnColumnSettingsChanged);
        _initialized = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_initialized && _selfRef == null)
        {
            _selfRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync(RGFClientBlazorTelerikConfiguration.JsBlazorTelerikUiNamespace + ".Grid.initializeTable", _selfRef, Manager.EntityDomId);
            await AutoFitColumnsAsync();
        }
    }

    private void OnColumnSettingsChanged(IRgfEventArgs<RgfListEventArgs> args) => Recreate();

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
        GridParameters.EventDispatcher.Unsubscribe(RgfListEventKind.CreateRowData, OnCreateAttributes);
        GridParameters.EventDispatcher.Unsubscribe(RgfListEventKind.ColumnSettingsChanged, OnColumnSettingsChanged);
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
            await Manager.ListHandler.SetVisibleColumnsAsync(columns);
            Recreate();
        }
        else
        {
            await Manager.ListHandler.SetSortAsync(args.GridState.SortDescriptors
               .Select((desc, i) => new { Alias = desc.Member, Sort = desc.SortDirection == ListSortDirection.Ascending ? i + 1 : -(i + 1) })
               .ToDictionary(e => e.Alias, e => e.Sort));
        }
    }

    protected virtual Task OnCreateAttributes(IRgfEventArgs<RgfListEventArgs> arg)
    {
        _logger.LogDebug("CreateAttributes");
        var rowData = arg.Args.Data ?? throw new ArgumentException();
        foreach (var prop in EntityDesc.SortedVisibleColumns)
        {
            string? propClass = null;
            if (prop.FormType == PropertyFormType.CheckBox)
            {
                propClass = "rg-center";
            }
            else if (prop.ListType == PropertyListType.Numeric)
            {
                propClass = "rg-numeric";
            }
            if (propClass != null)
            {
                var attributes = rowData.GetOrNew<RgfDynamicDictionary>("__attributes");
                var propAttributes = attributes.GetOrNew<RgfDynamicDictionary>(prop.Alias);
                propAttributes.Set<string>("class", (old) => string.IsNullOrEmpty(old) ? propClass : $"{old.Trim()} {propClass}");
            }
        }
        return Task.CompletedTask;
    }

    private void OnRowRender(GridRowRenderEventArgs args)
    {
        var rowData = (RgfDynamicDictionary)args.Item;
        var attributes = rowData.Get<RgfDynamicDictionary>("__attributes");
        if (attributes != null)
        {
            var val = attributes.Get<string>("class");
            if (val != null)
            {
                args.Class = val;
            }
            val = attributes.Get<string>("style");
            if (val != null)
            {
                //Not supported
                //args.? = val
            }
        }
    }

    private void OnCellRender(RgfProperty prop, GridCellRenderEventArgs args)
    {
        var rowData = (RgfDynamicDictionary)args.Item;
        var attributes = rowData.Get<RgfDynamicDictionary>("__attributes");
        if (attributes != null)
        {
            if (prop.ColPos > 0)
            {
                var propAttributes = attributes.Get<RgfDynamicDictionary>(prop.Alias);
                if (propAttributes != null)
                {
                    var val = propAttributes.Get<string>("class");
                    if (val != null)
                    {
                        args.Class = val;
                    }
                    val = propAttributes.Get<string>("style");
                    if (val != null)
                    {
                        //Not supported
                        //args.? = val;
                    }
                }
            }
        }
    }

    protected virtual async Task OnRowClick(GridRowClickEventArgs args)
    {
        var rowData = (RgfDynamicDictionary)args.Item;
        if (_rgfGridRef.SelectedItems.Any())
        {
            int idx = Manager.ListHandler.GetAbsoluteRowIndex(rowData);
            bool deselect = _rgfGridRef.SelectedItems.ContainsKey(idx);
            await _rgfGridRef.RowDeselectHandlerAsync(rowData);
            if (deselect)
            {
                return;
            }
        }
        await _rgfGridRef.RowSelectHandlerAsync(rowData);
    }

    protected virtual Task OnRowDoubleClick(GridRowClickEventArgs args) => _rgfGridRef.OnRecordDoubleClickAsync((RgfDynamicDictionary)args.Item);
}