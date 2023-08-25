using ClinicalTemplateReader;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using VMS.TPS.WashU;

[assembly: ESAPIScript(IsWriteable = true)]
namespace AutomationExamples.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private Patient _patient;
        private StructureSet _structureSet;
        private Application _app;
        private Course _course;
        private ExternalPlanSetup currentPlan;
        public ExternalPlanSetup CurrentPlan
        {
            get { return currentPlan; }
            set
            {
                SetProperty(ref currentPlan, value);
                SetRxCommand.RaiseCanExecuteChanged();
                OptimizeCommand.RaiseCanExecuteChanged();
                CalculateDoseCommand.RaiseCanExecuteChanged();

            }
        }
        private bool _patientMods;
        private string courseId;

        private string optStructureResult;

        public string OptStructureResult
        {
            get { return optStructureResult; }
            set { SetProperty(ref optStructureResult, value); }
        }

        public string CourseId
        {
            get { return courseId; }
            set
            {
                SetProperty(ref courseId, value);
                if (!String.IsNullOrEmpty(CourseId))
                {
                    CourseResult = _patient.Courses.Any(x => x.Id == CourseId) ? $"Course with Id {CourseId} already exists" : "Course name not in use";
                    GenerateCourseCommand.RaiseCanExecuteChanged();
                }
            }
        }
        private string courseResult;


        public string CourseResult
        {
            get { return courseResult; }
            set { SetProperty(ref courseResult, value); }
        }
        private string planResult;

        public string PlanResult
        {
            get { return planResult; }
            set { SetProperty(ref planResult, value); }
        }
        private string targetId;

        public string TargetId
        {
            get { return targetId; }
            set { SetProperty(ref targetId, value); }
        }

        //private VVector isocenterPosition;

        //public VVector IsocenterPosition
        //{
        //    get { return isocenterPosition; }
        //    set { SetProperty(ref isocenterPosition, value); }
        //}
        private PlanTemplate selectedPlan;

        public PlanTemplate SelectedPlanTemplate
        {
            get { return selectedPlan; }
            set
            {
                SetProperty(ref selectedPlan, value);
                GeneratePlanCommand.RaiseCanExecuteChanged();
                SetRx();
            }
        }
        private double dosePerFraction;

        public double DosePerFraction
        {
            get { return dosePerFraction; }
            set { SetProperty(ref dosePerFraction, value); }
        }
        private int numberOfFractions;

        public int NumberOfFractions
        {
            get { return numberOfFractions; }
            set { SetProperty(ref numberOfFractions, value); }
        }
        private double prescribedPercentage;

        public double PrescribedPercentage
        {
            get { return prescribedPercentage; }
            set { SetProperty(ref prescribedPercentage, value); }
        }
        private ObjectiveTemplate selectedOptimizationObjective;

        public ObjectiveTemplate SelectedOptimizationObjective
        {
            get { return selectedOptimizationObjective; }
            set
            {
                SetProperty(ref selectedOptimizationObjective, value);
                OptimizeCommand.RaiseCanExecuteChanged();
            }
        }
        private bool useObjectiveTemplate;

        public bool UseObjectiveTemplate
        {
            get { return useObjectiveTemplate; }
            set
            {
                SetProperty(ref useObjectiveTemplate, value);
                //UpdateObjectives();
                if (UseObjectiveTemplate)
                {
                    UseRapidPlan = false;
                }
            }
        }

        private bool useRapidplan;

        public bool UseRapidPlan
        {
            get { return useRapidplan; }
            set
            {
                SetProperty(ref useRapidplan, value);
                // UpdateObjectives();
                if (UseRapidPlan)
                {
                    UseObjectiveTemplate = false;
                }
            }
        }
        private DVHEstimationModel selectedRapidPlanModel;

        public DVHEstimationModel SelectedRapidPlanModel
        {
            get { return selectedRapidPlanModel; }
            set
            {
                SetProperty(ref selectedRapidPlanModel, value);
                OptimizeCommand.RaiseCanExecuteChanged();

            }
        }
        private string optimizerResult;

        public string OptimizerResult
        {
            get { return optimizerResult; }
            set { SetProperty(ref optimizerResult, value); }
        }
        private string calculationResult;

        public string CalculationResult
        {
            get { return calculationResult; }
            set { SetProperty(ref calculationResult,value); }
        }


        public ObservableCollection<ObjectiveTemplate> OptimizationObjectives { get; set; }
        public ObservableCollection<PlanTemplate> PlanTemplates { get; private set; }
        public ObservableCollection<DVHEstimationModel> RapidPlanModels { get; private set; }
        public DelegateCommand GenerateOptStructuresCommand { get; private set; }
        public DelegateCommand GenerateCourseCommand { get; private set; }
        public DelegateCommand GeneratePlanCommand { get; private set; }
        public DelegateCommand SaveCommand { get; private set; }
        public DelegateCommand SetRxCommand { get; private set; }
        public DelegateCommand OptimizeCommand { get; private set; }
        public DelegateCommand CalculateDoseCommand { get; private set; }
        public ClinicalTemplate ClinicalTemplates{ get; private set; }

        public MainViewModel(Patient patient, StructureSet structureSet, Application app)
        {
            _patient = patient;
            _structureSet = structureSet;
            _app = app;
            ClinicalTemplates = new ClinicalTemplate("bjcdev");
            PlanTemplates = new ObservableCollection<PlanTemplate>();
            OptimizationObjectives = new ObservableCollection<ObjectiveTemplate>();
            //RapidPlanModels = new ObservableCollection<DVHEstimationModel>();
            _patientMods = _patient.CanModifyData();
            if (_patientMods)
            {
                _patient.BeginModifications();
            }
            GenerateOptStructuresCommand = new DelegateCommand(OnGenerateOptStructures, CanGenerateOptStructures);
            GenerateCourseCommand = new DelegateCommand(OnGenerateCourse, CanGenerateCourse);
            GeneratePlanCommand = new DelegateCommand(OnGeneratePlan, CanGeneratePlan);
            CalculateDoseCommand = new DelegateCommand(OnCalculateDose, CanCalculateDose);
            SaveCommand = new DelegateCommand(OnSave);
            SetRxCommand = new DelegateCommand(OnSetRx, CanSetRx);
            OptimizeCommand = new DelegateCommand(OnOptimize, CanOptimize);
            SetInitials();
        }

        private void OnCalculateDose()
        {
            CurrentPlan.CalculateDose();
            CalculationResult = $"Calculation Successful:\nMax Dose = {CurrentPlan.Dose.DoseMax3D}";
        }

        private bool CanCalculateDose()
        {
            return CurrentPlan != null;
        }

        /// <summary>
        /// Cannot add a line objective.
        /// Currently Not Supported
        ///     Smoothing Defaults
        ///     Max Time
        ///     Geos
        ///     IMAT Features (Min/Max MU)
        /// Defaults to intermediate dose values. 
        /// </summary>
        private void OnOptimize()
        {
            var optimizer_string = ClinicalTemplates.OptimizeFromObjectiveTemplate(CurrentPlan, SelectedOptimizationObjective, DoseValue.DoseUnit.cGy);
            OptimizerResult = optimizer_string;// $"Structures Included in Optimization: {String.Join(", ", optimizer_string)}\nStructure not found on Structure Set for Optimiztion: {String.Join(", ", not_found_string)}";

        }

        private bool CanOptimize()
        {
            return CurrentPlan != null && SelectedOptimizationObjective != null && SelectedOptimizationObjective.ObjectivesAllStructures.Count() > 0;
        }

        private void OnSetRx()
        {

            CurrentPlan.SetPrescription(NumberOfFractions, new DoseValue(DosePerFraction, (ConfigurationManager.AppSettings["doseUnit"] == "cGy" ? DoseValue.DoseUnit.cGy : DoseValue.DoseUnit.Gy)), PrescribedPercentage);
        }

        private bool CanSetRx()
        {
            return DosePerFraction != 0 && NumberOfFractions != 0 && CurrentPlan != null;
        }

        private void OnSave()
        {
            AutomationExamples.App.SaveModifications("To fill later");
        }
        private void SetRx()
        {
            if (SelectedPlanTemplate != null)
            {

                DosePerFraction = Convert.ToDouble(SelectedPlanTemplate.DosePerFraction) * 100.0;
                NumberOfFractions = Convert.ToInt32(SelectedPlanTemplate.FractionCount);
                if (SelectedPlanTemplate.PrescribedPercentage == null)
                {
                    PrescribedPercentage = 1.0;
                }
                else
                {
                    PrescribedPercentage = Convert.ToDouble(SelectedPlanTemplate.PrescribedPercentage);
                }               

            }

        }
        private void OnGeneratePlan()
        {
            CurrentPlan = ClinicalTemplates.GeneratePlanFromTemplate(_course, _structureSet, SelectedPlanTemplate, null);
            ////generate plan from template. 
            PlanResult = $"Generate Plan {CurrentPlan.Id} with the following fields:\nBeam Id\tGantry Angle\tTechnique\n{String.Join("\n", CurrentPlan.Beams.Select(x => new { s = $"{x.Id}\t{x.ControlPoints.First().GantryAngle}\t{x.Technique}" }).Select(x => x.s))}";
        }     

        private bool CanGeneratePlan()
        {
            return SelectedPlanTemplate != null && (_course != null || _patient.Courses.Any(x => x.Id == CourseId));

        }

        private void SetInitials()
        {
            OptStructureResult = _structureSet.Structures.Any(x => x.Id.StartsWith(ConfigurationManager.AppSettings["optStructureDelimiter"])) ?
                String.Join(" ,", _structureSet.Structures.Where(x => x.Id.StartsWith(ConfigurationManager.AppSettings["optStructureDelimiter"]))) :
                $"No Structures found with opt structure delimiter '{ConfigurationManager.AppSettings["optStructureDelimiter"]}'";
            CourseId = "C1";
            PrescribedPercentage = 1.0;
            UseObjectiveTemplate = true;
            //IsocenterPosition = new VVector(0, 0, 0);
            BuildPlanTemplates();
            BuildOptimizationTemplates();
            //BuildRapidPlanModelTemplates();
        }

        private void BuildOptimizationTemplates()
        {
            foreach(var optTemplate in ClinicalTemplates.ObjectiveTemplates.Where(x=>x.Preview.ApprovalStatus.Contains("Approved")))
            {
                OptimizationObjectives.Add(optTemplate);
            }
        }

        private void BuildPlanTemplates()
        {
            foreach(var planTemplate in ClinicalTemplates.PlanTemplates.Where(x=>x.Preview.ApprovalStatus.Contains("Approved")))
            {
                PlanTemplates.Add(planTemplate);
            }
        }

        private void OnGenerateCourse()
        {
            _course = _patient.AddCourse();
            _course.Id = CourseId;
            CourseResult = $"Course {_course.Id} generated";
            GenerateCourseCommand.RaiseCanExecuteChanged();
        }
        private bool CanGenerateCourse()
        {
            return !String.IsNullOrEmpty(CourseId) && !_patient.Courses.Select(x => x.Id).Contains(CourseId);
        }
        private void OnGenerateOptStructures()
        {
            //TODO
            //Structure Naming Rules:
            //***WUSTL Naming Convention***
            //• All automation structure Ids will start with _
            //	○ They get sorted to the top of the list in External Beam Planning and in the Optimizer.
            //	○ Differentiates them from other non-standard structures such as NS_Fiducials which the API cannot easily automate(i.e.waiting for contour high density volume structure).
            //• All other conventions are for Structure Name.
            //	○ IRing for inner ring. ORing for outer ring. 
            //	○ We can use the name of the structure.It has 64 characters allowed.
            //	○ Rules
            //		§ Target denotes whatever the target volume of the plan is.
            //		§ Spaces to split the nomenclature.
            //		§ IRing denotes inner ring while ORing denotes outer ring.
            //		§ Gap is the distance to the start of the ring.
            //		§ Booleans are represented by the upper case version of their boolean.
            //			□ Grouping Booleans is done with parenthesis.
            //		§ Examples:
            //			□ Target ORing 05
            //				® --> Target
            //			□ Target ORing 05 GAP 05
            //				® --> Target that is 5mm margin with from a structure that is 5mm from target volume. Concentric rings would simply have the gap size increased.
            //			□ Target ORing 05 GAP 05 AND(Lung_L OR Lung_R OR Heart)
            //				® --> Target with 5mm margin from a 5mm expansion of target only coinciding with the LUNG.
            //			□ Rectum SUB Target 02
            //				® --> Rectum sub PTV with 2mm margin on PTV.  
            //	○ Implementation


        }

        private bool CanGenerateOptStructures()
        {
            return _patientMods && _structureSet.Structures.Any(x => x.Id.ToUpper().StartsWith(ConfigurationManager.AppSettings["optStructureDelimiter"]));
        }
    }
}
