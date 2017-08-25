using System;
using System.Web.UI;
using System.IO;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;
using umbraco.cms.presentation.Trees;
using Umbraco.Web._Legacy.Controls;
using Umbraco.Web;
using Umbraco.Web.Composing;

namespace umbraco.cms.presentation.settings.scripts
{
    public partial class editScript : Umbraco.Web.UI.Pages.UmbracoEnsuredPage
    {
        public editScript()
        {
            CurrentApp = Constants.Applications.Settings.ToString();

        }
        protected System.Web.UI.HtmlControls.HtmlForm Form1;
        protected Umbraco.Web._Legacy.Controls.TabView Panel1;
        protected System.Web.UI.WebControls.TextBox NameTxt;
        protected Umbraco.Web._Legacy.Controls.Pane Pane7;
        protected Umbraco.Web._Legacy.Controls.Pane Pane8;

        protected System.Web.UI.WebControls.Literal lttPath;
        protected System.Web.UI.WebControls.Literal editorJs;
        protected Umbraco.Web._Legacy.Controls.CodeArea editorSource;
        protected Umbraco.Web._Legacy.Controls.PropertyPanel pp_name;
        protected Umbraco.Web._Legacy.Controls.PropertyPanel pp_path;

        protected MenuButton SaveButton;

        private string filename;
        protected string ScriptTreeSyncPath { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // get the script, ensure it exists (not null) and validate (because
            // the file service ensures that it loads scripts from the proper location
            // but does not seem to validate extensions?) - in case of an error,
            // throw - that's what we did anyways.

            // also scrapping the code that added .cshtml and .vbhtml extensions, and
            // ~/Views directory - we're not using editScript.aspx for views anymore.

            var svce = Current.Services.FileService;
            var script = svce.GetScriptByName(filename);
            if (script == null) // not found
                throw new FileNotFoundException("Could not find file '" + filename + "'.");

            lttPath.Text = "<a id=\"" + lttPath.ClientID + "\" target=\"_blank\" href=\"" + script.VirtualPath + "\">" + script.VirtualPath + "</a>";
            editorSource.Text = script.Content;
            ScriptTreeSyncPath = BaseTree.GetTreePathFromFilePath(filename);

            // name derives from filename, clean for xss
            NameTxt.Text = filename.CleanForXss('\\', '/');

            Panel1.Text = Services.TextService.Localize("editscript");
            pp_name.Text = Services.TextService.Localize("name");
            pp_path.Text = Services.TextService.Localize("path");

            if (IsPostBack == false)
            {
                ClientTools
                    .SetActiveTreeType(Constants.Trees.Scripts)
                    .SyncTree(ScriptTreeSyncPath, false);
            }
        }

        override protected void OnInit(EventArgs e)
        {
            base.OnInit(e);

            filename = Request.QueryString["file"].Replace('\\', '/').TrimStart('/');

            //need to change the editor type if it is XML
            if (filename.EndsWith("xml"))
                editorSource.CodeBase = Umbraco.Web._Legacy.Controls.CodeArea.EditorType.XML;
            else if (filename.EndsWith("master"))
                editorSource.CodeBase = Umbraco.Web._Legacy.Controls.CodeArea.EditorType.HTML;


            var editor = Panel1.NewTabPage(Services.TextService.Localize("settings/script"));
            editor.Controls.Add(Pane7);

            var props = Panel1.NewTabPage(Services.TextService.Localize("properties"));
            props.Controls.Add(Pane8);


            SaveButton = Panel1.Menu.NewButton();
            SaveButton.Text = Services.TextService.Localize("save");
            SaveButton.ButtonType = MenuButtonType.Primary;
            SaveButton.ID = "save";
            SaveButton.CssClass = "client-side";

            if (editorSource.CodeBase == Umbraco.Web._Legacy.Controls.CodeArea.EditorType.HTML)
            {
                // Editing buttons
                Panel1.Menu.InsertSplitter();
                Umbraco.Web._Legacy.Controls.MenuIconI umbField = Panel1.Menu.NewIcon();
                umbField.ImageURL = SystemDirectories.Umbraco + "/images/editor/insField.gif";
                umbField.OnClickCommand = Umbraco.Web.UI.Pages.ClientTools.Scripts.OpenModalWindow(IOHelper.ResolveUrl(SystemDirectories.Umbraco) + "/dialogs/umbracoField.aspx?objectId=" + editorSource.ClientID + "&tagName=UMBRACOGETDATA", Services.TextService.Localize("template/insertPageField"), 640, 550);
                umbField.AltText = Services.TextService.Localize("template/insertPageField");

                // TODO: Update icon
                Umbraco.Web._Legacy.Controls.MenuIconI umbDictionary = Panel1.Menu.NewIcon();
                umbDictionary.ImageURL = GlobalSettings.Path + "/images/editor/dictionaryItem.gif";
                umbDictionary.OnClickCommand = Umbraco.Web.UI.Pages.ClientTools.Scripts.OpenModalWindow(IOHelper.ResolveUrl(SystemDirectories.Umbraco) + "/dialogs/umbracoField.aspx?objectId=" + editorSource.ClientID + "&tagName=UMBRACOGETDICTIONARY", Services.TextService.Localize("template/insertDictionaryItem"), 640, 550);
                umbDictionary.AltText = "Insert umbraco dictionary item";

                // Help
                Panel1.Menu.InsertSplitter();

                Umbraco.Web._Legacy.Controls.MenuIconI helpIcon = Panel1.Menu.NewIcon();
                helpIcon.OnClickCommand = Umbraco.Web.UI.Pages.ClientTools.Scripts.OpenModalWindow(Umbraco.Core.IO.IOHelper.ResolveUrl(Umbraco.Core.IO.SystemDirectories.Umbraco) + "/settings/modals/showumbracotags.aspx?alias=", Services.TextService.Localize("template/quickGuide"), 600, 580);
                helpIcon.ImageURL = SystemDirectories.Umbraco + "/images/editor/help.png";
                helpIcon.AltText = Services.TextService.Localize("template/quickGuide");

            }

        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            ScriptManager.GetCurrent(Page).Services.Add(new ServiceReference("../webservices/codeEditorSave.asmx"));
            ScriptManager.GetCurrent(Page).Services.Add(new ServiceReference("../webservices/legacyAjaxCalls.asmx"));
        }

    }
}