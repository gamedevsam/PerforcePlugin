using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using WeifenLuo.WinFormsUI.Docking;
using PluginCore.Localization;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore.Helpers;
using PluginCore;
using ScintillaNet;
using System.Collections;
using System.Diagnostics;
using System.Collections.Specialized;
using FlashDevelop;
using FlashDevelop.Managers;
using ProjectManager.Controls.TreeView;
using SamplePlugin.Resources;

namespace Perforce
{
    public class PluginMain : IPlugin
    {
        private String pluginName = "Perforce";
        private String pluginGuid = "FA54390C-BED6-47e8-80C2-9496D9DC7FB3";
        private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "Run perforce commands from context menu.";
        private String pluginAuth = "Sam Batista";

        private String settingFilename;
        private ProjectTreeView projectTree;
        private Settings settingObject;

        // I don't want to bother the user with a "File has been modified, want to reload?" dialog
        // when they hit revert. But for some reason, I can't get rid of it. So we're going to disable
        // the notification. When the program is closed we restore it.
        // 
        // This is probably the shittiest hack in the history of hacks. But I'm tired and I don't care.
        private Boolean previousFileChangedNotificationSetting;

        #region Required Properties

        /// <summary>
        /// For FD4 Compatibility
        /// </summary> 
        public Int32 Api 
        {
            get { return 1; } 
        }
        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public String Name
        {
            get { return this.pluginName; }
        }

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public String Guid
        {
            get { return this.pluginGuid; }
        }

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public String Author
        {
            get { return this.pluginAuth; }
        }

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public String Description
        {
            get { return this.pluginDesc; }
        }

        /// <summary>
        /// Web address for help
        /// </summary> 
        public String Help
        {
            get { return this.pluginHelp; }
        }

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public Object Settings
        {
            get { return this.settingObject; }
        }

        #endregion

        #region Required Methods

        /// <summary>
        /// Initializes the plugin
        /// </summary>
        public void Initialize()
        {
            this.InitBasics();
            this.LoadSettings();
            this.AddEventHandlers();
            this.InitLocalization();
            //this.CreatePluginPanel();
            this.CreateMenuItem();
        }

        /// <summary>
        /// Initializes important variables
        /// </summary>
        public void InitBasics()
        {
            String dataPath = Path.Combine(PathHelper.DataDir, "Perforce");
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            this.settingFilename = Path.Combine(dataPath, "Settings.fdb");
        }

