﻿using System;
using System.ServiceProcess;
using SerialPortListener.Serial;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Web.Http.Cors;
using System.IO.Ports;

namespace requestPeso
{
    public partial class Scheduler : ServiceBase
    {
        ValuesController _vc = null;
        const string _configPath = "./config.ini";

        static string _portName;

        public static string PortName
        {
            get
            {
                return _portName;
            }
        }

        public Scheduler()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (args.Length == 2)
                Start(args[1]);
            else if (File.Exists(_configPath))
            {
                getComToUse();
                Start(_portName);
            }
            else
            {
                Logs.errorLogs("Parametri errati o config.ini mancante, usare --port COMX per i parametri!");
                this.Stop();
            }
        }

        /// <summary>
        /// Inizializza la variabile _portName dal file C:\requestoPeso.txt e imposta le variabili per leggere dalla seriale
        /// Da mettere public per debug
        /// </summary>
        private void Start(string port)
        {
            _portName = port;
            bool _goAhead = false;

            string[] portExists = SerialPort.GetPortNames();
            foreach (string item in portExists)
            {
                if (item == _portName)
                    _goAhead = true;
            }

            if (_goAhead)
            {
                inizializzaThread();
                Logs.errorLogs("Servizio partito");
            }
            else
            {
                Logs.errorLogs("Porta non trovata");
                this.Stop();
            }
            //inizializzaServerWeb();
        }

        private void inizializzaThread()
        {
            Thread threadOnStart = new Thread(inizializzaServerWeb);
            threadOnStart.Name = "threadServerWeb";
            threadOnStart.IsBackground = false;
            threadOnStart.Start();
        }

        private void inizializzaServerWeb()
        {
            try
            {
                _vc = new ValuesController();

                HttpSelfHostConfiguration config = new HttpSelfHostConfiguration("http://localhost:8080");
                
                config.Routes.MapHttpRoute( name: "DefaultApi",
                                            routeTemplate: "api/{controller}/{id}",
                                            defaults: new { id = RouteParameter.Optional });
                
                config.EnableCors(new EnableCorsAttribute("*", "*", "*"));
                HttpSelfHostServer server = new HttpSelfHostServer(config);
                server.OpenAsync().Wait();

                Logs.errorLogs("WEBSERVER CREATO!");
            }
            catch (Exception ex)
            {
                Logs.errorLogs(ex);
            }
        }
        
        /// <summary>
        /// Ferma il servizio
        /// </summary>
        protected override void OnStop()
        {
            _vc.Dispose();
            Logs.errorLogs("Servizio fermato");
            
        }

        /// <summary>
        /// Legge la porta seriale da usare (COM X)
        /// </summary>
        private void getComToUse()
        {
            StreamReader sr = null;

            try
            {
                sr = new StreamReader(_configPath);
                _portName = sr.ReadLine();
                sr.Close();
            }
            catch (Exception ex)
            {
                Logs.errorLogs(ex);
            }
        }
    }
}
