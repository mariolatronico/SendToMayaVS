//------------------------------------------------------------------------------
// <copyright file="SendToMaya.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Net.Sockets;
using System.Net;

namespace SendToMaya
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SendToMaya
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int SendToMayaCmdId = 0x0100;
        public const int SendToMayaDebugCmdId = 0x0102;
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("dbbae0c6-3fa7-46b9-800c-1afae02535c0");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;


        private OutputWindowPane outputPane;
        /// <summary>
        /// Initializes a new instance of the <see cref="SendToMaya"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SendToMaya(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;

            OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var sendToMayaMenuCommandID = new CommandID(CommandSet, SendToMayaCmdId);
                var sendToMayaMenuItem = new MenuCommand(this.SendToMayaCallback, sendToMayaMenuCommandID);
                commandService.AddCommand(sendToMayaMenuItem);

                var sendToMayaDebugMenuCommandID = new CommandID(CommandSet, SendToMayaDebugCmdId);
                var sendToMayaDebugMenuItem = new MenuCommand(this.SendToMayaCallback, sendToMayaDebugMenuCommandID);
                commandService.AddCommand(sendToMayaDebugMenuItem);
                
            }
            outputPane = CreatePane("Maya");
            
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SendToMaya Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        OutputWindowPane CreatePane(string title)
        {
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)ServiceProvider.GetService(typeof(DTE));
            OutputWindowPanes panes =
                dte.ToolWindows.OutputWindow.OutputWindowPanes;
            OutputWindowPane pane;
            try
            {
                // If the pane exists already, write to it.  
                pane = panes.Item(title);
            }
            catch (ArgumentException)
            {
                // Create a new pane and write to it.  
                return panes.Add(title);
            }
            return pane;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new SendToMaya(package);
        }

        private void SendToMayaCallback(object sender, EventArgs e)
        {
            var dte = ServiceProvider.GetService(typeof(_DTE)) as _DTE;
            if (dte == null) return;

            var activeDocument = dte.ActiveDocument;
            if (activeDocument == null) return;
            var textDoc = activeDocument.Object() as TextDocument;
            if (textDoc == null) return;
            var text = textDoc.CreateEditPoint(textDoc.StartPoint).GetText(textDoc.EndPoint);


            // get the options
            SendToMayaPackage sendToMayaPackage = this.package as SendToMayaPackage;
            var port = sendToMayaPackage.OptionPort;
            var hostname = sendToMayaPackage.OptionHost;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPHostEntry hostEntry;

            hostEntry = Dns.GetHostEntry(hostname);

            IPAddress ip = null;
            // get the IPv4 address
            foreach (IPAddress ipAddr in hostEntry.AddressList)
            {
                if (ipAddr.AddressFamily == AddressFamily.InterNetwork)
                    ip = ipAddr;
            }

            MenuCommand menuCommand = sender as MenuCommand;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (menuCommand.CommandID.ID == SendToMayaDebugCmdId)
            {
                //sb.AppendFormat("python(\"");
                sb.AppendFormat("import ptvsd{0}", Environment.NewLine);
                sb.AppendFormat("ptvsd.enable_attach(secret='secret', address = ('{0}', {1}){2}", ip.ToString(), sendToMayaPackage.OptionDebugPort, Environment.NewLine);
                sb.AppendLine("ptvsd.wait_for_attach()");
              
            
            }
            sb.Append(text);
            text = sb.ToString();


            if (ip != null)
            {
                socket.Connect(ip, port);

                byte[] byData = System.Text.Encoding.ASCII.GetBytes(text);
                System.Console.WriteLine(text);
                socket.Send(byData);
                byte[] recvData = new byte[4096];
                int recvCount = socket.Receive(recvData);
                string returnString = System.Text.Encoding.ASCII.GetString(recvData, 0, recvCount);
                
                System.Console.WriteLine(returnString);
                outputPane.OutputString(returnString);

                socket.Disconnect(false);
                socket.Close();
            }
        }

    }
}
