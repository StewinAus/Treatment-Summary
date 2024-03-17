using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using VM = VMS.TPS.Common.Model.API;
using D = VMS.TPS.Common.Model.Types.DoseValuePresentation;
using V = VMS.TPS.Common.Model.Types.VolumePresentation;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ESAPIX.Extensions;
using OxyPlot.Legends;
using OxyPlot.Wpf;

namespace Treatment_Summary
{
    /// <summary>
    /// Interaction logic for PlanSelection.xaml
    /// </summary>
    
    public partial class PlanSelection : Window
    {
        
        public PlanSelection()

        {
            InitializeComponent();
        }
        [STAThread]
        private void TestConnection_Click(object sender, RoutedEventArgs e)

        {
            try
            {
                string ARIAServer = "server";
                string ARIADatabase = "variandw";
                using (SqlConnection cn = new SqlConnection("Server = " + ARIAServer + "; DataBase = " + ARIADatabase + "; Integrated Security = SSPI;"))
                {
                    if (cn.State == ConnectionState.Closed)
                        cn.Open();
                    MessageBox.Show("Connection Success", "Message",MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchCourse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ARIAServer = "server";
                string ARIADatabase = "variandw";
                using (SqlConnection cn = new SqlConnection("Server = " + ARIAServer + "; DataBase = " + ARIADatabase + "; Integrated Security = SSPI;"))
                {
                    if (cn.State == ConnectionState.Closed)
                        cn.Open();
                    using (DataTable dt = new DataTable("Result"))
                    {
                        //using (SqlCommand cmd = new SqlCommand("Select p.PatientId, p.FirstName, p.LastName From Patient p Where p.PatientId = '" + MRNtextBox.Text + "'", cn))
                        using (SqlCommand cmd = new SqlCommand((@"

Select distinct ltrim(rtrim(c.CourseId))
From DWH.DimPatient (nolock) p inner join DWH.DimCourse (nolock) c on c.DimPatientID = p.DimPatientID
inner join DWH.DimTreatmentTransaction (nolock) tt on tt.DimCourseID = c.DimCourseID
Where upper(p.PatientId) = upper('" + MRNTextBox.Text + "') Order By  ltrim(rtrim(c.CourseId))"), cn))
                        {
                            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                            DataTable dta = new DataTable("Courses");
                            
                            adapter.Fill(dta);
                            CourseDataGrid.ItemsSource = dta.DefaultView;

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        void selectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            DataRowView dataRow = (DataRowView)CourseDataGrid.SelectedItem;
            int index = CourseDataGrid.CurrentCell.Column.DisplayIndex;
            string cellValue = dataRow.Row.ItemArray[index].ToString();
            //MessageBox.Show(cellValue);
            try
            {
                string ARIAServer = "server";
                string ARIADatabase = "variandw";
                using (SqlConnection cn = new SqlConnection("Server = " + ARIAServer + "; DataBase = " + ARIADatabase + "; Integrated Security = SSPI;"))
                {
                    if (cn.State == ConnectionState.Closed)
                        cn.Open();
                    using (DataTable dt = new DataTable("Result"))
                    {
                       using (SqlCommand cmd = new SqlCommand((@"
SELECT Distinct Pl.PlanSetupId, P.PatientLastName, P.PatientFirstName, P.PatientId
	, format(P.PatientDateOfBirth, 'dd/MM/yyyy') as DOB, P.PatientHomePhone, P.PatientFullAddress, P.PatientEMailAddress
	, C.CourseId, C.DoseDelivered as 'Course Dose', C_Intent.LookupDescriptionENU as 'Course Intent'
	, Coursestatus.LookupDescriptionENU as 'Course Status'
	, Course_FirstTx_Date = format((SELECT min(Pl1.FirstDayOfTreatment)
							FROM variandw.DWH.DimPatient P1 
								INNER JOIN variandw.DWH.DimCourse C1 on C1.DimPatientID = P1.DimPatientID
								INNER JOIN variandw.DWH.DimPlan Pl1 on Pl1.DimCourseID = C1.DimCourseID
							WHERE P.PatientId = P1.PatientId), 'dd/MM/yyyy')
	, Course_LastTx_Date =  format((SELECT max(Pl1.LastDayOfTreatment)
							FROM variandw.DWH.DimPatient P1 
								INNER JOIN variandw.DWH.DimCourse C1 on C1.DimPatientID = P1.DimPatientID
								INNER JOIN variandw.DWH.DimPlan Pl1 on Pl1.DimCourseID = C1.DimCourseID
							WHERE P.PatientId = P1.PatientId), 'dd/MM/yyyy')
	, DC.DiagnosisCode, DC.DiagnosisClinicalDescriptionENU
	, FPD.TStage, FPD.NStage, FPD.MStage, FPD.SummaryStage
	, Pl_Status.LookupDescriptionENU as PlanStatus, round(Pl.PrimaryRefPointDelivered, 2), round(Pl.PrimaryRefPointPlanned,2)
	, Pl.NoFractionsTreated, Pl.NoFractionsPlanned
	, format(Pl.FirstDayOfTreatment, 'dd/MM/yyyy') as firstDayofTreatment, format(Pl.LastDayOfTreatment, 'dd/MM/yyyy')as LastDayofTreatment
	, Pl.PrimaryRefPointId, Pl.IMRTOrRapidArc, Pl.NoFractionsRemaining
    , case when Pl.IMRTOrRapidArc = 'IMRT' then 'IMRT' when Pl.IMRTOrRapidArc = 'RapidArc' then 'VMAT' when Pl.EnergyMode like '%X%' then 'Conformal' else 'Electron' end as Tech
	, Course_Elapsed_Days = datediff(day,
										(SELECT min(Pl1.FirstDayOfTreatment)
										FROM variandw.DWH.DimPatient P1 
											INNER JOIN variandw.DWH.DimCourse C1 on C1.DimPatientID = P1.DimPatientID
											INNER JOIN variandw.DWH.DimPlan Pl1 on Pl1.DimCourseID = C1.DimCourseID
										WHERE P.PatientId = P1.PatientId),
										(SELECT max(Pl1.LastDayOfTreatment)
										FROM variandw.DWH.DimPatient P1 
											INNER JOIN variandw.DWH.DimCourse C1 on C1.DimPatientID = P1.DimPatientID
											INNER JOIN variandw.DWH.DimPlan Pl1 on Pl1.DimCourseID = C1.DimCourseID
										WHERE P.PatientId = P1.PatientId)) +1
	, Plan_Elapsed_Days = datediff(day,Pl.FirstDayOfTreatment,Pl.LastDayOfTreatment) + 1
	, Doc.DoctorLastName as RO_LastName, Doc.DoctorFirstName as RO_FirstName
	
FROM variandw.DWH.DimPatient P 
	INNER JOIN variandw.DWH.DimCourse (nolock) C on C.DimPatientID = P.DimPatientID
	INNER JOIN variandw.DWH.DimPlan (nolock) Pl on Pl.DimCourseID = C.DimCourseID 
    inner join DWH.DimTreatmentTransaction (nolock) tt on tt.DimPlanID = Pl.DimPlanID
    LEFT OUTER JOIN variandw.DWH.FactCourseDiagnosis (nolock) FCD on FCD.DimCourseID = C.DimCourseID
	LEFT OUTER JOIN variandw.DWH.DimDiagnosisCode DC (nolock) on DC.DimDiagnosisCodeID = FCD.DimDiagnosisCodeID
	LEFT OUTER JOIN variandw.DWH.FactPatientDiagnosis (nolock) FPD on FPD.DimDiagnosisCodeID = FCD.DimDiagnosisCodeID and P.DimPatientID = FPD.DimPatientID
	LEFT OUTER JOIN variandw.DWH.DimLookup (nolock) Pl_Status on Pl_Status.DimLookupID = Pl.DimLookupID_PlanStatus
	LEFT OUTER JOIN variandw.DWH.DimLookup (nolock) Coursestatus on Coursestatus.DimLookupID = C.DimLookupID_ClinicalStatus
	LEFT OUTER JOIN variandw.DWH.DimLookup (nolock) C_Intent on C_Intent.DimLookupID = C.DimLookupID_TreatmentIntentType
	LEFT OUTER JOIN variandw.DWH.DimPatientDoctor (nolock) PD on PD.DimPatientID = P.DimPatientID AND PD.OncologistFlag = 1 and PD.PrimaryFlag = 1 --and PD.ActiveEntryIndicator = 1
	LEFT OUTER JOIN variandw.DWH.DimDoctor (nolock) Doc on Doc.ctrResourceSer = PD.ctrResourceSer
	LEFT OUTER JOIN variandw.DWH.RadiationRefPoint (nolock) RRP on RRP.RTPlanSer = Pl.ctrRTPlanSer
	LEFT OUTER JOIN AuraStaging.dbo.RefPoint (nolock) RP on RP.RefPointSer =RRP.RefPointSer
	LEFT OUTER JOIN variandw.DWH.DimCellType (nolock) CT on CT.DimCellTypeID = FPD.DimCellTypeID
Where upper(P.PatientId) = upper('" + MRNTextBox.Text + "') and upper(C.CourseId) =  upper('" + cellValue + " ')  Order By  P.PatientLastName, P.PatientFirstName, P.PatientId, C.CourseId, Pl.PlanSetupId"), cn))
                           
                        {
                            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                            DataTable dta = new DataTable("Plans");

                            adapter.Fill(dta);
                            PlanDataGrid.ItemsSource = dta.DefaultView;
                            PlanDataGrid.Columns[1].Visibility = Visibility.Collapsed;//Lastname
                            PlanDataGrid.Columns[2].Visibility = Visibility.Collapsed;//Firstname
                            PlanDataGrid.Columns[3].Visibility = Visibility.Collapsed;//Id
                            PlanDataGrid.Columns[4].Visibility = Visibility.Collapsed;//DOB
                            PlanDataGrid.Columns[5].Visibility = Visibility.Collapsed;//Phone
                            PlanDataGrid.Columns[6].Visibility = Visibility.Collapsed;//Adr
                            PlanDataGrid.Columns[7].Visibility = Visibility.Collapsed;//email

                            
                            PlanDataGrid.Columns[8].Visibility = Visibility.Collapsed;//CourseId
                            PlanDataGrid.Columns[9].Visibility = Visibility.Collapsed;//CdoseDel
                            PlanDataGrid.Columns[10].Visibility = Visibility.Collapsed;//Cintent
                            
                            PlanDataGrid.Columns[11].Visibility = Visibility.Collapsed;//CStatus
                            PlanDataGrid.Columns[12].Visibility = Visibility.Collapsed;//Coures1st
                            PlanDataGrid.Columns[13].Visibility = Visibility.Collapsed;//CourseLast
                            PlanDataGrid.Columns[14].Visibility = Visibility.Collapsed;//ICD
                            PlanDataGrid.Columns[15].Visibility = Visibility.Collapsed;//Diag
                            PlanDataGrid.Columns[16].Visibility = Visibility.Collapsed;//T
                            PlanDataGrid.Columns[17].Visibility = Visibility.Collapsed;//N
                            PlanDataGrid.Columns[18].Visibility = Visibility.Collapsed;//M
                            PlanDataGrid.Columns[19].Visibility = Visibility.Collapsed;//Stage

                            PlanDataGrid.Columns[20].Visibility = Visibility.Collapsed;//PlStatus
                            PlanDataGrid.Columns[21].Visibility = Visibility.Collapsed;//RPtDel
                            PlanDataGrid.Columns[22].Visibility = Visibility.Collapsed;//RPtPlan
                            PlanDataGrid.Columns[23].Visibility = Visibility.Collapsed;//#treat
                            PlanDataGrid.Columns[24].Visibility = Visibility.Collapsed;//#Plan

                            PlanDataGrid.Columns[25].Visibility = Visibility.Collapsed;//PL1st
                            PlanDataGrid.Columns[26].Visibility = Visibility.Collapsed;//PlLast
                            PlanDataGrid.Columns[27].Visibility = Visibility.Collapsed;//RefPt

                            PlanDataGrid.Columns[28].Visibility = Visibility.Collapsed;//IMRTVMAt
                            PlanDataGrid.Columns[29].Visibility = Visibility.Collapsed;//#Remain
                            PlanDataGrid.Columns[30].Visibility = Visibility.Collapsed;//Technique
                            PlanDataGrid.Columns[31].Visibility = Visibility.Collapsed;//CElapsedDays
                            PlanDataGrid.Columns[32].Visibility = Visibility.Collapsed;//PlElapsedDays
                            PlanDataGrid.Columns[33].Visibility = Visibility.Collapsed;//ROlast
                            PlanDataGrid.Columns[34].Visibility = Visibility.Collapsed;//RO1st
                            //PlanDataGrid.Columns[9].Visibility = Visibility.Collapsed;//


                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        void PlanSelected(object sender, SelectedCellsChangedEventArgs e)
        {
           
            try
            {
                DataRowView dataRow = (DataRowView)PlanDataGrid.SelectedItem;
                
                string fullName = dataRow.Row.ItemArray[2].ToString() + " " + dataRow.Row.ItemArray[1].ToString();
                string mrn = dataRow.Row.ItemArray[3].ToString();
                string dob = dataRow.Row.ItemArray[4].ToString();
                string addr = dataRow.Row.ItemArray[6].ToString();
                string ph = dataRow.Row.ItemArray[5].ToString();
                string email = dataRow.Row.ItemArray[7].ToString();
                string course = dataRow.Row.ItemArray[8].ToString();
                string intent= dataRow.Row.ItemArray[10].ToString();
                string diag = dataRow.Row.ItemArray[14].ToString() + ": " + dataRow.Row.ItemArray[15].ToString();
                string course1st = dataRow.Row.ItemArray[12].ToString();
                string courselast = dataRow.Row.ItemArray[13].ToString();
                string coursedur = dataRow.Row.ItemArray[31].ToString();
                string site = dataRow.Row.ItemArray[0].ToString();
                string pl1st = dataRow.Row.ItemArray[25].ToString();
                string pllast = dataRow.Row.ItemArray[26].ToString();
                string pldur = dataRow.Row.ItemArray[32].ToString();
                string pldose = dataRow.Row.ItemArray[21].ToString() + "Gy of " + dataRow.Row.ItemArray[22].ToString() + "Gy";
                string plfrac = dataRow.Row.ItemArray[23].ToString() + " of " + dataRow.Row.ItemArray[24].ToString() + "#";
                string pltech = dataRow.Row.ItemArray[30].ToString();



                using (var app = VM.Application.CreateApplication())
                {

                    var patient = app.OpenPatientById(mrn);
                    var courses = patient.Courses;
                    var allPlans = patient.Courses.SelectMany(c => c.PlanSetups);
                    var plan = allPlans.FirstOrDefault(p => p.Id == site);
                    var plot = CreatePlot(plan);
                    var showsummary = new Summary   ();
                    showsummary.DataContext = plot;
                    showsummary.Visibility = Visibility.Visible;                    //view.ShowDialog();


                    showsummary.nameTextBox.Text = fullName;
                    showsummary.mrnTextBox.Text = mrn;
                    showsummary.dobTextBox.Text = dob;
                    showsummary.addrTextBox.Text = addr;
                    showsummary.phTextBox.Text = ph;
                    showsummary.emailTextBox.Text = email;
                    showsummary.courseTextBox.Text = course;
                    showsummary.intentTextBox.Text = intent;
                    showsummary.diagTextBox.Text = diag;
                    showsummary.course1stTextBox.Text = course1st;
                    showsummary.courselastTextBox.Text = courselast;
                    showsummary.coursedurTextBox.Text = coursedur;
                    showsummary.siteTextBox.Text = site;
                    showsummary.pl1stTextBox.Text = pl1st;
                    showsummary.pllastTextBox.Text = pllast;
                    showsummary.pldurTextBox.Text = pldur;
                    showsummary.plfracTextBox.Text = plfrac;
                    showsummary.pldoseTextBox.Text = pldose;
                    showsummary.pltechTextBox.Text = pltech;

                    showsummary.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private static PlotModel CreatePlot(VM.PlanSetup plan)
        {
            var model = new PlotModel();
            //add legend
            model.Legends.Add(new Legend()
            {
                LegendTitle = "Legend",
                IsLegendVisible = true,
                LegendPosition = LegendPosition.RightBottom,
            });
            //Add Dose Axis
            model.Axes.Add(new LinearAxis()
            {
                Title = "Dose [cGy]",
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineThickness = 1,
                MinorGridlineColor = OxyColor.FromRgb(15, 15, 15),
            });
            //Add Volume Axis
            model.Axes.Add(new LinearAxis()
            {
                Title = "Volume [%]",
                Position = AxisPosition.Left,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromRgb(15, 15, 15)
            });

            {
                foreach (var str in plan.StructureSet.Structures)
                {
                    if (str.DicomType.Contains("ORGAN"))
                    {
                        var ser = new LineSeries();
                        ser.Color = OxyColor.FromRgb(str.Color.R, str.Color.G, str.Color.B);
                        ser.RenderInLegend = true;
                        ser.Title = str.Id;
                        foreach (var pt in plan.GetDVHCumulativeData(str, D.Absolute, V.Relative, 0.1).CurveData)
                        {
                            ser.Points.Add(new DataPoint(pt.DoseValue.GetDoseCGy(), pt.Volume));
                        }
                        model.Series.Add(ser);
                    }
                    if (str.DicomType.Contains("TV"))
                    {
                        var ser = new LineSeries();
                        ser.Color = OxyColor.FromRgb(str.Color.R, str.Color.G, str.Color.B);
                        ser.RenderInLegend = true;
                        ser.Title = str.Id;
                        foreach (var pt in plan.GetDVHCumulativeData(str, D.Absolute, V.Relative, 0.1).CurveData)
                        {
                            ser.Points.Add(new DataPoint(pt.DoseValue.GetDoseCGy(), pt.Volume));
                        }
                        model.Series.Add(ser);
                    }
                }
            }
            return model;
        }
    }
}
