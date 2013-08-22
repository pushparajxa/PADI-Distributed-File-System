using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Shared.Script;

namespace MasterComponent {
    public partial class Puppeteer : Form {
        private readonly CommandParser parser;
        private readonly PuppetMaster master;

        public Puppeteer(PuppetMaster master, CommandParser cp) {
            this.parser = cp;
            this.master = master;
            master.LogListener += WriteOutput;
            InitializeComponent();
        }


        private void LoadButton_Click(object sender, EventArgs e) {
            try {
                parser.LoadFile(FileNameTextBox.Text);
            }
            catch (System.IO.FileNotFoundException execp) {
                MessageBox.Show("File not found: " + execp.FileName);
                return;
            }
            catch (Exception ex) {
                MessageBox.Show("Exception while loading the script " + ex.StackTrace.ToString());
                return;
            }

            MessageBox.Show("Successfully opened the file.");
        }

        private void RunButton_Click(object sender, EventArgs e) {
            parser.ExecuteAll();
        }

        private void NextButton_Click(object sender, EventArgs e) {
            parser.ExecuteNext();
        }

        public void WriteOutput(string text) {
            outputBox.Text = text;
        }

        private void DataButton_Click(object sender, EventArgs e) {
            master.LaunchDataServer();
        }

        private void ClientButton_Click(object sender, EventArgs e) {
            master.LaunchClient();
        }

        private void MetaButton_Click(object sender, EventArgs e) {
            master.LaunchMetadataServer();
        }
    }
}
