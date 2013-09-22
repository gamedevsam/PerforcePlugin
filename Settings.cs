using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Design;
using System.Windows.Forms.Design;


namespace Perforce
{
    [Serializable]
    public class Settings
    {
        private const String PERFORCE_CATEGORY = "Perforce Settings";
        private const String DIFF_CATEGORY = "Diff Settings";
        private const String MISC_CATEGORY = "Misc Settings";
        private String userName = "";
        private String password = "";
        private Boolean ticketBasedAuth = false;
        private String client = "";
        private String diffProgram = "";
        private Boolean silentOpenForEdit = true;
        private Boolean blueIcons = false;

        [Category(PERFORCE_CATEGORY)]
        [DisplayName("Login Name")]
        [Description("Perforce user name.")]
        [DefaultValue("")]
        public String UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        [Category(PERFORCE_CATEGORY)]
        [DisplayName("Password")]
        [Description("Perforce password.")]
        [DefaultValue("")]
        public String Password
        {
            get { return password; }
            set { password = value; }
        }

        [Category(PERFORCE_CATEGORY)]
        [DisplayName("Ticket-based authentication")]
        [Description("Use Perforce tickets rather than sending your password with every command.")]
        [DefaultValue(false)]
        public Boolean TicketBasedAuth
        {
            get { return ticketBasedAuth; }
            set { ticketBasedAuth = value; }
        }

        [Category(PERFORCE_CATEGORY)]
        [DisplayName("Client")]
        [Description("Overrides P4CLIENT (Name of current client workspace) setting with the specified client name.")]
        [DefaultValue("")]
        public String Client
        {
            get { return client; }
            set { client = value; }
        }

        [Category(DIFF_CATEGORY)]
        [DisplayName("Diff Program")]
        [Description("Default program for Diffing files in Perforce. Full File Path only!")]
        [ DefaultValue("")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public String DiffProgram
        {
            get { return diffProgram; }
            set { diffProgram = value; }
        }

        [Category(MISC_CATEGORY)]
        [DisplayName("Open for Edit Silently")]
        [Description("Check this to perform open for edit operations when you try to edit a Read Only file.")]
        [DefaultValue(true)]        
        public Boolean SilentOpenForEdit
        {
            get { return silentOpenForEdit; }
            set { silentOpenForEdit = value; }
        }

        [Category(MISC_CATEGORY)]
        [DisplayName("Blue Icons")]
        [Description("Replaces the default P4V yellow icons with a more readable blue icon.")]
        [DefaultValue(false)]
        public Boolean BlueIcons
        {
            get { return blueIcons; }
            set { blueIcons = value; }
        }
    }
}
