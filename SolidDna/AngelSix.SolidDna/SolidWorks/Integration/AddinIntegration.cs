﻿using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace AngelSix.SolidDna
{
    /// <summary>
    /// Integrates into SolidWorks as an add-in and registers for callbacks provided by SolidWorks
    /// 
    /// IMPORTANT: The class that overrides <see cref="ISwAddin"/> MUST be the same class that 
    /// contains the ComRegister and ComUnregister functions due to how SolidWorks loads add-ins
    /// </summary>
    public abstract class AddInIntegration : ISwAddin
    {
        #region Protected Members

        /// <summary>
        /// A list of available plug-ins loaded once SolidWorks has connected
        /// </summary>
        protected List<ISolidPlugIn> mPlugIns = new List<ISolidPlugIn>();

        #endregion

        #region Public Properties

        /// <summary>
        /// The title displayed for this SolidWorks Add-in
        /// </summary>
        public static string SolidWorksAddInTitle { get; set; } = "AngelSix SolidDna AddIn";

        /// <summary>
        /// The description displayed for this SolidWorks Add-in
        /// </summary>
        public static string SolidWorksAddInDescription { get; set; } = "All your pixels are belong to us!";

        /// <summary>
        /// Represents the current SolidWorks application
        /// </summary>
        public static SolidWorksApplication SolidWorks { get; private set; }

        #endregion

        #region Public Events

        /// <summary>
        ///  Called once SolidWorks has loaded our add-in and is ready
        ///  Now is a good time to create taskpanes, meun bars or anything else
        /// </summary>
        public static event Action ConnectedToSolidWorks = () => { };

        /// <summary>
        ///  Called once SolidWorks has unloaded our add-in
        ///  Now is a good time to clean up taskpanes, meun bars or anything else
        /// </summary>
        public static event Action DisconnectedFromSolidWorks = () => { };

        /// <summary>
        /// Specific application startup code when SolidWorks is connected 
        /// and before any plug-ins or listeners are informed
        /// </summary>
        /// <returns></returns>
        public abstract void ApplicationStartup();

        #endregion

        #region SolidWorks Add-in Callbacks

        /// <summary>
        /// Called when SolidWorks has loaded our add-in and wants us to do our connection logic
        /// </summary>
        /// <param name="ThisSW">The current SolidWorks instance</param>
        /// <param name="Cookie">The current SolidWorks cookie Id</param>
        /// <returns></returns>
        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            // Setup IoC
            IoCContainer.Ensure();

            // Store a reference to the current SolidWorks instance
            // Initialize SolidWorks (SolidDNA class)
            SolidWorks = new SolidWorksApplication((SldWorks)ThisSW, Cookie);

            // Load all plug-in's at this stage for faster lookup
            mPlugIns = PlugInIntegration.SolidDnaPlugIns();

            // Perform any plug-in configuration
            PlugInIntegration.ConfigurePlugIns();

            // Call the application startup function for an entry point to the application
            ApplicationStartup();

            // Inform plug-ins
            mPlugIns.ForEach(plugin => plugin.ConnectedToSolidWorks());

            // Inform listeners
            ConnectedToSolidWorks();

            // Return ok
            return true;
        }

        /// <summary>
        /// Called when SolidWorks is about to unload our add-in and wants us to do our disconnection logic
        /// </summary>
        /// <returns></returns>
        public bool DisconnectFromSW()
        {
            // Inform plug-ins
            mPlugIns.ForEach(plugin => plugin.DisconnetedFromSolidWorks());

            // Inform listeners
            DisconnectedFromSolidWorks();

            // Dipose SolidWorks COM
            SolidWorks?.Dispose();
            SolidWorks = null;

            // Return ok
            return true;
        }

        #endregion

        #region Com Registration

        /// <summary>
        /// The COM registration call to add our registry entries to the SolidWorks add-in registry
        /// </summary>
        /// <param name="t"></param>
        [ComRegisterFunction()]
        protected static void ComRegister(Type t)
        {
            var keyPath = string.Format(@"SOFTWARE\SolidWorks\AddIns\{0:b}", t.GUID);

            // Create our registry folder for the add-in
            using (var rk = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(keyPath))
            {
                // Load add-in when SolidWorks opens
                rk.SetValue(null, 1);

                // Let plug-ins configure title and descriptions
                PlugInIntegration.ConfigurePlugIns();

                // Set SolidWorks add-in title and description
                rk.SetValue("Title", SolidWorksAddInTitle);
                rk.SetValue("Description", SolidWorksAddInDescription);
            }
        }

        /// <summary>
        /// The COM unregister call to remove our custom entries we added in the COM register function
        /// </summary>
        /// <param name="t"></param>
        [ComUnregisterFunction()]
        protected static void ComUnregister(Type t)
        {
            var keyPath = string.Format(@"SOFTWARE\SolidWorks\AddIns\{0:b}", t.GUID);

            // Remove our registry entry
            Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(keyPath);

        }

        #endregion
    }
}
