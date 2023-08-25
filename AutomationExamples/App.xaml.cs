using AutomationExamples.ViewModels;
using AutomationExamples.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;

namespace AutomationExamples
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        
        private static VMS.TPS.Common.Model.API.Application _app;
        private string _patientId;
        private Patient _patient;
        private string _structureSetId;
        private StructureSet _structureSet;
        private MainView mv;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                if(e.Args !=null && e.Args.Length >0)
                {
                    using (_app = VMS.TPS.Common.Model.API.Application.CreateApplication())
                    {
                        _patientId = e.Args.FirstOrDefault().Split(';').FirstOrDefault();
                        _app.ClosePatient();
                        _patient = _app.OpenPatientById(_patientId);
                        _structureSetId = e.Args.FirstOrDefault().Split(';').Last();
                        _structureSet = _patient.StructureSets.FirstOrDefault(x => x.Id == _structureSetId);
                        mv = new MainView();
                        mv.DataContext = new MainViewModel(_patient, _structureSet,_app);
                        mv.ShowDialog();
                    }
                }
                else
                {
                    MessageBox.Show("Missing input Arguments");
                    return;
                }
            }
            catch (ApplicationException ex)
            {
                string filepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string output_path = Path.Combine(filepath, "outputError.csv");
            }
        }
        public static void SaveModifications(string outputLogs)
        {
            _app.SaveModifications();
        }

      
    }
}
