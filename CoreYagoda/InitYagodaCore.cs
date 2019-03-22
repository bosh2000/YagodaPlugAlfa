using Resto.Front.Api.V6;
using System;
using System.IO;
using System.Xml.Serialization;
 
namespace Resto.Front.Api.YagodaPlugCore
{
    /// <summary>
    /// Класс инициализации YagodaCore , чтение настроек подключения.
    /// </summary>
    internal class InitYagodaCore
    {
        private SettingYagodaCore setting;
        private ILog logger;

        public InitYagodaCore(ILog logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Десерилизация xml файла с настройками в класс setting/
        /// </summary>
        /// <returns></returns>
        public SettingYagodaCore GetSetting()
        {
#if DEBUG
            logger.Info("GetSetting start");
#endif
            setting = new SettingYagodaCore();
            FileStream fileStream = null;
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingYagodaCore));
                var pathToPlugin = Environment.CurrentDirectory+ "\\Plugins\\Resto.Front.Api.YagodaPlugin\\";
                fileStream = new FileStream(pathToPlugin + "setting.xml", FileMode.Open, FileAccess.Read);
                setting = (SettingYagodaCore)xmlSerializer.Deserialize(fileStream);
#if DEBUG
                if (setting == null) { logger.Error("setting==null"); } else { logger.Info("setting!=null"); }
#endif
            }
            catch (Exception exp)
            {
                logger.Info("GetSetting-" + exp.Message);
            }
            finally
            {
                if (fileStream != null) { fileStream.Close(); };
            }

            return setting;
        }
    }
}