using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Recrovit.RecroGridFramework.Client.Blazor.TelerikUI.Components;
using System.Reflection;

namespace Recrovit.RecroGridFramework.Client.Blazor.TelerikUI;

public static class RGFClientBlazorTelerikConfiguration
{
    public static async Task InitializeRgfTelerikUIAsync(this IServiceProvider serviceProvider, string themeName = "kendo-theme-default/all", bool trial = false, bool loadResources = true)
    {
        RgfBlazorConfiguration.RegisterComponent<MenuComponent>(RgfBlazorConfiguration.ComponentType.Menu);
        RgfBlazorConfiguration.RegisterComponent<DialogComponent>(RgfBlazorConfiguration.ComponentType.Dialog);
        RgfBlazorConfiguration.RegisterEntityComponent<EntityComponent>(string.Empty);

        if (loadResources)
        {
            var jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
            await LoadResourcesAsync(jsRuntime, themeName, trial);
        }
    }

    public static async Task LoadResourcesAsync(IJSRuntime jsRuntime, string themeName, bool trial = false)
    {
        var libName = Assembly.GetExecutingAssembly().GetName().Name;
        string trialEx = trial ? ".Trial" : "";
        await jsRuntime.InvokeVoidAsync("import", $"{RgfClientConfiguration.AppRootUrl}_content/Telerik.UI.for.Blazor{trialEx}/js/telerik-blazor.js");
        await jsRuntime.InvokeVoidAsync("eval", "document.getElementsByTagName('body')[0].classList.add('k-body');");
        await jsRuntime.InvokeVoidAsync("Recrovit.LPUtils.AddStyleSheetLink", $"{RgfClientConfiguration.AppRootUrl}_content/Telerik.UI.for.Blazor{trialEx}/css/{themeName}.css", false, TelerikThemeId);

        await jsRuntime.InvokeVoidAsync("import", $"{RgfClientConfiguration.AppRootUrl}_content/{libName}/scripts/recrovit-rgf-blazor-telerik.js");
    }

    public static async Task UnloadResourcesAsync(IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("eval", $"document.getElementById('{TelerikThemeId}')?.remove();");
        await jsRuntime.InvokeVoidAsync("eval", "document.getElementsByTagName('body')[0].classList.remove('k-body');");
    }

    public static readonly string TelerikThemeId = "telerik-theme";
    public static readonly string JsBlazorTelerikUiNamespace = "Recrovit.RGF.Blazor.TelerikUI";
}
