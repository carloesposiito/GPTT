using GooglePhotoManager.Model;
using System.Net;
using System.Reflection;

class Program
{
    #region "Private fields"

    /// <summary>
    /// ADB manager object.
    /// </summary>
    private static AdbManager _adbManager = new AdbManager();
    private static UsersManager _userManager = new UsersManager();    

    #endregion

    static async Task Main()
    {
        Console.WriteLine($"GooglePhotoManager v{Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine();

        bool initializeResult = await _adbManager.Initialize();
        if (!initializeResult)
        {
            return;
        }
                
        // Keep flow block untill unlimited device is connected
        await WaitForUnlimitedDevice();

        // At this point is possible to get active user

        // Just to be sure, if arrived here kill ADB server
        await Exit();
    }

    #region "Methods"

    /// <summary>
    /// Loops untill unlimited device is connected.
    /// </summary>
    private async static Task WaitForUnlimitedDevice()
    {
        do
        {
            Console.WriteLine($"Ricerca del dispositivo di backup in corso...");

            // Scan devices to find unlimited device
            await _adbManager.ScanDevicesAsync();

            // Check that unlimited backup device is connected
            if (_adbManager.UnlimitedDevice != null)
            {
                Console.WriteLine($"{AdbManager.UNLIMITED_BK_DEVICE_NAME} connesso.");
            }
            else
            {
                Console.WriteLine($"\"{AdbManager.UNLIMITED_BK_DEVICE_NAME}\" non trovato.");
                Console.WriteLine($"(1) Cerca ancora");
                Console.WriteLine($"(2) Connetti via ADB Wireless");
                Console.WriteLine($"(3) Associa via ADB Wireless (se mai connesso precedentemente)");
                Console.WriteLine();
                Console.Write($"Inserisci la scelta desiderata oppure (0) per chiudere il programma: ");

                // Switch according to user choice
                switch (Console.ReadLine())
                {
                    case "0":
                        await Exit();
                        break;

                    case "1":
                        break;

                    case "2":
                        await WirelessAdbWizard(pairingNeeded: false);
                        break;

                    case "3":
                        await WirelessAdbWizard(pairingNeeded: true);
                        break;                   

                    default:
                        Console.WriteLine("La scelta effettuata non è valida.");
                        break;
                }
            }

            Console.WriteLine();
        }
        while (_adbManager.UnlimitedDevice == null);
    }

    /// <summary>
    /// Starts wizard to connect or pair a device via ADB Wireless.
    /// </summary>
    /// <param name="pairingNeeded">True if pairing is needed.</param>
    private static async Task WirelessAdbWizard(bool pairingNeeded = false)
    {
        string operationResult = string.Empty;

        Console.WriteLine("Attenzione: per utilizzare ADB Wireless è necessario che l'opzione \"Wireless ADB\" sia attiva nelle impostazioni sviluppatore del dispositivo.\n" +
            "Dalla schermata ADB Wireless è possibile ottenere tutti i dati necessari per la connessione Wireless.");
        Console.WriteLine();
        
        // Request IP address and check its validity
        Console.Write("Inserisci l'indirizzo IP del dispositivo: ");
        string deviceIpAddress = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(deviceIpAddress) || !IPAddress.TryParse(deviceIpAddress, out _))
        {
            Console.WriteLine("L'indirizzo IP del dispositivo non sembra essere valido");
            return;
        }

        // Request port and check its validity
        Console.Write("Inserisci la porta del dispositivo: ");
        string devicePort = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(devicePort))
        {
            Console.WriteLine("La porta del dispositivo non può essere vuota");
            return;
        }

        if (pairingNeeded)
        {
            // Request pairing code and check its validity
            Console.Write("Inserisci il codice di associazione del dispositivo: ");
            string devicePairingCode = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(devicePairingCode))
            {
                Console.WriteLine("Il codice di associazione del dispositivo non può essere vuoto");
                return;
            }

            // Pair
            operationResult = await _adbManager.PairWirelessAsync(deviceIpAddress, devicePort, devicePairingCode);
        }
        else
        { 
            // Connect
            operationResult = await _adbManager.ConnectWirelessAsync(deviceIpAddress, devicePort);
        }

        if (!string.IsNullOrWhiteSpace(operationResult))
        {
            Console.WriteLine(operationResult);
        }
    }

    /// <summary>
    /// Exits program killing ADB server.
    /// </summary>
    private static async Task Exit()
    {
        Console.WriteLine("Killing ADB service and exiting program...");
        await _adbManager.KillServiceAsync();
#if DEBUG
        // Just wait user input before close
        Console.ReadLine();
#endif
        Environment.Exit(0);
    }

    #endregion

}