        public void AddEventHandlers()
        {
            EventType eventMask = EventType.Command | EventType.FileOpen | EventType.FileSwitch | EventType.ApplySettings;
            EventManager.AddEventHandler(this, eventMask);
        }

        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        public void LoadSettings()
        {
            this.settingObject = new Settings();
            if (!File.Exists(this.settingFilename)) this.SaveSettings();
            else
            {
                Object obj = ObjectSerializer.Deserialize(this.settingFilename, this.settingObject);
                this.settingObject = (Settings)obj;
            }

            if (settingObject.DiffProgram != "")
            {
                if (!File.Exists(settingObject.DiffProgram))
                {
                    MessageBox.Show("Diff Program not found. Please ensure to use Full File Paths only.\n\n" + settingObject.DiffProgram, "Error", MessageBoxButtons.OK);
                    settingObject.DiffProgram = "";
                }
            }

            previousFileChangedNotificationSetting = PluginBase.MainForm.Settings.AutoReloadModifiedFiles;
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        public void SaveSettings()
        {
            ObjectSerializer.Serialize(this.settingFilename, this.settingObject);
            PluginBase.MainForm.Settings.AutoReloadModifiedFiles = previousFileChangedNotificationSetting;
        }

        /// <summary>
        /// Creates a menu item for the plugin and adds a ignored key
        /// </summary>
        public void CreateMenuItem()
        {
            AddItemsToContextMenu((ContextMenuStrip)PluginBase.MainForm.TabMenu, true);
            AddItemsToContextMenu((ContextMenuStrip)PluginBase.MainForm.EditorMenu, true);
        }

        /// <summary>
        /// Disposes the plugin
        /// </summary>
        public void Dispose()
        {
            this.SaveSettings();
        }

        /// <summary>
        /// Handles the incoming events
        /// </summary>
        public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority)
        {
            if (e.Type == EventType.ApplySettings)
            {
                // Diff Program not set, return.
                if (settingObject.DiffProgram == "")
                    return;

                if (!File.Exists(settingObject.DiffProgram))
                {
                    MessageBox.Show("Diff Program not found. Please ensure to use Full File Paths only.\n\n" + settingObject.DiffProgram, "Error", MessageBoxButtons.OK);
                    settingObject.DiffProgram = "";
                }

                previousFileChangedNotificationSetting = PluginBase.MainForm.Settings.AutoReloadModifiedFiles;

                // Arguably unnecessary
                try
                {
                    ScintillaControl sci = Globals.SciControl;
                    if (sci != null)
                    {                    
                        sci.ModifyAttemptRO -= new ModifyAttemptROHandler(onModifyAttemptRO);
                        sci.ModifyAttemptRO -= new ModifyAttemptROHandler(Globals.MainForm.OnScintillaControlModifyRO);
                        sci.ModifyAttemptRO += new ModifyAttemptROHandler(onModifyAttemptRO);
                    }
                }
                catch (System.Exception)
                {
                    // Do nothing
                }

            }
            else if (e.Type == EventType.FileOpen || e.Type == EventType.FileSwitch)
            {
                // Arguably unnecessary
                try
                {
                    ScintillaControl sci = Globals.SciControl;
                    if (sci != null)
                    {
                        sci.ModifyAttemptRO -= new ModifyAttemptROHandler(onModifyAttemptRO);
                        sci.ModifyAttemptRO -= new ModifyAttemptROHandler(Globals.MainForm.OnScintillaControlModifyRO);
                        sci.ModifyAttemptRO += new ModifyAttemptROHandler(onModifyAttemptRO);
                    }
                }
                catch (System.Exception)
                {
                    // Do nothing
                }
            }
            else if (e.Type == EventType.Command)
            {
                DataEvent de = e as DataEvent;
                string action = de.Action;
                if (action == "ProjectManager.TreeSelectionChanged" || action == "FileExplorer.TreeSelectionChanged")
                {
                    projectTree = sender as ProjectTreeView;
                    Control contextControl = (sender as Control);
                    if (contextControl.ContextMenuStrip != null)
                        AddItemsToContextMenu(contextControl.ContextMenuStrip, false);
                }
            }
        }

        /// <summary>
        /// Initializes the localization of the plugin
        /// </summary>
        public void InitLocalization()
        {
            LocaleVersion locale = PluginBase.MainForm.Settings.LocaleVersion;
            switch (locale)
            {
                default:
                    // Plugins should default to English...
                    LocaleHelper.Initialize(LocaleVersion.en_US);
                    break;
            }
        }

        #endregion

        #region Custom Methods

