using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Threading.Tasks;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Core;

namespace ProSymbolEditor
{
    internal class ProSymbolEditorModule : Module
    {
        private static ProSymbolEditorModule _this = null;
        public static bool _isEnabled = false;

        /// <summary>
        /// Retrieve the singleton instance to this module here
        /// </summary>
        public static ProSymbolEditorModule Current
        {
            get
            {
                return _this ?? (_this = (ProSymbolEditorModule)FrameworkApplication.FindModule("ProSymbolEditor_Module"));
            }
        }

        #region Overrides
        /// <summary>
        /// Called by Framework when ArcGIS Pro is closing
        /// </summary>
        /// <returns>False to prevent Pro from closing, otherwise True</returns>
        protected override bool CanUnload()
        {
            //TODO - add your business logic
            //return false to ~cancel~ Application close
            return true;
        }

        protected override bool Initialize()
        {
            //Add project opened listener
            ArcGIS.Desktop.Core.Events.ProjectOpenedEvent.Subscribe(async (args) =>
            {
                Task<bool> isEnabledMethod = ShouldAddInBeEnabledAsync();
                _isEnabled = await isEnabledMethod;

                //Give pane the enabled value
                var paneModel = FrameworkApplication.DockPaneManager.Find("ProSymbolEditor_MilitarySymbolDockpane") as MilitarySymbolDockpaneViewModel;
                paneModel.IsEnabled = _isEnabled;
            });


            return base.Initialize();
        }

        #endregion Overrides

        private async Task<bool> ShouldAddInBeEnabledAsync()
        {
            //If we can get the database, then enable the add-in
            if (Project.Current == null)
            {
                //No open project
                return false;
            }

            //Get database
            try
            {
                IEnumerable<GDBProjectItem> gdbProjectItems = Project.Current.GetItems<GDBProjectItem>();
                Geodatabase militaryGeodatabase = await ArcGIS.Desktop.Framework.Threading.Tasks.QueuedTask.Run(() =>
                {
                    foreach (GDBProjectItem gdbProjectItem in gdbProjectItems)
                    {
                        using (Datastore datastore = gdbProjectItem.GetDatastore())
                        {
                            //Unsupported datastores (non File GDB and non Enterprise GDB) will be of type UnknownDatastore
                            if (datastore is UnknownDatastore)
                                    continue;
                            Geodatabase geodatabase = datastore as Geodatabase;

                            string geodatabasePath = geodatabase.GetPath();
                            if (geodatabasePath.Contains("militaryoverlay.gdb"))
                            {
                                return geodatabase;
                            }
                        }
                    }

                    return null;
                });


                if (militaryGeodatabase == null)
                {
                    return false;
                }
            }
            catch (Exception exception)
            {
                return false;
            }

            return true;
        }

    }
}
