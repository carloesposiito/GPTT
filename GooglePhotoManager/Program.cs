using AdvancedSharpAdbClient.Models;
using GooglePhotoManager.Model;
using GooglePhotoManager.Utils;
using System.Reflection;

class Program
{
    #region "Private fields"

    private static AdbManager _adbManager = new AdbManager();

    #endregion

    private static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    static async Task Main()
    {
        ConsoleUI.ShowBanner(AppVersion);

        ConsoleUI.StartSpinner("Inizializzazione ADB in corso...");
        bool initializeResult = await _adbManager.Initialize();
        ConsoleUI.StopSpinner();

        if (!initializeResult)
        {
            ConsoleUI.ShowError("Inizializzazione ADB fallita.");
            ConsoleUI.Prompt("Premi INVIO per chiudere...");
            return;
        }

        ConsoleUI.ShowSuccess("ADB inizializzato correttamente.");
        Console.WriteLine();

        // Prima scansione automatica
        await ScanDevicesAsync();

        // Menu principale
        await MainMenuLoop();
    }

    #region "Main Menu"

    /// <summary>
    /// Loop principale del menu.
    /// </summary>
    private static async Task MainMenuLoop()
    {
        while (true)
        {
            ShowStatus();
            ShowMainMenu();

            string? choice = ConsoleUI.Prompt("Seleziona un'opzione: ");

            switch (choice)
            {
                case "1":
                    await ScanDevicesAsync();
                    break;
                case "2":
                    await ConnectDeviceWirelessAsync();
                    break;
                case "3":
                    await PairDeviceWirelessAsync();
                    break;
                case "4":
                    await TransferToDocumentsAsync();
                    break;
                case "5":
                    await BackupDeviceFolderAsync();
                    break;
                case "6":
                    await TransferPhotosToBackupAsync();
                    break;
                case "7":
                    await SetBackupDeviceAsync();
                    break;
                case "8":
                    await ScanDevicesAsync();
                    break;
                case "9":
                case "0":
                    await ExitAsync();
                    return;
                default:
                    ConsoleUI.ShowError("Opzione non valida.");
                    break;
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Mostra lo stato attuale: dispositivo backup e lista dispositivi connessi.
    /// </summary>
    private static void ShowStatus()
    {
        Console.WriteLine();
        ConsoleUI.ShowSectionTitle("Stato Connessione");

        // Stato dispositivo di backup
        if (_adbManager.IsBackupDeviceConnected)
        {
            ConsoleUI.ShowSuccess($"Dispositivo di backup: {_adbManager.BackupDeviceName} [CONNESSO]");
        }
        else
        {
            ConsoleUI.ShowWarning($"Dispositivo di backup: {_adbManager.BackupDeviceName} [NON CONNESSO]");
        }

        Console.WriteLine();

        // Lista dispositivi connessi
        if (_adbManager.Devices.Count > 0)
        {
            ConsoleUI.ShowInfo($"Dispositivi connessi: {_adbManager.Devices.Count}");
            Console.WriteLine();

            for (int i = 0; i < _adbManager.Devices.Count; i++)
            {
                var device = _adbManager.Devices[i];
                bool isBackup = _adbManager.BackupDevice != null &&
                               device.Serial == _adbManager.BackupDevice.Serial;

                string label = isBackup ? " [BACKUP]" : "";
                ConsoleUI.ShowDeviceCard(
                    i + 1,
                    device.Model,
                    device.Product,
                    $"{device.Serial}{label}"
                );
            }
        }
        else
        {
            ConsoleUI.ShowWarning("Nessun dispositivo connesso.");
        }
    }

    /// <summary>
    /// Mostra il menu principale.
    /// </summary>
    private static void ShowMainMenu()
    {
        Console.WriteLine();
        ConsoleUI.ShowSectionTitle("Menu Principale");

        ConsoleUI.ShowMenu(
            ("1", "Scansiona dispositivi"),
            ("2", "Connetti dispositivo (ADB Wireless)"),
            ("3", "Abbina dispositivo (ADB Wireless)"),
            ("4", "Trasferisci file in Documents"),
            ("5", "Backup cartella dispositivo"),
            ("6", "Trasferisci foto a dispositivo backup"),
            ("7", "Imposta dispositivo di backup"),
            ("8", "Ricarica dispositivi"),
            ("9", "Esci")
        );
    }

    #endregion

    #region "Commands"

    /// <summary>
    /// Scansiona i dispositivi connessi.
    /// </summary>
    private static async Task ScanDevicesAsync()
    {
        ConsoleUI.StartSpinner("Scansione dispositivi in corso...");
        await _adbManager.ScanDevicesAsync();
        ConsoleUI.StopSpinner();

        if (_adbManager.Devices.Count > 0)
        {
            ConsoleUI.ShowSuccess($"Trovati {_adbManager.Devices.Count} dispositivo/i.");
        }
        else
        {
            ConsoleUI.ShowWarning("Nessun dispositivo trovato.");
        }
    }

    /// <summary>
    /// Connette un dispositivo via ADB Wireless.
    /// </summary>
    private static async Task ConnectDeviceWirelessAsync()
    {
        Console.WriteLine();
        ConsoleUI.ShowInfo("Per utilizzare ADB Wireless è necessario che l'opzione \"Wireless ADB\"");
        ConsoleUI.ShowInfo("sia attiva nelle impostazioni sviluppatore del dispositivo.");
        Console.WriteLine();

        string? ip = ConsoleUI.Prompt("Indirizzo IP del dispositivo: ");
        if (string.IsNullOrWhiteSpace(ip))
        {
            ConsoleUI.ShowError("Indirizzo IP non valido.");
            return;
        }

        string? port = ConsoleUI.Prompt("Porta del dispositivo: ");
        if (string.IsNullOrWhiteSpace(port))
        {
            ConsoleUI.ShowError("Porta non valida.");
            return;
        }

        ConsoleUI.StartSpinner("Connessione in corso...");
        string result = await _adbManager.ConnectWirelessAsync(ip, port);
        ConsoleUI.StopSpinner();

        if (!string.IsNullOrWhiteSpace(result))
        {
            ConsoleUI.ShowInfo(result);
        }

        // Ricarica dispositivi dopo la connessione
        await ScanDevicesAsync();
    }

    /// <summary>
    /// Abbina un dispositivo via ADB Wireless.
    /// </summary>
    private static async Task PairDeviceWirelessAsync()
    {
        Console.WriteLine();
        ConsoleUI.ShowInfo("Per abbinare un dispositivo, vai nelle impostazioni sviluppatore");
        ConsoleUI.ShowInfo("e seleziona 'Associa dispositivo con codice di associazione'.");
        Console.WriteLine();

        string? ip = ConsoleUI.Prompt("Indirizzo IP del dispositivo: ");
        if (string.IsNullOrWhiteSpace(ip))
        {
            ConsoleUI.ShowError("Indirizzo IP non valido.");
            return;
        }

        string? port = ConsoleUI.Prompt("Porta di associazione: ");
        if (string.IsNullOrWhiteSpace(port))
        {
            ConsoleUI.ShowError("Porta non valida.");
            return;
        }

        string? pairingCode = ConsoleUI.Prompt("Codice di associazione: ");
        if (string.IsNullOrWhiteSpace(pairingCode))
        {
            ConsoleUI.ShowError("Codice di associazione non valido.");
            return;
        }

        ConsoleUI.StartSpinner("Associazione in corso...");
        string result = await _adbManager.PairWirelessAsync(ip, port, pairingCode);
        ConsoleUI.StopSpinner();

        if (!string.IsNullOrWhiteSpace(result))
        {
            ConsoleUI.ShowInfo(result);
        }
    }

    /// <summary>
    /// Trasferisce file dal PC alla cartella Documents del dispositivo.
    /// </summary>
    private static async Task TransferToDocumentsAsync()
    {
        if (_adbManager.Devices.Count == 0)
        {
            ConsoleUI.ShowWarning("Nessun dispositivo connesso.");
            return;
        }

        // Seleziona dispositivo se ce ne sono più di uno
        DeviceData? targetDevice = SelectDevice("Seleziona il dispositivo di destinazione:");
        if (targetDevice == null) return;

        Console.WriteLine();
        ConsoleUI.ShowInfo("Inserisci i percorsi dei file da trasferire (uno per riga).");
        ConsoleUI.ShowInfo("Inserisci una riga vuota per terminare.");
        Console.WriteLine();

        var filePaths = new List<string>();
        while (true)
        {
            string? path = ConsoleUI.Prompt("Percorso file: ");
            if (string.IsNullOrWhiteSpace(path)) break;

            if (File.Exists(path))
            {
                filePaths.Add(path);
                ConsoleUI.ShowSuccess($"Aggiunto: {Path.GetFileName(path)}");
            }
            else
            {
                ConsoleUI.ShowError($"File non trovato: {path}");
            }
        }

        if (filePaths.Count == 0)
        {
            ConsoleUI.ShowWarning("Nessun file selezionato.");
            return;
        }

        Console.WriteLine();
        ConsoleUI.StartSpinner("Trasferimento in corso...");
        int transferred = await _adbManager.PushToDocumentsAsync(targetDevice, filePaths);
        ConsoleUI.StopSpinner();

        ConsoleUI.ShowSummaryBox("Risultato Trasferimento",
            ("File da trasferire", filePaths.Count.ToString()),
            ("File trasferiti", transferred.ToString()),
            ("Esito", transferred == filePaths.Count ? "Completato" : "Parziale")
        );
    }

    /// <summary>
    /// Esegue il backup di una cartella del dispositivo sul PC.
    /// </summary>
    private static async Task BackupDeviceFolderAsync()
    {
        if (_adbManager.Devices.Count == 0)
        {
            ConsoleUI.ShowWarning("Nessun dispositivo connesso.");
            return;
        }

        // Seleziona dispositivo se ce ne sono più di uno
        DeviceData? targetDevice = SelectDevice("Seleziona il dispositivo da cui fare backup:");
        if (targetDevice == null) return;

        // Ottieni lista cartelle
        Console.WriteLine();
        ConsoleUI.StartSpinner("Recupero cartelle...");
        var folders = await _adbManager.GetRootFoldersAsync(targetDevice);
        ConsoleUI.StopSpinner();

        if (folders.Count == 0)
        {
            ConsoleUI.ShowWarning("Nessuna cartella trovata sul dispositivo.");
            return;
        }

        // Mostra cartelle disponibili
        Console.WriteLine();
        ConsoleUI.ShowInfo("Cartelle disponibili:");
        Console.WriteLine();

        for (int i = 0; i < folders.Count; i++)
        {
            ConsoleUI.ShowMenu(((i + 1).ToString(), folders[i]));
        }

        Console.WriteLine();
        string? folderChoice = ConsoleUI.Prompt("Seleziona una cartella (numero): ");

        if (!int.TryParse(folderChoice, out int folderIndex) ||
            folderIndex < 1 || folderIndex > folders.Count)
        {
            ConsoleUI.ShowError("Selezione non valida.");
            return;
        }

        string selectedFolder = folders[folderIndex - 1];

        // Esegui backup
        Console.WriteLine();
        ConsoleUI.StartSpinner($"Backup di '{selectedFolder}' in corso...");
        var result = await _adbManager.BackupFolderAsync(targetDevice, selectedFolder);
        ConsoleUI.StopSpinner();

        if (result.ToBePulledCount == 0)
        {
            ConsoleUI.ShowWarning("La cartella è vuota.");
            return;
        }

        ConsoleUI.ShowSummaryBox("Risultato Backup",
            ("File da copiare", result.ToBePulledCount.ToString()),
            ("File copiati", result.PulledCount.ToString()),
            ("Esito", result.AllFilesSynced ? "Completato" : "Parziale"),
            ("Cartella locale", result.FolderPath ?? "N/A")
        );
    }

    /// <summary>
    /// Trasferisce foto dal dispositivo origine al dispositivo di backup.
    /// </summary>
    private static async Task TransferPhotosToBackupAsync()
    {
        if (!_adbManager.IsBackupDeviceConnected)
        {
            ConsoleUI.ShowError("Il dispositivo di backup non è connesso.");
            ConsoleUI.ShowInfo($"Dispositivo atteso: {_adbManager.BackupDeviceName}");
            return;
        }

        if (_adbManager.OriginDevices.Count == 0)
        {
            ConsoleUI.ShowWarning("Nessun dispositivo di origine connesso.");
            return;
        }

        // Seleziona dispositivo origine se ce ne sono più di uno
        DeviceData? originDevice;
        if (_adbManager.OriginDevices.Count == 1)
        {
            originDevice = _adbManager.OriginDevices[0];
            ConsoleUI.ShowInfo($"Dispositivo origine: {originDevice.Model} ({originDevice.Product})");
        }
        else
        {
            originDevice = SelectDevice("Seleziona il dispositivo di origine:", _adbManager.OriginDevices);
            if (originDevice == null) return;
        }

        Console.WriteLine();
        string? deleteResponse = ConsoleUI.Prompt("Eliminare le foto dal dispositivo di origine dopo il backup? (S/N): ");
        bool deleteFromOrigin = deleteResponse?.Trim().ToUpper() == "S";

        Console.WriteLine();
        ConsoleUI.StartSpinner("Trasferimento in corso...");
        var result = await _adbManager.TransferPhotos(originDevice, _adbManager.BackupDevice!, deleteFromOrigin);
        ConsoleUI.StopSpinner();

        var summaryItems = new List<(string, string)>
        {
            ("Foto da estrarre", result.ToBePulledCount.ToString()),
            ("Foto estratte", result.PulledCount.ToString()),
            ("Foto da trasferire", result.ToBePushedCount.ToString()),
            ("Foto trasferite", result.PushedCount.ToString()),
            ("Sincronizzazione", result.AllFilesSynced ? "Completata" : "Fallita")
        };

        if (deleteFromOrigin)
        {
            summaryItems.Add(("Eliminazione", result.DeleteCompleted ? "Completata" : "Fallita"));
        }

        summaryItems.Add(("Cartella locale", result.FolderPath ?? "N/A"));

        ConsoleUI.ShowSummaryBox("Riepilogo Trasferimento", summaryItems.ToArray());

        if (result.AllFilesSynced)
        {
            ConsoleUI.ShowSuccess("Backup completato con successo!");
        }
        else
        {
            ConsoleUI.ShowWarning("Backup completato con alcuni errori.");
        }
    }

    /// <summary>
    /// Imposta il dispositivo di backup leggendo model e product dall'utente.
    /// </summary>
    private static async Task SetBackupDeviceAsync()
    {
        Console.WriteLine();
        ConsoleUI.ShowInfo("Imposta il dispositivo da usare come backup.");
        ConsoleUI.ShowInfo("Questi valori saranno salvati nel file config.xml.");
        Console.WriteLine();

        ConsoleUI.ShowInfo($"Configurazione attuale: {_adbManager.BackupDeviceName}");
        Console.WriteLine();

        string? model = ConsoleUI.Prompt("Model del dispositivo (es. Pixel_5): ");
        if (string.IsNullOrWhiteSpace(model))
        {
            ConsoleUI.ShowError("Model non valido.");
            return;
        }

        string? product = ConsoleUI.Prompt("Product del dispositivo (es. redfin): ");
        if (string.IsNullOrWhiteSpace(product))
        {
            ConsoleUI.ShowError("Product non valido.");
            return;
        }

        _adbManager.Config.SetBackupDevice(model.Trim(), product.Trim());

        ConsoleUI.ShowSuccess($"Dispositivo di backup impostato: {_adbManager.BackupDeviceName}");

        // Ricarica dispositivi per aggiornare lo stato
        await ScanDevicesAsync();
    }

    /// <summary>
    /// Seleziona un dispositivo dalla lista.
    /// </summary>
    private static DeviceData? SelectDevice(string prompt, List<DeviceData>? deviceList = null)
    {
        var devices = deviceList ?? _adbManager.Devices;

        if (devices.Count == 0)
        {
            ConsoleUI.ShowWarning("Nessun dispositivo disponibile.");
            return null;
        }

        if (devices.Count == 1)
        {
            return devices[0];
        }

        Console.WriteLine();
        ConsoleUI.ShowInfo(prompt);
        Console.WriteLine();

        for (int i = 0; i < devices.Count; i++)
        {
            var device = devices[i];
            ConsoleUI.ShowDeviceCard(i + 1, device.Model, device.Product, device.Serial);
        }

        Console.WriteLine();
        ConsoleUI.ShowMenu(("0", "Annulla"));
        Console.WriteLine();

        string? choice = ConsoleUI.Prompt("Seleziona un dispositivo: ");

        if (!int.TryParse(choice, out int index) || index < 0 || index > devices.Count)
        {
            ConsoleUI.ShowError("Selezione non valida.");
            return null;
        }

        if (index == 0)
        {
            return null;
        }

        return devices[index - 1];
    }

    /// <summary>
    /// Esce dal programma.
    /// </summary>
    private static async Task ExitAsync()
    {
        Console.WriteLine();
        ConsoleUI.StartSpinner("Chiusura del servizio ADB in corso...");
        await _adbManager.KillServiceAsync();
        ConsoleUI.StopSpinner();
        ConsoleUI.ShowInfo("Arrivederci!");
#if DEBUG
        Console.WriteLine();
        ConsoleUI.Prompt("Premi INVIO per chiudere...");
#endif
    }

    #endregion
}