        private void AddItemsToContextMenu(ContextMenuStrip menu, bool skipFileAvailabilityCheck)
        {
            if (menu != null)
            {
                string filePath = "";

                if(!skipFileAvailabilityCheck)
                {
                    // Check to see if the input came from the ProjectTree
                    if (projectTree != null && projectTree.Focused && projectTree.SelectedPath != null && projectTree.SelectedPath != "")
                        filePath = projectTree.SelectedPath;
                    else if (PluginBase.MainForm.CurrentDocument != null && PluginBase.MainForm.CurrentDocument.IsUntitled == false)
                        filePath = PluginBase.MainForm.CurrentDocument.FileName;
                }
               
                // Order items here
                if ((filePath != "" && File.Exists(filePath)) || skipFileAvailabilityCheck)
                {
                    System.Reflection.Assembly thisExe = System.Reflection.Assembly.GetExecutingAssembly();
                    System.IO.Stream file;
                    if(settingObject.BlueIcons)
                        file = thisExe.GetManifestResourceStream("Perforce.Resources.p4_blue.bmp");
                    else
                        file = thisExe.GetManifestResourceStream("Perforce.Resources.p4_v.bmp");
                             
                    ToolStripItem editFile = new ToolStripMenuItem("Edit File",
                    Image.FromStream(file),
                    new EventHandler(onEditFile));
                    ToolStripItem diffFile = new ToolStripMenuItem("Diff File",
                    Image.FromStream(file),
                    new EventHandler(onDiffFile));
                    ToolStripItem addFile = new ToolStripMenuItem("Add to Source Control",
                    Image.FromStream(file),
                    new EventHandler(onAddFile));
                    ToolStripItem revertFile = new ToolStripMenuItem("Revert File",
                    Image.FromStream(file),
                    new EventHandler(onRevertFile));
                    ToolStripItem exploreFile = new ToolStripMenuItem("Open Containing Folder",
                    null,
                    new EventHandler(onExploreFile));
                    ToolStripItem copyPath = new ToolStripMenuItem("Copy Full Path",
                    null,
                    new EventHandler(onCopyPath));
               
                    menu.Items.Add("-");
                    menu.Items.Add(copyPath);
                    menu.Items.Add(exploreFile);
                    menu.Items.Add("-");
                    menu.Items.Add(editFile);
                    menu.Items.Add(diffFile);
                    menu.Items.Add(revertFile);
                    menu.Items.Add(addFile);
                }                       
            }
        }

        public void onModifyAttemptRO(ScintillaControl sender)
        {
            string message = "You're attempting to edit a read only file. Would you like to attempt an Open for Edit operation from Perforce?";
            DialogResult res = DialogResult.Yes;
            if (settingObject.SilentOpenForEdit == false)
                res = MessageBox.Show(message, "Perforce Error", MessageBoxButtons.YesNo);
            if (res == DialogResult.Yes)
            {
                onEditFile(this, new EventArgs());
                Globals.SciControl.ModifyAttemptRO += new ModifyAttemptROHandler(Globals.MainForm.OnScintillaControlModifyRO);
            }
            else
            {
                Globals.SciControl.ModifyAttemptRO -= new ModifyAttemptROHandler(onModifyAttemptRO);
                Globals.SciControl.ModifyAttemptRO += new ModifyAttemptROHandler(Globals.MainForm.OnScintillaControlModifyRO);
            }
        }

