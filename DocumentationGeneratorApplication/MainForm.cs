﻿using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Configuration;
using net.datacowboy.SqlServerDatabaseDocumentationGenerator.Inspection;
using net.datacowboy.SqlServerDatabaseDocumentationGenerator.Document;
using net.datacowboy.SqlServerDatabaseDocumentationGenerator.Utility;
using net.datacowboy.SqlServerDatabaseDocumentationGenerator.Model;

namespace net.datacowboy.DocumentationGeneratorApplication
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			//provide defaults

            this.txtConnectionString.Text = this.getConnectionStringFromAppConfig() ?? String.Empty;

			this.txtDocFile.Text = "documentation.html";


		}

        /// <summary>
        /// Get database connection string from app.config file
        /// Connection string named "default" in app.config
        /// </summary>
        /// <returns></returns>
        private string getConnectionStringFromAppConfig()
        {
            string connString = null;

           
            var config = ConfigurationManager.ConnectionStrings["default"];
            if (config != null)
            {
                connString = config.ConnectionString;
            }
            

            return connString;
        }


        

        private void clearErrorMessages()
        {
            this.errorProviderMainForm.Clear();
        }

        /// <summary>
        /// Form input validation
        /// </summary>
        /// <param name="requireDocumentationFile">Is documentation file required, applies when generating document</param>
        /// <returns></returns>
        private bool validateFormInput(bool requireDocumentationFile=true)
        {

            bool errorFound = false;

            this.clearErrorMessages();

            //was a connection string provided?
            if (String.IsNullOrWhiteSpace(this.txtConnectionString.Text))
            {
                errorFound = true;
                this.errorProviderMainForm.SetError(this.txtConnectionString, "Please provide a connection string");
            }
            else
            {
                var connectionTestResult = SqlConnectionTester.TestConnectionString(this.txtConnectionString.Text, true);
                if (!connectionTestResult.Success)
                {
                    errorFound = true;
                    this.errorProviderMainForm.SetError(this.txtConnectionString, (connectionTestResult.ErrorMessage ?? "Invalid connection string"));
                }

            }

            //has an ouptut filename with optional path been provided?
            if (requireDocumentationFile && String.IsNullOrWhiteSpace(this.txtDocFile.Text))
            {
                errorFound = true;
                this.errorProviderMainForm.SetError(this.txtDocFile, "Please enter a filename for the documentation file to be created");
            }
            else
            {
                string proposedFilename = this.txtDocFile.Text.Trim();

                FileInfo fileInfo = null;

                try
                {
                    fileInfo = new FileInfo(proposedFilename);
                }
                catch
                {
                    errorFound = true;
                    this.errorProviderMainForm.SetError(this.txtDocFile, "Proposed filename is invalid");
                }


                //is an HTML file extension
                if(fileInfo != null && !( fileInfo.Extension.ToLower() == ".htm" || fileInfo.Extension.ToLower() == ".html"))
                {
                    errorFound = true;
                    this.errorProviderMainForm.SetError(this.txtDocFile, "File name must have .htm or .html file extension");
                }


            }
            

            return !errorFound; //input valid when no errors found

        }




        private static Task<Database> getDatabaseMetaDataAysnc(string connectionString)
        {
            return Task<Database>.Factory.StartNew(() =>
            {
                var dbi = new DatabaseInspector(connectionString);
                return dbi.GetDatabaseMetaData();

            });

        }


        private DocumentGeneratorConfiguration createDocumentGeneratorConfigurationFroUi()
        {
            var config = new DocumentGeneratorConfiguration();

            config.ForeignKeyToTableHyperLink = this.chkFkToTableHyperLink.Checked;

            config.CheckForDesignIssues = this.chkCheckForDatabaseDesignIssues.Checked;

            return config;
        }

		private void btnGenerateDoc_Click(object sender, EventArgs e)
		{
            if (!this.validateFormInput(true))
            {
                return;
            }

            this.lockUi();

            this.Cursor = Cursors.WaitCursor;
            Application.DoEvents();

            DocumentGeneratorConfiguration docGenConfig = this.createDocumentGeneratorConfigurationFroUi();

            //perform database operations aysnc
            var taskMeta = getDatabaseMetaDataAysnc(this.txtConnectionString.Text);

            var metadata = taskMeta.Result;

			DatabaseHtmlDocumentGenerator gen = new DatabaseHtmlDocumentGenerator();

			string docFilePath = this.txtDocFile.Text.Trim();

			using (var sw = new StreamWriter(docFilePath, false))
			{
				var str = gen.ExportToHtml(metadata, sw, docGenConfig);
			}



            this.Cursor = Cursors.Default;
            Application.DoEvents();

			if (this.chkOpenDoc.Checked && File.Exists(docFilePath))
			{
				Process.Start(docFilePath);
			}

            this.unLockUi();
		}

        private void btnDocFileBrowse_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = "html";
            dialog.ValidateNames = true;
            dialog.Filter = "HTML Files (*.htm;*.html)|*.htm;*.html";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.txtDocFile.Text = dialog.FileName;
            }
        }

        private void btnEditConnection_Click(object sender, EventArgs e)
        {
            this.showConnectionStringDialog();
        }

        private void showConnectionStringDialog()
        {
            var frmConnEditDialog = new FrmConnectionString(this.txtConnectionString.Text.Trim());

            frmConnEditDialog.ShowDialog();

            if (frmConnEditDialog.Result == System.Windows.Forms.DialogResult.OK)
            {
                this.txtConnectionString.Text = frmConnEditDialog.GetConnectionString();
            }



        }

        private void btnFindObjectsWithoutDescription_Click(object sender, EventArgs e)
        {
            if (!this.validateFormInput(false))
            {
                return;
            }

            this.lockUi();

            this.Cursor = Cursors.WaitCursor;

            Application.DoEvents();

            //perform database operations aysnc
            var taskMeta = getDatabaseMetaDataAysnc(this.txtConnectionString.Text);

            var metadata = taskMeta.Result;

            



            this.Cursor = Cursors.Default;
            Application.DoEvents();

            this.showObjectsWithoutDescriptionDialog(metadata);

            this.unLockUi();
        }

        private void showObjectsWithoutDescriptionDialog(Database dbMetadata)
        {
            var dialogForm = new FrmObjectsWithoutDescription(dbMetadata);
            dialogForm.ShowDialog();
        }

        /// <summary>
        /// Lock UI controls 
        /// </summary>
        /// <remarks>Call before call long running logic</remarks>
        private void lockUi()
        {
            this.txtConnectionString.Enabled = false;
            this.txtDocFile.Enabled = false;
            this.btnDocFileBrowse.Enabled = false;
            this.btnEditConnection.Enabled = false;
            this.btnGenerateDoc.Enabled = false;
            this.btnFindObjectsWithoutDescription.Enabled = false;
            this.chkOpenDoc.Enabled = false;
            this.chkFkToTableHyperLink.Enabled = false;
            this.chkCheckForDatabaseDesignIssues.Enabled = false;
        }


        /// <summary>
        /// Unlock UI controls
        /// </summary>
        /// <remarks>Call when locking UI no longer needed</remarks>
        private void unLockUi()
        {
            this.txtConnectionString.Enabled = true;
            this.txtDocFile.Enabled = true;
            this.btnDocFileBrowse.Enabled = true;
            this.btnEditConnection.Enabled = true;
            this.btnGenerateDoc.Enabled = true;
            this.btnFindObjectsWithoutDescription.Enabled = true;
            this.chkOpenDoc.Enabled = true;
            this.chkFkToTableHyperLink.Enabled = true;
            this.chkCheckForDatabaseDesignIssues.Enabled = true;
        }

	}
}
