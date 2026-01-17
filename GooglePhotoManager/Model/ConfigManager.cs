using GooglePhotoManager.Utils;
using System.Xml.Linq;

namespace GooglePhotoManager.Model
{
    /// <summary>
    /// Gestisce la configurazione del programma salvata in XML.
    /// </summary>
    internal class ConfigManager
    {
        #region "Constants"

        private const string CONFIG_FILENAME = "config.xml";
        private const string DEFAULT_BACKUP_DEVICE_MODEL = "Pixel_5";
        private const string DEFAULT_BACKUP_DEVICE_PRODUCT = "redfin";
        private const string DEFAULT_LANGUAGE = "Italian";

        #endregion

        #region "Private fields"

        private readonly string _configFilePath;
        private string _backupDeviceModel = DEFAULT_BACKUP_DEVICE_MODEL;
        private string _backupDeviceProduct = DEFAULT_BACKUP_DEVICE_PRODUCT;
        private Localization.Language _language = Localization.Language.Italian;

        #endregion

        #region "Properties"

        /// <summary>
        /// Model del dispositivo di backup (es. "Pixel_5").
        /// </summary>
        public string BackupDeviceModel
        {
            get => _backupDeviceModel;
            set => _backupDeviceModel = value;
        }

        /// <summary>
        /// Product del dispositivo di backup (es. "redfin").
        /// </summary>
        public string BackupDeviceProduct
        {
            get => _backupDeviceProduct;
            set => _backupDeviceProduct = value;
        }

        /// <summary>
        /// Nome descrittivo del dispositivo di backup.
        /// </summary>
        public string BackupDeviceName => $"{BackupDeviceModel} ({BackupDeviceProduct})";

        /// <summary>
        /// Lingua dell'applicazione.
        /// </summary>
        public Localization.Language Language
        {
            get => _language;
            set
            {
                _language = value;
                Localization.CurrentLanguage = value;
            }
        }

        #endregion

        #region "Constructor"

        public ConfigManager()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILENAME);
            Load();
        }

        #endregion

        #region "Methods"

        /// <summary>
        /// Carica la configurazione dal file XML.
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    XDocument doc = XDocument.Load(_configFilePath);
                    XElement? root = doc.Root;

                    if (root != null)
                    {
                        // Load backup device settings
                        XElement? backupDevice = root.Element("BackupDevice");
                        if (backupDevice != null)
                        {
                            string? model = backupDevice.Element("Model")?.Value;
                            string? product = backupDevice.Element("Product")?.Value;

                            if (!string.IsNullOrWhiteSpace(model))
                            {
                                _backupDeviceModel = model;
                            }

                            if (!string.IsNullOrWhiteSpace(product))
                            {
                                _backupDeviceProduct = product;
                            }
                        }

                        // Load language setting
                        XElement? settings = root.Element("Settings");
                        if (settings != null)
                        {
                            string? language = settings.Element("Language")?.Value;
                            if (!string.IsNullOrWhiteSpace(language))
                            {
                                _language = Localization.ParseLanguage(language);
                            }
                        }

                        // Apply language
                        Localization.CurrentLanguage = _language;
                    }
                }
                else
                {
                    // Crea il file con i valori di default
                    Save();
                }
            }
            catch
            {
                // In caso di errore usa i valori di default
                _backupDeviceModel = DEFAULT_BACKUP_DEVICE_MODEL;
                _backupDeviceProduct = DEFAULT_BACKUP_DEVICE_PRODUCT;
                _language = Localization.Language.Italian;
                Localization.CurrentLanguage = _language;
            }
        }

        /// <summary>
        /// Salva la configurazione nel file XML.
        /// </summary>
        public void Save()
        {
            try
            {
                XDocument doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", null),
                    new XElement("Configuration",
                        new XElement("BackupDevice",
                            new XElement("Model", _backupDeviceModel),
                            new XElement("Product", _backupDeviceProduct)
                        ),
                        new XElement("Settings",
                            new XElement("Language", Localization.LanguageToString(_language))
                        )
                    )
                );

                doc.Save(_configFilePath);
            }
            catch
            {
                // Ignora errori di salvataggio
            }
        }

        /// <summary>
        /// Imposta il dispositivo di backup e salva la configurazione.
        /// </summary>
        /// <param name="model">Model del dispositivo.</param>
        /// <param name="product">Product del dispositivo.</param>
        public void SetBackupDevice(string model, string product)
        {
            _backupDeviceModel = model;
            _backupDeviceProduct = product;
            Save();
        }

        /// <summary>
        /// Imposta la lingua e salva la configurazione.
        /// </summary>
        /// <param name="language">Lingua da impostare.</param>
        public void SetLanguage(Localization.Language language)
        {
            _language = language;
            Localization.CurrentLanguage = language;
            Save();
        }

        /// <summary>
        /// Verifica se un dispositivo corrisponde al dispositivo di backup configurato.
        /// </summary>
        /// <param name="model">Model del dispositivo.</param>
        /// <param name="product">Product del dispositivo.</param>
        /// <returns>True se corrisponde, altrimenti false.</returns>
        public bool IsBackupDevice(string model, string product)
        {
            return model.Equals(_backupDeviceModel, StringComparison.OrdinalIgnoreCase) &&
                   product.Equals(_backupDeviceProduct, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
