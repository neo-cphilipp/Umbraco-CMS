﻿using System;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Hosting;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Logging.Serilog;
using Umbraco.Web.Hosting;
using Current = Umbraco.Web.Composing.Current;

namespace Umbraco.Web
{
    /// <summary>
    /// Provides an abstract base class for the Umbraco HttpApplication.
    /// </summary>
    public abstract class UmbracoApplicationBase : HttpApplication
    {
        private IRuntime _runtime;

        private IFactory _factory;

        protected UmbracoApplicationBase()
        {

            if (!Umbraco.Composing.Current.IsInitialized)
            {
                var configFactory = new ConfigsFactory();

                var hostingSettings = configFactory.HostingSettings;
                var coreDebug = configFactory.CoreDebug;

                var hostingEnvironment = new AspNetHostingEnvironment(hostingSettings);
                var ioHelper = new IOHelper(hostingEnvironment);
                var configs = configFactory.Create(ioHelper);

                var logger = SerilogLogger.CreateWithDefaultConfiguration(hostingEnvironment,  new AspNetSessionIdResolver(), () => _factory?.GetInstance<IRequestCache>(), coreDebug, ioHelper, new FrameworkMarchal());
                var backOfficeInfo = new AspNetBackOfficeInfo(configs.Global(), ioHelper, configs.Settings(), logger);
                var profiler = new LogProfiler(logger);
                Umbraco.Composing.Current.Initialize(logger, configs, ioHelper, hostingEnvironment, backOfficeInfo, profiler);
            }

        }

        protected UmbracoApplicationBase(ILogger logger, Configs configs, IIOHelper ioHelper, IProfiler profiler, IHostingEnvironment hostingEnvironment, IBackOfficeInfo backOfficeInfo)
        {
            if (!Umbraco.Composing.Current.IsInitialized)
            {
                Umbraco.Composing.Current.Initialize(logger, configs, ioHelper, hostingEnvironment, backOfficeInfo, profiler);
            }
        }


        /// <summary>
        /// Gets a runtime.
        /// </summary>
        protected abstract IRuntime GetRuntime(Configs configs, IUmbracoVersion umbracoVersion, IIOHelper ioHelper, ILogger logger, IProfiler profiler, IHostingEnvironment hostingEnvironment, IBackOfficeInfo backOfficeInfo);

        /// <summary>
        /// Gets the application register.
        /// </summary>
        protected virtual IRegister GetRegister(IGlobalSettings globalSettings)
        {
            return RegisterFactory.Create(globalSettings);
        }

        // events - in the order they trigger

        // were part of the BootManager architecture, would trigger only for the initial
        // application, so they need not be static, and they would let ppl hook into the
        // boot process... but I believe this can be achieved with components as well and
        // we don't need these events.
        //public event EventHandler ApplicationStarting;
        //public event EventHandler ApplicationStarted;

        // this event can only be static since there will be several instances of this class
        // triggers for each application instance, ie many times per lifetime of the application
        public static event EventHandler ApplicationInit;

        // this event can only be static since there will be several instances of this class
        // triggers once per error
        public static event EventHandler ApplicationError;

        // this event can only be static since there will be several instances of this class
        // triggers once per lifetime of the application, before it is unloaded
        public static event EventHandler ApplicationEnd;

        #region Start

        // internal for tests
        internal void HandleApplicationStart(object sender, EventArgs evargs)
        {
            // ******** THIS IS WHERE EVERYTHING BEGINS ********


            var globalSettings =  Umbraco.Composing.Current.Configs.Global();
            var umbracoVersion = new UmbracoVersion(globalSettings);

            // create the register for the application, and boot
            // the boot manager is responsible for registrations
            var register = GetRegister(globalSettings);
            _runtime = GetRuntime(
                Umbraco.Composing.Current.Configs,
                umbracoVersion,
                Umbraco.Composing.Current.IOHelper,
                Umbraco.Composing.Current.Logger,
                Umbraco.Composing.Current.Profiler,
                Umbraco.Composing.Current.HostingEnvironment,
                Umbraco.Composing.Current.BackOfficeInfo);
            _factory =_runtime.Boot(register);
        }

