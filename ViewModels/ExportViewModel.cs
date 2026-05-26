using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lauter_Fichaje.Models;
using Lauter_Fichaje.Services;

namespace Lauter_Fichaje.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly IFichajeService _fichajeService;
    private readonly IExportService _exportService;

    [ObservableProperty]
    public partial DateTime DateFrom { get; set; }

    [ObservableProperty]
    public partial DateTime DateTo { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool HasRows { get; set; }

    public bool IsNotLoading => !IsLoading;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public ObservableCollection<FichajeExportRow> Rows { get; } = [];

    public ExportViewModel(IFichajeService fichajeService, IExportService exportService)
    {
        _fichajeService = fichajeService;
        _exportService = exportService;
        DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTo = DateTime.Today;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task LoadPreviewAsync()
    {
        IsLoading = true;
        StatusMessage = "Sincronizando datos...";
        try
        {
            Rows.Clear();
            await _fichajeService.SyncAllPendingAsync();
            StatusMessage = string.Empty;
            var to = DateTo.Date.AddDays(1);
            var rows = await _fichajeService.GetAllForExportAsync(DateFrom.Date, to);
            foreach (var r in rows) Rows.Add(r);
            HasRows = Rows.Count > 0;
            StatusMessage = Rows.Count == 0
                ? "No hay fichajes en el rango seleccionado"
                : $"{Rows.Count} registros encontrados";
        }
        catch (Exception ex)
        {
            HasRows = false;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (!HasRows) return;
        IsLoading = true;
        string path;
        try
        {
            path = await _exportService.GenerateExcelAsync([.. Rows], DateFrom, DateTo);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al generar el fichero: {ex.Message}";
            IsLoading = false;
            return;
        }

        // El fichero ya está listo: liberamos IsLoading ANTES del diálogo del sistema.
        // Share.RequestAsync puede bloquearse indefinidamente hasta que el usuario
        // interactúa con el cuadro de compartir/guardar; si IsLoading quedara a true
        // durante ese tiempo, el botón "Ver fichajes" permanecería deshabilitado.
        IsLoading = false;

        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Exportar fichajes",
                File = new ShareFile(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al compartir: {ex.Message}";
        }
    }
}