        private void onExploreFile(object sender, EventArgs e)
        {
            string filePath = "";

            // Check to see if the input came from the ProjectTree
            if (projectTree != null && projectTree.Focused && projectTree.SelectedPath != null && projectTree.SelectedPath != "")
                filePath = projectTree.SelectedPath;
            else if (PluginBase.MainForm.CurrentDocument.IsUntitled == false)
                filePath = PluginBase.MainForm.CurrentDocument.FileName;

            if (filePath != "" && File.Exists(filePath))
            {
                string argument = @"/select, " + filePath;
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }

        private void onCopyPath(object sender, EventArgs e)
        {
            string filePath = "";

            // Check to see if the input came from the ProjectTree
            if (projectTree != null && projectTree.Focused && projectTree.SelectedPath != null && projectTree.SelectedPath != "")
                filePath = projectTree.SelectedPath;
            else if (PluginBase.MainForm.CurrentDocument.IsUntitled == false)
                filePath = PluginBase.MainForm.CurrentDocument.FileName;

            if (filePath != "" && File.Exists(filePath))
                Clipboard.SetText(filePath);
        }
        #endregion

        #region Perforce Functionality
        private string PerforceGetTicket()
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            proc.StartInfo.Arguments = "/C echo " + settingObject.Password + "|p4 login -p";
            proc.StartInfo.FileName = "cmd";

            TraceManager.Add(" " + proc.StartInfo.Arguments, (Int32)TraceType.ProcessStart);
            proc.Start();
            proc.WaitForExit();
            if (!proc.StandardOutput.EndOfStream)
            {
                string output = proc.StandardOutput.ReadToEnd();
                TraceManager.Add("Operation Successful: " + output);
                string[] outputMsgs = output.Split('\n');                
                string ticket = Array.FindLast(outputMsgs, s => s.Trim() != "" && s.IndexOf(':') < 0);
                if (ticket == null)
                    return "";
                return ticket.Trim();
            }
            else
            {
                string output = proc.StandardError.ReadToEnd();
                TraceManager.Add("Operation Failed: " + output);
                string[] outputMsgs = output.Split('\n');
                foreach (string msg in outputMsgs)
                {
                    if (msg.Trim() == "")
                        continue;

                    int lastSlash = msg.LastIndexOf('\\');
                    if (lastSlash < 0)
                        lastSlash = msg.LastIndexOf('/');

                    string relevantInfo = msg.Substring(lastSlash + 1);
                    MessageBox.Show(relevantInfo, "Perforce Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }                
            }
            return "";
        }
        
        private string GetGlobalArguments()
        {
            string arguments = "";
            if (settingObject.UserName != "")
                arguments += " -u " + settingObject.UserName;
            if (settingObject.Password != "")
            {
                if (settingObject.TicketBasedAuth)
                {
                    string ticket = PerforceGetTicket();
                    if (ticket == "")
                    {
                        string message = "The plugin did not receive an authentication ticket from Perforce. " +
                        "This most likely means your Perforce password is incorrect.";
                        TraceManager.Add(message);
                    }
                    else
                        arguments += " -P " + ticket;
                }
                else
                    arguments += " -P " + settingObject.Password;                
            }

            if (settingObject.Client != "")
                arguments += " -c " + settingObject.Client;
            return arguments;
        }

        private void onEditFile(object sender, EventArgs e)
        {
            // Check to see if the input came from the ProjectTree
            if (projectTree != null && projectTree.Focused && projectTree.SelectedPaths.Length > 0)
            {
                foreach(string path in projectTree.SelectedPaths)
                {
                    PerforceEdit(path);
                }                
            }
            else if (PluginBase.MainForm.CurrentDocument.IsUntitled == false)
            {
                PerforceEdit(PluginBase.MainForm.CurrentDocument.FileName);                
            }                      
        }

        private void PerforceEdit(string filePath)
        {
            if (filePath != "" && File.Exists(filePath))
            {
                Process proc = new Process();
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                proc.StartInfo.Arguments = "/C p4" + GetGlobalArguments() + " edit \"" + filePath + "\"";
                proc.StartInfo.FileName = "cmd";

                TraceManager.Add(" " + proc.StartInfo.Arguments, (Int32)TraceType.ProcessStart);
                proc.Start();
                proc.WaitForExit();
                if (!proc.StandardOutput.EndOfStream)
                {
                    TraceManager.Add("Operation Successful: " + proc.StandardOutput.ReadToEnd());
                    PluginBase.MainForm.CurrentDocument.Reload(false);
                }
                else
                {
                    string output = proc.StandardError.ReadToEnd();
                    // If it was a password related problem it's very likely that the user's ticket has
                    // expired. Prompt them if they want to sign in.
                    if (output.Contains("P4PASSWD"))
                        attemptLogin(new EventHandler(onRevertFile));
                    else
                    {
                        TraceManager.Add("Operation Failed: " + output);
                        string[] outputMsgs = output.Split('\n');
                        foreach (string msg in outputMsgs)
                        {
                            if (msg.Trim() == "")
                                continue;

                            int lastSlash = msg.LastIndexOf('\\');
                            if (lastSlash < 0)
                                lastSlash = msg.LastIndexOf('/');

                            string relevantInfo = msg.Substring(lastSlash + 1);
                            MessageBox.Show(relevantInfo, "Perforce Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        private void onDiffFile(object sender, EventArgs e)
        {
            // Check to see if the input came from the ProjectTree
            if (projectTree != null && projectTree.Focused && projectTree.SelectedPaths.Length > 0)
            {
                foreach (string path in projectTree.SelectedPaths)
                {
                    PerforceDiff(path);
                }                
            }
            else if (PluginBase.MainForm.CurrentDocument.IsUntitled == false)
            {
                PerforceDiff(PluginBase.MainForm.CurrentDocument.FileName);
            }  
        }

        private void PerforceDiff(string filePath)
        {
            if (filePath != "" && File.Exists(filePath))
            {
                TraceManager.Add(" " + "p4" + GetGlobalArguments() + " diff \"" + filePath + "\"", (Int32)TraceType.ProcessStart);
                Process proc = new Process();

                if (settingObject.DiffProgram != "")
                {
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;

                    if (proc.StartInfo.EnvironmentVariables.ContainsKey("P4DIFF"))
                        proc.StartInfo.EnvironmentVariables["P4DIFF"] = settingObject.DiffProgram;
                    else
                        proc.StartInfo.EnvironmentVariables.Add("P4DIFF", settingObject.DiffProgram);

                    proc.StartInfo.Arguments = "/C p4" + GetGlobalArguments() + " diff \"" + filePath + "\"";
                }
                else
                    proc.StartInfo.Arguments = "/K p4" + GetGlobalArguments() + " diff \"" + filePath + "\"";
                proc.StartInfo.FileName = "cmd";

                TraceManager.Add(" " + proc.StartInfo.Arguments, (Int32)TraceType.ProcessStart);
                proc.Start();
            }
        }

        private void onAddFile(object sender, EventArgs e)
        {
            // Check to see if the input came from the ProjectTree
            if (projectTree != null && projectTree.Focused && projectTree.SelectedPaths.Length > 0)
            {
                foreach (string path in projectTree.SelectedPaths)
                {
                    PerforceAdd(path);
                }                
            }
            else if (PluginBase.MainForm.CurrentDocument.IsUntitled == false)
            {
                PerforceAdd(PluginBase.MainForm.CurrentDocument.FileName);
            }
       }

        private void PerforceAdd(string filePath)
        {
            if (filePath != "" && File.Exists(filePath))
            {
                Process proc = new Process();

                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                proc.StartInfo.Arguments = "/C p4" + GetGlobalArguments() + " add \"" + filePath + "\"";
                proc.StartInfo.FileName = "cmd";

                TraceManager.Add(" " + proc.StartInfo.Arguments, (Int32)TraceType.ProcessStart);
                proc.Start();
                proc.WaitForExit();
                if (!proc.StandardOutput.EndOfStream)
                {
                    string output = proc.StandardOutput.ReadToEnd();

                    TraceManager.Add("Operation Successful: " + output);
                    string[] outputMsgs = output.Split('\n');
                    foreach (string msg in outputMsgs)
                    {
                        if (msg.Trim() == "")
                            continue;

                        int lastSlash = msg.LastIndexOf('\\');
                        if (lastSlash < 0)
                            lastSlash = msg.LastIndexOf('/');

                        string relevantInfo = msg.Substring(lastSlash + 1);
                        MessageBox.Show(relevantInfo, "Perforce Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    string output = proc.StandardError.ReadToEnd();
                    // If it was a password related problem it's very likely that the user's ticket has
                    // expired. Prompt them if they want to sign in.
                    if (output.Contains("P4PASSWD"))
                        attemptLogin(new EventHandler(onAddFile));
                    else
                    {
                        TraceManager.Add("Operation Failed: " + output);
                        string[] outputMsgs = output.Split('\n');
                        foreach (string msg in outputMsgs)
                        {
                            if (msg.Trim() == "")
                                continue;

                            int lastSlash = msg.LastIndexOf('\\');
                            if (lastSlash < 0)
                                lastSlash = msg.LastIndexOf('/');

                            string relevantInfo = msg.Substring(lastSlash + 1);
                            MessageBox.Show(relevantInfo, "Perforce Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
  
        }

        private void onRevertFile(object sender, EventArgs e)
        {
            // Check to see if the input came from the ProjectTree
            if (projectTree != null && projectTree.Focused && projectTree.SelectedPaths.Length > 0)
            {
                foreach (string path in projectTree.SelectedPaths)
                {
                    PerforceRevert(path, e);
                }                
            }
            else if (PluginBase.MainForm.CurrentDocument.IsUntitled == false)
            {
                PerforceRevert(PluginBase.MainForm.CurrentDocument.FileName, e);
            }
        }

        private void PerforceRevert(string filePath, EventArgs callerEventArgs)
        {
            if (filePath != "" && File.Exists(filePath))
            {
                DialogResult res = DialogResult.Yes;
                if (callerEventArgs as DoNotAskAgainEventArgs == null)
                {
                    string message = "Reverting a file will override any edits made to it. Are you sure you want to Revert?";
                    res = MessageBox.Show(message, "Perforce Warning", MessageBoxButtons.YesNo);
                }
                if (res == DialogResult.No)
                    return;

                Process proc = new Process();
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                proc.StartInfo.Arguments = "/C p4" + GetGlobalArguments() + " revert \"" + filePath + "\"";
                proc.StartInfo.FileName = "cmd";

                TraceManager.Add(" " + proc.StartInfo.Arguments, (Int32)TraceType.ProcessStart);

                proc.Start();
                proc.WaitForExit();
                if (!proc.StandardOutput.EndOfStream)
                {
                    // Read comment at beginning of tile, above "previousFileChangedNotificationSetting" variable.
                    //PluginBase.MainForm.Settings.AutoReloadModifiedFiles = true;
                    //PluginBase.MainForm.CurrentDocument.Reload(false);
                    TraceManager.Add("Operation Successful: " + proc.StandardOutput.ReadToEnd());
                }
                else
                {
                    string output = proc.StandardError.ReadToEnd();
                    // If it was a password related problem it's very likely that the user's ticket has
                    // expired. Prompt them if they want to sign in.
                    if (output.Contains("P4PASSWD"))
                        attemptLogin(new EventHandler(onRevertFile));
                    else
                    {
                        TraceManager.Add("Operation Failed: " + output);
                        string[] outputMsgs = output.Split('\n');
                        foreach (string msg in outputMsgs)
                        {
                            if (msg.Trim() == "")
                                continue;

                            int lastSlash = msg.LastIndexOf('\\');
                            if (lastSlash < 0)
                                lastSlash = msg.LastIndexOf('/');

                            string relevantInfo = msg.Substring(lastSlash + 1);
                            MessageBox.Show(relevantInfo, "Perforce Output", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }                     
                }
            }
        }

        private void attemptLogin(EventHandler callBack)
        {
                string message = "The plugin did not receive a response from Perforce. " +
                "This is likely due to the fact that your session is expired. Would you like to attempt to login?" +
                "\n\nSet your login credentials in the \"Perforce\" tab in Settings to have this done automatically.";
                DialogResult res = MessageBox.Show(message, "Perforce Error", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes)
                {
                    TraceManager.Add(" " + "p4 login", (Int32)TraceType.ProcessStart);
                    Process proc = new Process();

                    proc.StartInfo.Arguments = "/C " + "p4 login";
                    proc.StartInfo.FileName = "cmd";

                    proc.Start();
                    proc.WaitForExit();
                    callBack.Invoke(null, new DoNotAskAgainEventArgs());
                }
            }

        private class DoNotAskAgainEventArgs : EventArgs
        { }
        #endregion
    }
}