        // called by ASP.NET (auto event wireup) once per app domain
        // do NOT set instance data here - only static (see docs)
        // sender is System.Web.HttpApplicationFactory, evargs is EventArgs.Empty
        protected void Application_Start(object sender, EventArgs evargs)
        {
            Thread.CurrentThread.SanitizeThreadCulture();
            HandleApplicationStart(sender, evargs);
        }

        #endregion

        #region Init

        private void OnApplicationInit(object sender, EventArgs evargs)
        {
            TryInvoke(ApplicationInit, "ApplicationInit", sender, evargs);
        }

        // called by ASP.NET for every HttpApplication instance after all modules have been created
        // which means that this will be called *many* times for different apps when Umbraco runs
        public override void Init()
        {
            // note: base.Init() is what initializes all of the http modules, ties up a bunch of stuff with IIS, etc...
            // therefore, since OWIN is an HttpModule when running in IIS/ASP.Net the OWIN startup is not executed
            // until this method fires and by that time - Umbraco has booted already

            base.Init();
            OnApplicationInit(this, new EventArgs());
        }


        #endregion

        #region End

        protected virtual void OnApplicationEnd(object sender, EventArgs evargs)
        {
            ApplicationEnd?.Invoke(this, EventArgs.Empty);
        }

        // internal for tests
        internal void HandleApplicationEnd()
        {
            if (_runtime != null)
            {
                _runtime.Terminate();
                _runtime.DisposeIfDisposable();

                _runtime = null;
            }

            // try to log the detailed shutdown message (typical asp.net hack: http://weblogs.asp.net/scottgu/433194)
            try
            {
                var runtime = (HttpRuntime) typeof(HttpRuntime).InvokeMember("_theRuntime",
                    BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField,
                    null, null, null);
                if (runtime == null)
                    return;

                var shutDownMessage = (string)runtime.GetType().InvokeMember("_shutDownMessage",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                    null, runtime, null);

                var shutDownStack = (string)runtime.GetType().InvokeMember("_shutDownStack",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                    null, runtime, null);

                Current.Logger.Info<UmbracoApplicationBase>("Application shutdown. Details: {ShutdownReason}\r\n\r\n_shutDownMessage={ShutdownMessage}\r\n\r\n_shutDownStack={ShutdownStack}",
                    HostingEnvironment.ShutdownReason,
                    shutDownMessage,
                    shutDownStack);
            }
            catch (Exception)
            {
                //if for some reason that fails, then log the normal output
                Current.Logger.Info<UmbracoApplicationBase>("Application shutdown. Reason: {ShutdownReason}", HostingEnvironment.ShutdownReason);
            }

            Current.Logger.DisposeIfDisposable();
            // dispose the container and everything
            Current.Reset();
        }

        // called by ASP.NET (auto event wireup) once per app domain
        // sender is System.Web.HttpApplicationFactory, evargs is EventArgs.Empty
        protected void Application_End(object sender, EventArgs evargs)
        {
            OnApplicationEnd(sender, evargs);
            HandleApplicationEnd();
        }

        #endregion

        #region Error

        protected virtual void OnApplicationError(object sender, EventArgs evargs)
        {
            ApplicationError?.Invoke(this, EventArgs.Empty);
        }

        private void HandleApplicationError()
        {
            var exception = Server.GetLastError();

            // ignore HTTP errors
            if (exception.GetType() == typeof(HttpException)) return;

            Current.Logger.Error<UmbracoApplicationBase>(exception, "An unhandled exception occurred");
        }

        // called by ASP.NET (auto event wireup) at any phase in the application life cycle
        protected void Application_Error(object sender, EventArgs e)
        {
            // when unhandled errors occur
            HandleApplicationError();
            OnApplicationError(sender, e);
        }

        #endregion

        #region Utilities

        private static void TryInvoke(EventHandler handler, string name, object sender, EventArgs evargs)
        {
            try
            {
                handler?.Invoke(sender, evargs);
            }
            catch (Exception ex)
            {
                Current.Logger.Error<UmbracoApplicationBase>(ex, "Error in {Name} handler.", name);
                throw;
            }
        }

        #endregion
    }
}