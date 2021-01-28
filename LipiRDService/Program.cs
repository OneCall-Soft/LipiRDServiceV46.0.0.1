using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace LipiRDService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //static void Main()
        //{
        //    ServiceBase[] ServicesToRun;
        //    ServicesToRun = new ServiceBase[]
        //    {
        //        new Service1()
        //    };
        //    ServiceBase.Run(ServicesToRun);
        //}

       
        static void Main(string[] args)
        {
#if (!DEBUG)
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new LipiRDService() 
			};
            ServiceBase.Run(ServicesToRun);
#else
            LipiRDService myService = new LipiRDService();
            myService.OnStart(args);
            while (true)
            { }
#endif
        }
    }
}
